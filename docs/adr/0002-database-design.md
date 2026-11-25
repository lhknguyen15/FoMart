# ADR-0002: Các quyết định thiết kế database

- Trạng thái: Đã chấp nhận
- Ngày: 25/11/2026
- Liên quan: [docs/database.md](../database.md), `database/migrations/V001__initial_schema.sql`

Mỗi mục dưới đây ghi quyết định, lý do và cái giá phải trả.

## 1. Tồn kho do trigger duy trì từ sổ kho

**Quyết định.** Ứng dụng chỉ `INSERT` vào `inventory.stock_moves`. Trigger `TR_stock_moves_apply` cộng `qty_change` vào `inventory.stock` (tạo dòng nếu chưa có). Trigger `TR_stock_moves_append_only` chặn `UPDATE`/`DELETE` trên sổ. Role `fomart_app` bị `DENY INSERT, UPDATE, DELETE` trên `stock`; trigger vẫn ghi được nhờ ownership chaining.

**Lý do.** BR-02 và bất biến "tổng `qty_change` = `stock.qty`" là quy tắc quan trọng nhất. Đặt ở database thì không đoạn code nào, kể cả SQL viết tay sau này, có thể vô tình phá nó. View `v_stock_ledger_mismatch` phải luôn rỗng và được kiểm trong integration test.

**Cái giá.** Có logic nằm trong trigger. EF Core cần biết bảng có trigger; EF Core 10 scaffold tự sinh `HasTrigger(...)` (đã thử). Tài khoản `db_owner`/`sysadmin` không bị `DENY`, nên API phải chạy bằng login thuộc role `fomart_app`.

## 2. FK thật thay cho `doc_type` + `doc_id` đa hình

**Quyết định.** `stock_moves` có 5 cột FK nullable: `receipt_line_id`, `sale_line_id`, `transfer_line_id`, `disposal_line_id`, `count_line_id`. `CK_stock_moves_doc_ref` buộc đúng một cột được điền và cột đó khớp với `doc_type`. `CK_stock_moves_sign` buộc dấu `qty_change` khớp với loại chứng từ.

**Lý do.** Spec mục 9 dùng `doc_id` chung. Cách đó không có FK, và còn vướng kiểu dữ liệu: id hóa đơn là GUID, id các chứng từ khác là INT. Trỏ tới *dòng* chứng từ thì truy ngược được chính xác từng lô đã trừ cho từng dòng hóa đơn, điều mà việc hủy hóa đơn cần (UC-06).

**Cái giá.** Thêm một loại chứng từ nghĩa là thêm cột và sửa CHECK bằng migration.

## 3. FK ghép để lô và đơn vị luôn thuộc đúng sản phẩm

**Quyết định.** `lots` và `product_units` có `UNIQUE (id, product_id)`. Các bảng tham chiếu dùng FK ghép `(lot_id, product_id)` và `(unit_id, product_id)`.

**Lý do.** Chặn ngay ở database lỗi kiểu "dòng bán cola nhưng trừ vào lô nước mắm". `stock` và `stock_moves` có thêm `product_id` (phi chuẩn hóa) để khóa và chọn lô theo sản phẩm mà không cần join.

## 4. Lô tạm khi bán hàng chưa từng có lô

**Quyết định.** Khi bán một sản phẩm mà tại kệ chưa từng có lô nào (thường là sản phẩm vừa tạo nhanh), hệ thống trừ vào **lô tạm** của sản phẩm: `lots.kind = 'provisional'`, `unit_cost` và `expiry_date` là NULL. Mỗi sản phẩm có tối đa một lô tạm (`UX_lots_provisional`) và lô này được dùng lại. Dòng bán có phần trừ vào lô tạm thì `cost_total = NULL` (giá vốn chưa xác định), và sản phẩm được đánh dấu `needs_count`. Lần kiểm kê sau sẽ đưa tồn lô tạm về đúng.

**Lý do.** Giữ được `stock_moves.lot_id NOT NULL` và khóa chính `stock(lot_id, location_id)` đơn giản. Phương án cho `lot_id` NULL bị loại vì làm phức tạp khóa chính và truy vấn FEFO.

**Cái giá.** Báo cáo lãi gộp phải hiện riêng phần doanh thu "chưa xác định giá vốn".

## 5. Không cho sửa giá trên giỏ hàng

**Quyết định.** Phiên bản 1 không cho sửa hay giảm giá khi bán. `sale_lines.unit_price` là snapshot giá bán của đơn vị tại thời điểm bán (BR-07). Riêng dòng "hàng chưa khai báo" có giá nhập tay. Không có cột giảm giá ở `sales` hay `sale_lines`.

**Lý do.** Giữ lượt bán nhanh (NFR-04), tránh phải phân quyền, và giữ tỷ lệ ghi nhận cùng lãi gộp dễ hiểu. Giảm giá hàng sắp hết hạn đã nằm trong danh sách phiên bản 2 (spec 11.1).

**Cái giá.** Muốn thêm sau thì cần migration bổ sung `list_price` và `discount` cho `sale_lines`.

## 6. Khóa của hóa đơn: GUID không làm khóa cụm

**Quyết định.** `sales.id` là `UNIQUEIDENTIFIER` (UUID v7 sinh trên máy), đặt làm **PRIMARY KEY NONCLUSTERED**. Khóa cụm là `seq BIGINT IDENTITY`. `sale_lines.id` là `BIGINT IDENTITY` do server sinh, vì tính idempotent khi đồng bộ đã được đảm bảo bởi `sales.id`.

**Lý do.** Spec 12.2 cho rằng UUID v7 tránh được phân mảnh index. Điều này **không đúng với SQL Server**: SQL Server sắp xếp `uniqueidentifier` theo nhóm byte cuối trước, trong khi phần thời gian của UUID v7 lại nằm ở các byte đầu. Vì vậy GUID v7 vẫn chèn ngẫu nhiên vào index cụm.

## 7. Collation `Latin1_General_100_CI_AI` cho cả database

**Quyết định.** Database được tạo với `COLLATE Latin1_General_100_CI_AI`, không đặt collation riêng từng cột.

**Lý do.** Đã kiểm trên SQL Server 2022:

| So sánh | `Vietnamese_CI_AI` | `Latin1_General_100_CI_AI` |
| --- | --- | --- |
| `Mì Hảo Hảo` = `mi hao hao` | khớp | khớp |
| `Nước mắm` = `nuoc mam` | **không khớp** | khớp |
| `Đường` = `duong` | **không khớp** | khớp |

`Vietnamese_CI_AI` coi ư, ơ, ă, â, đ là chữ cái riêng và chỉ bỏ dấu thanh. Vì vậy người bán gõ không dấu sẽ không tìm ra phần lớn tên hàng. Đặt collation ở cấp database còn tránh được lỗi "collation conflict" khi so sánh cột với biến hoặc table variable (lỗi này đã gặp khi chạy seed với collation riêng từng cột).

**Cái giá.** Các ràng buộc unique trên tên cũng không phân biệt dấu ("Kệ bán" trùng "ke ban"), điều này chấp nhận được. Bảng `#temp` dùng collation của tempdb, nên phải khai báo `COLLATE DATABASE_DEFAULT`. Sắp xếp theo bảng chữ cái tiếng Việt (ư sau u) không chuẩn tuyệt đối; danh sách sản phẩm không cần điều đó.

## 8. Kiểu dữ liệu và trạng thái

- Tiền dùng `BIGINT` (đồng) vì BR-11 yêu cầu số nguyên và `INT` có thể tràn ở báo cáo. Giá vốn theo đơn vị cơ sở dùng `DECIMAL(14,4)`, tổng giá vốn `DECIMAL(18,4)`.
- Trạng thái và loại là `VARCHAR` + `CHECK` (`'paid'`, `'storage'`...), thay vì `TINYINT`. Như vậy dữ liệu đọc được khi query tay và không lệch nghĩa với enum trong code. Domain giữ hằng số tương ứng.
- `count_lines.diff` là cột tính `PERSISTED` theo BR-19, nên công thức chỉ có một nơi.
- `core.stores` có đúng một dòng (`CHECK (id = 1)`). Phiên bản 1 không làm nhiều chi nhánh (spec 2.2), nên không thêm `store_id` vào mọi bảng.

## 9. Migration có phiên bản

**Quyết định.** Có bốn thành phần:

- `00_create_database.sql`: tạo database, bật `READ_COMMITTED_SNAPSHOT`. T-SQL thuần (không dùng biến sqlcmd) để chạy được thẳng trong VS Code/SSMS; `migrate.ps1` thay biến `@DatabaseName` theo `-Database`.
- `migrations/Vxxx__*.sql`: mỗi file một transaction.
- `seed/`: dữ liệu mẫu chỉ dùng cho dev.
- `migrate.ps1`: tạo `dbo.schema_versions`, chạy các file chưa áp dụng, ghi checksum, và báo lỗi nếu file đã áp dụng bị sửa.

**Lý do.** Database-first cần một cách lặp lại được để dựng schema trên mọi máy, CI và Testcontainers. Thiết kế này rút kinh nghiệm từ FoMed-API, nơi script không chạy lại được. Chưa dùng DbUp hay Flyway để đỡ thêm phụ thuộc; khi cần chạy migration trong CI hoặc trong test bằng C#, có thể thay bằng DbUp mà vẫn giữ nguyên các file SQL.

## 10. Tài khoản chủ tiệm đầu tiên

**Quyết định.** Migration không tạo user. API có bước "thiết lập lần đầu" và chỉ cho chạy khi chưa có owner nào (BR-15). Seed dev tạo sẵn tài khoản mẫu.

**Lý do.** Không để mật khẩu mặc định trong migration chạy trên môi trường thật.

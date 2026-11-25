# Thiết kế database FoMart

Nguồn sự thật của schema là các script trong [`database/migrations/`](../database/migrations/) (database-first, xem [ADR-0001](adr/0001-clean-architecture-database-first.md)). Tài liệu này giải thích cấu trúc; lý do của từng lựa chọn nằm ở [ADR-0002](adr/0002-database-design.md). Spec mục 9 là thiết kế ban đầu; chỗ nào khác nhau thì theo tài liệu này.

## 1. Tổng quan

| Schema | Bảng | Vai trò |
| --- | --- | --- |
| `auth` | `users`, `refresh_tokens` | Tài khoản chủ/nhân viên, phiên JWT |
| `core` | `stores` | Cấu hình cửa hàng (đúng một dòng ở phiên bản 1) |
| `catalog` | `categories`, `suppliers`, `locations`, `products`, `product_units`, `price_history` | Danh mục (UC-01, UC-02) |
| `inventory` | `lots`, `stock`, `stock_moves`, `transfers`, `transfer_lines`, `disposals`, `disposal_lines`, `stock_counts`, `count_lines` | Lô, tồn, sổ kho, chuyển kho, hủy, kiểm kê |
| `purchasing` | `purchase_orders`, `purchase_order_lines`, `goods_receipts`, `receipt_lines` | Đặt hàng, nhập hàng (UC-03, UC-04) |
| `sales` | `sales`, `sale_lines` | Bán hàng, hủy hóa đơn (UC-05, UC-06) |

Trái tim là bộ ba **`lots` → `stock_moves` → `stock`**. Ứng dụng chỉ **thêm dòng vào `stock_moves`**. Trigger cộng dòng đó vào `stock`, nên tồn không thể lệch khỏi sổ (BR-02).

```mermaid
flowchart LR
  subgraph Chứng từ
    RL[receipt_lines]
    SL[sale_lines]
    TL[transfer_lines]
    DL[disposal_lines]
    CL[count_lines]
  end
  RL & SL & TL & DL & CL -->|"đúng 1 FK / dòng"| SM[(stock_moves<br/>chỉ thêm)]
  SM -->|"trigger AFTER INSERT"| ST[(stock<br/>lot × location)]
  LOT[lots] --- SM
  LOT --- ST
```

## 2. Sơ đồ quan hệ

### 2.1. Danh mục và tài khoản

```mermaid
erDiagram
  USERS {
    int id PK
    nvarchar name
    varchar phone UK
    varchar password_hash "owner bắt buộc"
    varchar pin_hash "staff bắt buộc"
    varchar role "owner | staff"
    bit is_active
  }
  REFRESH_TOKENS {
    bigint id PK
    int user_id FK
    char token_hash UK "SHA-256"
    datetime2 expires_at
    datetime2 revoked_at
    bigint replaced_by_id FK
  }
  STORES {
    tinyint id PK "luôn = 1"
    nvarchar name
    varchar bank_bin "VietQR"
    varchar bank_account_no
    smallint expiry_warn_days
    smallint reorder_cover_days
    smallint sales_lookback_days
    smallint count_interval_days
    bit staff_can_see_cost
  }
  CATEGORIES {
    int id PK
    nvarchar name UK
  }
  SUPPLIERS {
    int id PK
    nvarchar name
    varchar phone
    varchar zalo
  }
  LOCATIONS {
    int id PK
    nvarchar name UK
    varchar type "storage | shelf"
    bit is_default_sale "đúng 1 dòng = 1"
  }
  PRODUCTS {
    int id PK
    nvarchar name "tìm không dấu"
    int category_id FK
    int default_supplier_id FK
    bit track_expiry
    int min_stock
    int min_shelf_stock
    varchar status "active | inactive | incomplete"
    bit needs_count
    smallint quick_button_order UK
    int created_by FK
  }
  PRODUCT_UNITS {
    int id PK
    int product_id FK
    nvarchar name "lon, lốc, thùng"
    int factor ">= 1"
    varchar barcode UK "nullable"
    bigint sale_price "đồng"
    bit is_base "đúng 1 / sản phẩm"
    bit is_default_purchase
  }
  PRICE_HISTORY {
    bigint id PK
    int unit_id FK
    bigint old_price
    bigint new_price
    int changed_by FK
  }

  USERS ||--o{ REFRESH_TOKENS : "có"
  CATEGORIES |o--o{ PRODUCTS : "nhóm"
  SUPPLIERS |o--o{ PRODUCTS : "NCC mặc định"
  PRODUCTS ||--|{ PRODUCT_UNITS : "có đơn vị"
  PRODUCT_UNITS ||--o{ PRICE_HISTORY : "lịch sử giá"
  USERS ||--o{ PRODUCTS : "tạo"
```

### 2.2. Tồn kho và chứng từ

```mermaid
erDiagram
  LOTS {
    int id PK
    int product_id FK
    varchar kind "receipt | count_adjust | provisional"
    date expiry_date
    decimal unit_cost "14,4 theo đv cơ sở; NULL nếu provisional"
    datetime2 received_at "thứ tự FIFO"
  }
  STOCK {
    int lot_id PK
    int location_id PK
    int product_id FK
    int qty "được âm (BR-01)"
    rowversion row_version
  }
  STOCK_MOVES {
    bigint id PK
    datetime2 created_at
    int product_id FK
    int lot_id FK
    int location_id FK
    int qty_change "có dấu"
    decimal unit_cost
    varchar doc_type
    int receipt_line_id FK
    bigint sale_line_id FK
    int transfer_line_id FK
    int disposal_line_id FK
    int count_line_id FK
    int user_id FK
  }
  PURCHASE_ORDERS {
    int id PK
    varchar code UK "DH..."
    int supplier_id FK
    varchar status "ordered | partial | received | cancelled"
  }
  PURCHASE_ORDER_LINES {
    int id PK
    int purchase_order_id FK
    int product_id FK
    int unit_id FK
    int qty
    int received_qty
  }
  GOODS_RECEIPTS {
    int id PK
    varchar code UK "PN..."
    int supplier_id FK
    int purchase_order_id FK
    int location_id FK
    varchar status "draft | completed | cancelled"
    bigint total
  }
  RECEIPT_LINES {
    int id PK
    int receipt_id FK
    int product_id FK
    int unit_id FK
    int qty
    int base_qty
    bigint unit_price "NULL khi ẩn giá"
    date expiry_date
    int lot_id FK "UK, điền khi hoàn tất"
  }
  SALES {
    uniqueidentifier id PK "UUID v7 từ máy"
    bigint seq "clustered"
    varchar code UK "HD..."
    varchar status "pending_payment | paid | cancelled"
    varchar payment_method "cash | transfer"
    int location_id FK
    bigint total
    datetime2 created_at "giờ máy bán"
    datetime2 synced_at "giờ server"
  }
  SALE_LINES {
    bigint id PK
    uniqueidentifier sale_id FK
    int product_id FK "NULL = hàng chưa khai báo"
    int unit_id FK
    nvarchar name_snapshot
    int qty
    bigint unit_price "snapshot (BR-07)"
    int base_qty
    decimal cost_total "BR-06"
  }
  TRANSFERS {
    int id PK
    varchar code UK "CK..."
    int from_location_id FK
    int to_location_id FK
    bit is_auto "BR-18"
    uniqueidentifier sale_id FK
  }
  TRANSFER_LINES {
    int id PK
    int transfer_id FK
    int product_id FK
    int base_qty
  }
  DISPOSALS {
    int id PK
    varchar code UK "XH..."
    varchar status "pending | approved | rejected"
    int decided_by FK
  }
  DISPOSAL_LINES {
    int id PK
    int disposal_id FK
    int product_id FK
    int location_id FK
    int lot_id FK "NULL = chọn FEFO khi duyệt"
    int base_qty
    varchar reason "expired | broken | damaged | other"
  }
  STOCK_COUNTS {
    int id PK
    varchar code UK "KK..."
    varchar scope_type "location | category | suggested | full"
    varchar status "counting | submitted | approved | cancelled"
    datetime2 created_at "thời điểm chốt số"
  }
  COUNT_LINES {
    int id PK
    int count_id FK
    int product_id FK
    int location_id FK
    int system_qty
    int moves_during
    int counted_qty
    int diff "computed (BR-19)"
    varchar reason
    decimal adjust_unit_cost
  }

  LOTS ||--o{ STOCK : "nằm tại"
  LOTS ||--o{ STOCK_MOVES : "biến động"
  PURCHASE_ORDERS ||--|{ PURCHASE_ORDER_LINES : "gồm"
  PURCHASE_ORDERS |o--o{ GOODS_RECEIPTS : "nhận qua"
  GOODS_RECEIPTS ||--|{ RECEIPT_LINES : "gồm"
  RECEIPT_LINES |o--o| LOTS : "tạo"
  SALES ||--|{ SALE_LINES : "gồm"
  SALES |o--o{ TRANSFERS : "sinh tự động"
  TRANSFERS ||--|{ TRANSFER_LINES : "gồm"
  DISPOSALS ||--|{ DISPOSAL_LINES : "gồm"
  STOCK_COUNTS ||--|{ COUNT_LINES : "gồm"
  RECEIPT_LINES ||--o{ STOCK_MOVES : "receipt, receipt_cancel"
  SALE_LINES ||--o{ STOCK_MOVES : "sale, sale_cancel"
  TRANSFER_LINES ||--o{ STOCK_MOVES : "transfer_out, transfer_in"
  DISPOSAL_LINES ||--o{ STOCK_MOVES : "disposal"
  COUNT_LINES ||--o{ STOCK_MOVES : "count_adjust"
```

Không vẽ để sơ đồ dễ đọc: mọi bảng chứng từ có `created_by` và các cột người duyệt/hủy trỏ về `users`. `stock`, `stock_moves`, các dòng chứng từ cũng có FK tới `products` và `locations`.

## 3. `doc_type` trong sổ kho

| doc_type | Dấu `qty_change` | FK được điền | Phát sinh khi |
| --- | --- | --- | --- |
| `receipt` | + | `receipt_line_id` | Hoàn tất phiếu nhập |
| `receipt_cancel` | − | `receipt_line_id` | Hủy phiếu nhập (lô chưa bị trừ) |
| `sale` | − | `sale_line_id` | Hóa đơn chuyển sang `paid` |
| `sale_cancel` | + | `sale_line_id` | Hủy hóa đơn, trả đúng các lô đã trừ |
| `transfer_out` / `transfer_in` | − / + | `transfer_line_id` | Chuyển kho (thủ công hoặc tự động BR-18) |
| `disposal` | − | `disposal_line_id` | Phiếu hủy được duyệt |
| `count_adjust` | ± | `count_line_id` | Duyệt chênh lệch kiểm kê, tồn đầu kỳ |

Mỗi dòng sổ có đúng một FK chứng từ được điền và phải khớp với `doc_type` (`CK_stock_moves_doc_ref`), nên không có dòng sổ "mồ côi".

## 4. Quy tắc nghiệp vụ được ép ở đâu

Database ép những gì nó ép được một cách rẻ và chắc chắn; còn lại do ứng dụng kiểm tra (`Domain/Rules` và `Application/Services`). Ứng dụng vẫn phải kiểm tra trước để báo lỗi thân thiện; ràng buộc DB là lưới an toàn cuối cùng.

| Quy tắc | Database | Ứng dụng |
| --- | --- | --- |
| BR-02 tồn chỉ đổi qua chứng từ | Trigger `TR_stock_moves_apply`; role `fomart_app` bị `DENY` ghi `stock` | Mọi nghiệp vụ đổi tồn đi qua `unitOfWork.StockRepository` |
| NFR-10 sổ chỉ thêm | Trigger `TR_stock_moves_append_only`; `DENY UPDATE, DELETE` | |
| BR-03 chứng từ đã hoàn tất không sửa | CHECK trạng thái + cột người/thời điểm | Chặn sửa dòng khi `status <> 'draft'` |
| BR-04 FEFO/FIFO | Index `IX_lots_product_expiry` | SQL chọn lô trong `StockRepository`, chia lô bằng `Domain/Rules/FefoAllocator` |
| BR-09 một đơn vị cơ sở | `UX_product_units_base`, `CK_product_units_base_factor` | Tạo đơn vị cơ sở cùng sản phẩm |
| BR-10 hệ số nguyên dương | `CK_product_units_factor` | |
| BR-11 kiểu tiền | `BIGINT` cho tiền, `DECIMAL(14,4)` cho giá vốn | Làm tròn khi chia |
| BR-12 khóa hệ số khi có giao dịch | Trigger `TR_product_units_lock_factor` | Ẩn ô sửa trên UI |
| BR-16 vị trí bán mặc định là kệ | `UX_locations_default_sale`, `CK_locations_default_sale_is_shelf` | Không cho xóa/tắt vị trí bán cuối cùng |
| BR-19 công thức chênh lệch | Cột tính `count_lines.diff` | Tính `moves_during` khi gửi |
| BR-20 mã vạch duy nhất | `UX_product_units_barcode` (filtered) | |
| Nguyên nhân khớp chiều lệch | `CK_count_lines_reason_sign` | |
| Lô thuộc đúng sản phẩm | FK ghép `(lot_id, product_id)`, `(unit_id, product_id)` | |
| BR-01, 05, 06, 07, 13, 15, 17, 18, 21 | | `Domain/Rules`, `Application/Services` |

## 5. Trừ kho (phác thảo cho `StockRepository`)

Service mở `unitOfWork.BeginWriteAsync` rồi gọi `unitOfWork.StockRepository`; tất cả chạy trong một transaction (NFR-06):

1. Khóa các dòng tồn của sản phẩm tại vị trí theo thứ tự `product_id` tăng dần, để hai máy bán cùng lúc không deadlock:
   ```sql
   SELECT s.lot_id, s.qty, l.expiry_date, l.received_at, l.unit_cost, l.kind
   FROM inventory.stock AS s WITH (UPDLOCK, ROWLOCK)
   JOIN inventory.lots  AS l ON l.id = s.lot_id
   WHERE s.product_id = @productId AND s.location_id = @locationId
   ORDER BY CASE WHEN l.kind = 'provisional' THEN 1 ELSE 0 END,
            CASE WHEN l.expiry_date IS NULL THEN 1 ELSE 0 END,
            l.expiry_date, l.received_at, l.id;        -- FEFO, rồi FIFO
   ```
2. `Domain/Rules/FefoAllocator` (thuần C#) chia số lượng cần trừ vào các lô. Lô còn hạn được ưu tiên (BR-05). Nếu không đủ thì phần thiếu trừ vào lô mới nhất tại vị trí đó, cho phép âm (BR-01). Nếu vị trí chưa từng có lô nào của sản phẩm thì trừ vào **lô tạm** của sản phẩm (tạo nếu chưa có).
3. Insert các dòng `stock_moves`; trigger tự cập nhật `stock`.
4. Đặt `products.needs_count = 1` nếu có lô bị âm hoặc phải dùng lô tạm.

Kệ thiếu mà kho sau còn hàng (BR-18): tạo `transfers` (`is_auto = 1`) trước bước 1, trong cùng transaction.

## 6. Database-first: chạy script và scaffold

```powershell
# Tạo database FoMart trong container Docker và chạy migration, kèm dữ liệu mẫu
./database/migrate.ps1 -Docker -Seed

# Scaffold lại sau mỗi migration (chạy ở thư mục backend/; dotnet-ef ghim trong .config/dotnet-tools.json)
dotnet ef dbcontext scaffold "Name=ConnectionStrings:FoMart" Microsoft.EntityFrameworkCore.SqlServer `
  --project FoMart.Infrastructure --startup-project FoMart.Api `
  --context FoMartDbContext --context-dir DbContext --context-namespace FoMart.Infrastructure `
  --output-dir ../FoMart.Domain/Entities --namespace FoMart.Domain.Entities `
  --schema auth --schema core --schema catalog --schema inventory --schema purchasing --schema sales `
  --no-onconfiguring --force
```

Đã thử với EF Core 10 trên schema này. Scaffold tự sinh `HasTrigger(...)` cho `stock_moves` và `product_units`, giữ `HasComputedColumnSql` cho `diff`, `IsRowVersion()` cho `row_version` và default sinh mã từ sequence.

Quy tắc:

- Thay đổi schema = thêm file `database/migrations/V00x__mo_ta.sql` mới. Không sửa file đã áp dụng, vì `migrate.ps1` sẽ báo lệch checksum.
- Không sửa tay file scaffold (`Domain/Entities/*.cs`, `Infrastructure/DbContext/FoMartDbContext.cs`). Quy tắc nghiệp vụ viết ở `Domain/Rules`; cấu hình EF bổ sung (value converter, query filter) viết ở `Infrastructure/DbContext/FoMartDbContext.Custom.cs` qua `OnModelCreatingPartial`.

## 7. Quy ước

- Tên bảng/cột `snake_case` tiếng Anh. EF scaffold tự đổi sang PascalCase.
- Trạng thái là `VARCHAR` + `CHECK`. Domain giữ hằng số trùng khớp (ví dụ `SaleStatus.Paid = "paid"`).
- Thời điểm là `DATETIME2(3)` UTC; chỉ đổi sang giờ Việt Nam khi hiển thị và khi gom báo cáo theo ngày. Hạn dùng là `DATE`.
- Tiền là `BIGINT` (đồng); giá vốn `DECIMAL(14,4)`; tổng giá vốn `DECIMAL(18,4)`; số lượng `INT` theo đơn vị cơ sở.
- Mã chứng từ sinh bằng sequence: `DH` đặt hàng, `PN` nhập, `HD` hóa đơn, `CK` chuyển kho, `XH` xuất hủy, `KK` kiểm kê, kèm 8 chữ số.
- Collation cả database là `Latin1_General_100_CI_AI`. Bảng `#temp` khai báo cột chuỗi bằng `COLLATE DATABASE_DEFAULT`.
- Tên ràng buộc: `PK_`, `FK_`, `UQ_`, `UX_` (unique index), `IX_`, `CK_`, `DF_`, `TR_`, `v_` (view).

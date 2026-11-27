# Mảng Purchasing – ghi chú

Đặt hàng, nhập hàng. UC-03, UC-04 · BR-02, BR-03, BR-08, BR-09, BR-11.

### 27/11/2026 – API phiếu nhập (UC-04)

Nhánh `feature/purchasing-goods-receipt`. Phiếu đặt hàng (UC-03) chưa làm: phiếu nhập tạo được không cần phiếu đặt, `purchase_order_id` luôn NULL.

**Thư mục mới**

| Thư mục | Vì sao |
| --- | --- |
| `backend/FoMart.Application/DTO/Purchasing/`, `Services/Purchasing/`, `Api/Controllers/Purchasing/` | DTO, service, controller của mảng |

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `Domain/Enums/GoodsReceiptStatus.cs` | `draft` / `completed` / `cancelled` (trùng CHECK) |
| `Domain/Rules/ExpiryRules.cs` | Mức cảnh báo hạn dùng (đã hết hạn / còn dưới N ngày), nhận "hôm nay" làm tham số để test được |
| `Application/DTO/Purchasing/GoodsReceiptModels.cs` | Request/response phiếu nhập, dòng nhập, giá lần trước |
| `Application/DTO/Purchasing/PurchasingResponseMessageDTO.cs` | Thông báo tiếng Việt |
| `Application/Interfaces/Repositories/IGoodsReceiptRepository.cs` | Interface repository phiếu nhập |
| `Application/Services/Purchasing/GoodsReceiptService.cs` | Nháp → hoàn tất → hủy |
| `Infrastructure/Repositories/GoodsReceiptRepository.cs` | Tìm phiếu, lấy phiếu kèm dòng, giá nhập lần trước |
| `Api/Controllers/Purchasing/GoodsReceiptController.cs` | Endpoint `api/goods-receipts` |

Dùng chung với mảng Inventory (ghi ở `inventory.md`): `Domain/Enums/DocType.cs`, `LotKind.cs`, `Domain/Rules/UnitConverter.cs`, `StockRepository`.

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `Application/Interfaces/Repositories/IProductRepository.cs`, `Infrastructure/Repositories/ProductRepository.cs` | `GetUnitsWithProductAsync`: lấy đơn vị kèm sản phẩm theo danh sách id (AsNoTracking) |
| `Application/Interfaces/Repositories/ILocationRepository.cs`, `Infrastructure/Repositories/LocationRepository.cs` | `GetDefaultReceivingAsync`: kho sau đang dùng, không có thì vị trí bán mặc định |
| `Application/Interfaces/Repositories/IRepositoryBase.cs` | Comment `Delete`: được xóa chứng từ còn nháp |
| `Interfaces/UnitOfWork/IUnitOfWork.cs`, `UnitOfWork/UnitOfWork.cs` | Thêm `GoodsReceiptRepository`, `StockRepository` |
| `Api/Program.cs` | Đăng ký `GoodsReceiptService`, `StockService` |
| `CLAUDE.md` | Mục trạng thái |

**API**

| Endpoint | Ai gọi | Chức năng |
| --- | --- | --- |
| `GET /api/goods-receipts` | đã đăng nhập | Tìm theo mã, trạng thái, nhà cung cấp, khoảng ngày tạo (giờ VN); phân trang |
| `GET /api/goods-receipts/{id}` | đã đăng nhập | Chi tiết + dòng: base qty, giá vốn/đơn vị cơ sở, cảnh báo hạn dùng |
| `GET /api/goods-receipts/last-price?unitId=` | người thấy giá | Giá nhập lần trước (bỏ phiếu đã hủy) |
| `POST /api/goods-receipts` | đã đăng nhập | Tạo nháp, có thể kèm luôn các dòng; bỏ trống vị trí = kho sau |
| `PUT /api/goods-receipts/{id}`, `DELETE /api/goods-receipts/{id}` | đã đăng nhập | Sửa đầu phiếu / xóa phiếu nháp |
| `POST/PUT/DELETE /api/goods-receipts/{id}/lines[/{lineId}]` | đã đăng nhập | Thêm/sửa/xóa dòng của phiếu nháp |
| `POST /api/goods-receipts/{id}/complete` | đã đăng nhập | Hoàn tất: tạo lô, ghi sổ kho, cộng tồn |
| `POST /api/goods-receipts/{id}/cancel` | chủ tiệm | Hủy bằng chứng từ đảo ngược `receipt_cancel` |

**Ghi chú**

- **Hoàn tất:** chạy trong một transaction (`BeginWriteAsync`).
  - Mỗi dòng tạo một lô `kind = receipt` với giá vốn = giá nhập ÷ hệ số, làm tròn 4 chữ số (BR-11).
  - Mỗi dòng cũng thêm một dòng sổ `receipt` với `qty_change = qty × factor`; trigger tự cộng vào `stock` (BR-02).
  - Hai máy cùng bấm hoàn tất thì `row_version` của `goods_receipts` chặn máy thứ hai, máy đó trả 409 và rollback.
- **Hủy:** chỉ khi các lô của phiếu chưa có biến động nào khác `receipt` (chưa bán, chuyển, hủy, kiểm kê).
  - Trước khi kiểm, khóa dòng tồn của các lô bằng `UPDLOCK, ROWLOCK` để không bán chen vào giữa.
  - Phiếu gốc và dòng sổ cũ giữ nguyên, chỉ thêm dòng sổ âm (BR-03).
- **Ẩn giá (BR-08):** nhân viên khi `stores.staff_can_see_cost = 0` sẽ:
  - không thấy giá, thành tiền, giá vốn trên phản hồi (null, `pricesHidden = true`);
  - nhận 403 nếu gửi giá lên;
  - sửa dòng thì giá chủ tiệm đã điền được giữ nguyên (đổi sang đơn vị khác thì giá bị xóa vì không còn đúng);
  - không hoàn tất được khi còn dòng thiếu giá (409), chủ tiệm điền giá trước.
- **Hạn dùng:**
  - Sản phẩm có quản lý hạn dùng bắt buộc nhập hạn dùng (400). Lúc hoàn tất kiểm lại, vì chủ tiệm có thể vừa bật quản lý hạn dùng.
  - Sản phẩm không quản lý thì bỏ qua hạn dùng gửi lên.
  - Hạn đã qua hoặc còn dưới `stores.expiry_warn_days` ngày: cảnh báo trên dòng (`expiryWarning`), hoàn tất trả 409 cho tới khi gửi `confirmExpiryWarning: true`. Ngày tính theo giờ Việt Nam.
- **Xóa:** phiếu nháp và dòng nháp được xóa vì chưa đổi tồn. Schema không cho trạng thái `cancelled` mà không có `completed_by`, nên "bỏ nháp" là xóa.
- **Từ chối dòng nhập:** đơn vị đã ngừng dùng và sản phẩm `inactive` (400). Sản phẩm `incomplete` (tạo nhanh) vẫn nhận.
- **Lỗi phát hiện khi test:**
  1. PowerShell 5.1 đọc sai script test không có BOM: lưu UTF-8 có BOM (bài học cũ).
  2. `sqlcmd` mặc định `QUOTED_IDENTIFIER OFF` nên INSERT vào bảng có filtered index bị lỗi 1934: thêm `-I` khi chạy SQL tay.
- **Hạn chế:**
  - Chưa nhận theo phiếu đặt (UC-03, so số đặt/nhận).
  - Chưa tạo nhanh sản phẩm khi quét mã mới (UC-01, làm cùng màn hình).
  - Ai đăng nhập cũng sửa/xóa được phiếu nháp của người khác.
  - Kịch bản "hai máy hoàn tất cùng lúc" khi chạy thử thì hai request đến lệch nhau khoảng 20 ms nên đi tuần tự. Nhánh `row_version` đúng theo thiết kế nhưng chưa được ép xảy ra thật; cần integration test khi có `FoMart.Tests`.
- **Test:** 64 kịch bản HTTP trên database tạm có seed (owner + staff), tất cả qua. Trong đó có kiểm đối soát sổ kho rỗng sau mỗi bước và mô phỏng chuyển kho bằng SQL để thử chặn hủy.
  - Swagger: login owner → `POST /api/goods-receipts` `{ "lines": [{ "unitId": 3, "qty": 2, "unitPrice": 190000, "expiryDate": "2027-05-01" }] }` → `POST /api/goods-receipts/{id}/complete` `{}` → `GET /api/stock?productId=1`.

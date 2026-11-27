# Mảng Sales – ghi chú

Bán hàng, hủy hóa đơn. UC-05, UC-06 · BR-01, BR-03, BR-04, BR-05, BR-06, BR-07, BR-08 · NFR-05, NFR-06.

### 27/11/2026 – API bán hàng FEFO và hủy hóa đơn

Nhánh `feature/sales-checkout`. Đã chốt với người dùng: BR-18 (kệ thiếu mà kho sau còn thì tự tạo phiếu chuyển kho) để làm cùng module chuyển kho (UC-07). Bản này kệ thiếu thì trừ âm ở kệ và đánh dấu cần kiểm kê.

**Thư mục mới**

| Thư mục | Vì sao |
| --- | --- |
| `backend/FoMart.Application/DTO/Sales/`, `Services/Sales/`, `Api/Controllers/Sales/` | DTO, service, controller của mảng |

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `Domain/Enums/SaleStatus.cs`, `PaymentMethod.cs` | `pending_payment` / `paid` / `cancelled`; `cash` / `transfer` (trùng CHECK) |
| `Domain/Rules/FefoAllocator.cs` | Chia số lượng cần trừ vào các lô (`LotStock`, `LotAllocation`, `AllocationResult`): BR-01, BR-04, BR-05 |
| `Application/DTO/Sales/SaleModels.cs` | Request/response hóa đơn, dòng, lô đã trừ |
| `Application/DTO/Sales/SalesResponseMessageDTO.cs` | Thông báo và cảnh báo tiếng Việt |
| `Application/Interfaces/Repositories/ISaleRepository.cs` | Interface repository hóa đơn |
| `Application/Services/Sales/SaleService.cs` | Lưu hóa đơn, xác nhận chuyển khoản, hủy; trừ kho FEFO |
| `Infrastructure/Repositories/SaleRepository.cs` | Tìm hóa đơn, lấy hóa đơn kèm dòng |
| `Api/Controllers/Sales/SaleController.cs` | Endpoint `api/sales` |

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `Application/Interfaces/Repositories/IStockRepository.cs`, `Infrastructure/Repositories/StockRepository.cs` | Thêm phần trừ kho cho bán hàng (chi tiết ở `inventory.md`) |
| `Interfaces/UnitOfWork/IUnitOfWork.cs`, `UnitOfWork/UnitOfWork.cs` | Thêm `SaleRepository` |
| `Api/Program.cs` | Đăng ký `SaleService` |
| `CLAUDE.md` | Mục trạng thái |

**API**

| Endpoint | Ai gọi | Chức năng |
| --- | --- | --- |
| `GET /api/sales` | đã đăng nhập | Tìm theo mã, trạng thái, hình thức, người bán, khoảng ngày (giờ VN); phân trang |
| `GET /api/sales/{id}` | đã đăng nhập | Chi tiết: dòng, các lô đã trừ; giá vốn và lãi gộp chỉ chủ tiệm thấy |
| `POST /api/sales` | đã đăng nhập | Lưu hóa đơn (Id UUID v7 từ máy bán). Tiền mặt: trừ kho ngay. Chuyển khoản: chờ thanh toán |
| `POST /api/sales/{id}/confirm-payment` | đã đăng nhập | "Đã nhận" tiền chuyển khoản: thanh toán và trừ kho |
| `POST /api/sales/{id}/cancel` | ai cũng được với hóa đơn chờ; chủ tiệm với hóa đơn đã thanh toán | Bỏ hóa đơn chờ / hủy hóa đơn bằng `sale_cancel` |

**Ghi chú**

- **Trừ kho:** chạy trong một transaction, tại vị trí bán mặc định.
  1. Khóa dòng `products` (`UPDLOCK, ROWLOCK`, id tăng dần), để hai lượt bán cùng sản phẩm phải chờ nhau, kể cả khi kệ chưa có dòng tồn nào để khóa (không tạo trùng lô tạm).
  2. Đọc tồn theo lô tại kệ (khóa dòng tồn).
  3. `FefoAllocator` chia lô.
  4. Thêm dòng sổ `sale`; trigger trừ `stock`.
- **Thứ tự chia lô:**
  1. Lô còn hạn: hạn gần nhất trước, lô không hạn để cuối, rồi theo ngày nhận.
  2. Lô hết hạn: chỉ khi người bán xác nhận (`confirmExpired`). Nếu không thì 409 và không trừ gì (BR-05).
  3. Lô tạm còn tồn dương.
  4. Còn thiếu: trừ âm vào lô mới nhất tại kệ. Kệ chưa từng có lô thì trừ vào lô tạm của sản phẩm (tạo nếu chưa có, dùng lại cho các lần sau).
  5. Trường hợp 4 thì bật `needs_count` (BR-01).
- Hai dòng cùng sản phẩm (lon + thùng) chia trên cùng bảng tồn đang trừ dở, nên không trừ trùng một lô.
- **Giá vốn (BR-06):** `cost_total` = tổng (số lượng × giá vốn lô) của các lô bị trừ. Bằng NULL nếu có phần trừ vào lô tạm. Lãi gộp chỉ tính khi mọi dòng biết giá vốn.
  - Nhân viên không thấy giá vốn và lãi trên hóa đơn (BR-08), không phụ thuộc `staff_can_see_cost` (cài đặt đó chỉ áp cho giá nhập).
- **Không sửa giá (BR-07):**
  - Hàng đã khai báo lấy giá theo đơn vị. Máy bán gửi kèm giá đang hiện thì chỉ bị từ chối khi giá khác giá đơn vị.
  - `name_snapshot` = "tên sản phẩm (đơn vị)".
- **Hàng chưa khai báo:** gửi `name` + `unitPrice`, không có `unitId`. Không trừ kho, không có giá vốn.
- **Không ghi trùng (NFR-05):**
  - Id UUID v7 do máy bán sinh (từ chối version khác).
  - Cùng người gửi lại cùng Id: trả hóa đơn đã ghi (200). Người khác dùng Id đó: 409.
  - `createdAt` là giờ máy bán, không được sớm hơn giờ server quá 5 phút về phía tương lai.
- **Hủy:**
  - Hóa đơn chờ thanh toán: ai cũng bỏ được, không đụng kho.
  - Hóa đơn đã thanh toán: chỉ chủ tiệm. Thêm `sale_cancel` cộng lại đúng từng lô và vị trí đã trừ (kể cả phần âm). Hóa đơn gốc giữ nguyên (BR-03).
- **Lỗi phát hiện khi test (đều ở script test PowerShell 5.1):**
  1. Biến `$pId` trùng biến hệ thống chỉ đọc `$PID`, vì tên biến không phân biệt hoa thường.
  2. `.Count` trên một `PSCustomObject` đơn lẻ trả rỗng: phải bọc `@(...)`.
- **Hạn chế:**
  - Chưa có BR-18 (tự chuyển kho).
  - Chưa có tạo nhanh sản phẩm khi quét mã mới.
  - Chưa có màn hình VietQR; frontend tạo mã từ `stores.bank_*` + mã hóa đơn.
  - Hai request cùng Id gửi đồng thời thì request thứ hai nhận 409 (trùng khóa) thay vì 200.
  - Nhân viên xem được mọi hóa đơn trong danh sách. Báo cáo "doanh thu ca của mình" thuộc mảng Report.
- **Test:** 54 kịch bản HTTP trên database tạm. Kệ được nhập 2 lô cola (hạn +60 và +30 ngày) và 1 lô mì đã hết hạn, rồi chạy:
  - FEFO;
  - giá vốn và lãi;
  - gửi lại cùng Id;
  - bán vượt tồn;
  - lô tạm;
  - lô hết hạn;
  - hàng chưa khai báo;
  - chuyển khoản;
  - hủy;
  - 6 lượt bán song song.

  Đối soát sổ kho rỗng ở cuối. Tất cả qua.
  - Swagger: login → `POST /api/sales` `{ "id": "<uuid v7>", "lines": [{ "unitId": 1, "qty": 2 }] }`. Id v7 tạo bằng `Guid.CreateVersion7()` hoặc trang uuidgenerator; bản `uuid` v7 của frontend sẽ tự sinh.

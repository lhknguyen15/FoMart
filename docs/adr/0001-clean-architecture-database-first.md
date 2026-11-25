# ADR-0001: Kiến trúc backend – Clean Architecture theo khuôn tổ chức của FoMed-API

- Trạng thái: Đã chấp nhận (thay bản ngày 25/11/2026 dùng Features/Abstractions)
- Ngày: 25/11/2026
- Quy tắc chi tiết (cây thư mục, tên file, luồng request): `CLAUDE.md`, mục "Kiến trúc backend"

## Bối cảnh

Backend là ASP.NET Core trên .NET 10 với SQL Server, database-first (schema viết bằng SQL, EF Core scaffold theo), Repository + Unit of Work.

FoMed-API (dự án trước của tác giả) có cách tổ chức dễ đọc. Thư mục theo mảng (`DTO/<Mảng>/`, `Services/<Mảng>/`, `Controllers/<Mảng>/`), tên file đoán được (`XxxModels.cs`, `XxxResponseMessageDTO.cs`, `XxxService.cs`), một khuôn phản hồi `HTTPResponseData<T>`, và luồng `Controller → Service → unitOfWork.XxxRepository`. Nhưng đó là **kiến trúc phân lớp**, chưa phải Clean Architecture:

- `Application` tham chiếu `Infrastructure`, nên tầng nghiệp vụ phụ thuộc EF Core.
- Entity (`Models/`) nằm trong `Infrastructure`.
- `IUnitOfWork` và `IXxxRepository` nằm trong `Infrastructure`, chung file với class cài đặt.

Bản thử đầu tiên của FoMart theo một template Clean Architecture (`src/`, `Abstractions/`, `Features/`, `DependencyInjection.cs` mỗi project, `I*Queries`, ProblemDetails) đúng chiều phụ thuộc nhưng có quá nhiều khái niệm và kiểu đặt tên, khó theo dõi luồng code. Bản đó đã bị xóa.

## Quyết định

**Giữ nguyên khuôn tổ chức của FoMed, chỉ dời đúng 3 thứ để đạt chiều phụ thuộc của Clean Architecture:**

| Thứ | FoMed | FoMart |
| --- | --- | --- |
| Entity scaffold | `Infrastructure/Models/` | `Domain/Entities/` |
| `IXxxRepository` | chung file với class trong `Infrastructure/Repositories/` | `Application/Interfaces/Repositories/` |
| `IUnitOfWork`, `IWriteScope` | `Infrastructure/UnitOfWork/` | `Application/Interfaces/UnitOfWork/` |

Chiều phụ thuộc: `Api → Infrastructure → Application → Domain`. Domain không tham chiếu gì; Application không biết EF Core. Interface ở Application và class cài đặt ở Infrastructure dùng cùng tên thư mục.

Thêm vào so với FoMed:

- `Domain/Rules/` chứa quy tắc nghiệp vụ thuần (FEFO, quy đổi đơn vị, chênh lệch kiểm kê), vì FoMart có nghiệp vụ tồn kho phức tạp hơn. `Domain/Enums/` tách khỏi `Entities/` để scaffold lại không đụng tới.
- Thư mục entity dùng tên `Entities/` theo thông lệ Clean Architecture (FoMed dùng `Models/`).
- Chưa có project test ở giai đoạn này; khi thêm sẽ là một project `FoMart.Tests`.

Ba điểm cố ý làm khác FoMed (không đổi luồng code):

1. `UnitOfWork` tự tạo repository khi được gọi lần đầu, thay vì nhận tất cả qua constructor. Nhờ vậy không phải đăng ký từng repository trong `Program.cs`, và thêm repository chỉ sửa `UnitOfWork.cs`.
2. `BeginWriteAsync` không dùng `sp_getapplock` khóa toàn cục. Trừ kho khóa dòng `UPDLOCK, ROWLOCK`, để hai máy bán khác sản phẩm không phải chờ nhau (NFR-06).
3. DbContext dùng namespace `FoMart.Infrastructure` dù nằm trong thư mục `DbContext/`. FoMed phải sửa tay file scaffold (`: Microsoft.EntityFrameworkCore.DbContext`) vì namespace `...DbContext` trùng tên lớp; FoMart scaffold lại không phải sửa gì.

Database-first: schema ở `database/migrations/`, chạy bằng `database/migrate.ps1`, không dùng EF Migrations. Entity scaffold vào `FoMart.Domain/Entities` bằng fluent API (không data annotation), nên Domain không cần package EF.

CORS: một policy `"Frontend"`, origin đọc từ cấu hình.

## Phương án bị loại

| Phương án | Lý do loại |
| --- | --- |
| Giữ nguyên kiến trúc phân lớp của FoMed | Application phụ thuộc EF Core, không đạt mục tiêu Clean Architecture |
| Template Clean Architecture kiểu `Features/` + `Abstractions/` (bản thử đầu) | Nhiều khái niệm và nơi đặt code, khó theo luồng |
| Tách hai lớp model (persistence model ở Infrastructure, domain model viết tay) | Gấp đôi code và mapping cho dự án 1 người; entity scaffold POCO ở Domain đã đủ |
| MediatR / CQRS | Controller không còn gọi thẳng service, mỗi use case tách 2–3 file, khó lần luồng; MediatR đã chuyển sang giấy phép thương mại; pipeline behavior thay được bằng middleware, filter và `BeginWriteAsync` |
| AutoMapper | Map tay trong service rõ ràng hơn, lỗi map thấy ngay lúc build |
| Interface cho từng service | Không có cài đặt thứ hai; controller dùng thẳng class service như FoMed |

## Hệ quả

- Mỗi repository là 2 file (interface và class) thay vì 1 như FoMed. Đây là cái giá duy nhất về số file.
- Quy tắc nghiệp vụ trong `Domain/Rules` test được mà không cần database, khi có project test.
- Thêm một chủ đề luôn đụng đúng 6 chỗ đã liệt kê trong CLAUDE.md. Thêm thư mục ngoài danh sách đóng phải sửa CLAUDE.md trước.
- Entity scaffold là POCO không có hành vi. Hành vi nghiệp vụ nằm ở `Domain/Rules` và service, không viết vào entity.

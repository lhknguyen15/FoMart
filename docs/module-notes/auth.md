# Mảng Auth – ghi chú

Đăng nhập, phiên, quản lý tài khoản. UC-11 · SC-01 · BR-15 · NFR-09.

### 25/11/2026 – JWT, PIN, refresh token, quản lý tài khoản

Nhánh `feature/auth-jwt`.

**Thư mục mới**

| Thư mục | Vì sao |
| --- | --- |
| `backend/FoMart.Application/Interfaces/Authentication/` | `ITokenService`, `IPasswordHasher` |
| `backend/FoMart.Infrastructure/Authentication/` | Cài đặt JWT và BCrypt |
| `backend/FoMart.Application/DTO/Auth/`, `Services/Auth/`, `Api/Controllers/Auth/` | DTO, service, controller của mảng |

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `Domain/Enums/UserRole.cs` | `owner` / `staff` (trùng CHECK và claim `role`) |
| `Application/Interfaces/Authentication/ITokenService.cs` | Tạo access/refresh token (`IssuedAccessToken`, `IssuedRefreshToken`) |
| `Application/Interfaces/Authentication/IPasswordHasher.cs` | Băm/kiểm tra mật khẩu, PIN |
| `Application/Interfaces/Repositories/IUserRepository.cs` | Người dùng + refresh token |
| `Application/DTO/Auth/AuthModels.cs`, `UserModels.cs`, `AuthResponseMessageDTO.cs` | Request/response, thông báo |
| `Application/Services/Auth/AuthService.cs` | Setup, login, login PIN, refresh, logout, me, đổi mật khẩu |
| `Application/Services/Auth/UserService.cs` | Danh sách, tạo, khóa/mở, đặt lại PIN |
| `Infrastructure/Authentication/JwtOptions.cs`, `JwtTokenService.cs`, `BcryptPasswordHasher.cs` | Cấu hình và cài đặt |
| `Infrastructure/Repositories/UserRepository.cs` | Truy vấn người dùng, refresh token |
| `Api/Controllers/Auth/AuthController.cs`, `UserController.cs` | Endpoint |

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `Api/Program.cs` | JWT bearer, fallback policy bắt đăng nhập, rate limiter "auth", Bearer trong OpenAPI/Swagger, đăng ký service; 401/403/429 trả `HTTPResponseData` |
| `Api/Controllers/Catalog/*.cs` | Ghi chỉ chủ tiệm; `ProductController` đọc claim `sub` |
| `Api/Controllers/Store/StoreController.cs` | `GET` công khai (màn hình đăng nhập) |
| `Api/appsettings.json` | Section `Jwt` (không có khóa), `RateLimiting:AuthPermitLimit` |
| `Api/appsettings.Development.json` | Bỏ `DevelopmentUser` |
| `Directory.Packages.props`, `FoMart.Api.csproj`, `FoMart.Infrastructure.csproj` | JwtBearer 10.0.14, BCrypt.Net-Next 4.2.0, IdentityModel 8.23.0 (ghim cả bộ một phiên bản) |
| `Interfaces/UnitOfWork/IUnitOfWork.cs`, `UnitOfWork/UnitOfWork.cs` | Thêm `UserRepository` |
| `database/README.md`, `CLAUDE.md` | Cách đặt `Jwt:SigningKey`; mục "Xác thực và phân quyền" |

**File xóa**

- `Api/Middleware/DevelopmentUserMiddleware.cs`: chỉ dùng tạm khi chưa có JWT.

**API**

| Endpoint | Ai gọi | Chức năng |
| --- | --- | --- |
| `POST /api/auth/setup` | công khai, chỉ khi chưa có chủ tiệm | Tạo chủ tiệm đầu tiên |
| `POST /api/auth/login`, `/login-pin` | công khai (giới hạn 10 lần/phút/IP) | Mật khẩu / PIN |
| `POST /api/auth/refresh` | công khai | Đổi refresh token lấy cặp mới (xoay vòng) |
| `POST /api/auth/logout`, `GET /api/auth/me`, `PUT /api/auth/password` | đã đăng nhập | |
| `GET/POST/PUT /api/users`, `PUT /api/users/{id}/pin` | chủ tiệm | Quản lý tài khoản |

**Ghi chú**

- Quyết định: refresh token 64 byte ngẫu nhiên, chỉ lưu SHA-256; dùng lại token đã thu hồi → thu hồi mọi phiên. Đăng nhập sai luôn cùng một thông báo và vẫn chạy BCrypt khi không có tài khoản. Khóa tài khoản / đổi mật khẩu → thu hồi mọi phiên. Không khóa được chủ tiệm cuối cùng (BR-15).
- Lỗi phát hiện: (1) token cấp lúc setup có `sub = 0` vì tạo trước khi lưu user → lưu trước. (2) Giới hạn tần suất áp cả `refresh` sẽ khóa nhầm các máy chung IP của tiệm → chỉ áp cho đăng nhập.
- Hạn chế: access token của tài khoản vừa bị khóa còn dùng được tối đa 30 phút (refresh bị thu hồi ngay). Chưa có endpoint liệt kê nhân viên cho màn hình đổi ca (làm cùng frontend).
- Việc người dùng tự làm: mỗi máy dev đặt `dotnet user-secrets set "Jwt:SigningKey" "<≥ 32 ký tự>" --project backend/FoMart.Api`.
- Test: 52 kịch bản HTTP (database tạm trống → setup → seed), cộng chạy lại toàn bộ kịch bản Catalog với JWT; tất cả qua. Swagger: `POST /api/auth/login` (`0900000001` / `Test@123`) → Authorize → dán `accessToken`.

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

### 27/11/2026 – Màn hình đăng nhập và phiên trên frontend

Nhánh `feature/frontend-auth`. UC-11, SC-01, NFR-07, NFR-09.

**Quyết định cùng người dùng**
- Màn hình theo kiểu "máy nhớ người dùng + PIN": chạm tên rồi gõ 4–6 số PIN để đổi ca. Người mới dùng số điện thoại + mật khẩu hoặc PIN.
- Tự khóa khi không dùng để sau, làm cùng màn hình bán hàng.
- Giữ BR-01 (bán không bị chặn khi tồn thiếu), người dùng xác nhận lại khi làm PRODUCT.md.

**Thư mục mới:** `frontend/src/stores/`, `frontend/src/features/auth/{pages,components,hooks,schemas}/`

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `stores/useAuthStore.ts` | Zustand + `persist` (localStorage `fomart-auth`): `session`, `knownUsers`, `signIn`, `replaceTokens`, `signOut`, `forgetUser` |
| `features/auth/schemas/loginSchema.ts` | Zod khớp backend: SĐT `^[0-9+]{8,15}$`, PIN `^[0-9]{4,6}$`, `discriminatedUnion` theo `method` |
| `features/auth/hooks/useAuth.ts` | `useLogin` (mật khẩu/PIN, xóa cache người trước), `useLogout` (thu hồi refresh token, bỏ phiên, xóa cache) |
| `features/auth/components/PinPad.tsx` | Bàn phím PIN 64px, hỗ trợ bàn phím máy tính (`useEffectEvent`) |
| `features/auth/components/KnownUserList.tsx` | Danh sách người trên máy: tên, vai trò, 4 số cuối SĐT, nút bỏ khỏi máy |
| `features/auth/components/PhoneLoginForm.tsx` | React Hook Form + `zodResolver`, `useWatch` (tương thích React Compiler) |
| `features/auth/pages/LoginPage.tsx` | 3 khung: chọn người / PIN hoặc mật khẩu / số điện thoại; tiêu điểm vào tiêu đề khi đổi khung |
| `app/RequireAuth.tsx` | Chưa có phiên thì sang `/dang-nhap`, nhớ trang đang định vào |

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `api/client.ts` | Tự gắn token; làm mới trước khi hết hạn 30 s và sau 401 (thử lại một lần); nhiều request dùng chung **một** lần refresh; refresh bị từ chối thì đăng xuất, lỗi mạng thì giữ phiên; tùy chọn `anonymous` thay cho `accessToken` tạm |
| `api/auth.ts` | `loginWithPin`, `logout`, kiểu `UserRole` |
| `api/store.ts`, `api/catalog.ts` | `anonymous` cho `/api/stores`; `getProductByBarcode(code, signal)` |
| `app/router.tsx`, `app/AppLayout.tsx` | Route `/dang-nhap`; mọi trang khác bọc `RequireAuth`; thanh đầu trang có tên người dùng + nút đăng xuất (Phosphor `SignOut`) |
| `features/sales/pages/ScanTestPage.tsx` | Bỏ form đăng nhập riêng |
| `package.json` | `zustand` 5.1.0, `react-hook-form` 7.92.0, `zod` 4.6.7, `@hookform/resolvers` 7.0.6, `@phosphor-icons/react` 2.1.10, `@fontsource/be-vietnam-pro` 5.3.0 |

**Ghi chú**
- **Vì sao phải dùng chung một lần refresh:** gọi thử qua proxy cho thấy refresh lần hai bằng token cũ bị server coi là đánh cắp. Server thu hồi mọi phiên, kể cả token vừa cấp (401).
- **Rủi ro đã biết:** refresh token lưu ở localStorage. Đổi lại, F5 hay mở lại app không phải đăng nhập. App không được chạy HTML/JS lạ (XSS).
- **Impeccable (không có trình duyệt nên chấm trên code):** critique sửa 4 lỗi trước khi commit:
  1. Ẩn lựa chọn Mật khẩu/PIN với người đã biết.
  2. Chuyển tiêu điểm khi đổi khung nhìn.
  3. Enter trên nút của bàn phím PIN gửi hai lần.
  4. Theo màu thiết kế cho vùng bôi chọn và con trỏ nhập.
- **Hạn chế:**
  - Chưa chụp ảnh màn hình thật để duyệt giao diện.
  - Chưa có tự khóa và màn hình thiết lập chủ tiệm lần đầu (`POST /api/auth/setup`).
  - Chưa có đổi mật khẩu trên giao diện.
- **Test:**
  - Build + lint không lỗi.
  - Qua proxy: login PIN 200, PIN sai 401 ("Số điện thoại hoặc PIN không đúng."), refresh 200, dùng lại token cũ 401 rồi token mới cũng 401, logout 200, refresh sau logout 401.

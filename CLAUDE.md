# FoMart – Hệ thống quản lý bán hàng và tồn kho cho tiệm tạp hóa

Dự án cá nhân để đưa vào CV. Web app (PWA) giúp tiệm tạp hóa nhỏ (1–5 người, 300–1.000 mã hàng) quản lý tồn kho theo lô, bán hàng bằng quét mã vạch qua camera điện thoại, và nhập hàng dựa trên dữ liệu bán thực tế.

## Tài liệu (đọc trước khi code)

- `docs/spec.md` – Đặc tả đầy đủ: phạm vi, 11 use case (UC-01…UC-11), quy tắc nghiệp vụ (BR-xx), yêu cầu phi chức năng (NFR-xx), màn hình (SC-xx), thiết kế dữ liệu, lộ trình, công nghệ.
- `docs/module-workflows.md` – Sơ đồ luồng (Mermaid) cho 6 module: danh mục, nhập hàng, bán hàng, chuyển kho, xuất hủy, kiểm kê.
- `docs/dev-workflow.md` – Quy trình phát triển, vòng lặp mỗi tính năng, việc cần làm ở giai đoạn 1.
- `docs/database.md` – Thiết kế database đang áp dụng: sơ đồ ERD, `doc_type`, quy tắc nào ép ở DB/ứng dụng, cách trừ kho, lệnh scaffold.
- `docs/adr/` – Quyết định kỹ thuật: 0001 kiến trúc backend, 0002 thiết kế database.
- `database/` – Script SQL là nguồn sự thật của schema (`migrate.ps1`, `migrations/Vxxx__*.sql`, `seed/`).
- `docs/module-notes/` – Ghi chú từng mảng đã làm: file/thư mục đã tạo, API, quyết định, lỗi đã gặp, hạn chế, cách test. Đọc trước khi sửa một mảng có sẵn.

Khi code một tính năng, luôn dẫn chiếu UC/BR liên quan trong spec. Nếu thay đổi nghiệp vụ, cập nhật `docs/spec.md` hoặc thêm ADR.

## Công nghệ đã chốt

- Backend: ASP.NET Core Web API trên .NET 10, Clean Architecture với cách tổ chức thư mục/file theo FoMed-API (mục "Kiến trúc backend" bên dưới), EF Core **database-first** (scaffold từ DB, KHÔNG dùng EF Migrations) + SQL viết tay cho trừ kho/FEFO và báo cáo nặng, Repository + Unit of Work, JWT có claim vai trò, CORS, SignalR (giai đoạn 5), Serilog, Swagger, health check. Chi tiết: ADR-0001.
- Database: SQL Server 2022 chạy bằng Docker khi dev: `docker-compose.yml` (container `fomart-sqlserver`, cổng 14330, collation server `Latin1_General_100_CI_AI`), mật khẩu SA trong `.env` (không commit). Dựng database: `./database/migrate.ps1 -Docker -Seed`. KHÔNG dùng container `azuresqledge` của khóa học cũ.
- Frontend: Vite + React + TypeScript + Tailwind CSS v4, React Router, TanStack Query (dữ liệu server), Zustand (state trên máy), React Hook Form + Zod (form), Dexie (IndexedDB, ngoại tuyến), `vite-plugin-pwa`, `uuid` (v7), thư viện quét mã (`@zxing/browser` hoặc `html5-qrcode`, chốt sau buổi thử camera, ADR-0003). Chi tiết: mục "Kiến trúc frontend".
- Test: chưa tạo project test ở giai đoạn này. Khi thêm: xUnit + Testcontainers (SQL Server), một project `FoMart.Tests` (phải bổ sung vào cây thư mục bên dưới trước). CI: GitHub Actions.

## Cấu trúc repo

```
backend/
database/   (00_create_database.sql, migrations/, seed/, migrate.ps1)
frontend/
docs/       (spec.md, database.md, module-workflows.md, dev-workflow.md, adr/, module-notes/)
.claude/skills/impeccable/   (skill thiết kế giao diện, xem mục "Kiến trúc frontend")
PRODUCT.md, DESIGN.md        (định hướng sản phẩm và hệ thống thiết kế cho skill Impeccable; tạo ở bước đăng nhập)
docker-compose.yml
README.md
```

## Kiến trúc backend (BẮT BUỘC tuân theo)

Clean Architecture, nhưng tên thư mục, tên file và luồng code giống FoMed-API. So với FoMed chỉ dời đúng 3 thứ để đúng chiều phụ thuộc: entity sang `Domain/Entities/`, interface repository và Unit of Work sang `Application/Interfaces/`. Mọi thứ khác giữ như FoMed.

### Chiều phụ thuộc

```
FoMart.Api ──► FoMart.Infrastructure ──► FoMart.Application ──► FoMart.Domain
     └──────────────────────────────────────►┘
```

- `Domain` không tham chiếu project hay package nào (kể cả EF Core).
- `Application` chỉ tham chiếu `Domain`. KHÔNG tham chiếu `Infrastructure`, KHÔNG dùng EF Core.
- `Infrastructure` tham chiếu `Application` (để cài đặt interface) và `Domain`.
- `Api` tham chiếu `Application` + `Infrastructure`; chỉ `Program.cs` được dùng tới kiểu của `Infrastructure` (đăng ký DI).

### Cây thư mục (danh sách ĐÓNG)

Mỗi project chỉ có đúng các thư mục dưới đây. Muốn thêm thư mục mới: sửa mục này trong CLAUDE.md trước. Thư mục trong danh sách nhưng chưa có file nào (ví dụ `Domain/Enums`, `Domain/Rules`, `Authentication/`) thì chỉ tạo khi có file đầu tiên.

```
backend/
  FoMart.slnx  global.json  Directory.Build.props  Directory.Packages.props  .config/dotnet-tools.json

  FoMart.Domain/
    Entities/                   entity scaffold từ DB — KHÔNG sửa tay
    Enums/                      hằng số trạng thái trùng CHECK trong DB: SaleStatus, DocType, LotKind...
    Rules/                      quy tắc nghiệp vụ thuần: FefoAllocator, UnitConverter, CountDiffCalculator
    Exceptions/                 BusinessRuleException

  FoMart.Application/
    DTO/HTTPResponseData.cs     khuôn phản hồi chung + HTTPResponse.Success/Fail để tạo nhanh trong Service
    DTO/PagedResult.cs          khuôn phân trang chung (Items, Page, PageSize, TotalCount, TotalPages)
    DTO/<Mảng>/                 <ChủĐề>Models.cs, <Mảng>ResponseMessageDTO.cs
    Interfaces/Repositories/    I<ChủĐề>Repository.cs, IRepositoryBase.cs
    Interfaces/UnitOfWork/      IUnitOfWork.cs, IWriteScope.cs
    Interfaces/Authentication/  ITokenService.cs, IPasswordHasher.cs
    Services/<Mảng>/            <ChủĐề>Service.cs

  FoMart.Infrastructure/
    DbContext/                  FoMartDbContext.cs (scaffold, KHÔNG sửa tay), FoMartDbContext.Custom.cs (viết tay)
    Repositories/               RepositoryBase.cs, <ChủĐề>Repository.cs
    UnitOfWork/                 UnitOfWork.cs, WriteScope.cs
    Authentication/             JwtTokenService.cs, JwtOptions.cs, BcryptPasswordHasher.cs

  FoMart.Api/
    Controllers/<Mảng>/         <ChủĐề>Controller.cs
    Middleware/                 ApiExceptionHandler.cs
    Properties/launchSettings.json   cổng chạy: http://localhost:5224, https://localhost:7036
    Program.cs                  đăng ký DI, CORS, auth, Swagger, health check — một chỗ duy nhất
    appsettings.json, appsettings.Development.json
```

Quy tắc dễ nhớ: interface ở Application và class cài đặt ở Infrastructure dùng **cùng tên thư mục** (`Repositories/`, `UnitOfWork/`, `Authentication/`): `Application/Interfaces/Repositories/IProductRepository.cs` ↔ `Infrastructure/Repositories/ProductRepository.cs`.

### Mảng (area) và tên file

Mảng là tên thư mục con trong `DTO/`, `Services/`, `Controllers/`. Chủ đề là tên file. Dùng đúng danh sách sau:

| Mảng | Chủ đề | UC |
| --- | --- | --- |
| `Auth` | Auth, User | UC-11 |
| `Store` | Store | UC-11 |
| `Catalog` | Product, Category, Supplier, Location | UC-01, UC-02 |
| `Purchasing` | PurchaseOrder, GoodsReceipt | UC-03, UC-04 |
| `Sales` | Sale | UC-05, UC-06 |
| `Inventory` | Stock, Transfer, Disposal, StockCount | UC-07, UC-08, UC-09 |
| `Report` | Report | UC-10 |

Ví dụ: `Application/DTO/Catalog/ProductModels.cs`, `Application/Services/Catalog/ProductService.cs`, `Api/Controllers/Catalog/ProductController.cs`.

| Loại | Quy ước |
| --- | --- |
| DTO | `sealed record` trong `<ChủĐề>Models.cs`: `<ChủĐề>Response`, `Create<ChủĐề>Request`, `Update<ChủĐề>Request`, `<ChủĐề>SearchRequest` |
| Thông báo | `static class <Mảng>ResponseMessageDTO` chứa `const string` tiếng Việt |
| Phản hồi | Mọi API trả `HTTPResponseData<T>` (`DataResponse`, `Message`, `StatusCode`, `Timestamp`), kể cả khi lỗi |
| Service | `sealed class <ChủĐề>Service(IUnitOfWork unitOfWork, ...)`, KHÔNG có interface; method `...Async(..., CancellationToken cancellationToken)` trả `Task<HTTPResponseData<T>>`; lỗi nghiệp vụ dự kiến trả `StatusCode` 400/404/409, không ném exception |
| Controller | `[ApiController] [Route("api/<kebab-số-nhiều>")]`, gọi service rồi `return StatusCode(response.StatusCode, response);`, có `[ProducesResponseType]` |
| Repository | Một repository cho một chủ đề (bảng cha + bảng con: `ProductRepository` gồm `products`, `product_units`, `price_history`). Interface kế thừa `IRepositoryBase<T>` |
| Namespace | = tên project + đường dẫn thư mục. Ngoại lệ: mọi controller dùng `FoMart.Api.Controllers` (như FoMed); DbContext dùng `FoMart.Infrastructure` (tránh trùng tên lớp `DbContext`, nên scaffold lại không phải sửa tay) |
| Comment | Một dòng tiếng Việt phía trên mỗi method public, nói method làm gì |

### Luồng một request

```
<ChủĐề>Controller ─► <ChủĐề>Service ─► unitOfWork.<ChủĐề>Repository ─► FoMartDbContext
   (Api)               (Application)     (interface: Application,          (Infrastructure)
                                          class: Infrastructure)
```

- Service chỉ truy cập dữ liệu qua `unitOfWork.<ChủĐề>Repository`, không inject repository hay DbContext trực tiếp.
- `UnitOfWork` tự tạo repository khi được gọi lần đầu (`_x ??= new XRepository(dbContext)`), nên KHÔNG đăng ký repository trong `Program.cs`.
- Ghi nhiều bảng (nhập hàng, bán hàng, kiểm kê...) phải dùng transaction:
  ```csharp
  await using var write = await unitOfWork.BeginWriteAsync(cancellationToken);
  // ... thao tác qua repository ...
  await unitOfWork.SaveChangesAsync(cancellationToken);
  await write.CommitAsync(cancellationToken);
  ```
  `BeginWriteAsync` KHÔNG khóa toàn cục (khác FoMed): trừ kho khóa dòng `UPDLOCK, ROWLOCK` trong `StockRepository`.
- Truy vấn danh sách/báo cáo: method repository trả thẳng DTO của `Application/DTO` (projection `Select`, `AsNoTracking`, hoặc SQL viết tay). Không tạo tầng query riêng.
- Map entity ↔ DTO bằng tay trong service (method `private static Map(...)`).
- Quy tắc nghiệp vụ thuần (FEFO, quy đổi đơn vị, chênh lệch kiểm kê) viết ở `Domain/Rules`; vi phạm thì ném `BusinessRuleException`, `ApiExceptionHandler` đổi thành `HTTPResponseData` 409.
- `ApiExceptionHandler` là lưới an toàn: đổi lỗi SQL (2601/2627 trùng, 547 ràng buộc, 50001/50002 trigger, 1205 deadlock) và `DbUpdateConcurrencyException` thành `HTTPResponseData` 409.

### Thêm một chủ đề mới (ví dụ Product)

1. `Application/DTO/Catalog/ProductModels.cs` (+ thông báo vào `CatalogResponseMessageDTO.cs`)
2. `Application/Interfaces/Repositories/IProductRepository.cs` → thêm property vào `IUnitOfWork.cs`
3. `Infrastructure/Repositories/ProductRepository.cs` → thêm field + property vào `UnitOfWork.cs`
4. `Application/Services/Catalog/ProductService.cs`
5. `Api/Controllers/Catalog/ProductController.cs`
6. `Program.cs`: thêm `builder.Services.AddScoped<ProductService>();`

### Program.cs và CORS

- Đăng ký DI chỉ ở `Program.cs`, chia khối có comment: Database, Unit of Work, Authentication, Services, CORS, API.
- CORS: một policy tên `"Frontend"`, danh sách origin đọc từ `Cors:AllowedOrigins` trong appsettings. KHÔNG `AllowAnyOrigin()` ở production; không cần `AllowCredentials()` (JWT gửi qua header). CORS không thay cho kiểm tra quyền ở server (NFR-09).

### Xác thực và phân quyền

- JWT (HMAC-SHA256): claim `sub` = id người dùng, `name`, `role` (`owner` | `staff`, hằng số ở `Domain/Enums/UserRole.cs`). Access token 30 phút, refresh token 7 ngày, xoay vòng mỗi lần refresh; dùng lại refresh token đã thu hồi thì thu hồi mọi phiên của người dùng đó.
- Mặc định MỌI endpoint phải đăng nhập (fallback policy). Endpoint công khai đánh dấu `[AllowAnonymous]` (hiện có: `/api/auth/setup|login|login-pin|refresh`, `GET /api/stores`, `/health`, `/openapi`).
- Thao tác chỉ chủ tiệm: `[Authorize(Roles = UserRole.Owner)]` trên action (hoặc cả controller như `UserController`). Phân quyền luôn kiểm ở server (NFR-09).
- Controller cần người dùng hiện tại thì có method `private int? GetCurrentUserId()` đọc claim `JwtRegisteredClaimNames.Sub`, truyền `userId` vào service (như FoMed). Service không đọc HttpContext.
- Endpoint đăng nhập gắn `[EnableRateLimiting("auth")]` (mặc định 10 lần/phút/IP, cấu hình `RateLimiting:AuthPermitLimit`). 401/403/429 đều trả `HTTPResponseData`.
- Khóa ký `Jwt:SigningKey` (≥ 32 ký tự) đặt bằng User Secrets / biến môi trường, không ghi vào appsettings.

### KHÔNG được dùng (muốn dùng phải sửa mục này và ghi ADR trước)

MediatR, CQRS, AutoMapper, microservices, event sourcing, file `DependencyInjection.cs` riêng từng project, inject `IRepositoryBase<T>`/DbContext thẳng vào service, interface cho service, khuôn phản hồi khác `HTTPResponseData<T>` (kể cả ProblemDetails), thư mục ngoài danh sách đóng ở trên.

## Kiến trúc frontend (BẮT BUỘC tuân theo)

SPA chạy trên điện thoại (PWA), gọi `FoMart.Api`. Chia theo **mảng** giống backend (`auth`, `store`, `catalog`, `purchasing`, `sales`, `inventory`, `report`), để một tính năng tìm được ở cùng tên mảng ở cả hai phía.

### Bốn loại state, mỗi loại một công cụ

| Loại | Ví dụ | Công cụ | Nằm ở |
| --- | --- | --- | --- |
| Dữ liệu từ server | sản phẩm, hóa đơn, tồn, thẻ kho | TanStack Query (`useQuery`, `useMutation`) | `features/<mảng>/hooks/` |
| State dùng chung trên máy | phiên đăng nhập, giỏ hàng, giỏ chờ chuyển khoản | Zustand (+ `persist` khi cần giữ qua F5) | `stores/` |
| Form | đăng nhập, sản phẩm + đơn vị, dòng nhập hàng | React Hook Form + Zod | `features/<mảng>/schemas/` + component form |
| Ngoại tuyến | hóa đơn bán lúc mất mạng, hàng đợi đồng bộ | Dexie (IndexedDB) | `offline/` |

State chỉ một component dùng (modal mở, tab đang chọn) thì dùng `useState`. KHÔNG chép dữ liệu server vào Zustand; KHÔNG gọi API trong component.

### Cây thư mục (danh sách ĐÓNG, thêm thư mục mới phải sửa mục này trước)

```
frontend/
  index.html  vite.config.ts  tsconfig.json  tsconfig.app.json  tsconfig.node.json  eslint.config.js  package.json  .env.example
  public/                         icon, ảnh cho PWA
  src/
    main.tsx                      gắn QueryClientProvider + RouterProvider — một chỗ duy nhất
    index.css                     @import "tailwindcss" + @theme (màu, font của FoMart)
    app/                          router.tsx, queryClient.ts, AppLayout.tsx, RequireAuth.tsx, NotFoundPage.tsx
    api/
      client.ts                   hàm request() bọc fetch: gắn JWT, refresh 1 lần khi 401, timeout, đọc HTTPResponseData
      types.ts                    HTTPResponseData<T>, PagedResult<T>, ApiError
      <mảng>.ts                   kiểu DTO (giống record của backend, camelCase) + hàm gọi endpoint của mảng
    features/<mảng>/
      pages/<ChủĐề>Page.tsx       một route = một page
      components/<Tên>.tsx        component chỉ mảng này dùng
      hooks/use<ChủĐề>.ts         query key + useQuery/useMutation của chủ đề
      schemas/<chủĐề>Schema.ts    schema Zod của form
    stores/use<Tên>Store.ts       store Zustand: useAuthStore, useCartStore
    offline/                      db.ts (Dexie), syncQueue.ts — giai đoạn 6
    components/ui/<Tên>.tsx       component dùng chung, chỉ Tailwind: Button, Input, MoneyText, Sheet...
    lib/                          format.ts (18.000đ, dd/mm/yyyy, giờ VN), id.ts (uuid v7)
```

### Luồng một màn hình

```
<ChủĐề>Page ─► use<ChủĐề> (TanStack Query) ─► api/<mảng>.ts ─► api/client.ts ─► FoMart.Api
     │
     └─► use<Tên>Store (Zustand) cho giỏ hàng / phiên đăng nhập
```

- **Query key** của mỗi chủ đề khai báo một lần trong hook của nó (`productKeys.list(params)`, `productKeys.detail(id)`). Mutation xong thì `invalidateQueries` đúng key bị ảnh hưởng (bán xong: `stock`, `products`, `sales`).
- **Lỗi API:** `client.ts` ném `ApiError { status, message, fieldErrors? }` cho mọi mã ngoài 2xx. 400 có `dataResponse` dạng `{ field: [lỗi] }` thì form gán vào `setError`. 409 cần xác nhận (cảnh báo giá, lô hết hạn) thì màn hình hỏi người dùng rồi gửi lại với cờ `confirm…`.
- **Token:** access token trong `useAuthStore` (bộ nhớ + `persist`), refresh token chỉ dùng trong `client.ts`. Nhiều request cùng gặp 401 thì chỉ refresh một lần.
- **Form:** mỗi form một schema Zod khớp ràng buộc của backend (độ dài, số dương...); `useForm({ resolver: zodResolver(schema) })`. Backend vẫn là nơi kiểm tra cuối cùng.
- **Id hóa đơn:** sinh bằng `uuid` v7 (`lib/id.ts`) ngay khi tạo giỏ, gửi lại cùng id khi thử lại.
- **Hiển thị:** tiền `18.000đ`, ngày `dd/mm/yyyy`, giờ Việt Nam, qua `lib/format.ts`. Server trả UTC.

### Tên file và quy ước

| Loại | Quy ước | Ví dụ |
| --- | --- | --- |
| Page | `<ChủĐề>Page.tsx`, export có tên (không dùng `export default`) | `SalePage.tsx` |
| Component | PascalCase `.tsx`, một component chính một file | `CartLine.tsx` |
| Hook TanStack Query | `use<ChủĐề>.ts`, export nhiều hook + `<chủĐề>Keys` | `useProducts.ts` |
| Store | `use<Tên>Store.ts` | `useCartStore.ts` |
| Schema | `<chủĐề>Schema.ts`, kiểu suy ra bằng `z.infer` | `productSchema.ts` |
| API | `api/<mảng>.ts`: `type <ChủĐề>Response` (trùng tên DTO backend) + hàm `getX`, `createX` | `api/sales.ts` |

- Chuỗi giao diện tiếng Việt viết thẳng trong component (chưa cần i18n); tên biến, hàm tiếng Anh.
- Style chỉ bằng class Tailwind; màu và font khai báo token trong `@theme` ở `index.css`, không viết mã màu rời trong component.
- Giao diện thiết kế cho điện thoại trước (bấm bằng ngón cái, nút ≥ 44px), máy tính là phụ.
- Biến môi trường chỉ `VITE_*` trong `.env` (không commit), mẫu ở `.env.example`.

### Chạy khi dev

- `npm run dev` chạy HTTPS (`@vitejs/plugin-basic-ssl`) và `server.host: true` để điện thoại cùng Wi-Fi mở được; camera chỉ chạy qua HTTPS.
- Vite **proxy** `/api` sang `http://localhost:5224` (API), nên frontend gọi đường dẫn tương đối `/api/...`: không vướng CORS, điện thoại chỉ cần truy cập máy dev qua Vite.
- Kiểm tra trước khi mở PR: `npm run build` (gồm `tsc`) và `npm run lint` không lỗi.
- Node ≥ 22.22 (React Router 8). Nên dùng Node 24 LTS: Node 23 đã hết hỗ trợ nên ESLint 10 không cài được, đang ghim ESLint 9.
- Package ghim đúng phiên bản (không `^`), đã phát hành ít nhất 2 tuần. TypeScript ghim 6.0.x vì typescript-eslint chưa hỗ trợ TypeScript 7.
- Chỉ cài package khi bắt đầu dùng tới nó (Zustand, React Hook Form, Zod, `uuid` ở bước đăng nhập/bán hàng; Dexie, `vite-plugin-pwa` ở giai đoạn 6).

### Icon và logo

- Bộ icon DUY NHẤT: **Phosphor** (`@phosphor-icons/react`, MIT). Import từng icon (`import { Barcode } from '@phosphor-icons/react'`) để chỉ icon được dùng vào bundle.
- Kiểu (`weight`): `regular` mặc định; `bold` cho nút chính và màn hình bán hàng (nhìn rõ khi đứng quầy, ngoài nắng); `duotone` chỉ làm điểm nhấn (trạng thái rỗng, nút quét). Không dùng `thin`/`light` (khó nhìn trên điện thoại). Cỡ theo chữ đi kèm (`size="1.25em"`) hoặc token, màu theo `currentColor`.
- **KHÔNG** tự vẽ SVG icon, không dùng emoji hay ký tự Unicode thay icon, không trộn bộ icon khác. Phosphor thiếu icon cần thiết thì báo người dùng trước, không tự vẽ bù.
- Ngoại lệ duy nhất: **logo** FoMart (tài sản thương hiệu). Không dùng icon Phosphor làm logo.
- Logo đã chốt: phương án A "Mái hiên" (mái che sọc vàng `#f2b33d` / kem `#fdf3dc` trên chữ F, nền `#0f6b54`; bản phác thảo trên canvas claude.ai "FoMart Logo"). File trong `frontend/public/`: `logo-mark.svg` (dùng trong giao diện), `favicon.svg`, `apple-touch-icon.png` (180, vuông, iOS tự bo góc), `pwa-192.png`, `pwa-512.png`, `pwa-maskable-512.png` (nền tràn viền, hình thu vào vùng an toàn 75%). Chữ "FoMart" cạnh logo viết bằng font của app, chưa có bản chữ chuyển thành path.

### Thiết kế giao diện: skill Impeccable

- Skill nằm ở `.claude/skills/impeccable/` (Apache-2.0, nguồn và commit trong `SOURCE.md`). Chỉ có `SKILL.md` + `reference/`; **không có và không được thêm `scripts/`**, không chạy launcher/binary `impeccable` (binary gọi dịch vụ ngoài và gửi telemetry). Luôn đi theo nhánh "Launcher unavailable" của `SKILL.md`.
- Mọi màn hình FoMart là chế độ **Operate** (ứng dụng thao tác). Định hướng sản phẩm ở `PRODUCT.md`, hệ thống thiết kế ở `DESIGN.md` (gốc repo, tạo bằng `init` / `document` của skill).
- Xong mỗi màn hình chính (đăng nhập, bán hàng, nhập hàng, danh mục...) chạy `critique` rồi `audit`, sửa, cuối cùng `polish`. Kết quả ghi vào module note của mảng.
- Khi skill mâu thuẫn với CLAUDE.md thì **CLAUDE.md thắng**: Tailwind + token `@theme`, không thư viện component UI, điện thoại trước, font hệ thống (đã chốt trong `DESIGN.md` vì chạy ngoại tuyến và hiển thị tiếng Việt), cây thư mục đóng ở trên.
- Cập nhật skill: làm theo `SOURCE.md` (đọc bản mới trước khi chép đè).

### KHÔNG được dùng (muốn dùng phải sửa mục này và ghi ADR trước)

Redux/Redux Toolkit, axios (đã chọn `fetch` bọc trong `api/client.ts`; nếu đổi thì chỉ đổi file này), thư viện component UI dựng sẵn (MUI, Ant Design, Chakra...), CSS-in-JS, gọi `fetch` ngoài `api/client.ts`, `useEffect` để tải dữ liệu server, lưu token vào cookie tự viết, thư mục ngoài danh sách đóng ở trên.

## Báo cáo sau mỗi module (BẮT BUỘC)

Mỗi khi làm xong một module/mảng (hoặc một phần đáng kể của nó) có sinh ra code, file hoặc thư mục, phải làm CẢ HAI việc:

1. **Báo cáo trong chatbox** theo đúng khuôn:
   - **Thư mục mới**: đường dẫn, vì sao cần.
   - **File mới**: bảng `đường dẫn | vai trò` (gom theo project: Domain, Application, Infrastructure, Api, database, docs).
   - **File sửa**: bảng `đường dẫn | thay đổi gì`.
   - **File xóa** (nếu có) và lý do.
   - **Ghi chú**: quyết định thiết kế, lỗi phát hiện trong lúc làm và cách sửa, hạn chế còn lại, cách test (Swagger/ví dụ request), việc cần người dùng tự làm.
2. **Ghi lại vào `docs/module-notes/<mảng>.md`** (tên mảng chữ thường theo bảng Mảng: `catalog.md`, `auth.md`, `sales.md`...). Mỗi lần làm thêm thì THÊM một mục mới có ngày (`### dd/mm/yyyy – <việc>`) ở cuối file, không xóa mục cũ. Nội dung giống báo cáo trong chatbox, kèm nhánh/PR, UC/BR liên quan. File ghi chú đi cùng commit của tính năng đó.

3. **Cập nhật trang tiến độ online** (dashboard claude.ai "FoMart Tiến Độ": https://claude.ai/artifact/MyXftVoWzFxNwmwYnan1Tw). Mỗi hạng mục là một dòng trong collection `data/progress/modules` của trang, ghi bằng công cụ ArtifactData (`update`, có `if_version`). Trường: `phase`, `phaseName`, `order`, `title`, `refs`, `status` (`done` | `review` = đã push, chờ merge | `in_progress` | `next` | `planned`), `branch`, `pr`, `tests`, `updated` (`yyyy-mm-dd`), `note`. Bắt đầu module thì chuyển `in_progress`; push xong thì `review`; PR merge thì `done`.

Không tính: file sinh tự động (`bin/`, `obj/`), file scaffold trong `Domain/Entities/` (chỉ ghi "scaffold lại, N entity"), script test tạm trong scratchpad.

## Database-first

- Đổi schema = thêm file `database/migrations/V00x__mo_ta.sql` mới (một transaction), chạy `database/migrate.ps1`, rồi scaffold lại (lệnh ở `docs/database.md` mục 6). Không sửa file migration đã áp dụng.
- Scaffold: entity vào `FoMart.Domain/Entities` (namespace `FoMart.Domain.Entities`), DbContext vào `FoMart.Infrastructure/DbContext` (namespace `FoMart.Infrastructure`). KHÔNG sửa tay file scaffold; cấu hình EF bổ sung viết ở `FoMartDbContext.Custom.cs` qua `OnModelCreatingPartial`.

## Các quyết định nghiệp vụ đã chốt

1. Không cố định thiết bị: điện thoại có camera là tối thiểu; hỗ trợ thêm máy quét USB/Bluetooth dạng bàn phím. Mọi nguồn quét quy về một sự kiện "đã nhận mã X".
2. Tồn kho tính theo **đơn vị cơ sở** (lon). Đơn vị khác (lốc, thùng) có hệ số quy đổi nguyên dương và giá bán riêng.
3. Ghi nhận bán theo "phương án C": bán nhanh có ghi nhận + xuất hủy cho hư hỏng đã biết + kiểm kê định kỳ bù sai lệch chưa biết.
4. Thanh toán tiền mặt hoặc chuyển khoản VietQR, nhân viên tự xác nhận (không tích hợp ngân hàng).
5. Một hệ thống phục vụ cả tiệm truyền thống (1 người, 1 vị trí) lẫn minimart (nhiều nhân viên, kho sau + kệ) bằng cấu hình.

## Quy tắc lõi KHÔNG được vi phạm

- **BR-02:** Mọi thay đổi tồn kho phải đi qua một chứng từ và ghi vào `stock_moves`. Không bao giờ sửa trực tiếp `stock.qty`: trigger `TR_stock_moves_apply` tự cộng vào `stock`, role `fomart_app` bị DENY ghi `stock`.
- `stock_moves` chỉ thêm, không sửa, không xóa. Tổng `qty_change` theo (lot, location) luôn bằng `stock.qty`.
- **BR-03:** Chứng từ đã hoàn tất không sửa; hủy bằng chứng từ đảo ngược.
- **BR-04:** Trừ kho theo FEFO (có hạn dùng) hoặc FIFO (không hạn dùng).
- **BR-01:** Bán hàng KHÔNG bị chặn khi tồn không đủ; cho phép âm tạm thời, đánh dấu sản phẩm "cần kiểm kê". (Chuyển kho thì chặn.) Kệ chưa từng có lô của sản phẩm thì trừ vào lô tạm (`lots.kind = provisional`, giá vốn NULL).
- Không cho sửa/giảm giá trên giỏ ở phiên bản 1; `sale_lines.unit_price` là snapshot giá đơn vị.
- **BR-06/07:** Giá vốn lấy theo lô bị trừ; giá bán lưu snapshot trên hóa đơn.
- **BR-11:** Giá vốn lưu DECIMAL(14,4) theo đơn vị cơ sở; tiền trên hóa đơn và doanh thu là số nguyên (đồng).
- **BR-12:** Không sửa hệ số quy đổi/đơn vị cơ sở khi đã có giao dịch.
- Trừ kho luôn chạy trong một transaction, khóa dòng (`UPDLOCK, ROWLOCK`) hoặc `rowversion` (NFR-06).

## Lưu ý kỹ thuật SQL Server

- Mã vạch nullable nhưng duy nhất → filtered unique index `WHERE barcode IS NOT NULL`. Mỗi sản phẩm đúng một đơn vị cơ sở → filtered unique index `WHERE is_base = 1`.
- Collation cả database `Latin1_General_100_CI_AI` (tìm không dấu cả ư/ơ/đ). KHÔNG dùng `Vietnamese_CI_AI` (chỉ bỏ dấu thanh). Bảng `#temp` khai báo `COLLATE DATABASE_DEFAULT`.
- Id hóa đơn sinh trên client bằng UUID v7 (`uniqueidentifier`), để đồng bộ offline không trùng; .NET dùng `Guid.CreateVersion7()`. Là PK nonclustered; khóa cụm là `seq` IDENTITY (SQL Server không sắp GUID theo byte đầu).
- Tiền `BIGINT` (đồng), giá vốn `DECIMAL(14,4)`, trạng thái `VARCHAR` + CHECK, thời điểm `DATETIME2(3)` UTC. Database bật `READ_COMMITTED_SNAPSHOT`.
- Bảng có trigger (`stock_moves`, `product_units`) phải có `HasTrigger(...)` trong EF (scaffold EF Core 10 tự sinh).
- Script SQL viết bằng T-SQL thuần (không biến sqlcmd `$(...)`) để chạy được thẳng trong VS Code/SSMS.
- Filtered unique index KHÔNG được chỉ gồm đúng cột khóa ngoại (ví dụ `(product_id) WHERE is_base = 1`): EF bỏ qua bộ lọc và scaffold thành quan hệ một-một. Thêm cột của điều kiện vào khóa: `(product_id, is_base) WHERE is_base = 1` (xem V003).
- EF Core 10 luôn đưa cột `bool` vào câu INSERT, nên DEFAULT của database (ví dụ `is_active DEFAULT 1`) KHÔNG có tác dụng: khi tạo entity mới phải gán tường minh `IsActive = true` (và mọi cột bool có default khác `false`).

## Quy ước

- Code, tên bảng, tên biến bằng tiếng Anh; UI, comment và tài liệu bằng tiếng Việt.
- Tiền hiển thị dạng `18.000đ`, ngày `dd/mm/yyyy`, múi giờ Việt Nam.
- Commit theo Conventional Commits (`feat:`, `fix:`, `test:`, `docs:`, `refactor:`, `chore:`…). Quy trình nhánh: mục "Git flow" bên dưới.
- Quy tắc nghiệp vụ (FEFO, quy đổi đơn vị, chênh lệch kiểm kê) để thuần trong `Domain/Rules` để khi có project test là viết unit test được ngay.
- Không commit secret (dùng User Secrets / biến môi trường).
- Camera trên điện thoại chỉ chạy qua HTTPS: dùng `@vitejs/plugin-basic-ssl` hoặc tunnel khi test.
- Package NuGet khai báo phiên bản ở `Directory.Packages.props`; chọn bản cho .NET 10 (không lấy bản 11.x) và đã phát hành ít nhất 2 tuần.

## Git flow

Repo: https://github.com/lhknguyen15/FoMart. Mô hình Git Flow rút gọn (không có nhánh `release/*`).

| Nhánh | Tách từ | Merge vào | Dùng cho |
| --- | --- | --- | --- |
| `main` | | | Bản ổn định. Chỉ nhận merge từ `develop` (cuối mỗi giai đoạn) hoặc `hotfix/*`. Gắn tag `v0.<giai đoạn>.0` |
| `develop` | `main` | `main` | Nơi gộp tính năng; luôn build được. Nhánh mặc định trên GitHub |
| `feature/<mảng>-<mô-tả>` | `develop` | `develop` | Một tính năng/issue, ví dụ `feature/catalog-product-api`, `feature/auth-jwt` |
| `fix/<mô-tả>` | `develop` | `develop` | Sửa lỗi chưa lên `main` |
| `hotfix/<mô-tả>` | `main` | `main` rồi `develop` | Sửa gấp lỗi trên bản ổn định |

- KHÔNG commit thẳng vào `main` hay `develop`; mọi thay đổi đi qua Pull Request.
- Tên nhánh: chữ thường, gạch ngang, tiếng Anh; `<mảng>` lấy theo bảng Mảng ở trên (`catalog`, `auth`, `sales`...). Docs/chore nhỏ đi kèm tính năng thì nằm trong nhánh feature đó.
- PR `feature/*` → `develop`: **Squash and merge**, tiêu đề PR theo Conventional Commits (thành commit trên `develop`), mô tả ghi UC/BR liên quan. Trước khi mở PR: build 0 lỗi 0 cảnh báo, chạy thử trên Swagger. Merge xong xóa nhánh.
- Phát hành cuối giai đoạn: PR `develop` → `main` bằng **Create a merge commit**, rồi tag `v0.1.0`, `v0.2.0`...
- Hotfix: PR `hotfix/*` → `main`, tag bản vá (`v0.1.1`), rồi merge `main` ngược về `develop`.
- Bắt đầu việc mới: `git switch develop && git pull && git switch -c feature/<mảng>-<mô-tả>`.

## Trạng thái hiện tại

Đã xong phân tích, đặc tả, script database (`database/`, đã chạy thử trên SQL Server 2022) và quy tắc kiến trúc backend (mục trên, ADR-0001). Khung backend đã dựng theo mục "Kiến trúc backend" (4 project, scaffold 26 entity, Repository + UoW, luồng mẫu `GET /api/stores`, CORS, Serilog, Swagger tại `/swagger`, health check tại `/health`; build 0 cảnh báo). Mảng Catalog xong API (UC-01, UC-02): `api/products` (tìm, chi tiết, tra mã vạch, tạo/sửa, thêm/sửa đơn vị), `api/categories`, `api/suppliers`, `api/locations`; quy tắc ở `Domain/Rules/ProductUnitRules.cs`. Mảng Auth xong (UC-11): `api/auth` (setup chủ tiệm lần đầu, login mật khẩu, login PIN, refresh, logout, me, đổi mật khẩu), `api/users` (chủ tiệm quản lý tài khoản, BR-15). Mảng Purchasing có phiếu nhập (UC-04): `api/goods-receipts` (nháp, thêm/sửa/xóa dòng, hoàn tất tạo lô + ghi sổ kho, hủy bằng chứng từ đảo ngược, giá nhập lần trước; ẩn giá với nhân viên theo BR-08). Lõi kho ở `StockRepository` (chỉ thêm `lots`/`stock_moves`, khóa tồn theo lô) và `api/stock` (tồn theo lô, thẻ kho, đối soát sổ kho). Mảng Sales có bán hàng và hủy hóa đơn (UC-05, UC-06): `api/sales` (Id UUID v7 từ máy bán, tiền mặt trừ kho ngay, chuyển khoản chờ "Đã nhận", trừ kho FEFO qua `Domain/Rules/FefoAllocator.cs`, tồn âm + lô tạm + `needs_count`, hủy bằng `sale_cancel`); BR-18 tự chuyển kho để làm cùng UC-07. Test trên Swagger: gọi `POST /api/auth/login` (dev: `0900000001` / `Test@123`), bấm Authorize, dán `accessToken`. Chạy: `docker compose up -d` rồi `dotnet run --project backend/FoMart.Api`. Chuỗi kết nối nằm trong User Secrets của FoMart.Api. Frontend đã dựng khung theo mục "Kiến trúc frontend" (`frontend/`: Vite 8 + React 19 + TypeScript 6 + Tailwind 4, React Router, TanStack Query, `api/client.ts` bọc fetch, proxy `/api`, HTTPS khi dev); chạy `npm install` rồi `npm run dev` trong `frontend/` (API phải chạy trước). Bước tiếp theo: thử quét mã bằng camera (ADR-0003), rồi đăng nhập.

Vấn đề còn mở (spec mục 11.2): xử lý tồn âm khi đồng bộ hóa đơn offline đến trễ; nguồn dữ liệu ban đầu (Excel hay khai báo dần); tồn đầu kỳ (schema đã hỗ trợ phiếu kiểm kê `scope_type = full` có `adjust_unit_cost`).

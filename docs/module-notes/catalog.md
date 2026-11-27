# Mảng Catalog – ghi chú

Sản phẩm, đơn vị, nhóm hàng, nhà cung cấp, vị trí. UC-01, UC-02 · BR-09, BR-10, BR-12, BR-16, BR-20.

### 25/11/2026 – API Catalog lần đầu

Nhánh `feature/catalog-api` · PR #2 (lỡ merge vào `main`; `develop` đã được fast-forward theo).

**Thư mục mới**

| Thư mục | Vì sao |
| --- | --- |
| `backend/FoMart.Domain/Enums/` | Hằng số `ProductStatus`, `LocationType` trùng CHECK trong DB |
| `backend/FoMart.Domain/Rules/` | Quy tắc thuần của sản phẩm/đơn vị |
| `backend/FoMart.Application/DTO/Catalog/`, `Services/Catalog/` | DTO và service của mảng |
| `backend/FoMart.Api/Controllers/Catalog/` | Controller của mảng |

**File mới**

| Đường dẫn | Vai trò |
| --- | --- |
| `database/migrations/V003__fix_filtered_unique_indexes.sql` | Sửa 3 filtered unique index để EF scaffold đúng quan hệ một-nhiều |
| `Domain/Enums/ProductStatus.cs`, `Domain/Enums/LocationType.cs` | Hằng số trạng thái sản phẩm, loại vị trí |
| `Domain/Rules/ProductUnitRules.cs` | BR-09/10/12/20 và cảnh báo giá đơn vị lớn (`UnitSpec`, `UnitPriceWarning`) |
| `Application/DTO/PagedResult.cs` | Khuôn phân trang chung |
| `Application/DTO/Catalog/ProductModels.cs`, `CategoryModels.cs`, `SupplierModels.cs`, `LocationModels.cs` | Request/response |
| `Application/DTO/Catalog/CatalogResponseMessageDTO.cs` | Thông báo tiếng Việt |
| `Application/Interfaces/Repositories/IProductRepository.cs`, `ICategoryRepository.cs`, `ISupplierRepository.cs`, `ILocationRepository.cs` | Interface repository |
| `Application/Services/Catalog/ProductService.cs`, `CategoryService.cs`, `SupplierService.cs`, `LocationService.cs` | Nghiệp vụ |
| `Infrastructure/Repositories/ProductRepository.cs`, `CategoryRepository.cs`, `SupplierRepository.cs`, `LocationRepository.cs` | Truy vấn EF |
| `Api/Controllers/Catalog/ProductController.cs`, `CategoryController.cs`, `SupplierController.cs`, `LocationController.cs` | Endpoint |
| `Api/Middleware/DevelopmentUserMiddleware.cs` | User giả lập tạm thời (đã xóa ở mảng Auth) |

**File sửa**

| Đường dẫn | Thay đổi |
| --- | --- |
| `Application/DTO/HTTPResponseData.cs` | Thêm `HTTPResponse.Success/Fail` |
| `Application/Interfaces/UnitOfWork/IUnitOfWork.cs`, `Infrastructure/UnitOfWork/UnitOfWork.cs` | Thêm 4 repository |
| `Api/Program.cs` | Đăng ký 4 service; ghi log request bọc ngoài exception handler |
| `Domain/Entities/*`, `Infrastructure/DbContext/FoMartDbContext.cs` | Scaffold lại sau V003 (26 entity) |
| `database/README.md`, `CLAUDE.md` | Ghi V003 và các bài học bên dưới |

**API**

| Endpoint | Chức năng |
| --- | --- |
| `GET /api/products` | Tìm theo tên (không dấu) hoặc mã vạch, lọc nhóm/trạng thái, phân trang |
| `GET /api/products/{id}` | Chi tiết: đơn vị (cờ khóa hệ số), tồn theo vị trí |
| `GET /api/products/barcode/{barcode}` | Tra mã vạch → sản phẩm + đúng đơn vị |
| `POST/PUT /api/products`, `POST/PUT /api/products/{id}/units[/{unitId}]` | Tạo/sửa sản phẩm và đơn vị, ghi lịch sử giá |
| `GET/POST/PUT /api/categories`, `/api/suppliers`, `/api/locations` | Danh mục phụ |

**Ghi chú**

- Quyết định: cảnh báo giá đơn vị lớn > hệ số × giá cơ sở trả 409, gửi lại với `confirmPriceWarning: true` để lưu. Đổi đơn vị nhập mặc định / vị trí bán mặc định dùng transaction 2 lần lưu vì filtered unique index không cho hai dòng cùng mặc định.
- Lỗi phát hiện: (1) filtered unique index chỉ trên `product_id` làm EF scaffold quan hệ một-một → V003. (2) EF Core 10 luôn INSERT cột bool nên `is_active DEFAULT 1` không có tác dụng → mọi bản ghi mới bị `is_active = 0`; sửa bằng gán `IsActive = true`. (3) `GroupBy` rồi `OrderBy` theo record đã dựng không dịch được SQL → sắp theo khóa nhóm trước.
- Hạn chế: chưa có "tạo nhanh sản phẩm" cho nhân viên khi quét mã mới (UC-01 nhánh tạo nhanh) – làm cùng màn hình bán hàng.
- Test: 52 kịch bản HTTP trên database tạm, tất cả qua.

### 25/11/2026 – Phân quyền theo vai trò

Nhánh `feature/auth-jwt`. Thao tác ghi của 4 controller Catalog gắn `[Authorize(Roles = UserRole.Owner)]`; đọc thì ai đăng nhập cũng được. `ProductController` đọc user từ claim `sub`. Chi tiết ở `auth.md`.

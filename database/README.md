# database/

Schema của FoMart theo hướng database-first. Thiết kế nằm ở [docs/database.md](../docs/database.md), lý do ở [docs/adr/0002-database-design.md](../docs/adr/0002-database-design.md).

| File | Nội dung |
| --- | --- |
| `00_create_database.sql` | Tạo database (collation `Latin1_General_100_CI_AI`), bật `READ_COMMITTED_SNAPSHOT`. T-SQL thuần, chạy thẳng trên master được; tên database ở biến `@DatabaseName` |
| `migrations/V001__initial_schema.sql` | Schema, sequence, index, trigger, view, role `fomart_app` |
| `migrations/V002__required_data.sql` | Cấu hình cửa hàng và vị trí "Kệ bán" mặc định |
| `migrations/V003__fix_filtered_unique_indexes.sql` | Sửa 3 filtered unique index để EF scaffold đúng quan hệ một-nhiều (`Product.ProductUnits`, `Product.Lots`) |
| `seed/dev_sample_data.sql` | Dữ liệu mẫu dev: 2 tài khoản, 5 sản phẩm, 1 phiếu nhập đã hoàn tất |
| `migrate.ps1` | Chạy các migration chưa áp dụng theo thứ tự, kiểm checksum |

## Chạy

Cần Docker Desktop và `sqlcmd` trên máy.

```powershell
Copy-Item .env.example .env                  # lần đầu: đặt MSSQL_SA_PASSWORD riêng trong .env
docker compose up -d                         # SQL Server 2022 tại localhost,14330 (container fomart-sqlserver)
./database/migrate.ps1 -Docker -Seed         # tạo database FoMart + dữ liệu mẫu
```

Chuỗi kết nối cho API (đặt bằng User Secrets, không ghi vào `appsettings.json`):

```
Server=localhost,14330;Database=FoMart;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True
```

Khóa ký JWT cho API (một lần, chuỗi ngẫu nhiên ≥ 32 ký tự):

```powershell
dotnet user-secrets set "Jwt:SigningKey" "<chuỗi ngẫu nhiên dài>" --project backend/FoMart.Api
```

Dùng SQL Server cài trên Windows thay cho Docker: `./database/migrate.ps1 -Seed` (Windows auth), hoặc `-Server ... -User ... -Password ...`.

Làm lại database từ đầu: `docker compose down -v` (xóa cả volume dữ liệu), rồi `up -d` và migrate lại.

Tài khoản trong dữ liệu mẫu: chủ tiệm `0900000001` / `Test@123` (PIN `1234`), nhân viên `0900000002` / PIN `1234`.

## Thay đổi schema

1. Thêm file mới `migrations/V003__mo_ta_ngan.sql`, bắt đầu bằng `SET XACT_ABORT ON; BEGIN TRANSACTION;` và kết thúc bằng `COMMIT`. Không sửa file đã áp dụng.
2. `./database/migrate.ps1`
3. Scaffold lại EF Core (lệnh ở docs/database.md mục 6), build, chạy test.

/* ============================================================
   FoMart - Tạo database (chạy trên master)
   T-SQL thuần: chạy thẳng trong VS Code / SSMS / Azure Data Studio, không cần SQLCMD mode.
   migrate.ps1 cũng chạy file này và thay giá trị @DatabaseName theo tham số -Database.
   Chạy lại nhiều lần không sao (có kiểm tra tồn tại).
   ============================================================ */
SET NOCOUNT ON;

DECLARE @DatabaseName SYSNAME = N'FoMart';   -- đổi tên database ở đây nếu cần
DECLARE @sql NVARCHAR(MAX);

-- Collation cả database là Latin1_General_100_CI_AI: bỏ qua MỌI dấu tiếng Việt kể cả ư/ơ/ă/â/đ
-- ("nuoc mam" tìm ra "Nước mắm"); Vietnamese_CI_AI chỉ bỏ dấu thanh nên không dùng (ADR-0002).
-- Đặt ở cấp database để không bị "collation conflict" khi so sánh cột với biến / table variable.
-- Bảng #temp dùng collation của tempdb: khai báo cột chuỗi bằng COLLATE DATABASE_DEFAULT.
IF DB_ID(@DatabaseName) IS NULL
BEGIN
    SET @sql = N'CREATE DATABASE ' + QUOTENAME(@DatabaseName) + N' COLLATE Latin1_General_100_CI_AI;';
    EXEC (@sql);
END;

-- Database có sẵn mà sai collation thì dừng, không migrate tiếp.
-- CAST sang NVARCHAR: DATABASEPROPERTYEX trả về sql_variant, so sánh trực tiếp phụ thuộc collation của server.
DECLARE @collation NVARCHAR(128) = CAST(DATABASEPROPERTYEX(@DatabaseName, 'Collation') AS NVARCHAR(128));
IF @collation IS NULL OR @collation <> N'Latin1_General_100_CI_AI'
BEGIN
    DECLARE @msg NVARCHAR(400) = CONCAT(N'Database ', @DatabaseName, N' có collation ', ISNULL(@collation, N'NULL'),
        N', cần Latin1_General_100_CI_AI. Tạo lại database hoặc đổi collation trước khi migrate.');
    THROW 50000, @msg, 1;
END;

-- Đọc không chặn ghi (báo cáo không khóa màn hình bán hàng).
-- Trừ kho vẫn khóa dòng bằng UPDLOCK nên không bị ảnh hưởng (NFR-06).
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = @DatabaseName AND is_read_committed_snapshot_on = 1)
BEGIN
    SET @sql = N'ALTER DATABASE ' + QUOTENAME(@DatabaseName) + N' SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;';
    EXEC (@sql);
END;

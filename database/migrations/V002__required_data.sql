/* ============================================================
   FoMart - V002: dữ liệu bắt buộc để hệ thống chạy được
   - Một dòng cấu hình cửa hàng (core.stores, id = 1).
   - Một vị trí "Kệ bán" là vị trí bán mặc định (BR-16).
   Tài khoản chủ tiệm đầu tiên KHÔNG tạo ở đây: API có bước thiết lập lần đầu
   (chỉ chạy khi chưa có owner nào, BR-15). Dữ liệu dev nằm ở seed/.
   ============================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM core.stores)
    INSERT INTO core.stores (id, name) VALUES (1, N'Tiệm tạp hóa');

IF NOT EXISTS (SELECT 1 FROM catalog.locations WHERE is_default_sale = 1)
    INSERT INTO catalog.locations (name, type, is_default_sale) VALUES (N'Kệ bán', 'shelf', 1);

COMMIT TRANSACTION;
GO

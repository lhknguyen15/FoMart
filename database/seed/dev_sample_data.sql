/* ============================================================
   FoMart - dữ liệu mẫu cho môi trường phát triển
   CHỈ dùng cho dev/demo. Mã vạch là mã giả, không phải mã sản phẩm thật.
   Chạy được nhiều lần: bỏ qua nếu đã có tài khoản 0900000001.

   Tài khoản:
     Chủ tiệm  0900000001 / Test@123  (PIN 1234)
     Nhân viên 0900000002 / PIN 1234
   Hash: BCrypt cost 11 ($2a$), tương thích BCrypt.Net-Next.
   ============================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM auth.users WHERE phone = '0900000001')
BEGIN
    PRINT N'Seed đã chạy trước đó, bỏ qua.';
    RETURN;
END

BEGIN TRANSACTION;

DECLARE @pwd VARCHAR(255) = '$2a$11$uRaRg1zeS7msLEt2KTzEXeuCUteabeenZV7CxDFXVnqzeEEKk3W0a'; -- Test@123
DECLARE @pin VARCHAR(255) = '$2a$11$yz2YKch42y.hpl/XCMujW.rNZhI92EY4pe.ttdad.VEeosVdBhnx6'; -- 1234

INSERT INTO auth.users (name, phone, password_hash, pin_hash, role)
VALUES (N'Chủ tiệm (dev)', '0900000001', @pwd, @pin, 'owner'),
       (N'Nhân viên (dev)', '0900000002', NULL, @pin, 'staff');

DECLARE @owner INT = (SELECT id FROM auth.users WHERE phone = '0900000001');

UPDATE core.stores
SET name = N'Tạp hóa Mẫu', bank_bin = '970436', bank_account_no = '0000000000',
    bank_account_name = N'NGUYEN VAN MAU', updated_by = @owner, updated_at = SYSUTCDATETIME()
WHERE id = 1;

-- Thêm kho sau để thử mô hình minimart (2 vị trí).
INSERT INTO catalog.locations (name, type) VALUES (N'Kho sau', 'storage');
DECLARE @shelf INT   = (SELECT id FROM catalog.locations WHERE is_default_sale = 1);
DECLARE @storage INT = (SELECT id FROM catalog.locations WHERE name = N'Kho sau');

INSERT INTO catalog.categories (name, sort_order)
VALUES (N'Nước giải khát', 1), (N'Mì gói', 2), (N'Gia vị', 3), (N'Trứng, sữa', 4);

INSERT INTO catalog.suppliers (name, phone, zalo, note)
VALUES (N'Đại lý nước ngọt Minh Tâm', '0911111111', '0911111111', N'Giao thứ 2, thứ 5'),
       (N'NPP thực phẩm An Phát',     '0922222222', NULL,         N'Gọi trước 1 ngày');

DECLARE @drinks INT = (SELECT id FROM catalog.categories WHERE name = N'Nước giải khát');
DECLARE @noodle INT = (SELECT id FROM catalog.categories WHERE name = N'Mì gói');
DECLARE @spice  INT = (SELECT id FROM catalog.categories WHERE name = N'Gia vị');
DECLARE @egg    INT = (SELECT id FROM catalog.categories WHERE name = N'Trứng, sữa');
DECLARE @supA   INT = (SELECT id FROM catalog.suppliers WHERE phone = '0911111111');
DECLARE @supB   INT = (SELECT id FROM catalog.suppliers WHERE phone = '0922222222');

-- Sản phẩm: (tên, nhóm, NCC, có hạn dùng, tồn tối thiểu, tồn kệ tối thiểu, nút nhanh)
DECLARE @p TABLE (k VARCHAR(10), name NVARCHAR(200), category_id INT, supplier_id INT,
                  track_expiry BIT, min_stock INT, min_shelf INT, quick SMALLINT);
INSERT INTO @p VALUES
 ('cola',   N'Nước ngọt Cola lon 330ml',  @drinks, @supA, 1, 48, 12, NULL),
 ('water',  N'Nước suối chai 500ml',      @drinks, @supA, 1, 48, 12, NULL),
 ('noodle', N'Mì tôm chua cay gói 75g',   @noodle, @supB, 1, 60, 20, NULL),
 ('fish',   N'Nước mắm chai 500ml',       @spice,  @supB, 1, 6,  3,  NULL),
 ('egg',    N'Trứng gà',                  @egg,    NULL,  1, 30, 10, 1);

INSERT INTO catalog.products (name, category_id, default_supplier_id, track_expiry, min_stock, min_shelf_stock, quick_button_order, created_by)
SELECT name, category_id, supplier_id, track_expiry, min_stock, min_shelf, quick, @owner FROM @p;

-- Đơn vị: (sản phẩm, tên, hệ số, mã vạch, giá, cơ sở, đơn vị nhập mặc định)
DECLARE @u TABLE (k VARCHAR(10), name NVARCHAR(30), factor INT, barcode VARCHAR(32),
                  price BIGINT, is_base BIT, is_purchase BIT);
INSERT INTO @u VALUES
 ('cola',   N'lon',   1,  '8930000000017', 10000,  1, 0),
 ('cola',   N'lốc',   6,  '8930000000024', 58000,  0, 0),
 ('cola',   N'thùng', 24, '8930000000031', 220000, 0, 1),
 ('water',  N'chai',  1,  '8930000000048', 5000,   1, 0),
 ('water',  N'thùng', 24, '8930000000055', 100000, 0, 1),
 ('noodle', N'gói',   1,  '8930000000062', 4000,   1, 0),
 ('noodle', N'thùng', 30, '8930000000079', 110000, 0, 1),
 ('fish',   N'chai',  1,  '8930000000086', 38000,  1, 1),
 ('egg',    N'quả',   1,  NULL,            3500,   1, 0),
 ('egg',    N'vỉ 10', 10, NULL,            33000,  0, 1);

INSERT INTO catalog.product_units (product_id, name, factor, barcode, sale_price, is_base, is_default_purchase)
SELECT pr.id, u.name, u.factor, u.barcode, u.price, u.is_base, u.is_purchase
FROM @u AS u
JOIN @p AS p ON p.k = u.k
JOIN catalog.products AS pr ON pr.name = p.name;

INSERT INTO catalog.price_history (unit_id, old_price, new_price, changed_by)
SELECT id, NULL, sale_price, @owner FROM catalog.product_units;

/* ---------- Một phiếu nhập đã hoàn tất, đi đúng luồng: lô -> stock_moves -> (trigger) stock ---------- */
INSERT INTO purchasing.goods_receipts (supplier_id, location_id, status, created_by)
VALUES (@supA, @storage, 'draft', @owner);
DECLARE @receipt INT = SCOPE_IDENTITY();

-- (sản phẩm, đơn vị, số lượng, giá nhập/đơn vị, hạn dùng)
DECLARE @r TABLE (line_no SMALLINT, k VARCHAR(10), unit NVARCHAR(30), qty INT, price BIGINT, expiry DATE);
INSERT INTO @r VALUES
 (1, 'cola',   N'thùng', 2, 185000, DATEADD(MONTH, 6, CAST(SYSUTCDATETIME() AS DATE))),
 (2, 'water',  N'thùng', 2, 80000,  DATEADD(MONTH, 9, CAST(SYSUTCDATETIME() AS DATE))),
 (3, 'noodle', N'thùng', 1, 95000,  DATEADD(MONTH, 4, CAST(SYSUTCDATETIME() AS DATE)));

INSERT INTO purchasing.receipt_lines (receipt_id, line_no, product_id, unit_id, qty, base_qty, unit_price, line_total, expiry_date)
SELECT @receipt, r.line_no, pu.product_id, pu.id, r.qty, r.qty * pu.factor, r.price, r.qty * r.price, r.expiry
FROM @r AS r
JOIN @p AS p ON p.k = r.k
JOIN catalog.products AS pr ON pr.name = p.name
JOIN catalog.product_units AS pu ON pu.product_id = pr.id AND pu.name = r.unit;

-- Mỗi dòng nhập tạo một lô; giá vốn = giá nhập / hệ số (BR-11).
DECLARE @lots TABLE (lot_id INT, receipt_line_id INT);
MERGE inventory.lots AS t
USING (SELECT rl.id, rl.product_id, rl.expiry_date, CAST(rl.unit_price AS DECIMAL(18,4)) / (rl.base_qty / rl.qty) AS unit_cost
       FROM purchasing.receipt_lines AS rl WHERE rl.receipt_id = @receipt) AS s
ON 1 = 0
WHEN NOT MATCHED THEN
    INSERT (product_id, kind, expiry_date, unit_cost, received_at)
    VALUES (s.product_id, 'receipt', s.expiry_date, ROUND(s.unit_cost, 4), SYSUTCDATETIME())
OUTPUT inserted.id, s.id INTO @lots (lot_id, receipt_line_id);

UPDATE rl SET rl.lot_id = l.lot_id
FROM purchasing.receipt_lines AS rl JOIN @lots AS l ON l.receipt_line_id = rl.id;

INSERT INTO inventory.stock_moves (product_id, lot_id, location_id, qty_change, unit_cost, doc_type, receipt_line_id, user_id)
SELECT rl.product_id, rl.lot_id, @storage, rl.base_qty, lo.unit_cost, 'receipt', rl.id, @owner
FROM purchasing.receipt_lines AS rl
JOIN inventory.lots AS lo ON lo.id = rl.lot_id
WHERE rl.receipt_id = @receipt;

UPDATE purchasing.goods_receipts
SET status = 'completed', completed_by = @owner, completed_at = SYSUTCDATETIME(),
    total = (SELECT SUM(line_total) FROM purchasing.receipt_lines WHERE receipt_id = @receipt)
WHERE id = @receipt;

COMMIT TRANSACTION;
PRINT N'Đã nạp dữ liệu mẫu.';
GO

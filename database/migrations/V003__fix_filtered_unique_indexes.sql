/* ============================================================
   FoMart - V003: sửa 3 filtered unique index để EF scaffold đúng quan hệ một-nhiều
   Vấn đề: index unique chỉ trên product_id (dù có WHERE) khiến EF Core scaffold suy ra
   quan hệ MỘT-MỘT (Product.ProductUnit, Product.Lot) thay vì một-nhiều, vì EF bỏ qua bộ lọc.
   Cách sửa: thêm cột của điều kiện lọc vào khóa index. Dưới bộ lọc cột đó là hằng số,
   nên ràng buộc giữ nguyên (BR-09, ADR-0002 mục 4), nhưng EF không còn coi FK là duy nhất.
   ============================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Mỗi sản phẩm đúng một đơn vị cơ sở (BR-09).
DROP INDEX UX_product_units_base ON catalog.product_units;
CREATE UNIQUE INDEX UX_product_units_base
    ON catalog.product_units(product_id, is_base) WHERE is_base = 1;

-- Mỗi sản phẩm tối đa một đơn vị nhập mặc định.
DROP INDEX UX_product_units_default_purchase ON catalog.product_units;
CREATE UNIQUE INDEX UX_product_units_default_purchase
    ON catalog.product_units(product_id, is_default_purchase) WHERE is_default_purchase = 1;

-- Mỗi sản phẩm tối đa một lô tạm.
DROP INDEX UX_lots_provisional ON inventory.lots;
CREATE UNIQUE INDEX UX_lots_provisional
    ON inventory.lots(product_id, kind) WHERE kind = 'provisional';

COMMIT TRANSACTION;

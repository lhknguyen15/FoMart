/* ============================================================
   FoMart - V001: schema ban đầu
   SQL Server 2019+. Chạy qua migrate.ps1 (sqlcmd -b), cả file nằm trong một transaction.
   Tài liệu: docs/database.md · Quyết định: docs/adr/0002-database-design.md

   Quy ước
   - snake_case, tiếng Anh. Trạng thái lưu VARCHAR + CHECK (đọc được khi query tay).
   - Thời điểm: DATETIME2(3) theo UTC. Hạn dùng: DATE.
   - Tiền trên hóa đơn, doanh thu, giá bán: BIGINT (đồng) (BR-11).
   - Giá vốn theo đơn vị cơ sở: DECIMAL(14,4) (BR-11).
   - Số lượng tồn: INT theo đơn vị cơ sở (BR-09).
   ============================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

/* ============================================================
   1. SCHEMA
   ============================================================ */
EXEC (N'CREATE SCHEMA auth');        -- tài khoản, phiên đăng nhập
EXEC (N'CREATE SCHEMA core');        -- cấu hình cửa hàng
EXEC (N'CREATE SCHEMA catalog');     -- danh mục
EXEC (N'CREATE SCHEMA inventory');   -- lô, tồn, sổ kho, chuyển kho, hủy, kiểm kê
EXEC (N'CREATE SCHEMA purchasing');  -- đặt hàng, nhập hàng
EXEC (N'CREATE SCHEMA sales');       -- bán hàng
GO

/* ============================================================
   2. SEQUENCE - mã chứng từ, an toàn khi nhiều máy cùng ghi
   ============================================================ */
CREATE SEQUENCE purchasing.seq_purchase_order_code AS INT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE purchasing.seq_goods_receipt_code  AS INT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE sales.seq_sale_code                AS INT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE inventory.seq_transfer_code        AS INT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE inventory.seq_disposal_code        AS INT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE inventory.seq_stock_count_code     AS INT START WITH 1 INCREMENT BY 1;
GO

/* ============================================================
   3. AUTH
   ============================================================ */
CREATE TABLE auth.users (
    id             INT IDENTITY(1,1) NOT NULL,
    name           NVARCHAR(100)     NOT NULL,
    phone          VARCHAR(15)       NOT NULL,
    password_hash  VARCHAR(255)      NULL,      -- chủ tiệm đăng nhập bằng mật khẩu
    pin_hash       VARCHAR(255)      NULL,      -- PIN 4-6 số để đổi ca (UC-11)
    role           VARCHAR(10)       NOT NULL,
    is_active      BIT               NOT NULL CONSTRAINT DF_users_is_active DEFAULT 1,
    last_login_at  DATETIME2(3)      NULL,
    created_at     DATETIME2(3)      NOT NULL CONSTRAINT DF_users_created_at DEFAULT SYSUTCDATETIME(),
    updated_at     DATETIME2(3)      NULL,
    row_version    ROWVERSION        NOT NULL,
    CONSTRAINT PK_users PRIMARY KEY (id),
    CONSTRAINT UQ_users_phone UNIQUE (phone),
    CONSTRAINT CK_users_role CHECK (role IN ('owner', 'staff')),
    -- Chủ tiệm bắt buộc có mật khẩu; nhân viên bắt buộc có PIN.
    CONSTRAINT CK_users_credentials CHECK (
        (role = 'owner' AND password_hash IS NOT NULL) OR
        (role = 'staff' AND pin_hash IS NOT NULL))
);
GO

CREATE TABLE auth.refresh_tokens (
    id              BIGINT IDENTITY(1,1) NOT NULL,
    user_id         INT          NOT NULL,
    token_hash      CHAR(64)     NOT NULL,      -- SHA-256 hex, không lưu token gốc
    expires_at      DATETIME2(3) NOT NULL,
    created_at      DATETIME2(3) NOT NULL CONSTRAINT DF_refresh_tokens_created_at DEFAULT SYSUTCDATETIME(),
    revoked_at      DATETIME2(3) NULL,
    replaced_by_id  BIGINT       NULL,          -- xoay vòng token
    CONSTRAINT PK_refresh_tokens PRIMARY KEY (id),
    CONSTRAINT UQ_refresh_tokens_token_hash UNIQUE (token_hash),
    CONSTRAINT FK_refresh_tokens_user FOREIGN KEY (user_id) REFERENCES auth.users(id),
    CONSTRAINT FK_refresh_tokens_replaced_by FOREIGN KEY (replaced_by_id) REFERENCES auth.refresh_tokens(id)
);
CREATE INDEX IX_refresh_tokens_user ON auth.refresh_tokens(user_id);
GO

/* ============================================================
   4. CORE - cấu hình cửa hàng (phiên bản 1: đúng một dòng)
   ============================================================ */
CREATE TABLE core.stores (
    id                   TINYINT       NOT NULL CONSTRAINT DF_stores_id DEFAULT 1,
    name                 NVARCHAR(150) NOT NULL,
    address              NVARCHAR(300) NULL,
    phone                VARCHAR(15)   NULL,
    bank_bin             VARCHAR(8)    NULL,    -- mã BIN ngân hàng cho VietQR
    bank_account_no      VARCHAR(30)   NULL,
    bank_account_name    NVARCHAR(100) NULL,
    expiry_warn_days     SMALLINT      NOT NULL CONSTRAINT DF_stores_expiry_warn_days DEFAULT 7,     -- UC-08
    reorder_cover_days   SMALLINT      NOT NULL CONSTRAINT DF_stores_reorder_cover_days DEFAULT 7,   -- BR-17
    sales_lookback_days  SMALLINT      NOT NULL CONSTRAINT DF_stores_sales_lookback_days DEFAULT 14, -- BR-17
    count_interval_days  SMALLINT      NOT NULL CONSTRAINT DF_stores_count_interval_days DEFAULT 30, -- BR-14
    staff_can_see_cost   BIT           NOT NULL CONSTRAINT DF_stores_staff_can_see_cost DEFAULT 0,   -- BR-08
    idle_lock_minutes    SMALLINT      NOT NULL CONSTRAINT DF_stores_idle_lock_minutes DEFAULT 15,   -- NFR-09
    updated_at           DATETIME2(3)  NULL,
    updated_by           INT           NULL,
    row_version          ROWVERSION    NOT NULL,
    CONSTRAINT PK_stores PRIMARY KEY (id),
    CONSTRAINT CK_stores_singleton CHECK (id = 1),
    CONSTRAINT CK_stores_days CHECK (expiry_warn_days > 0 AND reorder_cover_days > 0
                                     AND sales_lookback_days > 0 AND count_interval_days > 0
                                     AND idle_lock_minutes > 0),
    CONSTRAINT FK_stores_updated_by FOREIGN KEY (updated_by) REFERENCES auth.users(id)
);
GO

/* ============================================================
   5. CATALOG - danh mục (UC-01, UC-02)
   ============================================================ */
CREATE TABLE catalog.categories (
    id          INT IDENTITY(1,1) NOT NULL,
    name        NVARCHAR(100) NOT NULL,
    sort_order  SMALLINT      NOT NULL CONSTRAINT DF_categories_sort_order DEFAULT 0,
    is_active   BIT           NOT NULL CONSTRAINT DF_categories_is_active DEFAULT 1,
    CONSTRAINT PK_categories PRIMARY KEY (id),
    CONSTRAINT UQ_categories_name UNIQUE (name)
);
GO

CREATE TABLE catalog.suppliers (
    id          INT IDENTITY(1,1) NOT NULL,
    name        NVARCHAR(150) NOT NULL,
    phone       VARCHAR(15)   NULL,
    zalo        VARCHAR(30)   NULL,
    note        NVARCHAR(500) NULL,     -- ví dụ: ngày giao hàng thường lệ
    is_active   BIT           NOT NULL CONSTRAINT DF_suppliers_is_active DEFAULT 1,
    created_at  DATETIME2(3)  NOT NULL CONSTRAINT DF_suppliers_created_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_suppliers PRIMARY KEY (id)
);
GO

CREATE TABLE catalog.locations (
    id               INT IDENTITY(1,1) NOT NULL,
    name             NVARCHAR(100) NOT NULL,
    type             VARCHAR(10)   NOT NULL,
    is_default_sale  BIT           NOT NULL CONSTRAINT DF_locations_is_default_sale DEFAULT 0,
    is_active        BIT           NOT NULL CONSTRAINT DF_locations_is_active DEFAULT 1,
    created_at       DATETIME2(3)  NOT NULL CONSTRAINT DF_locations_created_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_locations PRIMARY KEY (id),
    CONSTRAINT UQ_locations_name UNIQUE (name),
    CONSTRAINT CK_locations_type CHECK (type IN ('storage', 'shelf')),
    -- Vị trí bán mặc định phải là kệ bán (BR-16).
    CONSTRAINT CK_locations_default_sale_is_shelf CHECK (is_default_sale = 0 OR type = 'shelf')
);
-- Đúng một vị trí bán mặc định (ràng buộc "ít nhất một" do dữ liệu bắt buộc ở V002 + ứng dụng).
CREATE UNIQUE INDEX UX_locations_default_sale ON catalog.locations(is_default_sale) WHERE is_default_sale = 1;
GO

CREATE TABLE catalog.products (
    id                   INT IDENTITY(1,1) NOT NULL,
    name                 NVARCHAR(200) NOT NULL,  -- tìm không dấu
    category_id          INT           NULL,
    default_supplier_id  INT           NULL,
    track_expiry         BIT           NOT NULL CONSTRAINT DF_products_track_expiry DEFAULT 0,
    min_stock            INT           NOT NULL CONSTRAINT DF_products_min_stock DEFAULT 0,        -- tổng mọi vị trí
    min_shelf_stock      INT           NOT NULL CONSTRAINT DF_products_min_shelf_stock DEFAULT 0,  -- trên kệ (UC-07)
    status               VARCHAR(12)   NOT NULL CONSTRAINT DF_products_status DEFAULT 'active',
    needs_count          BIT           NOT NULL CONSTRAINT DF_products_needs_count DEFAULT 0,      -- BR-01, BR-14
    last_counted_at      DATETIME2(3)  NULL,
    quick_button_order   SMALLINT      NULL,   -- vị trí trên lưới nút bấm nhanh; NULL = không hiện
    created_by           INT           NOT NULL,
    created_at           DATETIME2(3)  NOT NULL CONSTRAINT DF_products_created_at DEFAULT SYSUTCDATETIME(),
    updated_at           DATETIME2(3)  NULL,
    row_version          ROWVERSION    NOT NULL,
    CONSTRAINT PK_products PRIMARY KEY (id),
    CONSTRAINT CK_products_status CHECK (status IN ('active', 'inactive', 'incomplete')),
    CONSTRAINT CK_products_min_stock CHECK (min_stock >= 0 AND min_shelf_stock >= 0),
    CONSTRAINT FK_products_category FOREIGN KEY (category_id) REFERENCES catalog.categories(id),
    CONSTRAINT FK_products_default_supplier FOREIGN KEY (default_supplier_id) REFERENCES catalog.suppliers(id),
    CONSTRAINT FK_products_created_by FOREIGN KEY (created_by) REFERENCES auth.users(id)
);
CREATE INDEX IX_products_name ON catalog.products(name) INCLUDE (status);
CREATE INDEX IX_products_category ON catalog.products(category_id);
CREATE INDEX IX_products_needs_count ON catalog.products(id) WHERE needs_count = 1;
CREATE UNIQUE INDEX UX_products_quick_button ON catalog.products(quick_button_order) WHERE quick_button_order IS NOT NULL;
GO

CREATE TABLE catalog.product_units (
    id                   INT IDENTITY(1,1) NOT NULL,
    product_id           INT          NOT NULL,
    name                 NVARCHAR(30) NOT NULL,     -- lon, lốc, thùng
    factor               INT          NOT NULL,     -- số đơn vị cơ sở (BR-10)
    barcode              VARCHAR(32)  NULL,
    sale_price           BIGINT       NOT NULL,     -- đồng
    is_base              BIT          NOT NULL CONSTRAINT DF_product_units_is_base DEFAULT 0,
    is_default_purchase  BIT          NOT NULL CONSTRAINT DF_product_units_is_default_purchase DEFAULT 0,
    is_active            BIT          NOT NULL CONSTRAINT DF_product_units_is_active DEFAULT 1,
    created_at           DATETIME2(3) NOT NULL CONSTRAINT DF_product_units_created_at DEFAULT SYSUTCDATETIME(),
    updated_at           DATETIME2(3) NULL,
    row_version          ROWVERSION   NOT NULL,
    CONSTRAINT PK_product_units PRIMARY KEY (id),
    CONSTRAINT UQ_product_units_product_name UNIQUE (product_id, name),
    CONSTRAINT UQ_product_units_id_product UNIQUE (id, product_id),   -- đích của FK ghép (unit thuộc đúng product)
    CONSTRAINT CK_product_units_factor CHECK (factor >= 1),
    CONSTRAINT CK_product_units_base_factor CHECK (is_base = 0 OR factor = 1),
    CONSTRAINT CK_product_units_sale_price CHECK (sale_price >= 0),
    CONSTRAINT FK_product_units_product FOREIGN KEY (product_id) REFERENCES catalog.products(id)
);
-- Mã vạch rỗng được, nhưng có thì duy nhất (BR-20).
CREATE UNIQUE INDEX UX_product_units_barcode ON catalog.product_units(barcode) WHERE barcode IS NOT NULL;
-- Mỗi sản phẩm đúng một đơn vị cơ sở (BR-09); "ít nhất một" do ứng dụng tạo cùng sản phẩm.
CREATE UNIQUE INDEX UX_product_units_base ON catalog.product_units(product_id) WHERE is_base = 1;
CREATE UNIQUE INDEX UX_product_units_default_purchase ON catalog.product_units(product_id) WHERE is_default_purchase = 1;
GO

CREATE TABLE catalog.price_history (
    id          BIGINT IDENTITY(1,1) NOT NULL,
    unit_id     INT          NOT NULL,
    old_price   BIGINT       NULL,      -- NULL khi tạo đơn vị
    new_price   BIGINT       NOT NULL,
    changed_by  INT          NOT NULL,
    changed_at  DATETIME2(3) NOT NULL CONSTRAINT DF_price_history_changed_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_price_history PRIMARY KEY (id),
    CONSTRAINT FK_price_history_unit FOREIGN KEY (unit_id) REFERENCES catalog.product_units(id),
    CONSTRAINT FK_price_history_changed_by FOREIGN KEY (changed_by) REFERENCES auth.users(id)
);
CREATE INDEX IX_price_history_unit ON catalog.price_history(unit_id, changed_at);
GO

/* ============================================================
   6. INVENTORY - lô và tồn
   ============================================================ */
CREATE TABLE inventory.lots (
    id           INT IDENTITY(1,1) NOT NULL,
    product_id   INT           NOT NULL,
    kind         VARCHAR(12)   NOT NULL,
    expiry_date  DATE          NULL,
    lot_no       VARCHAR(50)   NULL,
    unit_cost    DECIMAL(14,4) NULL,      -- theo đơn vị cơ sở (BR-11); NULL chỉ với lô tạm
    received_at  DATETIME2(3)  NOT NULL,  -- thứ tự FIFO
    created_at   DATETIME2(3)  NOT NULL CONSTRAINT DF_lots_created_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_lots PRIMARY KEY (id),
    CONSTRAINT UQ_lots_id_product UNIQUE (id, product_id),
    -- receipt: tạo từ dòng nhập; count_adjust: kiểm kê dương / tồn đầu kỳ;
    -- provisional: lô tạm khi bán hàng chưa từng có lô (ADR-0002).
    CONSTRAINT CK_lots_kind CHECK (kind IN ('receipt', 'count_adjust', 'provisional')),
    CONSTRAINT CK_lots_unit_cost CHECK (
        (kind = 'provisional' AND unit_cost IS NULL AND expiry_date IS NULL) OR
        (kind <> 'provisional' AND unit_cost IS NOT NULL AND unit_cost >= 0)),
    CONSTRAINT FK_lots_product FOREIGN KEY (product_id) REFERENCES catalog.products(id)
);
-- FEFO/FIFO: chọn lô theo sản phẩm, hạn dùng, ngày nhận.
CREATE INDEX IX_lots_product_expiry ON inventory.lots(product_id, expiry_date, received_at) INCLUDE (kind, unit_cost);
-- Mỗi sản phẩm tối đa một lô tạm, dùng lại cho các lần bán sau.
CREATE UNIQUE INDEX UX_lots_provisional ON inventory.lots(product_id) WHERE kind = 'provisional';
GO

-- Tồn theo (lô, vị trí). KHÔNG ghi trực tiếp: do trigger trên stock_moves duy trì (BR-02).
CREATE TABLE inventory.stock (
    lot_id       INT          NOT NULL,
    location_id  INT          NOT NULL,
    product_id   INT          NOT NULL,   -- phi chuẩn hóa để khóa và chọn lô theo sản phẩm
    qty          INT          NOT NULL,   -- được âm tạm thời (BR-01)
    updated_at   DATETIME2(3) NOT NULL CONSTRAINT DF_stock_updated_at DEFAULT SYSUTCDATETIME(),
    row_version  ROWVERSION   NOT NULL,   -- NFR-06
    CONSTRAINT PK_stock PRIMARY KEY (lot_id, location_id),
    CONSTRAINT FK_stock_lot FOREIGN KEY (lot_id, product_id) REFERENCES inventory.lots(id, product_id),
    CONSTRAINT FK_stock_location FOREIGN KEY (location_id) REFERENCES catalog.locations(id)
);
CREATE INDEX IX_stock_product_location ON inventory.stock(product_id, location_id) INCLUDE (qty);
GO

/* ============================================================
   7. PURCHASING - đặt hàng, nhập hàng (UC-03, UC-04)
   ============================================================ */
CREATE TABLE purchasing.purchase_orders (
    id            INT IDENTITY(1,1) NOT NULL,
    code          VARCHAR(12)   NOT NULL CONSTRAINT DF_purchase_orders_code
                  DEFAULT ('DH' + RIGHT('00000000' + CAST(NEXT VALUE FOR purchasing.seq_purchase_order_code AS VARCHAR(10)), 8)),
    supplier_id   INT           NOT NULL,
    status        VARCHAR(10)   NOT NULL CONSTRAINT DF_purchase_orders_status DEFAULT 'ordered',
    note          NVARCHAR(500) NULL,
    created_by    INT           NOT NULL,
    created_at    DATETIME2(3)  NOT NULL CONSTRAINT DF_purchase_orders_created_at DEFAULT SYSUTCDATETIME(),
    cancelled_by  INT           NULL,
    cancelled_at  DATETIME2(3)  NULL,
    row_version   ROWVERSION    NOT NULL,
    CONSTRAINT PK_purchase_orders PRIMARY KEY (id),
    CONSTRAINT UQ_purchase_orders_code UNIQUE (code),
    CONSTRAINT CK_purchase_orders_status CHECK (status IN ('ordered', 'partial', 'received', 'cancelled')),
    CONSTRAINT CK_purchase_orders_cancelled CHECK (
        (status = 'cancelled' AND cancelled_by IS NOT NULL AND cancelled_at IS NOT NULL) OR
        (status <> 'cancelled' AND cancelled_by IS NULL AND cancelled_at IS NULL)),
    CONSTRAINT FK_purchase_orders_supplier FOREIGN KEY (supplier_id) REFERENCES catalog.suppliers(id),
    CONSTRAINT FK_purchase_orders_created_by FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT FK_purchase_orders_cancelled_by FOREIGN KEY (cancelled_by) REFERENCES auth.users(id)
);
CREATE INDEX IX_purchase_orders_supplier_status ON purchasing.purchase_orders(supplier_id, status);
GO

CREATE TABLE purchasing.purchase_order_lines (
    id                 INT IDENTITY(1,1) NOT NULL,
    purchase_order_id  INT NOT NULL,
    product_id         INT NOT NULL,
    unit_id            INT NOT NULL,
    qty                INT NOT NULL,                 -- theo đơn vị unit_id
    received_qty       INT NOT NULL CONSTRAINT DF_purchase_order_lines_received_qty DEFAULT 0,
    CONSTRAINT PK_purchase_order_lines PRIMARY KEY (id),
    CONSTRAINT UQ_purchase_order_lines_unit UNIQUE (purchase_order_id, product_id, unit_id),
    CONSTRAINT CK_purchase_order_lines_qty CHECK (qty > 0 AND received_qty >= 0),
    CONSTRAINT FK_purchase_order_lines_order FOREIGN KEY (purchase_order_id) REFERENCES purchasing.purchase_orders(id),
    CONSTRAINT FK_purchase_order_lines_unit FOREIGN KEY (unit_id, product_id) REFERENCES catalog.product_units(id, product_id)
);
GO

CREATE TABLE purchasing.goods_receipts (
    id                 INT IDENTITY(1,1) NOT NULL,
    code               VARCHAR(12)   NOT NULL CONSTRAINT DF_goods_receipts_code
                       DEFAULT ('PN' + RIGHT('00000000' + CAST(NEXT VALUE FOR purchasing.seq_goods_receipt_code AS VARCHAR(10)), 8)),
    supplier_id        INT           NULL,      -- NULL: tự đi mua, không qua nhà cung cấp
    purchase_order_id  INT           NULL,
    location_id        INT           NOT NULL,  -- vị trí nhận
    status             VARCHAR(10)   NOT NULL CONSTRAINT DF_goods_receipts_status DEFAULT 'draft',
    total              BIGINT        NOT NULL CONSTRAINT DF_goods_receipts_total DEFAULT 0,
    note               NVARCHAR(500) NULL,
    created_by         INT           NOT NULL,
    created_at         DATETIME2(3)  NOT NULL CONSTRAINT DF_goods_receipts_created_at DEFAULT SYSUTCDATETIME(),
    completed_by       INT           NULL,
    completed_at       DATETIME2(3)  NULL,
    cancelled_by       INT           NULL,
    cancelled_at       DATETIME2(3)  NULL,
    cancel_reason      NVARCHAR(300) NULL,
    row_version        ROWVERSION    NOT NULL,
    CONSTRAINT PK_goods_receipts PRIMARY KEY (id),
    CONSTRAINT UQ_goods_receipts_code UNIQUE (code),
    CONSTRAINT CK_goods_receipts_status CHECK (status IN ('draft', 'completed', 'cancelled')),
    CONSTRAINT CK_goods_receipts_total CHECK (total >= 0),
    CONSTRAINT CK_goods_receipts_completed CHECK (
        (status = 'draft' AND completed_at IS NULL) OR
        (status IN ('completed', 'cancelled') AND completed_by IS NOT NULL AND completed_at IS NOT NULL)),
    CONSTRAINT CK_goods_receipts_cancelled CHECK (
        (status = 'cancelled' AND cancelled_by IS NOT NULL AND cancelled_at IS NOT NULL AND cancel_reason IS NOT NULL) OR
        (status <> 'cancelled' AND cancelled_by IS NULL AND cancelled_at IS NULL)),
    CONSTRAINT FK_goods_receipts_supplier FOREIGN KEY (supplier_id) REFERENCES catalog.suppliers(id),
    CONSTRAINT FK_goods_receipts_order FOREIGN KEY (purchase_order_id) REFERENCES purchasing.purchase_orders(id),
    CONSTRAINT FK_goods_receipts_location FOREIGN KEY (location_id) REFERENCES catalog.locations(id),
    CONSTRAINT FK_goods_receipts_created_by FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT FK_goods_receipts_completed_by FOREIGN KEY (completed_by) REFERENCES auth.users(id),
    CONSTRAINT FK_goods_receipts_cancelled_by FOREIGN KEY (cancelled_by) REFERENCES auth.users(id)
);
CREATE INDEX IX_goods_receipts_status_created ON purchasing.goods_receipts(status, created_at);
CREATE INDEX IX_goods_receipts_order ON purchasing.goods_receipts(purchase_order_id) WHERE purchase_order_id IS NOT NULL;
GO

CREATE TABLE purchasing.receipt_lines (
    id           INT IDENTITY(1,1) NOT NULL,
    receipt_id   INT          NOT NULL,
    line_no      SMALLINT     NOT NULL,
    product_id   INT          NOT NULL,
    unit_id      INT          NOT NULL,
    qty          INT          NOT NULL,      -- theo đơn vị unit_id
    base_qty     INT          NOT NULL,      -- qty × factor (BR-09)
    unit_price   BIGINT       NULL,          -- giá nhập mỗi unit_id; NULL khi nhân viên bị ẩn giá (UC-04)
    line_total   BIGINT       NULL,
    expiry_date  DATE         NULL,          -- bắt buộc nếu products.track_expiry (ứng dụng kiểm tra)
    lot_no       VARCHAR(50)  NULL,
    lot_id       INT          NULL,          -- điền khi hoàn tất phiếu
    CONSTRAINT PK_receipt_lines PRIMARY KEY (id),
    CONSTRAINT UQ_receipt_lines_line_no UNIQUE (receipt_id, line_no),
    CONSTRAINT CK_receipt_lines_qty CHECK (qty > 0 AND base_qty >= qty),
    CONSTRAINT CK_receipt_lines_price CHECK (
        (unit_price IS NULL AND line_total IS NULL) OR
        (unit_price >= 0 AND line_total = unit_price * qty)),
    CONSTRAINT FK_receipt_lines_receipt FOREIGN KEY (receipt_id) REFERENCES purchasing.goods_receipts(id),
    CONSTRAINT FK_receipt_lines_unit FOREIGN KEY (unit_id, product_id) REFERENCES catalog.product_units(id, product_id),
    CONSTRAINT FK_receipt_lines_lot FOREIGN KEY (lot_id, product_id) REFERENCES inventory.lots(id, product_id)
);
CREATE UNIQUE INDEX UX_receipt_lines_lot ON purchasing.receipt_lines(lot_id) WHERE lot_id IS NOT NULL;
CREATE INDEX IX_receipt_lines_product ON purchasing.receipt_lines(product_id);
GO

/* ============================================================
   8. SALES - bán hàng (UC-05, UC-06)
   Không cho sửa giá trên giỏ (ADR-0002): unit_price = giá của đơn vị lúc bán.
   ============================================================ */
CREATE TABLE sales.sales (
    id              UNIQUEIDENTIFIER NOT NULL,   -- UUID v7 sinh trên máy (NFR-05)
    seq             BIGINT IDENTITY(1,1) NOT NULL, -- khóa cụm tăng dần (xem ADR-0002 về thứ tự GUID trong SQL Server)
    code            VARCHAR(12)   NOT NULL CONSTRAINT DF_sales_code
                    DEFAULT ('HD' + RIGHT('00000000' + CAST(NEXT VALUE FOR sales.seq_sale_code AS VARCHAR(10)), 8)),
    status          VARCHAR(16)   NOT NULL,
    payment_method  VARCHAR(10)   NOT NULL,
    location_id     INT           NOT NULL,      -- kệ bị trừ
    total           BIGINT        NOT NULL,
    cash_given      BIGINT        NULL,          -- tùy chọn, để tính tiền thối
    created_by      INT           NOT NULL,
    created_at      DATETIME2(3)  NOT NULL,      -- giờ trên máy bán (UTC)
    paid_at         DATETIME2(3)  NULL,
    synced_at       DATETIME2(3)  NOT NULL CONSTRAINT DF_sales_synced_at DEFAULT SYSUTCDATETIME(), -- giờ server nhận
    is_offline      BIT           NOT NULL CONSTRAINT DF_sales_is_offline DEFAULT 0,
    cancelled_by    INT           NULL,
    cancelled_at    DATETIME2(3)  NULL,
    cancel_reason   NVARCHAR(300) NULL,
    row_version     ROWVERSION    NOT NULL,
    CONSTRAINT PK_sales PRIMARY KEY NONCLUSTERED (id),
    CONSTRAINT UQ_sales_code UNIQUE (code),
    CONSTRAINT CK_sales_status CHECK (status IN ('pending_payment', 'paid', 'cancelled')),
    CONSTRAINT CK_sales_payment_method CHECK (payment_method IN ('cash', 'transfer')),
    CONSTRAINT CK_sales_total CHECK (total >= 0),
    CONSTRAINT CK_sales_cash_given CHECK (cash_given IS NULL OR (payment_method = 'cash' AND cash_given >= total)),
    CONSTRAINT CK_sales_paid CHECK (
        (status = 'pending_payment' AND paid_at IS NULL) OR
        (status = 'paid' AND paid_at IS NOT NULL) OR
        (status = 'cancelled')),
    CONSTRAINT CK_sales_cancelled CHECK (
        (status = 'cancelled' AND cancelled_by IS NOT NULL AND cancelled_at IS NOT NULL AND cancel_reason IS NOT NULL) OR
        (status <> 'cancelled' AND cancelled_by IS NULL AND cancelled_at IS NULL)),
    CONSTRAINT FK_sales_location FOREIGN KEY (location_id) REFERENCES catalog.locations(id),
    CONSTRAINT FK_sales_created_by FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT FK_sales_cancelled_by FOREIGN KEY (cancelled_by) REFERENCES auth.users(id)
);
CREATE UNIQUE CLUSTERED INDEX CX_sales_seq ON sales.sales(seq);
CREATE INDEX IX_sales_created_status ON sales.sales(created_at, status) INCLUDE (total, payment_method, created_by);
CREATE INDEX IX_sales_pending ON sales.sales(created_at) WHERE status = 'pending_payment';
GO

CREATE TABLE sales.sale_lines (
    id                 BIGINT IDENTITY(1,1) NOT NULL,
    sale_id            UNIQUEIDENTIFIER NOT NULL,
    line_no            SMALLINT      NOT NULL,
    product_id         INT           NULL,      -- NULL: "hàng chưa khai báo" (UC-05)
    unit_id            INT           NULL,
    name_snapshot      NVARCHAR(200) NOT NULL,  -- BR-07
    qty                INT           NOT NULL,
    unit_price         BIGINT        NOT NULL,  -- snapshot giá đơn vị (BR-07)
    line_total         BIGINT        NOT NULL,
    base_qty           INT           NULL,      -- qty × factor
    cost_total         DECIMAL(18,4) NULL,      -- tổng giá vốn các lô bị trừ (BR-06); NULL nếu có phần trừ vào lô tạm
    expired_confirmed  BIT           NOT NULL CONSTRAINT DF_sale_lines_expired_confirmed DEFAULT 0, -- BR-05
    CONSTRAINT PK_sale_lines PRIMARY KEY (id),
    CONSTRAINT UQ_sale_lines_line_no UNIQUE (sale_id, line_no),
    CONSTRAINT CK_sale_lines_qty CHECK (qty > 0 AND unit_price >= 0 AND line_total = qty * unit_price),
    CONSTRAINT CK_sale_lines_declared CHECK (
        (product_id IS NULL AND unit_id IS NULL AND base_qty IS NULL AND cost_total IS NULL) OR
        (product_id IS NOT NULL AND unit_id IS NOT NULL AND base_qty >= qty)),
    CONSTRAINT FK_sale_lines_sale FOREIGN KEY (sale_id) REFERENCES sales.sales(id),
    CONSTRAINT FK_sale_lines_unit FOREIGN KEY (unit_id, product_id) REFERENCES catalog.product_units(id, product_id)
);
CREATE INDEX IX_sale_lines_sale ON sales.sale_lines(sale_id);
CREATE INDEX IX_sale_lines_product ON sales.sale_lines(product_id) WHERE product_id IS NOT NULL;
CREATE INDEX IX_sale_lines_undeclared ON sales.sale_lines(sale_id) WHERE product_id IS NULL;
GO

/* ============================================================
   9. INVENTORY - chuyển kho, xuất hủy, kiểm kê (UC-07, UC-08, UC-09)
   ============================================================ */
CREATE TABLE inventory.transfers (
    id                INT IDENTITY(1,1) NOT NULL,
    code              VARCHAR(12)      NOT NULL CONSTRAINT DF_transfers_code
                      DEFAULT ('CK' + RIGHT('00000000' + CAST(NEXT VALUE FOR inventory.seq_transfer_code AS VARCHAR(10)), 8)),
    from_location_id  INT              NOT NULL,
    to_location_id    INT              NOT NULL,
    is_auto           BIT              NOT NULL CONSTRAINT DF_transfers_is_auto DEFAULT 0,
    sale_id           UNIQUEIDENTIFIER NULL,     -- hóa đơn sinh ra phiếu tự động (BR-18)
    note              NVARCHAR(500)    NULL,
    created_by        INT              NOT NULL,
    created_at        DATETIME2(3)     NOT NULL CONSTRAINT DF_transfers_created_at DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_transfers PRIMARY KEY (id),
    CONSTRAINT UQ_transfers_code UNIQUE (code),
    CONSTRAINT CK_transfers_locations CHECK (from_location_id <> to_location_id),
    CONSTRAINT CK_transfers_auto CHECK ((is_auto = 1 AND sale_id IS NOT NULL) OR (is_auto = 0 AND sale_id IS NULL)),
    CONSTRAINT FK_transfers_from FOREIGN KEY (from_location_id) REFERENCES catalog.locations(id),
    CONSTRAINT FK_transfers_to FOREIGN KEY (to_location_id) REFERENCES catalog.locations(id),
    CONSTRAINT FK_transfers_sale FOREIGN KEY (sale_id) REFERENCES sales.sales(id),
    CONSTRAINT FK_transfers_created_by FOREIGN KEY (created_by) REFERENCES auth.users(id)
);
CREATE INDEX IX_transfers_created ON inventory.transfers(created_at);
GO

CREATE TABLE inventory.transfer_lines (
    id           INT IDENTITY(1,1) NOT NULL,
    transfer_id  INT NOT NULL,
    product_id   INT NOT NULL,
    base_qty     INT NOT NULL,     -- chi tiết theo lô nằm trong stock_moves
    CONSTRAINT PK_transfer_lines PRIMARY KEY (id),
    CONSTRAINT UQ_transfer_lines_product UNIQUE (transfer_id, product_id),
    CONSTRAINT CK_transfer_lines_qty CHECK (base_qty > 0),
    CONSTRAINT FK_transfer_lines_transfer FOREIGN KEY (transfer_id) REFERENCES inventory.transfers(id),
    CONSTRAINT FK_transfer_lines_product FOREIGN KEY (product_id) REFERENCES catalog.products(id)
);
GO

CREATE TABLE inventory.disposals (
    id             INT IDENTITY(1,1) NOT NULL,
    code           VARCHAR(12)   NOT NULL CONSTRAINT DF_disposals_code
                   DEFAULT ('XH' + RIGHT('00000000' + CAST(NEXT VALUE FOR inventory.seq_disposal_code AS VARCHAR(10)), 8)),
    status         VARCHAR(10)   NOT NULL CONSTRAINT DF_disposals_status DEFAULT 'pending',
    note           NVARCHAR(500) NULL,
    created_by     INT           NOT NULL,
    created_at     DATETIME2(3)  NOT NULL CONSTRAINT DF_disposals_created_at DEFAULT SYSUTCDATETIME(),
    decided_by     INT           NULL,      -- người duyệt / từ chối (BR-21); chủ lập thì tự duyệt (BR-15)
    decided_at     DATETIME2(3)  NULL,
    reject_reason  NVARCHAR(300) NULL,
    row_version    ROWVERSION    NOT NULL,
    CONSTRAINT PK_disposals PRIMARY KEY (id),
    CONSTRAINT UQ_disposals_code UNIQUE (code),
    CONSTRAINT CK_disposals_status CHECK (status IN ('pending', 'approved', 'rejected')),
    CONSTRAINT CK_disposals_decided CHECK (
        (status = 'pending' AND decided_by IS NULL AND decided_at IS NULL) OR
        (status IN ('approved', 'rejected') AND decided_by IS NOT NULL AND decided_at IS NOT NULL)),
    CONSTRAINT FK_disposals_created_by FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT FK_disposals_decided_by FOREIGN KEY (decided_by) REFERENCES auth.users(id)
);
CREATE INDEX IX_disposals_pending ON inventory.disposals(created_at) WHERE status = 'pending';
GO

CREATE TABLE inventory.disposal_lines (
    id           INT IDENTITY(1,1) NOT NULL,
    disposal_id  INT           NOT NULL,
    product_id   INT           NOT NULL,
    location_id  INT           NOT NULL,
    lot_id       INT           NULL,      -- NULL: hệ thống chọn lô theo FEFO khi duyệt
    base_qty     INT           NOT NULL,
    reason       VARCHAR(10)   NOT NULL,
    note         NVARCHAR(300) NULL,
    CONSTRAINT PK_disposal_lines PRIMARY KEY (id),
    CONSTRAINT CK_disposal_lines_qty CHECK (base_qty > 0),
    CONSTRAINT CK_disposal_lines_reason CHECK (reason IN ('expired', 'broken', 'damaged', 'other')),
    CONSTRAINT FK_disposal_lines_disposal FOREIGN KEY (disposal_id) REFERENCES inventory.disposals(id),
    CONSTRAINT FK_disposal_lines_product FOREIGN KEY (product_id) REFERENCES catalog.products(id),
    CONSTRAINT FK_disposal_lines_location FOREIGN KEY (location_id) REFERENCES catalog.locations(id),
    CONSTRAINT FK_disposal_lines_lot FOREIGN KEY (lot_id, product_id) REFERENCES inventory.lots(id, product_id)
);
CREATE INDEX IX_disposal_lines_disposal ON inventory.disposal_lines(disposal_id);
GO

CREATE TABLE inventory.stock_counts (
    id            INT IDENTITY(1,1) NOT NULL,
    code          VARCHAR(12)   NOT NULL CONSTRAINT DF_stock_counts_code
                  DEFAULT ('KK' + RIGHT('00000000' + CAST(NEXT VALUE FOR inventory.seq_stock_count_code AS VARCHAR(10)), 8)),
    scope_type    VARCHAR(10)   NOT NULL,
    location_id   INT           NULL,
    category_id   INT           NULL,
    status        VARCHAR(10)   NOT NULL CONSTRAINT DF_stock_counts_status DEFAULT 'counting',
    note          NVARCHAR(500) NULL,
    created_by    INT           NOT NULL,
    created_at    DATETIME2(3)  NOT NULL CONSTRAINT DF_stock_counts_created_at DEFAULT SYSUTCDATETIME(), -- thời điểm chốt số hệ thống
    submitted_by  INT           NULL,
    submitted_at  DATETIME2(3)  NULL,
    approved_by   INT           NULL,
    approved_at   DATETIME2(3)  NULL,
    row_version   ROWVERSION    NOT NULL,
    CONSTRAINT PK_stock_counts PRIMARY KEY (id),
    CONSTRAINT UQ_stock_counts_code UNIQUE (code),
    -- full: kiểm toàn bộ (dùng cho tồn đầu kỳ, spec 11.2)
    CONSTRAINT CK_stock_counts_scope CHECK (
        (scope_type = 'location'  AND location_id IS NOT NULL AND category_id IS NULL) OR
        (scope_type = 'category'  AND category_id IS NOT NULL) OR
        (scope_type IN ('suggested', 'full'))),
    CONSTRAINT CK_stock_counts_status CHECK (status IN ('counting', 'submitted', 'approved', 'cancelled')),
    CONSTRAINT CK_stock_counts_flow CHECK (
        (status IN ('counting', 'cancelled') AND approved_at IS NULL) OR
        (status = 'submitted' AND submitted_at IS NOT NULL AND approved_at IS NULL) OR
        (status = 'approved' AND submitted_at IS NOT NULL AND approved_by IS NOT NULL AND approved_at IS NOT NULL)),
    CONSTRAINT FK_stock_counts_location FOREIGN KEY (location_id) REFERENCES catalog.locations(id),
    CONSTRAINT FK_stock_counts_category FOREIGN KEY (category_id) REFERENCES catalog.categories(id),
    CONSTRAINT FK_stock_counts_created_by FOREIGN KEY (created_by) REFERENCES auth.users(id),
    CONSTRAINT FK_stock_counts_submitted_by FOREIGN KEY (submitted_by) REFERENCES auth.users(id),
    CONSTRAINT FK_stock_counts_approved_by FOREIGN KEY (approved_by) REFERENCES auth.users(id)
);
CREATE INDEX IX_stock_counts_status ON inventory.stock_counts(status, created_at);
GO

CREATE TABLE inventory.count_lines (
    id                  INT IDENTITY(1,1) NOT NULL,
    count_id            INT           NOT NULL,
    product_id          INT           NOT NULL,
    location_id         INT           NOT NULL,
    system_qty          INT           NOT NULL,   -- chốt lúc tạo phiếu, không hiện cho người đếm
    moves_during        INT           NULL,       -- biến động từ lúc tạo đến lúc gửi (BR-19), điền khi gửi
    counted_qty         INT           NULL,       -- NULL = chưa đếm
    diff                AS (counted_qty - (system_qty + moves_during)) PERSISTED,
    reason              VARCHAR(20)   NULL,       -- chủ gán khi duyệt (BR-13)
    adjust_unit_cost    DECIMAL(14,4) NULL,       -- giá vốn cho lô điều chỉnh khi lệch dương / tồn đầu kỳ
    adjust_expiry_date  DATE          NULL,       -- hạn dùng cho lô điều chỉnh
    note                NVARCHAR(300) NULL,
    CONSTRAINT PK_count_lines PRIMARY KEY (id),
    CONSTRAINT UQ_count_lines_product_location UNIQUE (count_id, product_id, location_id),
    CONSTRAINT CK_count_lines_counted CHECK (counted_qty IS NULL OR counted_qty >= 0),
    CONSTRAINT CK_count_lines_reason CHECK (reason IS NULL OR reason IN
        ('unrecorded_sale', 'loss', 'unknown', 'unrecorded_receipt', 'undelivered_sale', 'other')),
    -- Nguyên nhân phải khớp chiều chênh lệch (UC-09 bước 5).
    CONSTRAINT CK_count_lines_reason_sign CHECK (
        reason IS NULL OR reason = 'other' OR
        (reason IN ('unrecorded_sale', 'loss', 'unknown') AND diff < 0) OR
        (reason IN ('unrecorded_receipt', 'undelivered_sale') AND diff > 0)),
    CONSTRAINT CK_count_lines_adjust_cost CHECK (adjust_unit_cost IS NULL OR adjust_unit_cost >= 0),
    CONSTRAINT FK_count_lines_count FOREIGN KEY (count_id) REFERENCES inventory.stock_counts(id),
    CONSTRAINT FK_count_lines_product FOREIGN KEY (product_id) REFERENCES catalog.products(id),
    CONSTRAINT FK_count_lines_location FOREIGN KEY (location_id) REFERENCES catalog.locations(id)
);
GO

/* ============================================================
   10. SỔ BIẾN ĐỘNG KHO (BR-02, NFR-10)
   Mỗi dòng trỏ tới đúng một dòng chứng từ bằng FK thật (không dùng doc_id đa hình).
   ============================================================ */
CREATE TABLE inventory.stock_moves (
    id                BIGINT IDENTITY(1,1) NOT NULL,
    created_at        DATETIME2(3)  NOT NULL CONSTRAINT DF_stock_moves_created_at DEFAULT SYSUTCDATETIME(),
    product_id        INT           NOT NULL,
    lot_id            INT           NOT NULL,
    location_id       INT           NOT NULL,
    qty_change        INT           NOT NULL,   -- có dấu, theo đơn vị cơ sở
    unit_cost         DECIMAL(14,4) NULL,       -- snapshot giá vốn lô; NULL với lô tạm
    doc_type          VARCHAR(16)   NOT NULL,
    receipt_line_id   INT           NULL,
    sale_line_id      BIGINT        NULL,
    transfer_line_id  INT           NULL,
    disposal_line_id  INT           NULL,
    count_line_id     INT           NULL,
    user_id           INT           NOT NULL,
    CONSTRAINT PK_stock_moves PRIMARY KEY (id),
    CONSTRAINT CK_stock_moves_qty CHECK (qty_change <> 0),
    CONSTRAINT CK_stock_moves_doc_type CHECK (doc_type IN
        ('receipt', 'receipt_cancel', 'sale', 'sale_cancel', 'transfer_out', 'transfer_in', 'disposal', 'count_adjust')),
    -- Chiều tăng/giảm theo loại chứng từ.
    CONSTRAINT CK_stock_moves_sign CHECK (
        (doc_type IN ('receipt', 'sale_cancel', 'transfer_in') AND qty_change > 0) OR
        (doc_type IN ('receipt_cancel', 'sale', 'transfer_out', 'disposal') AND qty_change < 0) OR
        (doc_type = 'count_adjust')),
    -- Đúng một FK chứng từ được điền, và khớp với doc_type.
    CONSTRAINT CK_stock_moves_doc_ref CHECK (
        (doc_type IN ('receipt', 'receipt_cancel')
            AND receipt_line_id IS NOT NULL AND sale_line_id IS NULL AND transfer_line_id IS NULL AND disposal_line_id IS NULL AND count_line_id IS NULL) OR
        (doc_type IN ('sale', 'sale_cancel')
            AND sale_line_id IS NOT NULL AND receipt_line_id IS NULL AND transfer_line_id IS NULL AND disposal_line_id IS NULL AND count_line_id IS NULL) OR
        (doc_type IN ('transfer_out', 'transfer_in')
            AND transfer_line_id IS NOT NULL AND receipt_line_id IS NULL AND sale_line_id IS NULL AND disposal_line_id IS NULL AND count_line_id IS NULL) OR
        (doc_type = 'disposal'
            AND disposal_line_id IS NOT NULL AND receipt_line_id IS NULL AND sale_line_id IS NULL AND transfer_line_id IS NULL AND count_line_id IS NULL) OR
        (doc_type = 'count_adjust'
            AND count_line_id IS NOT NULL AND receipt_line_id IS NULL AND sale_line_id IS NULL AND transfer_line_id IS NULL AND disposal_line_id IS NULL)),
    CONSTRAINT FK_stock_moves_lot FOREIGN KEY (lot_id, product_id) REFERENCES inventory.lots(id, product_id),
    CONSTRAINT FK_stock_moves_location FOREIGN KEY (location_id) REFERENCES catalog.locations(id),
    CONSTRAINT FK_stock_moves_receipt_line FOREIGN KEY (receipt_line_id) REFERENCES purchasing.receipt_lines(id),
    CONSTRAINT FK_stock_moves_sale_line FOREIGN KEY (sale_line_id) REFERENCES sales.sale_lines(id),
    CONSTRAINT FK_stock_moves_transfer_line FOREIGN KEY (transfer_line_id) REFERENCES inventory.transfer_lines(id),
    CONSTRAINT FK_stock_moves_disposal_line FOREIGN KEY (disposal_line_id) REFERENCES inventory.disposal_lines(id),
    CONSTRAINT FK_stock_moves_count_line FOREIGN KEY (count_line_id) REFERENCES inventory.count_lines(id),
    CONSTRAINT FK_stock_moves_user FOREIGN KEY (user_id) REFERENCES auth.users(id)
);
-- Thẻ kho, lượng bán trung bình (BR-17), biến động trong lúc kiểm kê (BR-19).
CREATE INDEX IX_stock_moves_product_created ON inventory.stock_moves(product_id, created_at)
    INCLUDE (location_id, qty_change, doc_type);
CREATE INDEX IX_stock_moves_lot_location ON inventory.stock_moves(lot_id, location_id) INCLUDE (qty_change);
CREATE INDEX IX_stock_moves_sale_line ON inventory.stock_moves(sale_line_id) WHERE sale_line_id IS NOT NULL;
CREATE INDEX IX_stock_moves_receipt_line ON inventory.stock_moves(receipt_line_id) WHERE receipt_line_id IS NOT NULL;
CREATE INDEX IX_stock_moves_transfer_line ON inventory.stock_moves(transfer_line_id) WHERE transfer_line_id IS NOT NULL;
CREATE INDEX IX_stock_moves_disposal_line ON inventory.stock_moves(disposal_line_id) WHERE disposal_line_id IS NOT NULL;
CREATE INDEX IX_stock_moves_count_line ON inventory.stock_moves(count_line_id) WHERE count_line_id IS NOT NULL;
GO

/* ============================================================
   11. TRIGGER - bất biến của sổ kho
   EF Core: các bảng có trigger phải khai báo HasTrigger(...) (xem docs/database.md).
   ============================================================ */

-- Ghi sổ là cách DUY NHẤT để đổi tồn: mỗi dòng stock_moves cộng vào stock (BR-02).
CREATE TRIGGER inventory.TR_stock_moves_apply
ON inventory.stock_moves
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;

    UPDATE s
    SET    s.qty = s.qty + d.qty_change,
           s.updated_at = SYSUTCDATETIME()
    FROM   inventory.stock AS s WITH (UPDLOCK, HOLDLOCK)
    JOIN  (SELECT lot_id, location_id, SUM(qty_change) AS qty_change
           FROM inserted GROUP BY lot_id, location_id) AS d
           ON d.lot_id = s.lot_id AND d.location_id = s.location_id;

    INSERT INTO inventory.stock (lot_id, location_id, product_id, qty)
    SELECT d.lot_id, d.location_id, d.product_id, d.qty_change
    FROM  (SELECT lot_id, location_id, product_id, SUM(qty_change) AS qty_change
           FROM inserted GROUP BY lot_id, location_id, product_id) AS d
    WHERE NOT EXISTS (SELECT 1 FROM inventory.stock AS s WITH (UPDLOCK, HOLDLOCK)
                      WHERE s.lot_id = d.lot_id AND s.location_id = d.location_id);
END;
GO

-- Sổ kho chỉ thêm, không sửa, không xóa (NFR-10).
CREATE TRIGGER inventory.TR_stock_moves_append_only
ON inventory.stock_moves
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 50001, N'stock_moves is append-only: create a reversing document instead (BR-03).', 1;
END;
GO

-- Khóa hệ số quy đổi / đơn vị cơ sở khi đã có giao dịch (BR-12).
CREATE TRIGGER catalog.TR_product_units_lock_factor
ON catalog.product_units
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT (UPDATE(factor) OR UPDATE(is_base) OR UPDATE(product_id)) RETURN;

    IF EXISTS (
        SELECT 1
        FROM inserted AS i
        JOIN deleted  AS d ON d.id = i.id
        WHERE (i.factor <> d.factor OR i.is_base <> d.is_base OR i.product_id <> d.product_id)
          AND (EXISTS (SELECT 1 FROM inventory.stock_moves AS m WHERE m.product_id = d.product_id)
            OR EXISTS (SELECT 1 FROM sales.sale_lines AS sl WHERE sl.unit_id = d.id)
            OR EXISTS (SELECT 1 FROM purchasing.receipt_lines AS rl WHERE rl.unit_id = d.id)))
        THROW 50002, N'Cannot change factor/base unit of a unit that has transactions: create a new unit instead (BR-12).', 1;
END;
GO

/* ============================================================
   12. VIEW
   ============================================================ */

-- Tồn theo sản phẩm và vị trí (dùng cho màn hình, gợi ý đặt hàng, chuyển kho).
CREATE VIEW inventory.v_product_stock
AS
SELECT s.product_id, s.location_id, SUM(s.qty) AS qty
FROM inventory.stock AS s
GROUP BY s.product_id, s.location_id;
GO

-- Đối soát: phải luôn rỗng. Dùng trong integration test và job kiểm tra.
CREATE VIEW inventory.v_stock_ledger_mismatch
AS
SELECT COALESCE(s.lot_id, m.lot_id)           AS lot_id,
       COALESCE(s.location_id, m.location_id) AS location_id,
       s.qty                                  AS stock_qty,
       m.ledger_qty
FROM inventory.stock AS s
FULL OUTER JOIN (SELECT lot_id, location_id, SUM(qty_change) AS ledger_qty
                 FROM inventory.stock_moves GROUP BY lot_id, location_id) AS m
     ON m.lot_id = s.lot_id AND m.location_id = s.location_id
WHERE s.qty IS NULL OR m.ledger_qty IS NULL OR s.qty <> m.ledger_qty;
GO

/* ============================================================
   13. QUYỀN CHO TÀI KHOẢN ỨNG DỤNG
   Gán login của API vào role này (không dùng db_owner):
     CREATE USER fomart_api FOR LOGIN fomart_api; ALTER ROLE fomart_app ADD MEMBER fomart_api;
   Trigger vẫn ghi được stock nhờ ownership chaining (cùng owner dbo).
   ============================================================ */
CREATE ROLE fomart_app;
GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON SCHEMA::auth       TO fomart_app;
GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON SCHEMA::core       TO fomart_app;
GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON SCHEMA::catalog    TO fomart_app;
GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON SCHEMA::inventory  TO fomart_app;
GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON SCHEMA::purchasing TO fomart_app;
GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON SCHEMA::sales      TO fomart_app;
DENY  INSERT, UPDATE, DELETE ON inventory.stock       TO fomart_app;  -- BR-02
DENY  UPDATE, DELETE         ON inventory.stock_moves TO fomart_app;  -- NFR-10
GO

COMMIT TRANSACTION;
GO

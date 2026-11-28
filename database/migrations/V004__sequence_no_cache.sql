/* ============================================================
   FoMart - V004: mã chứng từ liên tục, không nhảy số khi SQL Server khởi động lại
   Vấn đề: 6 SEQUENCE sinh mã chứng từ (V001) dùng bộ đệm mặc định. SQL Server giữ sẵn một
   loạt số trong bộ nhớ; khi dịch vụ / container khởi động lại, các số đã giữ mà chưa dùng bị
   mất, nên mã nhảy (PN00000001 -> PN00000051). Không sai dữ liệu nhưng chủ tiệm tưởng mất phiếu.
   Cách sửa: NO CACHE. Mỗi lần lấy số ghi xuống đĩa; với vài trăm chứng từ mỗi ngày thì chi phí
   không đáng kể. Số đã nhảy trước đây giữ nguyên (không lùi sequence để tránh trùng mã).
   Lưu ý: số vẫn có thể hụt khi transaction lấy số rồi rollback (lỗi giữa chừng); đó là hành vi
   đúng của sequence, mã chứng từ không cam kết liên tục tuyệt đối.
   ============================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

ALTER SEQUENCE purchasing.seq_purchase_order_code NO CACHE;
ALTER SEQUENCE purchasing.seq_goods_receipt_code  NO CACHE;
ALTER SEQUENCE sales.seq_sale_code                NO CACHE;
ALTER SEQUENCE inventory.seq_transfer_code        NO CACHE;
ALTER SEQUENCE inventory.seq_disposal_code        NO CACHE;
ALTER SEQUENCE inventory.seq_stock_count_code     NO CACHE;

COMMIT TRANSACTION;

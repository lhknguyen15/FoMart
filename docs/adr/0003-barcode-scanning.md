# ADR-0003: Quét mã vạch bằng camera

- Trạng thái: Đã chấp nhận
- Ngày: 27/11/2026
- Liên quan: UC-04, UC-05, quyết định đã chốt số 1 trong CLAUDE.md ("mọi nguồn quét quy về một sự kiện đã nhận mã X"), [docs/module-notes/frontend.md](../module-notes/frontend.md)

## Bối cảnh

Màn hình bán hàng và nhập hàng mở camera điện thoại, quét liên tục mã EAN-13/EAN-8 (hàng Việt Nam `893…`), UPC (hàng nhập Mỹ) và Code 128 (mã nội bộ). Trước khi làm giao diện, nhánh `feature/frontend-scan-spike` dựng trang thử `/thu-quet` gồm:

- Bốn bộ đọc dùng chung một component camera: `@zxing/browser` 0.2.1 (ZXing viết lại bằng JavaScript), `html5-qrcode` 2.3.8, `BarcodeDetector` có sẵn của trình duyệt, và zxing-cpp biên dịch sang WebAssembly (gói `barcode-detector` 3.2.3 / `zxing-wasm` 3.1.6).
- Đo thời gian quét và thời gian tra mã qua API tách riêng.
- Khung chẩn đoán: độ phân giải, chế độ lấy nét, số khung giải mã mỗi giây, lỗi giải mã gần nhất.
- Zoom, đèn flash, chụp ảnh bằng app camera gốc để đọc, và nhập mã tay.

Kết quả thử trên điện thoại thật của người dùng:

- Mã in trên màn hình laptop đọc được ngay.
- Chai nhựa tròn (Aquafina) trong phòng thiếu sáng không đọc được; bật thêm đèn thì đọc tốt.
- API tra mã mất khoảng 6 ms khi gọi thẳng, khoảng 25 ms qua proxy HTTPS của Vite (đo trên máy dev), nên phần chậm nằm ở khâu quét trên thiết bị chứ không ở máy chủ.
- Trình duyệt trên điện thoại thử không có `AbortSignal.timeout` / `AbortSignal.any`, nên toàn bộ lệnh gọi API lỗi cho tới khi `api/client.ts` bỏ hai hàm này.

Giới hạn của bằng chứng: mới thử trên một điện thoại, chưa ghi số liệu so sánh từng bộ đọc. Kết luận về thư viện dựa nhiều vào tình trạng bảo trì và độ phủ trình duyệt hơn là số đo tốc độ.

## 1. Bộ đọc: zxing-cpp (WebAssembly) qua gói `barcode-detector`

**Quyết định.** Dùng `import { BarcodeDetector } from 'barcode-detector/ponyfill'` (zxing-cpp chạy bằng WebAssembly) trên mọi trình duyệt, gói trong `frontend/src/lib/barcodeDetector.ts`.
- File `zxing_reader.wasm` do chính FoMart phục vụ: `prepareZXingModule` trỏ `locateFile` về file đi kèm bản build, không tải từ CDN jsDelivr như mặc định.
- Chưa dùng `BarcodeDetector` có sẵn của trình duyệt cho bản chính thức.

**Lý do.**
- `html5-qrcode` không có bản mới từ 04/2023, nên bị loại.
- `@zxing/browser` là bản ZXing viết lại bằng JavaScript từ lâu, đọc mã một chiều kém hơn zxing-cpp.
- `BarcodeDetector` có sẵn chỉ có trên Chrome Android, iPhone không có.
- zxing-cpp chạy trên mọi trình duyệt, được bảo trì đều (bản 10/2026), giấy phép MIT. Nó dùng đúng giao diện `BarcodeDetector`, nên sau này bật bộ đọc có sẵn cho Android chỉ cần đổi một dòng nếu đo được nó nhanh hơn.
- Một đường code cho mọi máy giúp kết quả giống nhau giữa các điện thoại của tiệm.
- Tự phục vụ file `.wasm` để quét được khi mất mạng (NFR-05).

**Cái giá.**
- File `.wasm` nặng 947 KB (414 KB gzip). Phần quét phải tải lười (chỉ tải khi mở màn hình có camera). Khi làm PWA thì đưa file vào bộ nhớ đệm trước.
- Lần mở camera đầu tiên phải chờ tải và khởi tạo WebAssembly.

## 2. Ánh sáng, đèn flash, zoom, lấy nét

**Quyết định.** Màn hình có camera luôn có:
- nút bật/tắt đèn flash (hiện khi camera hỗ trợ `torch`);
- nút zoom 1× / 2× (khi hỗ trợ `zoom`);
- bật lấy nét liên tục nếu camera có `focusMode: continuous`;
- yêu cầu độ phân giải 1920×1080;
- gợi ý ngắn khi chưa đọc được: đủ sáng, cách 20–25 cm, mã nằm ngang.

**Lý do.**
- Buổi thử cho thấy ánh sáng là yếu tố quyết định.
- Camera điện thoại không lấy nét được ở khoảng cách quá gần. Đứng xa rồi zoom thì mã vừa nét vừa đủ to.

**Cái giá.** `torch`, `zoom`, `focusMode` không phải trình duyệt nào cũng hỗ trợ (iPhone hạn chế). Giao diện phải ẩn nút khi không có, không được giả định là luôn có.

## 3. Phản hồi khi quét được

**Quyết định.** Mỗi lần đọc được mã:
- tiếng bíp + rung (`navigator.vibrate`, máy nào hỗ trợ);
- viền xanh chớp quanh khung camera;
- kết quả hiện ngay sát camera, không bắt người dùng cuộn.

Cùng một mã trong 2 giây chỉ tính một lần. Âm thanh tạo `AudioContext` trong lúc người dùng bấm nút bật camera, vì điện thoại chặn âm thanh không do người dùng khởi phát.

**Lý do.** Người dùng thử không biết đã quét được hay chưa vì kết quả nằm dưới danh sách. Ở quầy bán hàng, người bán nhìn món hàng chứ không nhìn màn hình (UC-05: 3 lần chạm, 5 giây).

**Cái giá.** Không đáng kể.

## 4. Chuẩn hóa mã trước khi tra

**Quyết định.** Mã UPC-A 12 số được thêm `0` ở đầu thành EAN-13 trước khi tra hoặc lưu. Phải áp dụng ở cả ba chỗ:
- lúc quét (frontend);
- lúc khai báo mã cho đơn vị sản phẩm (backend, `ProductService`);
- lúc tra `GET /api/products/barcode/{code}`.

**Lý do.**
- Cùng một vạch mã bắt đầu bằng số 0, có bộ đọc trả 13 số (EAN-13), có bộ đọc trả 12 số (UPC-A).
- Không chuẩn hóa thì sản phẩm khai báo 13 số sẽ báo "chưa có trong danh mục" khi quét ra 12 số.
- Chuẩn hóa ở backend để mã nhập tay và mã từ máy quét bàn phím cũng đi qua cùng quy tắc.

**Cái giá.** Phải sửa backend (Catalog), làm cùng màn hình bán hàng. Mã đã lưu trước đó cần kiểm tra lại (dữ liệu mẫu hiện dùng EAN-13 nên không bị ảnh hưởng).

## 5. Camera không phải nguồn duy nhất

**Quyết định.** Màn hình bán hàng và nhập hàng nhận mã từ bốn nguồn, đều đi vào cùng một hàm xử lý "đã nhận mã X":
1. camera (bộ đọc ở mục 1);
2. máy quét cầm tay USB/Bluetooth dạng bàn phím (gõ mã rồi Enter);
3. ô tìm theo tên / gõ số mã;
4. chụp ảnh bằng app camera gốc (`<input type="file" accept="image/*" capture="environment">`) rồi đọc mã từ ảnh.

**Lý do.**
- Chai nhựa tròn, bao bì bóng, chỗ tối là trường hợp camera điện thoại có thể không đọc được.
- Máy quét cầm tay giá 300–500 nghìn đồng đọc những ca này tốt, và đã có trong spec.
- Ảnh từ app camera gốc lấy nét tốt hơn luồng video trên web.

**Cái giá.** Thêm một bộ nghe phím toàn màn hình cho máy quét bàn phím, phải phân biệt với việc gõ vào ô nhập.

## 6. Trình duyệt tối thiểu

**Quyết định.**
- Mức hỗ trợ tối thiểu là mục tiêu build mặc định của Vite ("baseline widely available").
- Code không dùng API trình duyệt mới hơn mức đó nếu không có đường dự phòng.
- Thêm thư viện hoặc API mới thì kiểm tra trên điện thoại thật trước khi coi là xong.

**Lý do.** Điện thoại của tiệm tạp hóa thường không phải đời mới. `AbortSignal.any` (Chrome 116 / Safari 17.4) đã làm hỏng toàn bộ lệnh gọi API trên máy thử.

**Cái giá.** Đôi khi phải viết thêm vài dòng thay cho một API tiện lợi.

## Hệ quả

- Gỡ `html5-qrcode`, `@zxing/browser`, `@zxing/library` khỏi `frontend/package.json`.
- Trang thử `/thu-quet` giữ hai bộ đọc (zxing-cpp và có sẵn) để đo tiếp trên các máy khác. Trang này bị xóa khi màn hình bán hàng có component quét chính thức.
- Việc cần làm cùng màn hình bán hàng:
  - component quét chính thức trong `features/sales/components/`, tải lười phần WebAssembly;
  - nút đèn và nút zoom;
  - bộ nghe máy quét bàn phím;
  - chuẩn hóa UPC-A → EAN-13 ở backend và frontend.

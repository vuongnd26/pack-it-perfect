# Pack It Perfect — Game Spec (cho Claude Code / Claude Code Game Studios)

## Core loop
Khay lưới ô vuông (kích thước thay đổi theo level, 3x4 đến 7x7). Mảnh là polyomino
(3-5 ô, hình L/T/S/Z/I/O/P/U/V/W/F). Kéo-thả mảnh từ khay chờ vào lưới, tap để xoay 90°.
Đặt đủ 100% ô trống (không chồng, không đè ô khóa) thì nắp đóng, thắng level.

## Luật nâng cao (mở dần qua các level)
- Ô khóa: không đặt được mảnh lên đó.
- Khu vực (zone): lưới chia 2+ vùng màu, mảnh có nhãn khu và chỉ đặt được trong khu của nó.
- Cặp (pair): 2 mảnh cùng loại phải nằm cạnh nhau (chung cạnh) thì mới thắng.
- Đồ nặng (heavy): mảnh phải có ít nhất 1 ô chạm hàng đáy của lưới.
- Đồ đứng thẳng (upright): mảnh không xoay được, chỉ có đúng 1 hướng.
- Hình in chìm (imprint): lưới hiện mờ vị trí đúng của 1-2 mảnh làm gợi ý (chỉ ở level dễ).

## Độ khó tăng dần — 15 level
1-5: làm quen, không luật nâng cao, 4x3 đến 6x4, nhiều lời giải (dễ).
6-10: thêm lần lượt imprint, zone, pair; kết hợp dần ở level 9-10; bàn 4x4 đến 6x5.
11-15: bỏ gần hết gợi ý, thêm heavy + upright, mảnh 5 ô, bàn 6x5 đến 7x7;
mỗi bố cục được chọn sao cho chỉ còn 1-2 lời giải hợp lệ (đo bằng thuật toán duyệt toàn bộ).

## Thắng / thua
- Thắng: lấp đủ 100% ô + (nếu có) mọi cặp đã chạm nhau → nắp đóng, hiệu ứng confetti + âm "tách".
- Level thường: không giới hạn thời gian/lượt, có thể nhấc mảnh ra xếp lại tự do.
- Level khó (biến thể B, bật bằng cờ `hardMode`): giới hạn thời gian đếm ngược (30-75s tùy level),
  hết giờ mà chưa xong thì thua, hiện màn "Out of time" với CTA thử lại / cài đặt.

## Cảm giác (feel) cần giữ khi chuyển sang Unity
- ASMR: âm thanh nhấc/thả/khớp/đóng nắp rõ ràng, khác nhau theo chất liệu vật (vải/kim loại/gốm).
- Phản hồi tức thì: mảnh nảy nhẹ (scale bounce) khi đặt đúng, rung nhẹ (haptic) trên mobile.
- Before/after: cảnh mở đầu lộn xộn, cảnh kết thúc gọn gàng — đây là hook chính cho playable ad.
- Bóng xem trước (ghost preview) màu xanh/đỏ khi kéo mảnh gần lưới, báo đặt được hay không.

## Tham khảo kỹ thuật (đã chạy thử, dùng được)
Bản HTML/JS prototype đầy đủ 15 level (grid logic, xoay, zone/pair/heavy/upright,
timer, win/fail state, âm thanh Web Audio) đã được viết và kiểm thử bằng cách tự động
giải cả 15 level. Dùng file này làm tài liệu tham khảo logic khi viết lại bằng C#:
không copy trực tiếp code JS, nhưng toàn bộ state machine (grid occupancy check,
rotation, zone/pair/heavy validation) nên port 1-1 vì logic đã được kiểm chứng đúng.

## Việc cần Claude Code làm trước (Unity, C#)
1. ScriptableObject `PieceShapeData`: định nghĩa polyomino bằng danh sách Vector2Int.
2. ScriptableObject `LevelData`: map lưới (string[] hoặc Tile enum 2D), danh sách mảnh
   + thuộc tính (zone/pair/heavy/upright/imprint), giới hạn thời gian (0 = không giới hạn).
3. `GridManager`: occupancy grid, hàm CanPlace(piece, origin) kiểm tra đủ mọi luật trên.
4. `PieceController`: kéo-thả (Input System), xoay bằng tap, snap-to-grid, ghost preview.
5. `LevelManager`: load LevelData, check win condition, chuyển level, timer cho hardMode.
6. Scene đầu tiên: 1 scene test với LevelData của level 1 (map đơn giản 4x3, 3 mảnh,
   không luật nâng cao) để xác nhận core loop chạy đúng trước khi làm UI/hiệu ứng.

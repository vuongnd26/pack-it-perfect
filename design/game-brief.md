# Pack It Perfect — Game Spec v2

_Đồng bộ theo Big Team GDD, sheet `00_Overview_Pack_It` + `01_Pack It_CoreLoop_Mechanics`.
Đây là bản thay thế `design/game-brief.md` — Claude Code nên dùng các con số/text
trong file này làm nguồn chính thức (GDD), không dùng khoảng mơ hồ nữa._

## Short pitch (nguyên văn GDD)

> Pack It Perfect là game giải đố xếp đồ theo phong cách ASMR packing. Người chơi xoay
> và đặt từng món đồ vào vali, túi, hộp cơm, cốp xe cho đến khi lấp kín mọi ô và nắp đóng
> lại. Qua 15 màn, luật mới được mở dần: khuôn gợi ý, khu vực riêng cho từng loại đồ, cặp
> đồ phải nằm cạnh nhau, đồ nặng phải chạm đáy và chai lọ phải đứng thẳng.

Thể loại: Puzzle, ASMR packing. Platform / TA / hướng MO: **chưa điền trong GDD** (xem mục Gaps).

## Core loop (3 bước, đúng thứ tự GDD)

1. **Chọn và xoay** — chọn một món đồ trong khay, chạm để xoay cho vừa chỗ trống.
2. **Đặt đồ** — kéo thả món đồ vào đồ chứa, đúng luật của màn (khu vực, đồ nặng, cặp đồ…).
3. **Đóng nắp** — lấp kín 100% các ô, nắp đóng, kéo khóa, nhận 2–3 sao, sang màn tiếp.

## Điều kiện thắng / thua

- **Thắng:** mọi món đồ đã nằm trong đồ chứa và thỏa mọi luật của màn (kể cả cặp đồ phải
  nằm cạnh nhau). Số sao: **3 sao nếu không dùng Gợi ý, 2 sao nếu có dùng Gợi ý ở màn đó**.
- **Thua — chế độ Thư giãn:** không có trạng thái thua.
- **Thua — chế độ Hẹn giờ** (chỉ áp dụng ở màn khó 5, 10, 15): hết giờ là thua. Màn hình
  thua, nguyên văn: **"Out of time! The taxi is leaving and the trunk won't close."**,
  kèm 2 nút: **"Try again"** và **"Install to keep packing"**.

## Điều khiển (Input)

- Kéo thả món đồ từ khay vào lưới.
- Chạm (không kéo) món đồ **trong khay** → xoay 90°.
- Chạm món đồ **đã đặt trong lưới** → trả về khay. Kéo món đã đặt → đổi chỗ/di chuyển.
- Không giới hạn số lần lấy đồ ra xếp lại.

## Cơ chế chính (bảng mechanics, verbatim)

| Cơ chế                             | Mô tả                                                                                                     | Kích hoạt                                                              | Ghi chú                                                                                                                                                                |
| ---------------------------------- | --------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Kéo thả đặt đồ                     | Bóng xem trước: **xanh nếu đặt được, đỏ nếu không**. Thả đúng → khớp vào ô. Thả sai → về lại khay + rung. | Kéo món đồ trong khay                                                  | Đặt đúng có: hiệu ứng nảy, tia sáng, rung nhẹ (haptic), chữ khen ngẫu nhiên. Thanh % cho biết đã lấp bao nhiêu ô.                                                      |
| Chữ khen (ngẫu nhiên khi đặt khớp) | **"Snug!", "Nice fit!", "Click!", "Perfect!", "Neat!"**                                                   | Mỗi lần đặt đúng                                                       | Danh sách đóng — không tự thêm chữ khác.                                                                                                                               |
| Xoay đồ                            | Xoay 90°/lần chạm                                                                                         | Chạm (không kéo) món trong khay                                        | **Màn 1**: món đã đúng hướng sẵn (không cần xoay) — dùng để dạy thao tác. **Từ màn 2**: nhiều món xuất hiện sai hướng. Món có luật **Đứng thẳng** thì không xoay được. |
| Lấy đồ ra                          | Trả về khay hoặc đổi vị trí                                                                               | Chạm/kéo món đã đặt                                                    | Không giới hạn số lần                                                                                                                                                  |
| Khuôn gợi ý (outline/imprint)      | Hình mờ + icon hiện sẵn, báo vị trí đúng của 1 vài món                                                    | **Chỉ ở màn 6, 8, 9**                                                  | Banner giới thiệu ở màn 6, nguyên văn: **"New: outlines — Faded shapes show where items go"**. **Màn 10 và 15 ghi rõ KHÔNG có khuôn** (khó hơn hẳn).                   |
| Hẹn giờ (chế độ Hẹn giờ)           | Đếm ngược, hết giờ = thua                                                                                 | **Chỉ màn khó 5, 10, 15**: **5 = 30 giây, 10 = 45 giây, 15 = 75 giây** | Banner: **"Hard level! Pack before the taxi leaves"**. Đồng hồ chuyển đỏ khi còn **10 giây**, kêu tích tắc khi còn **5 giây**.                                         |

## Obstacles (bảng, verbatim — số màn là tuyệt đối, không phải khoảng)

| Obstacle             | Mô tả                                                                     | Áp dụng ở màn                                                                        | Text khi vi phạm                                                                                                       |
| -------------------- | ------------------------------------------------------------------------- | ------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------- |
| Ô bị chặn            | Không đặt lên được, không cần lấp                                         | **3, 9, 13**                                                                         | —                                                                                                                      |
| Khu vực (zone)       | Đồ chứa chia 2 khu màu + nhãn, món có icon khu và phải nằm trọn khu đó    | **7 (Food/Stuff), 9 (Kitchen/Toys), 10 (Clothes/Toiletries), 15 (Carry-on/Checked)** | **"[Món] goes on the [khu] side"**                                                                                     |
| Cặp đồ (pair)        | 2 món có icon cặp phải nằm **sát cạnh nhau** — kiểm tra khi đã đặt hết đồ | **8, 9, 10, 14, 15**                                                                 | Đúng: **"Pair! 💞"**. Sai (đặt hết mà chưa sát nhau): đồ chứa rung + **"Pairs must touch – move the [món]s together"** |
| Đồ nặng (heavy)      | Icon ⚓, phải chạm hàng ô **dưới cùng**                                   | **12, 14, 15**                                                                       | **"⚓ Heavy items must touch the bottom"**                                                                             |
| Đứng thẳng (upright) | Icon ⬆️ (chai lọ), không xoay được                                        | **13, 14, 15**                                                                       | Món lắc + **"⬆️ Bottles stay upright – no turning"**                                                                   |

## Booster

- **Gợi ý (Hint):** xoay 1 món chưa đặt về đúng hướng, trả về khay các món đang chiếm chỗ
  của nó (người chơi vẫn tự kéo vào). **Không giới hạn số lần dùng.** Dùng Gợi ý ⇒ màn đó
  tối đa **2 sao**. Hiệu ứng: ô đích sáng ~2 giây, món trong khay được đánh dấu. Nút: **"💡 Hint"**.

## Progression & Meta

- 15 màn chơi **tuần tự**, có **bảng chọn lại màn** (level select).
- Màn hình chính hiện **tổng số sao** và **số màn đã qua**.
- Lưới từ **4x3 đến 7x7 ô**, **3–13 món mỗi màn**.
- Màn 1 có **bàn tay hướng dẫn** kéo thả (tutorial hand).

## Features (build order — `/create-stories` đọc mục này)

Tất cả lấy từ các mục phía trên, không thêm luật mới. `[ ]` = chưa dựng.

- [ ] 1. Lõi xếp đồ: lưới, khay, món nhiều ô, kéo thả có bóng xanh/đỏ, thả sai về khay + rung, chạm xoay 90°, chạm món đã đặt để trả về khay, thanh % lấp đầy, kiểm tra thắng, đóng nắp
- [ ] 2. Dữ liệu 15 màn dạng data-driven (lưới 4x3–7x7, 3–13 món, cờ luật theo màn)
- [ ] 3. Luật vật cản: ô chặn (3, 9, 13), khu vực (7, 9, 10, 15), cặp đồ (8, 9, 10, 14, 15), đồ nặng (12, 14, 15), đứng thẳng (13, 14, 15), text vi phạm nguyên văn
- [ ] 4. Khuôn gợi ý (màn 6, 8, 9; KHÔNG có ở màn 10 và 15)
- [ ] 5. Hint "💡 Hint" + luật sao (3 sao nếu không dùng Hint, tối đa 2 sao nếu có dùng)
- [ ] 6. Chế độ Hẹn giờ ở màn 5, 10, 15 (30 / 45 / 75 giây) + màn thua
- [ ] 7. Meta: lưu tiến độ và sao, chọn level, màn hình chính hiện tổng sao + số màn đã qua
- [ ] 8. Tutorial hand màn 1, banner giới thiệu luật, chữ khen, confetti, haptic
- [ ] 9. Art & audio: Fredoka, hoa văn món đồ, màu khung theo màn, SFX, nhạc

**Build order:** 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9.

## Quyết định đã chốt (12/11/2026)

Nguồn: bản chơi thử https://claude.ai/artifact/PskwEekJTZ85nAVkZywy4j + quyết định của team.
Khi bản chơi thử khác brief, **brief thắng**.

- **Dữ liệu 15 màn:** `Assets/Data/Levels/levels.json`, trích từ bản chơi thử. Mỗi màn là
  một lưới chữ cái: mỗi chữ là một món, vị trí chữ chính là **lời giải tham chiếu**, `#` là ô
  chặn. Đã kiểm tra khớp bảng luật ở trên (ô chặn, khuôn, khu, cặp, nặng, đứng thẳng, timer).
- **Hẹn giờ là bắt buộc** ở màn 5, 10, 15 (30 / 45 / 75 giây). Không có công tắc chế độ
  Thư giãn / Hẹn giờ. Các màn khác không có đồng hồ và không thể thua.
- **Mở khóa tuần tự** (bản chơi thử mở sẵn mọi màn, bản game thì không).
- **Đồ nặng:** kiểm tra **ngay khi thả**. Thả không chạm đáy thì món về khay + toast.
- **Khu vực:** kiểm tra **ngay khi thả**. Thả sai khu thì món về khay + toast đúng câu GDD.
- **Cặp đồ:** "sát cạnh" = **chung một cạnh ô** (chạm góc không tính). Kiểm tra khi đã đặt hết.
- **Khuôn gợi ý:** chỉ gợi ý, **không ép** vị trí.
- **Hint:** chọn món **chưa đặt đầu tiên** theo thứ tự dữ liệu. Món đứng thẳng vốn đã đúng
  hướng nên Hint chỉ dọn chỗ và làm sáng ô đích.
- **Banner luật mới** (lấy từ bản chơi thử, vì GDD chỉ có câu của khuôn và màn khó): xem
  `bannerTitle` / `bannerSubtitle` trong file dữ liệu. Màn có timer chỉ hiện banner
  **"Hard level! Pack before the taxi leaves"**.
- **Nút "Install to keep packing":** giữ đúng chữ GDD, **không gắn link** (bản nộp chấm
  core gameplay, không phải bản live).
- **Package name Android:** `com.sonat.packitperfect`. Keystore release chưa cần.

## Art & Audio (verbatim)

- 2D cartoon phẳng, **viền đậm màu tối**, font chữ bo tròn **(Fredoka)**.
- Mỗi món đồ: màu + hoa văn riêng (**sọc, chấm, ca rô, len đan, sọc chéo**) + icon đồ vật.
- Mỗi màn đổi **màu khung đồ chứa**; vali có **tay cầm và bánh xe**.
- VFX: món nảy khi đặt khớp, chữ khen, **pháo giấy (confetti)** khi xếp xong màn.
- SFX: nhấc, đặt, xoay, đặt sai, **tiếng kéo khóa**, nhạc chiến thắng, **tiếng tích tắc khi
  còn 5 giây** (chế độ Hẹn giờ). **Rung nhẹ (haptic) khi đặt đồ đúng.**

## USP (điểm khác biệt, theo GDD)

- Cảm giác ASMR khi xếp đồ (nảy + chữ khen + nắp đóng + tiếng khóa).
- Bối cảnh đời thường đổi theo từng màn: vali, túi đi biển, hộp bento, kệ tủ lạnh, cốp xe,
  tủ đồ, thùng chuyển nhà (GDD liệt kê ví dụ — bản build hiện có đủ **15 bối cảnh riêng**
  cho 15 màn, nhiều hơn danh sách ví dụ này, không mâu thuẫn).
- Luật xếp đồ dựa trên logic đời thật: đồ nặng ở đáy, chai lọ đứng thẳng, đồ ăn tách đồ
  dùng, đôi tất nằm cạnh nhau.

---

## ⚠️ Khoảng trống trong GDD — cần bổ sung trước khi nộp (chưa có số liệu, đừng tự bịa)

Các ô sau trong sheet `00_Overview_Pack_It` đang **để trống**, Claude Code không nên tự suy
đoán số liệu cho các mục này:

- **Nền tảng, Đối tượng (TA), Định hướng MO** (phần TỔNG QUAN)
- **MONETIZATION** (toàn bộ phần kiếm tiền — soft currency, ads — chưa định nghĩa cho Pack It,
  khác với sheet Hook_line đã có đầy đủ Coin/Ads)
- **GAME THAM CHIẾU & RỦI RO CHÍNH** — trống hoàn toàn (sheet Hook_line có ghi "Water Sort,
  Bubble Fish" nhưng Pack It chưa ghi gì). **Nên bổ sung "Satiszone: Perfect ASMR Tidy"**
  làm game tham chiếu (đã thảo luận trước đó: Pack It kế thừa tệp người chơi/cảm giác ASMR
  từ Satiszone, core khác là xếp khít theo hình dạng thay vì "tidy"). Rủi ro cần ghi: có thể
  bị giám khảo coi là gần core cũ nếu không giải thích rõ — nên hỏi BTC qua sheet QnA.
- **KPI MỤC TIÊU**: D1/D7/D30 Retention, Engagement Time, IMP/DAU, IAP — toàn bộ còn trống.
- Tab **Target Audience_Pack_It**: nội dung đang **giống hệt** tab `Target Audience_Hook_line`
  (cùng điểm Bartle/Quantic) — dấu hiệu đây là bản copy template chưa được điền riêng cho
  Pack It. Không dùng các điểm số đó làm căn cứ thiết kế cho tới khi được điền lại.

## Deadline liên quan (từ 02_Plan, sprint SP2 — team Pack It)

- **Setup project Unity** (package name, keystore, chạy được trên device) — việc đang làm
  dở hiện tại, thuộc Pre-prod.
- **"final (release, signed)" — APK final, code freeze: hạn 15/11.** Bản build hiện tại
  dùng Scripting Backend **Mono** để test nhanh — nhớ **chuyển sang IL2CPP + ký keystore
  release thật** trước khi đóng gói bản nộp cuối, vì Mono + debug keystore chỉ dùng để test.
- Smoke test final (cài đặt + chơi hết level) cũng hạn **15/11**.

## Ghi chú cho Claude Code

Toàn bộ số màn, text UI (toast, banner, màn thua) trong file này là **nguyên văn từ GDD
chính thức** — khi viết C#, dùng đúng các chuỗi text này (kể cả icon ⚓ ⬆️ 💞 💡), không
diễn giải lại hay rút gọn, vì đây là tài liệu sẽ được đối chiếu khi chấm điểm tài liệu
(30% điểm theo quy định "Thuyết trình 20% – Tài liệu 30% – Kết quả UA 50%").

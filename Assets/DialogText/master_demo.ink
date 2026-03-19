// // ======== master_demo.ink ========
// //INCLUDE globals.ink

// // 宣告外部函式（由 Unity/C# 綁定）
// EXTERNAL PlaySfx(name)
// EXTERNAL PlayEmote(name)

// // ===== 基本結構：Knot / Stitch / 轉跳 =====
// #speaker:Narrator #layout:center
// === start ===                       // Knot：章
// = intro                             // Stitch：節
// 歡迎來到 Ink 語法全集示例，{player_name}！
// -> how_to_play

// = how_to_play
// 本示例涵蓋：變數、條件、選項、黏合、函式、隧道、標籤等。
// -> main_menu

// === main_menu ===
// #speaker:System
// 請選擇主題：
// + [變數與條件] -> vars_and_conditions
// + [選項與編織] -> choices_and_weave
// + [黏合 / 富文本] -> glue_and_richtext
// + [函式（內/外）] -> functions_demo
// + [隧道（Tunnels）] -> tunnel_demo
// + [標籤（Tags）與 UI] -> tags_for_unity
// + [跨節共享（Include / Globals）] -> cross_file_demo
// * [結束] -> END

// // ===== 變數、條件、運算、內嵌條件 =====
// === vars_and_conditions ===
// #speaker:Professor
// ~ gold += 5
// 你獲得 5 金幣，現有 {gold}。

// { gold >= 10:
//     足夠買藥水了。
// - else:
//     還差一點錢。
// }

// ~ has_key = true
// { has_key:
//     你撿到鑰匙。
// }

// ~ mood = "happy"
// 現在心情：{mood}。 { mood == "happy": 開心地哼著歌。 }

// ~ temp hp = MAX_HP               // temp：區域變數
// 你的 HP = {hp}（這個變數出此段就失效）。
// -> main_menu

// // ===== 選項與編織（一次性/黏性/彙流） =====
// === choices_and_weave ===
// #speaker:Guide
// 今天要去哪裡？
// * [去市集]
//     市集人聲鼎沸。
//     - 市集中你遇到舊識。
// + [回旅店]
//     旅店很安靜。（黏性：可重複顯示直到你離開本段）
// - 故事繼續往前走……
// -> choices_detail

// === choices_detail ===
// 你還想做什麼？
// * [買藥水]
//     ~ gold -= 5
//     { gold < 0:
//         （其實你不夠錢，被店家趕走，金幣重置。）
//         ~ gold = 0
//     - else:
//         （買到了藥水。）
//     }
// + [打聽消息]
//     你打聽到某地有秘寶。
// - 收尾後回到主選單。
// -> main_menu

// // ===== 黏合（Glue）與富文本（Rich Text） =====
// === glue_and_richtext ===
// #speaker:NPC1 #portrait:default #layout:left #audio:default
// 這些文字<>
// <b>會被黏在一起</b>。
// 下一句則正常分段。

// 帶富文本：<b><i><color=#FF1E35>重要訊息</color></i></b>。
// 此外，也可以 {"插值"} 變數：{player_name}。

// // 替代/序列/隨機：
// 今天的天氣 {晴朗|多雲|小雨}。       // 序列，循環取值
// 幸運色是 {~紅|藍|綠}。              // ~ 隨機
// -> main_menu

// // ===== 函式（內部）與 EXTERNAL（外部） =====
// === functions_demo ===
// #speaker:Professor
// ~ temp a = 4
// ~ temp b = 7
// a={a}, b={b}，平方和 = {sum_of_squares(a,b)}。

// ~ PlaySfx("ui_click")             // 外部函式（C# 綁定）
// ~ PlayEmote("pangolin-walk")

// -> main_menu

// === function sum_of_squares(x,y) ===
// ~ return x*x + y*y

// // ===== 隧道（Tunnels）：可重用段落，返回呼叫點 =====
// === tunnel_demo ===
// 這裡進入商店隧道……
// -> shop ->                     // 進入隧道（會在結束時自動回來）
// 回來了，繼續冒險。
// -> main_menu

// === shop ===
// #speaker:Shopkeeper
// 歡迎光臨！要買點什麼？
// + [藥水 5 金幣] 
//     { gold >= 5:
//         ~ gold -= 5
//         買到了藥水。你還剩 {gold}。
//     - else:
//         錢不夠啊……
//     }
// + [地圖 1 金幣]
//     { gold >= 1:
//         ~ gold -= 1
//         得到地圖。你還剩 {gold}。
//     - else:
//         錢不夠……
//     }
// - 逛完了，離開商店。
// ->->                               // 從隧道返回呼叫點

// // ===== 標籤（Tags）與 UI 互動（範例） =====
// === tags_for_unity ===
// #speaker:NPC2
// #portrait:surprised
// #layout:right
// #audio:soft
// 這段加了多個標籤，Unity 端可解析對應（名字/頭像/版面/音訊設定）。
// -> main_menu

// // ===== 跨檔/跨段（Include / Globals / 共享變數） =====
// === cross_file_demo ===
// #speaker:System
// 請選擇一個數字，將寫入 chooseNumber 並跨段使用。
// + [選 1] -> chosen_number("1")
// + [選 2] -> chosen_number("2")
// + [回主選單] -> main_menu

// === chosen_number(n) ===
// ~ chooseNumber = n
// 已選擇：{chooseNumber}。
// （切換到「另一段故事」時，只要也 INCLUDE 同一份 globals，就能讀到：{chooseNumber}）
// -> cross_file_followup

// === cross_file_followup ===
// { chooseNumber == "": 尚未選擇任何數字。 | 你選擇的是 {chooseNumber}。 }
// -> main_menu

// // ===== 補充：條件顯示選項 / 訪問次數 =====
// //=== extras ===
// //你來過這裡 {visited(extras): {visit_count(extras)} 次 | 第一次}。
// //* { not visited(hint) } [看提示] -> hint
// //+ [返回主選單] -> main_menu

// //=== hint ===
// //這裡是提示。看過後此選項就不再顯示（因為 visited(hint) 成立）。
// //-> extras
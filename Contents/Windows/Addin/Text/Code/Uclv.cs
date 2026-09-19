using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CADAddin.Common;                    // ← THÊM để gọi Utils
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.TextTools
{
    public class Doichuinhoa
    {
        [CommandMethod("Doichuinhoa")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            // ═══════════════════════════════════════════════════════
            //  Menu chọn kiểu chuyển đổi
            // ═══════════════════════════════════════════════════════
            ed.WriteMessage("\nChọn kiểu chuyển đổi:");
            ed.WriteMessage("\n 1. Hoa           (UPPERCASE)");
            ed.WriteMessage("\n 2. Thường        (lowercase)");
            ed.WriteMessage("\n 3. Hoa đầu dòng  (In hoa chữ cái đầu MỖI DÒNG)");
            ed.WriteMessage("\n 4. Hoa đầu từ    (In hoa chữ cái đầu MỖI TỪ)");
            ed.WriteMessage("\n 5. Hoa đầu tiên  (Chỉ chữ cái đầu tiên, còn lại thường)");

            var kwo = new PromptKeywordOptions(
                "\nNhập lựa chọn [Hoa/Thuong/HoaDauDong/HoaDauTu/HoaDauTien] <Hoa>: ")
            { AllowNone = true };
            kwo.Keywords.Add("Hoa");
            kwo.Keywords.Add("Thuong");
            kwo.Keywords.Add("HoaDauDong");
            kwo.Keywords.Add("HoaDauTu");
            kwo.Keywords.Add("HoaDauTien");
            var kwr = ed.GetKeywords(kwo);

            string opt = (kwr.Status == PromptStatus.OK && !string.IsNullOrEmpty(kwr.StringResult))
                ? kwr.StringResult
                : "Hoa";

            // ═══════════════════════════════════════════════════════
            //  Chọn đối tượng Text / MText
            // ═══════════════════════════════════════════════════════
            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "TEXT,MTEXT") });

            var sel = ed.GetSelection(filter);
            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("Không có đối tượng nào được chọn.");
                return;
            }

            // ═══════════════════════════════════════════════════════
            //  Chuyển đổi
            // ═══════════════════════════════════════════════════════
            int count = 0;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var ent = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Entity;
                    if (ent == null) continue;

                    string oldTxt = null;
                    if (ent is MText mt) oldTxt = mt.Contents;
                    else if (ent is DBText txt) oldTxt = txt.TextString;
                    if (string.IsNullOrEmpty(oldTxt)) continue;

                    string newTxt;
                    switch (opt)
                    {
                        case "Thuong": newTxt = CaseConvert(oldTxt, true); break;
                        case "HoaDauDong": newTxt = SentenceCase(oldTxt); break;
                        case "HoaDauTu": newTxt = TitleCase(oldTxt); break;
                        case "HoaDauTien": newTxt = FirstUpperRestLower(oldTxt); break;
                        default: newTxt = CaseConvert(oldTxt, false); break; // Hoa
                    }

                    if (newTxt != oldTxt)
                    {
                        if (ent is MText mt2) mt2.Contents = newTxt;
                        else if (ent is DBText txt2) txt2.TextString = newTxt;
                        count++;
                    }
                }
                tr.Commit();
            }

            ed.Regen();
            Utils.Print($"Hoàn tất – đã xử lý {count} đối tượng.");
        }

        // ═══════════════════════════════════════════════════════════
        //  1 & 2) UPPER / lower — giữ nguyên MTEXT format codes
        // ═══════════════════════════════════════════════════════════
        private static string CaseConvert(string s, bool toLower)
        {
            var sb = new StringBuilder(s.Length);
            int i = 0;
            while (i < s.Length)
            {
                char ch = s[i];

                // Escape sequence
                if (ch == '\\' && i + 1 < s.Length)
                {
                    int consumed;
                    AppendEscape(s, i, sb, out consumed);
                    i += consumed;
                    continue;
                }

                sb.Append(toLower ? char.ToLowerInvariant(ch) : char.ToUpperInvariant(ch));
                i++;
            }
            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════
        //  3) Hoa đầu dòng (Sentence case)
        //     - Chữ cái đầu tiên của chuỗi HOẶC đầu mỗi dòng (\P, \n, \r)
        //       → in hoa
        //     - Các chữ cái còn lại → in thường
        // ═══════════════════════════════════════════════════════════
        private static string SentenceCase(string s)
        {
            var sb = new StringBuilder(s.Length);
            bool startOfLine = true;
            int i = 0;

            while (i < s.Length)
            {
                char ch = s[i];

                // ── Escape sequence ──
                if (ch == '\\' && i + 1 < s.Length)
                {
                    char next = s[i + 1];

                    // \P = paragraph break → xuống dòng
                    if (next == 'P' || next == 'p')
                    {
                        sb.Append(s, i, 2);
                        i += 2;
                        startOfLine = true;
                        continue;
                    }

                    int consumed;
                    AppendEscape(s, i, sb, out consumed);
                    i += consumed;
                    continue;
                }

                // ── Ký tự chữ cái ──
                if (char.IsLetter(ch))
                {
                    sb.Append(startOfLine
                        ? char.ToUpperInvariant(ch)
                        : char.ToLowerInvariant(ch));
                    startOfLine = false;
                }
                else
                {
                    // Xuống dòng thật (\n) cũng tính là đầu dòng mới
                    if (ch == '\n' || ch == '\r')
                        startOfLine = true;

                    sb.Append(ch);
                }

                i++;
            }

            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════
        //  4) Hoa đầu từ (Title Case)
        //     - Chữ cái đầu tiên của mỗi từ → in hoa
        //     - Chữ cái còn lại → in thường
        //     - Từ phân tách bởi ký tự KHÔNG phải chữ cái (space, dấu câu,
        //       \P, tab, ...)
        // ═══════════════════════════════════════════════════════════
        private static string TitleCase(string s)
        {
            var sb = new StringBuilder(s.Length);
            bool startOfWord = true;
            int i = 0;

            while (i < s.Length)
            {
                char ch = s[i];

                // ── Escape sequence ──
                if (ch == '\\' && i + 1 < s.Length)
                {
                    char next = s[i + 1];

                    if (next == 'P' || next == 'p')
                    {
                        sb.Append(s, i, 2);
                        i += 2;
                        startOfWord = true;    // sau \P → từ mới
                        continue;
                    }

                    int consumed;
                    AppendEscape(s, i, sb, out consumed);
                    i += consumed;

                    // \~ (non-breaking space) — coi như ranh giới từ
                    startOfWord = true;
                    continue;
                }

                // ── Ký tự chữ cái ──
                if (char.IsLetter(ch))
                {
                    sb.Append(startOfWord
                        ? char.ToUpperInvariant(ch)
                        : char.ToLowerInvariant(ch));
                    startOfWord = false;
                }
                else
                {
                    // Bất kỳ ký tự nào không phải chữ cái → từ mới
                    startOfWord = true;
                    sb.Append(ch);
                }

                i++;
            }

            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════
        //  5) Hoa đầu tiên (First Upper, Rest Lower)
        //     - CHỈ chữ cái đầu tiên của TOÀN chuỗi → in hoa
        //     - Mọi chữ cái còn lại (kể cả sau \P, \n) → in thường
        //     - Escape & format code được giữ nguyên, KHÔNG tính là chữ cái
        // ═══════════════════════════════════════════════════════════
        private static string FirstUpperRestLower(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            var sb = new StringBuilder(s.Length);
            bool firstLetterDone = false;
            int i = 0;

            while (i < s.Length)
            {
                char ch = s[i];

                // ── 1. Escape sequence ──
                if (ch == '\\' && i + 1 < s.Length)
                {
                    int consumed;
                    AppendEscape(s, i, sb, out consumed);
                    i += consumed;
                    continue;   // KHÔNG đụng tới firstLetterDone
                }

                // ── 2. Ký tự chữ cái ──
                if (char.IsLetter(ch))
                {
                    if (!firstLetterDone)
                    {
                        sb.Append(char.ToUpperInvariant(ch));
                        firstLetterDone = true;
                    }
                    else
                    {
                        sb.Append(char.ToLowerInvariant(ch));
                    }
                }
                // ── 3. Ký tự khác (số, dấu câu, space, {, }) ──
                else
                {
                    sb.Append(ch);
                }

                i++;
            }

            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════
        //  Hàm phụ: copy escape sequence và trả về số ký tự đã tiêu thụ
        //    - \P  \p         → 2 ký tự
        //    - \\  \{  \}  \~ → 2 ký tự
        //    - \A...; \H...; \f...; ... → tới dấu ; (bao gồm ;)
        // ═══════════════════════════════════════════════════════════
        private static void AppendEscape(string s, int i, StringBuilder sb, out int consumed)
        {
            char next = s[i + 1];

            // \P \p
            if (next == 'P' || next == 'p')
            {
                sb.Append(s, i, 2);
                consumed = 2;
                return;
            }

            // \\ \{ \} \~
            if (next == '\\' || next == '{' || next == '}' || next == '~')
            {
                sb.Append(s, i, 2);
                consumed = 2;
                return;
            }

            // Code khác: copy tới dấu ; (bao gồm cả ;)
            int j = i + 1;
            while (j < s.Length && s[j] != ';') j++;
            if (j < s.Length) j++;   // bỏ qua ;
            sb.Append(s, i, j - i);
            consumed = j - i;
        }
    }
}
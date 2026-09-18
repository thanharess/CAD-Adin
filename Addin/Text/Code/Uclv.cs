using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class Doichuinhoa
    {
        [CommandMethod("Doichuinhoa")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            ed.WriteMessage("\nChọn kiểu chuyển đổi:");
            ed.WriteMessage("\n 1. Hoa");
            ed.WriteMessage("\n 2. Thường");

            var kwo = new PromptKeywordOptions("\nNhập lựa chọn [Hoa/Thuong] <Hoa>: ")
            { AllowNone = true };
            kwo.Keywords.Add("Hoa");
            kwo.Keywords.Add("Thuong");
            var kwr = ed.GetKeywords(kwo);
            string opt = (kwr.Status == PromptStatus.OK && !string.IsNullOrEmpty(kwr.StringResult))
                ? kwr.StringResult : "Hoa";

            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "TEXT,MTEXT") });
            var sel = ed.GetSelection(filter);
            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("Không có đối tượng nào được chọn.");
                return;
            }

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

                    string newTxt = (opt == "Hoa")
                        ? CaseConvert(oldTxt, true)
                        : CaseConvert(oldTxt, false);

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
        // Case conversion giữ nguyên MTEXT format codes
        // ═══════════════════════════════════════════════════════════
        private static string CaseConvert(string s, bool toUpper)
        {
            var sb = new StringBuilder(s.Length);
            int i = 0;
            while (i < s.Length)
            {
                char ch = s[i];

                // Xử lý escape \...
                if (ch == '\\' && i + 1 < s.Length)
                {
                    // \P → copy nguyên
                    if (s[i + 1] == 'P' || s[i + 1] == 'p')
                    {
                        sb.Append(s, i, 2);
                        i += 2;
                        continue;
                    }
                    // \\ \{ \} \~ → copy nguyên 2 ký tự
                    if (s[i + 1] == '\\' || s[i + 1] == '{' ||
                        s[i + 1] == '}' || s[i + 1] == '~')
                    {
                        sb.Append(s, i, 2);
                        i += 2;
                        continue;
                    }
                    // Các code khác: copy tới dấu ; (bao gồm cả ;)
                    int j = i + 1;
                    while (j < s.Length && s[j] != ';') j++;
                    if (j < s.Length) j++;   // bỏ qua ;
                    sb.Append(s, i, j - i);
                    i = j;
                    continue;
                }

                // Ký tự thường → convert
                sb.Append(toUpper ? char.ToUpperInvariant(ch) : char.ToLowerInvariant(ch));
                i++;
            }
            return sb.ToString();
        }
    }
}
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using CADAddin.Common;                    // ← THÊM để gọi Utils
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.TextTools
{
    public class FontChangeAllTimeNewRoman
    {
        private const string TargetFont = "Times New Roman";

        [CommandMethod("FontChangeAllTimeNewRoman")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            Utils.Print("Đang ép font Times New Roman cho tất cả style + text...");

            int styleCount = 0;
            int mtextFixed = 0;
            int dbtextFound = 0;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                // ═══════════════════════════════════════════════════
                //  1) Ép tất cả TEXT STYLE sang Times New Roman
                // ═══════════════════════════════════════════════════
                var st = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                foreach (ObjectId id in st)
                {
                    var rec = tr.GetObject(id, OpenMode.ForWrite) as TextStyleTableRecord;
                    if (rec == null) continue;

                    try
                    {
                        var fd = new FontDescriptor(TargetFont, false, false, 0, 34);
                        rec.Font = fd;
                        styleCount++;
                    }
                    catch { }
                }

                // ═══════════════════════════════════════════════════
                //  2) Quét CHỈ ModelSpace + PaperSpace
                //     → Strip inline \f...; trong MTEXT
                //     → Đếm DBText
                // ═══════════════════════════════════════════════════
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                foreach (ObjectId btrId in bt)
                {
                    var btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
                    if (btr == null) continue;

                    // ⚠️ BỎ QUA tất cả block definition — chỉ xử lý layout
                    if (!btr.IsLayout) continue;

                    foreach (ObjectId entId in btr)
                    {
                        var ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        // ── MTEXT: strip inline font codes ──
                        if (ent is MText mtext)
                        {
                            string oldC = mtext.Contents;
                            if (string.IsNullOrEmpty(oldC)) continue;

                            string newC = StripMTextFontCodes(oldC);
                            if (newC != oldC)
                            {
                                mtext.UpgradeOpen();
                                mtext.Contents = newC;
                                mtextFixed++;
                            }
                        }
                        // ── DBText: chỉ đếm ──
                        else if (ent is DBText)
                        {
                            dbtextFound++;
                        }
                    }
                }

                tr.Commit();
            }

            ed.Regen();
            Utils.Print($"✓ Hoàn thành:");
            Utils.Print($"   • Style đã đổi font: {styleCount}");
            Utils.Print($"   • MTEXT có inline font bị xử lý: {mtextFixed}");
            Utils.Print($"   • DBText tìm thấy (theo style mới): {dbtextFound}");
        }

        // ═══════════════════════════════════════════════════════════
        //  Xóa inline font code trong MTEXT
        //    \fFontName|b0|i0|c0|p34;  → xóa
        //    \F...;                     → xóa
        //  Giữ nguyên mọi escape khác (\P, \~, \A1;, \H2.5x;, \C1;, ...)
        // ═══════════════════════════════════════════════════════════
        private static string StripMTextFontCodes(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            var sb = new StringBuilder(s.Length);
            int i = 0;

            while (i < s.Length)
            {
                char ch = s[i];

                // ── Escape sequence ──
                if (ch == '\\' && i + 1 < s.Length)
                {
                    char next = s[i + 1];

                    // \f hoặc \F: font code → skip tới ;
                    if (next == 'f' || next == 'F')
                    {
                        int j = i + 2;
                        while (j < s.Length && s[j] != ';') j++;
                        if (j < s.Length) j++;   // bỏ dấu ;
                        i = j;
                        continue;
                    }

                    // Escape khác: copy '\' rồi để vòng lặp sau copy ký tự tiếp theo
                    sb.Append(ch);
                    i++;
                    continue;
                }

                sb.Append(ch);
                i++;
            }

            return sb.ToString();
        }
    }
}
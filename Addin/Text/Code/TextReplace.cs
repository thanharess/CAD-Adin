using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class TextReplace
    {
        [CommandMethod("TextReplace")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            Utils.Print("=== LỆNH: TEXTREPLACE - Thay nội dung theo mẫu chọn ===");

            var peo = new PromptEntityOptions("\nChọn 1 TEXT/MTEXT/LEADER/MLEADER làm mẫu: ");
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
            {
                Utils.Print("⚠️ Không chọn được đối tượng mẫu.");
                return;
            }

            string srcStr = null;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = (Entity)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                srcStr = ExtractText(ent, tr);
                tr.Commit();
            }

            if (srcStr == null)
            {
                Utils.Print("⚠️ Loại đối tượng không được hỗ trợ.");
                return;
            }
            Utils.Print($"→ Nội dung mẫu: \"{srcStr}\"");

            ed.WriteMessage("\nChọn các TEXT / MTEXT / ATTRIBUTE / LEADER / MLEADER cần thay: ");
            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "TEXT,MTEXT,ATTRIB,LEADER,MULTILEADER") });
            var sel = ed.GetSelection(filter);

            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("⚠️ Không có đối tượng được chọn.");
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

                    if (ApplyText(ent, tr, srcStr))
                        count++;
                }
                tr.Commit();
            }

            ed.Regen();
            Utils.Print($"✅ Đã thay {count} đối tượng bằng nội dung: \"{srcStr}\"");
        }

        // ═══════════════════════════════════════════════════════════
        // ĐỌC VĂN BẢN TỪ ENTITY
        // ═══════════════════════════════════════════════════════════
        private static string ExtractText(Entity ent, Transaction tr)
        {
            // TEXT
            if (ent is DBText txt) return txt.TextString;

            // MTEXT
            if (ent is MText mt) return mt.Contents;

            // ATTRIBUTE (block attribute reference)
            if (ent is AttributeReference attr) return attr.TextString;

            // LEADER cổ điển — trỏ tới Annotation (DBText/MText/Tolerance)
            if (ent is Leader ld)
            {
                var annId = ld.Annotation;
                if (!annId.IsNull)
                {
                    var ann = tr.GetObject(annId, OpenMode.ForRead) as Entity;
                    if (ann is DBText t) return t.TextString;
                    if (ann is MText m) return m.Contents;
                }
                return "";
            }

            // ✅ MULTILEADER — dùng thuộc tính MText, KHÔNG dùng TextString
            if (ent is MLeader ml)
            {
                if (ml.ContentType == ContentType.MTextContent)
                {
                    MText innerMt = ml.MText;
                    if (innerMt != null) return innerMt.Contents ?? "";
                }
                return "";
            }

            return null;   // Không hỗ trợ
        }

        // ═══════════════════════════════════════════════════════════
        // GHI VĂN BẢN VÀO ENTITY
        // ═══════════════════════════════════════════════════════════
        private static bool ApplyText(Entity ent, Transaction tr, string text)
        {
            // TEXT
            if (ent is DBText txt) { txt.TextString = text; return true; }

            // MTEXT
            if (ent is MText mt) { mt.Contents = text; return true; }

            // ATTRIBUTE
            if (ent is AttributeReference attr) { attr.TextString = text; return true; }

            // LEADER
            if (ent is Leader ld)
            {
                var annId = ld.Annotation;
                if (!annId.IsNull)
                {
                    var ann = tr.GetObject(annId, OpenMode.ForWrite) as Entity;
                    if (ann is DBText t) { t.TextString = text; return true; }
                    if (ann is MText m) { m.Contents = text; return true; }
                }
                return false;
            }

            // ✅ MULTILEADER — dùng MText, KHÔNG dùng TextString
            if (ent is MLeader ml)
            {
                if (ml.ContentType == ContentType.MTextContent)
                {
                    MText innerMt = ml.MText;
                    if (innerMt != null)
                    {
                        innerMt.Contents = text;
                        ml.MText = innerMt;   // gán lại để cập nhật
                        return true;
                    }
                }
                return false;
            }

            return false;
        }
    }
}
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CADAddin.Common;                    // ← THÊM để gọi Utils
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.TextTools
{
    public class ThemChuVaoText
    {
        [CommandMethod("ThemChuVaoText")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "TEXT,MTEXT,MULTILEADER") });
            var sel = ed.GetSelection(filter);
            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("Không có đối tượng nào được chọn.");
                return;
            }

            ed.WriteMessage("\n=== Nhập nội dung cần THÊM ===");
            ed.WriteMessage("\n- Gõ từng dòng rồi Enter");
            ed.WriteMessage("\n- Nhập dòng trống (chỉ Enter) để KẾT THÚC\n");

            var allTxt = new StringBuilder();
            while (true)
            {
                var pso = new PromptStringOptions("\nDòng text: ") { AllowSpaces = true };
                var psr = ed.GetString(pso);
                if (psr.Status != PromptStatus.OK) break;
                string line = psr.StringResult;
                if (string.IsNullOrEmpty(line)) break;

                if (allTxt.Length > 0) allTxt.Append("\\P");
                allTxt.Append(line);
            }

            Utils.Print("Đã kết thúc nhập nội dung.");
            string addTxt = allTxt.ToString();
            if (string.IsNullOrEmpty(addTxt))
            {
                Utils.Print("Không có nội dung để thêm.");
                return;
            }

            int count = 0;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    // ── TEXT → chuyển thành MTEXT ──
                    if (ent is DBText txt)
                    {
                        string oldStr = txt.TextString ?? "";
                        var insPt = txt.Position;
                        double h = txt.Height;
                        string layer = txt.Layer;
                        ObjectId styleId = txt.TextStyleId;

                        var mt = new MText
                        {
                            Location = insPt,
                            TextHeight = h,
                            Layer = layer,
                            TextStyleId = styleId,
                            Contents = oldStr + "\\P" + addTxt
                        };
                        ms.AppendEntity(mt);
                        tr.AddNewlyCreatedDBObject(mt, true);

                        txt.UpgradeOpen();
                        txt.Erase();
                        count++;
                    }
                    // ── MTEXT ──
                    else if (ent is MText mt2)
                    {
                        mt2.UpgradeOpen();
                        string old = mt2.Contents ?? "";
                        mt2.Contents = old + "\\P" + addTxt;
                        count++;
                    }
                    // ── ✅ MULTILEADER — dùng MText ──
                    else if (ent is MLeader ml)
                    {
                        if (ml.ContentType == ContentType.MTextContent)
                        {
                            ml.UpgradeOpen();
                            MText innerMt = ml.MText;
                            if (innerMt != null)
                            {
                                string old = innerMt.Contents ?? "";
                                innerMt.Contents = old + "\\P" + addTxt;
                                ml.MText = innerMt;
                                count++;
                            }
                        }
                    }
                }
                tr.Commit();
            }

            ed.Regen();
            Utils.Print($"Đã thêm nội dung vào {count} đối tượng.");
        }
    }
}
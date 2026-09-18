using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CADAddin.Common;                    // ← THÊM để gọi Utils
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.TextTools
{
    public class TextFillNone
    {
        [CommandMethod("TextFillNone")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "MTEXT") });
            var sel = ed.SelectAll(filter);
            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("Không tìm thấy đối tượng MTEXT nào.");
                return;
            }

            int count = 0;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var mt = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as MText;
                    if (mt == null) continue;

                    if (mt.BackgroundFill)
                    {
                        mt.BackgroundFill = false;
                        mt.UseBackgroundColor = false;
                        count++;
                    }
                }
                tr.Commit();
            }

            ed.Regen();
            Utils.Print($"Đã tắt fill color cho {count} MTEXT.");
        }
    }
}
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Dimtools
{
    public class ScaleDimValue
    {
        [CommandMethod("SCALEDIMVALUE")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            ed.WriteMessage("\nLệnh SCALEDIMVALUE - Scale Dim Linear factor.");

            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "DIMENSION") });
            var sel = ed.GetSelection(filter);
            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("Không có DIM nào được chọn.");
                return;
            }

            // Chọn mode
            var kwo = new PromptKeywordOptions("\nChọn chế độ [1:Tăng / 2:Giảm / 3:1:1] <1>: ");
            kwo.Keywords.Add("1"); kwo.Keywords.Add("2"); kwo.Keywords.Add("3");
            kwo.AllowNone = true;
            var kwr = ed.GetKeywords(kwo);
            string mode = (kwr.Status == PromptStatus.OK && !string.IsNullOrEmpty(kwr.StringResult))
                ? kwr.StringResult : "1";

            double factor = 1.0;
            if (mode == "1")
            {
                var pr = ed.GetDouble("\nNhập hệ số tăng (VD: 2 = nhân 2:1): ");
                factor = (pr.Status == PromptStatus.OK) ? pr.Value : 1.0;
            }
            else if (mode == "2")
            {
                var pr = ed.GetDouble("\nNhập hệ số giảm (VD: 5 = chia 1:5): ");
                factor = (pr.Status == PromptStatus.OK && pr.Value != 0) ? 1.0 / pr.Value : 1.0;
            }

            int count = 0;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var dim = tr.GetObject(so.ObjectId, OpenMode.ForWrite) as Dimension;
                    if (dim == null) continue;

                    if (mode == "3")
                    {
                        dim.Dimlfac = 1.0;
                    }
                    else
                    {
                        double old = dim.Dimlfac;
                        if (old == 0) old = 1.0;
                        dim.Dimlfac = old * factor;
                    }
                    count++;
                }
                tr.Commit();
            }

            if (mode == "3")
                Utils.Print($"Đã đặt {count} DIM về giá trị 1.0");
            else
                Utils.Print($"Đã scale {count} DIM, hệ số nhân: {factor:F4}");
        }
    }
}
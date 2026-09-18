using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class DeleteTextAndLeader
    {
        [CommandMethod("DeleteTextAndLeader")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            Utils.Print("LỆNH DELETETEXTANDLEADER - Xoá Text / Leader / Leader Note.");

            var kwo = new PromptKeywordOptions(
                "\nChọn loại cần xoá [1:Text / 2:Leader / 3:LeaderNote / 4:Tất cả] <1>: ")
            { AllowNone = true };
            kwo.Keywords.Add("1"); kwo.Keywords.Add("2");
            kwo.Keywords.Add("3"); kwo.Keywords.Add("4");
            var kwr = ed.GetKeywords(kwo);
            string choice = (kwr.Status == PromptStatus.OK && !string.IsNullOrEmpty(kwr.StringResult))
                ? kwr.StringResult : "1";

            // ✅ SỬA: thay switch expression bằng if-else
            string typeStr;
            if (choice == "1") typeStr = "TEXT,MTEXT";
            else if (choice == "2") typeStr = "LEADER";
            else if (choice == "3") typeStr = "MULTILEADER";
            else if (choice == "4") typeStr = "TEXT,MTEXT,LEADER,MULTILEADER";
            else typeStr = "TEXT,MTEXT";

            var kwo2 = new PromptKeywordOptions("\n[1:Xoá tất cả / 2:Chọn vùng] <1>: ")
            { AllowNone = true };
            kwo2.Keywords.Add("1"); kwo2.Keywords.Add("2");
            var kwr2 = ed.GetKeywords(kwo2);
            string mode = (kwr2.Status == PromptStatus.OK && !string.IsNullOrEmpty(kwr2.StringResult))
                ? kwr2.StringResult : "1";

            var pso = new PromptStringOptions("\nNhập tên Layer muốn xoá (Enter = tất cả): ")
            { AllowSpaces = true };
            var psr = ed.GetString(pso);
            string layerFilter = (psr.Status == PromptStatus.OK) ? psr.StringResult.Trim() : "";

            var filterList = new System.Collections.Generic.List<TypedValue>();
            filterList.Add(new TypedValue((int)DxfCode.Start, typeStr));
            if (!string.IsNullOrEmpty(layerFilter))
                filterList.Add(new TypedValue((int)DxfCode.LayerName, layerFilter));

            var filter = new SelectionFilter(filterList.ToArray());

            PromptSelectionResult sel;
            if (mode == "1")
                sel = ed.SelectAll(filter);
            else
                sel = ed.GetSelection(filter);

            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("Không tìm thấy đối tượng cần xoá.");
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
                    ent.Erase();
                    count++;
                }
                tr.Commit();
            }

            ed.Regen();
            Utils.Print($"→ Đã xoá {count} đối tượng.");
        }
    }
}
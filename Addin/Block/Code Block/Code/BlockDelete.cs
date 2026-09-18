using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.Block
{
    public class BlockDelete
    {
        [CommandMethod("BLDELETE")]
        public void DeleteByPickAndSelection() => DoDelete(false);

        [CommandMethod("BLERASE")]
        public void EraseAllSameName() => DoDelete(true);

        private void DoDelete(bool selectAll)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var peo = new PromptEntityOptions("\nChọn block mẫu: ");
            peo.SetRejectMessage("\nĐối tượng được chọn không phải block.");
            peo.AddAllowedClass(typeof(BlockReference), false);
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var br = (BlockReference)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                string name = br.Name;
                Utils.Print($"🔹 Block được chọn: {name}");

                PromptSelectionResult ss;
                if (selectAll)
                {
                    ss = ed.SelectAll(Utils.BlockNameFilter(name));
                }
                else
                {
                    ss = ed.GetSelection(Utils.BlockNameFilter(name));
                }

                if (ss.Status != PromptStatus.OK)
                {
                    Utils.Print("❌ Không tìm thấy block.");
                    return;
                }

                int cnt = 0;
                foreach (SelectedObject so in ss.Value)
                {
                    if (so == null) continue;
                    var r = (BlockReference)tr.GetObject(so.ObjectId, OpenMode.ForWrite);
                    r.Erase();
                    cnt++;
                }
                tr.Commit();
                Utils.Print($"🗑️ Đã xóa {cnt} block \"{name}\".");
            }
        }
    }
}
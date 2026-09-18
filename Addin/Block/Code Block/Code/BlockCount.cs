using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.Block
{
    public class BlockCount
    {
        [CommandMethod("BLCOUNT")]
        public void Count()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var peo = new PromptEntityOptions("\nChọn một block (INSERT): ");
            peo.SetRejectMessage("\nĐối tượng được chọn không phải block.");
            peo.AddAllowedClass(typeof(BlockReference), false);
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var br = (BlockReference)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                string name = br.Name;
                Utils.Print($"🔹 Block được chọn: {name}");

                var ss = ed.SelectAll(Utils.BlockNameFilter(name));
                int count = ss.Status == PromptStatus.OK ? ss.Value.Count : 0;
                Utils.Print($"📊 Block \"{name}\" xuất hiện: {count} lần.");
                tr.Commit();
            }
        }
    }
}
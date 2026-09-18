using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using CADAddin.Common;                    // ← THÊM để gọi Utils

namespace CADAddin.Block
{
    public class BlockRename
    {
        [CommandMethod("BLRENAME")]
        public void Rename()
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
                string oldName = br.Name;
                Utils.Print($"🔹 Block được chọn: {oldName}");

                var pso = new PromptStringOptions($"\nNhập tên mới cho block \"{oldName}\": ")
                { AllowSpaces = true };
                var psr = ed.GetString(pso);
                if (psr.Status != PromptStatus.OK) return;
                string newName = psr.StringResult.Trim();

                if (string.IsNullOrEmpty(newName))
                {
                    Utils.Print("❌ Tên mới không hợp lệ.");
                    return;
                }

                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (bt.Has(newName))
                {
                    Utils.Print($"❌ Block \"{newName}\" đã tồn tại.");
                    return;
                }

                var btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForWrite);
                btr.Name = newName;
                tr.Commit();
                Utils.Print($"✅ Đã đổi tên \"{oldName}\" → \"{newName}\".");
            }
        }
    }
}
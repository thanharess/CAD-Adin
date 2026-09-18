using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.Block
{
    public class BlockSwap
    {
        [CommandMethod("BLSWAP")]
        public void Swap()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var peo1 = new PromptEntityOptions("\nChọn block thứ nhất: ");
            peo1.SetRejectMessage("\nKhông phải block.");
            peo1.AddAllowedClass(typeof(BlockReference), false);
            var per1 = ed.GetEntity(peo1);
            if (per1.Status != PromptStatus.OK) return;

            var peo2 = new PromptEntityOptions("\nChọn block thứ hai: ");
            peo2.SetRejectMessage("\nKhông phải block.");
            peo2.AddAllowedClass(typeof(BlockReference), false);
            var per2 = ed.GetEntity(peo2);
            if (per2.Status != PromptStatus.OK) return;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var r1 = (BlockReference)tr.GetObject(per1.ObjectId, OpenMode.ForRead);
                var r2 = (BlockReference)tr.GetObject(per2.ObjectId, OpenMode.ForRead);

                string n1 = r1.Name, n2 = r2.Name;
                var p1 = r1.Position; var p2 = r2.Position;
                double rot1 = r1.Rotation, rot2 = r2.Rotation;
                var s1 = r1.ScaleFactors; var s2 = r2.ScaleFactors;
                var defId1 = r1.BlockTableRecord;
                var defId2 = r2.BlockTableRecord;

                var ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                var new1 = new BlockReference(p2, defId1) { Rotation = rot1, ScaleFactors = s1 };
                var new2 = new BlockReference(p1, defId2) { Rotation = rot2, ScaleFactors = s2 };
                ms.AppendEntity(new1); tr.AddNewlyCreatedDBObject(new1, true);
                ms.AppendEntity(new2); tr.AddNewlyCreatedDBObject(new2, true);

                r1.UpgradeOpen(); r1.Erase();
                r2.UpgradeOpen(); r2.Erase();

                tr.Commit();
                Utils.Print($"✅ Đã hoán đổi vị trí giữa \"{n1}\" và \"{n2}\".");
            }
        }
    }
}
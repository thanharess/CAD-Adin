using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class BlockBasePoint
    {
        [CommandMethod("BMBASEPOINT")]
        public void ChangeBasePoint() => DoChangeBasePoint();

        [CommandMethod("BLBASEPOINT")]
        public void ChangeBasePointLegacy() => DoChangeBasePoint();

        private void DoChangeBasePoint()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var peo = new PromptEntityOptions("\nChọn một block (INSERT) để thay đổi base point: ");
            peo.SetRejectMessage("\nĐối tượng được chọn không phải block.");
            peo.AddAllowedClass(typeof(BlockReference), false);
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var srcRef = (BlockReference)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                string blkName = srcRef.Name;
                Utils.Print($"🔹 Block được chọn: {blkName}");

                var ppo = new PromptPointOptions("\nChọn điểm gốc mới (trong UCS hiện hành): ")
                { UseBasePoint = true, BasePoint = srcRef.Position };
                var ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) return;

                // Chuyển điểm chọn sang block coordinate
                var invXform = srcRef.BlockTransform.Inverse();
                var baseLocal = ppr.Value.TransformBy(invXform);

                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var srcDef = (BlockTableRecord)tr.GetObject(srcRef.BlockTableRecord, OpenMode.ForRead);

                string tempName = blkName + "TAOTHEM";
                int k = 0;
                while (bt.Has(tempName)) tempName = blkName + "TAOTHEM" + (++k);

                var newDef = new BlockTableRecord { Name = tempName };
                bt.UpgradeOpen();
                var newDefId = bt.Add(newDef);
                tr.AddNewlyCreatedDBObject(newDef, true);

                // Clone entity từ block gốc
                var ids = new List<ObjectId>();
                foreach (ObjectId id in srcDef) ids.Add(id);
                var idMap = new IdMapping();
                db.DeepCloneObjects(new ObjectIdCollection(ids.ToArray()), newDefId, idMap, false);

                // Dịch chuyển toàn bộ entity bằng -baseLocal
                var offset = Point3d.Origin - baseLocal;   // đã là Vector3d
                var mat = Matrix3d.Displacement(offset);   // truyền thẳng
                foreach (ObjectId id in newDef)
                {
                    var ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                    ent.TransformBy(mat);
                }

                // Cập nhật tất cả instance
                var ss = ed.SelectAll(Utils.BlockNameFilter(blkName));
                int count = 0;
                if (ss.Status == PromptStatus.OK)
                {
                    foreach (SelectedObject so in ss.Value)
                    {
                        if (so == null) continue;
                        var br = (BlockReference)tr.GetObject(so.ObjectId, OpenMode.ForWrite);
                        var newPos = baseLocal.TransformBy(br.BlockTransform);
                        br.Position = newPos;
                        br.BlockTableRecord = newDefId;
                        count++;
                    }
                }

                // Xóa block def cũ + đổi tên
                srcDef.UpgradeOpen();
                srcDef.Erase();
                newDef.UpgradeOpen();
                newDef.Name = blkName;

                tr.Commit();
                Utils.Print($"✅ Đã chỉnh base point của block <{blkName}>: {count} đối tượng.");
            }
        }
    }
}
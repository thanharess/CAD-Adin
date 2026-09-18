using System;
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

                // ✅ SỬA LỖI 2: Xử lý dynamic block
                ObjectId srcDefId = srcRef.IsDynamicBlock
                    ? srcRef.DynamicBlockTableRecord
                    : srcRef.BlockTableRecord;

                var srcDef = (BlockTableRecord)tr.GetObject(srcDefId, OpenMode.ForRead);
                string blkName = srcDef.Name;

                // Nếu là dynamic block, tên có thể là anonymous → dùng tên gốc
                if (srcRef.IsDynamicBlock)
                {
                    var origDef = (BlockTableRecord)tr.GetObject(
                        srcRef.AnonymousBlockTableRecord, OpenMode.ForRead);
                    // AnonymousBlockTableRecord là def gốc cho dynamic block
                }

                Utils.Print($"🔹 Block được chọn: {blkName}");

                // ── Chọn điểm gốc mới ──
                var ppo = new PromptPointOptions("\nChọn điểm gốc mới (trong UCS hiện hành): ")
                { UseBasePoint = true, BasePoint = srcRef.Position };
                var ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) return;

                // Chuyển điểm chọn sang block coordinate
                var invXform = srcRef.BlockTransform.Inverse();
                var baseLocal = ppr.Value.TransformBy(invXform);

                // ── Tạo block definition mới ──
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                string tempName = blkName + "TAOTHEM";
                int k = 0;
                while (bt.Has(tempName)) tempName = blkName + "TAOTHEM" + (++k);

                var newDef = new BlockTableRecord { Name = tempName };
                bt.UpgradeOpen();
                var newDefId = bt.Add(newDef);
                tr.AddNewlyCreatedDBObject(newDef, true);

                // ── Clone entity từ block gốc ──
                var ids = new List<ObjectId>();
                foreach (ObjectId id in srcDef) ids.Add(id);
                var idMap = new IdMapping();
                db.DeepCloneObjects(new ObjectIdCollection(ids.ToArray()), newDefId, idMap, false);

                // ── Dịch chuyển toàn bộ entity bằng -baseLocal ──
                var offset = Point3d.Origin - baseLocal;
                var mat = Matrix3d.Displacement(offset);
                foreach (ObjectId id in newDef)
                {
                    var ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                    ent.TransformBy(mat);
                }

                // ✅ SỬA LỖI 4: Quét TOÀN BỘ BlockTable để tìm mọi reference
                int count = 0;
                foreach (ObjectId btrId in bt)
                {
                    var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

                    // Duyệt snapshot để tránh modify collection khi đang foreach
                    var entIds = new List<ObjectId>();
                    foreach (ObjectId id in btr) entIds.Add(id);

                    foreach (ObjectId entId in entIds)
                    {
                        var ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                        if (!(ent is BlockReference br)) continue;

                        // ✅ Kiểm tra br trỏ tới srcDef (kể cả dynamic)
                        ObjectId brDefId = br.IsDynamicBlock
                            ? br.DynamicBlockTableRecord
                            : br.BlockTableRecord;

                        if (brDefId != srcDef.ObjectId) continue;

                        // ✅ SỬA LỖI 3: Lưu attribute values trước khi đổi def
                        var attrValues = new Dictionary<string, string>();
                        foreach (ObjectId attId in br.AttributeCollection)
                        {
                            var att = tr.GetObject(attId, OpenMode.ForRead)
                                as AttributeReference;
                            if (att != null) attrValues[att.Tag] = att.TextString;
                        }

                        br.UpgradeOpen();

                        // Tính lại vị trí block reference
                        var newPos = baseLocal.TransformBy(br.BlockTransform);
                        br.Position = newPos;
                        br.BlockTableRecord = newDefId;

                        // ✅ Gán lại attribute values theo tag
                        foreach (ObjectId attId in br.AttributeCollection)
                        {
                            var att = tr.GetObject(attId, OpenMode.ForWrite)
                                as AttributeReference;
                            if (att != null && attrValues.TryGetValue(att.Tag, out string v))
                                att.TextString = v;
                        }

                        count++;
                    }
                }

                // ✅ SỬA LỖI 1: Đổi tên srcDef trước, rồi mới đổi newDef, rồi erase
                srcDef.UpgradeOpen();
                string oldTempName = blkName + "_OLD_" + Guid.NewGuid()
                    .ToString("N").Substring(0, 6);
                srcDef.Name = oldTempName;

                newDef.UpgradeOpen();
                newDef.Name = blkName;

                srcDef.Erase();

                tr.Commit();
                Utils.Print($"✅ Đã chỉnh base point của block <{blkName}>: {count} đối tượng.");
            }
        }
    }
}
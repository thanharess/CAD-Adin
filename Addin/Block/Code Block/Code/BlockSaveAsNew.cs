using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class BlockSaveAsNew
    {
        [CommandMethod("BLSAVEASNEWBLOCK")]
        public void SaveAsNewBlock()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var peo = new PromptEntityOptions("\nChọn block cần copy: ");
            peo.SetRejectMessage("\nĐối tượng được chọn không phải block.");
            peo.AddAllowedClass(typeof(BlockReference), false);
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            var pso = new PromptStringOptions("\nNhập tên block mới: ") { AllowSpaces = true };
            var psr = ed.GetString(pso);
            if (psr.Status != PromptStatus.OK) return;
            string newName = psr.StringResult.Trim();

            var ppr = ed.GetPoint("\nChọn điểm chèn block mới: ");
            if (ppr.Status != PromptStatus.OK) return;
            var insPt = ppr.Value;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var srcRef = (BlockReference)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                if (bt.Has(newName))
                {
                    Utils.Print($"❌ Block '{newName}' đã tồn tại.");
                    return;
                }

                var srcDef = (BlockTableRecord)tr.GetObject(srcRef.BlockTableRecord, OpenMode.ForRead);
                var ids = new List<ObjectId>();
                foreach (ObjectId id in srcDef) ids.Add(id);

                var newDef = new BlockTableRecord { Name = newName };
                bt.UpgradeOpen();
                var newDefId = bt.Add(newDef);
                tr.AddNewlyCreatedDBObject(newDef, true);

                // Clone toàn bộ entity trong block def
                var idMap = new IdMapping();
                db.DeepCloneObjects(new ObjectIdCollection(ids.ToArray()), newDefId, idMap, false);

                // Chèn block mới
                var ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                var newRef = new BlockReference(insPt, newDefId);
                ms.AppendEntity(newRef);
                tr.AddNewlyCreatedDBObject(newRef, true);

                tr.Commit();
            }
            Utils.Print($"✅ Đã tạo block mới '{newName}'.");
        }
    }
}
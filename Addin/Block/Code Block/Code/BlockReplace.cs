using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class BlockReplace
    {
        [CommandMethod("BLREPLACE")]
        public void Replace()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var kwo = new PromptKeywordOptions("\nChọn block mẫu bằng [Pick/List] <Pick>: ")
            { AllowNone = true };
            kwo.Keywords.Add("Pick"); kwo.Keywords.Add("List");
            var kwr = ed.GetKeywords(kwo);
            string mode = kwr.Status == PromptStatus.OK ? kwr.StringResult : "Pick";

            string refName = null;
            ObjectId sampleId = ObjectId.Null;
            bool delSample = false;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                if (mode == "Pick")
                {
                    var peo = new PromptEntityOptions("\nChọn block mẫu: ");
                    peo.SetRejectMessage("\nĐối tượng được chọn không phải block.");
                    peo.AddAllowedClass(typeof(BlockReference), false);
                    var per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK) return;

                    var br = (BlockReference)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                    refName = br.Name;
                    sampleId = per.ObjectId;

                    var kwo2 = new PromptKeywordOptions("\nXóa block mẫu sau khi thay? [Yes/No] <No>: ")
                    { AllowNone = true };
                    kwo2.Keywords.Add("Yes"); kwo2.Keywords.Add("No");
                    var kwr2 = ed.GetKeywords(kwo2);
                    delSample = (kwr2.Status == PromptStatus.OK && kwr2.StringResult == "Yes");
                }
                else
                {
                    var names = new List<string>();
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    foreach (ObjectId id in bt)
                    {
                        var b = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                        if (b.IsLayout) continue;
                        names.Add(b.Name);
                        ed.WriteMessage($"\n  - {b.Name}");
                    }
                    var pso = new PromptStringOptions("\nNhập tên block mẫu: ") { AllowSpaces = true };
                    var psr = ed.GetString(pso);
                    if (psr.Status != PromptStatus.OK) return;
                    refName = psr.StringResult.Trim();
                    if (!names.Contains(refName))
                    {
                        Utils.Print("❌ Block không tồn tại.");
                        return;
                    }
                }
                tr.Commit();
            }

            Utils.Print($"🔹 Block mẫu: {refName}");

            var sel = ed.GetSelection(new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "INSERT") }));
            if (sel.Status != PromptStatus.OK) return;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var refDefId = bt[refName];
                var ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                int cnt = 0;
                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var oldRef = (BlockReference)tr.GetObject(so.ObjectId, OpenMode.ForWrite);
                    var p = oldRef.Position;
                    var rot = oldRef.Rotation;
                    var sx = oldRef.ScaleFactors.X;
                    var sy = oldRef.ScaleFactors.Y;
                    var sz = oldRef.ScaleFactors.Z;

                    var newRef = new BlockReference(p, refDefId)
                    {
                        Rotation = rot,
                        ScaleFactors = new Scale3d(sx, sy, sz)
                    };
                    ms.AppendEntity(newRef);
                    tr.AddNewlyCreatedDBObject(newRef, true);
                    oldRef.Erase();
                    cnt++;
                }

                if (delSample && !sampleId.IsNull)
                {
                    var sref = (BlockReference)tr.GetObject(sampleId, OpenMode.ForWrite);
                    sref.Erase();
                }

                tr.Commit();
                Utils.Print($"✅ Đã thay thế {cnt} block bằng \"{refName}\".");
            }
        }
    }
}
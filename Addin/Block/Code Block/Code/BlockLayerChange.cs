using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class BlockLayerChange
    {
        [CommandMethod("LAYERCHANGEBLOCK")]
        public void ChangeLayer()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var layerNames = new List<string>();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                int i = 1;
                ed.WriteMessage("\nDanh sách layer:");
                foreach (ObjectId id in lt)
                {
                    var lay = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    layerNames.Add(lay.Name);
                    ed.WriteMessage($"\n {i++}. {lay.Name}");
                }
                tr.Commit();
            }

            var pio = new PromptIntegerOptions("\nNhập STT layer cần đổi: ");
            var pir = ed.GetInteger(pio);
            if (pir.Status != PromptStatus.OK) return;
            int idx = pir.Value;
            if (idx < 1 || idx > layerNames.Count)
            {
                Utils.Print("❌ STT không hợp lệ.");
                return;
            }
            string targetLayer = layerNames[idx - 1];

            var sel = ed.GetSelection(new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "INSERT") }));
            if (sel.Status != PromptStatus.OK) return;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var br = (BlockReference)tr.GetObject(so.ObjectId, OpenMode.ForRead);
                    var btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForWrite);

                    foreach (ObjectId eid in btr)
                    {
                        var ent = tr.GetObject(eid, OpenMode.ForWrite) as Entity;
                        if (ent == null) continue;
                        string cn = ent.GetType().Name;
                        if (cn == "Line" || cn == "Circle" || cn == "Arc"
                            || cn == "Polyline" || cn == "Polyline2d")
                        {
                            ent.Layer = targetLayer;
                        }
                    }
                }
                tr.Commit();
            }
            ed.Regen();
            Utils.Print($"✔ Đã đổi layer LINE/CIRCLE/PLINE trong block sang: {targetLayer}");
        }
    }
}
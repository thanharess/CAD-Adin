using System.Collections.Generic;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.Block
{
    public class BlockHighlight
    {
        [CommandMethod("BLHLAYER")]
        public void Highlight()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            var peo = new PromptEntityOptions("\nChọn một block (INSERT): ");
            peo.SetRejectMessage("\nĐối tượng được chọn không phải block.");
            peo.AddAllowedClass(typeof(BlockReference), false);
            var per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK) return;

            var kwo = new PromptKeywordOptions("\nChọn màu highlight [1=Đỏ 2=Xanh lá 3=Vàng 4=Hồng] <1>: ")
            { AllowNone = true };
            kwo.Keywords.Add("1"); kwo.Keywords.Add("2"); kwo.Keywords.Add("3"); kwo.Keywords.Add("4");
            var kwr = ed.GetKeywords(kwo);
            string choice = kwr.Status == PromptStatus.OK ? kwr.StringResult : "1";

            short color = 1;
            switch (choice)
            {
                case "2": color = 3; break;
                case "3": color = 2; break;
                case "4": color = 6; break;
            }

            const string HL_LAYER = "BM_HIGHLIGHT";

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var br = (BlockReference)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                string blkName = br.Name;
                Utils.Print($"🔹 Block được chọn: {blkName}");

                // Đảm bảo layer tồn tại
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(HL_LAYER))
                {
                    lt.UpgradeOpen();
                    var lay = new LayerTableRecord
                    {
                        Name = HL_LAYER,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, color)
                    };
                    lt.Add(lay);
                    tr.AddNewlyCreatedDBObject(lay, true);
                }

                var ss = ed.SelectAll(Utils.BlockNameFilter(blkName));
                if (ss.Status != PromptStatus.OK)
                {
                    Utils.Print("❌ Không tìm thấy block cùng tên.");
                    return;
                }

                var oldLayers = new List<string>();
                foreach (SelectedObject so in ss.Value)
                {
                    if (so == null) continue;
                    var r = (BlockReference)tr.GetObject(so.ObjectId, OpenMode.ForWrite);
                    oldLayers.Add(r.Layer);
                    r.Layer = HL_LAYER;
                }

                ed.Regen();
                Utils.Print($"🔍 Đã highlight {ss.Value.Count} block. Nhấn Enter để khôi phục...");
                ed.GetString("\nNhấn Enter...");

                int i = 0;
                foreach (SelectedObject so in ss.Value)
                {
                    if (so == null) continue;
                    var r = (BlockReference)tr.GetObject(so.ObjectId, OpenMode.ForWrite);
                    r.Layer = oldLayers[i++];
                }
                ed.Regen();
                Utils.Print("✅ Đã khôi phục layer gốc.");
                tr.Commit();
            }
        }
    }
}
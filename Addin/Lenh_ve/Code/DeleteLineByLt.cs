using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.Block
{
    public class DeleteLineByLt
    {
        // Danh sách linetype cần xoá (chỉnh ở đây nếu cần)
        private static readonly HashSet<string> LinetypeList = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            "CENTER2", "CENTER", "ACAD_ISO10W100", "AM_ISO08W050",
            "AM_ISO09W050", "ACISOTGB", "ACANSTGB", "AM_ISO08W050x2",
            "PHANTOM2", "双点画线", "点画线"
        };

        [CommandMethod("DeleteLineByLt")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            Utils.Print("LỆNH DELETELINEBYLT - Xoá LINE theo linetype.");

            var kwo = new PromptKeywordOptions(
                "\n[1:Xoá tất cả theo linetype / 2:Chọn vùng] <2>: ")
            { AllowNone = true };
            kwo.Keywords.Add("1");
            kwo.Keywords.Add("2");
            var kwr = ed.GetKeywords(kwo);
            string choice = (kwr.Status == PromptStatus.OK && !string.IsNullOrEmpty(kwr.StringResult))
                ? kwr.StringResult : "2";

            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "LINE") });

            PromptSelectionResult sel;
            if (choice == "1")
            {
                Utils.Print("→ Tìm tất cả LINE...");
                sel = ed.SelectAll(filter);
            }
            else
            {
                ed.WriteMessage("\nChọn vùng cần xoá:");
                sel = ed.GetSelection(filter);
            }

            if (sel.Status != PromptStatus.OK)
            {
                Utils.Print("Không có LINE nào để kiểm tra.");
                return;
            }

            // Build layer linetype cache
            var layerLTCache = BuildLayerLTCache(db);

            // Lọc theo linetype hiệu quả
            int count = 0;
            var layersToUnlock = new HashSet<string>();
            var objectIds = new List<ObjectId>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject so in sel.Value)
                {
                    if (so == null) continue;
                    var ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    string effLt = GetEffectiveLinetype(ent, layerLTCache);
                    if (LinetypeList.Contains(effLt))
                    {
                        objectIds.Add(so.ObjectId);

                        // Ghi lại layer cần unlock
                        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                        if (lt.Has(ent.Layer))
                        {
                            var lay = (LayerTableRecord)tr.GetObject(lt[ent.Layer], OpenMode.ForRead);
                            if (lay.IsLocked) layersToUnlock.Add(ent.Layer);
                        }
                    }
                }

                // Unlock tạm thời
                foreach (var layName in layersToUnlock)
                {
                    var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    var lay = (LayerTableRecord)tr.GetObject(lt[layName], OpenMode.ForWrite);
                    lay.IsLocked = false;
                }
                if (layersToUnlock.Count > 0)
                    Utils.Print($"→ Đã unlock tạm thời các layer: {string.Join(", ", layersToUnlock)}");

                // Xoá
                foreach (var id in objectIds)
                {
                    var ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                    ent.Erase();
                    count++;
                }

                // Relock
                foreach (var layName in layersToUnlock)
                {
                    var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                    var lay = (LayerTableRecord)tr.GetObject(lt[layName], OpenMode.ForWrite);
                    lay.IsLocked = true;
                }
                if (layersToUnlock.Count > 0)
                    Utils.Print($"→ Đã relock các layer: {string.Join(", ", layersToUnlock)}");

                tr.Commit();
            }

            if (count > 0)
                Utils.Print($"→ Đã xoá {count} LINE theo linetype.");
            else
                Utils.Print("Không tìm thấy LINE theo linetype nào.");
        }

        private static Dictionary<string, string> BuildLayerLTCache(Database db)
        {
            var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in lt)
                {
                    var lay = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    string ltName = lay.LinetypeObjectId.IsValid
                        ? ((LinetypeTableRecord)tr.GetObject(lay.LinetypeObjectId, OpenMode.ForRead)).Name
                        : "Continuous";
                    cache[lay.Name] = ltName;
                }
                tr.Commit();
            }
            return cache;
        }

        private static string GetEffectiveLinetype(Entity ent, Dictionary<string, string> cache)
        {
            string lt = ent.Linetype;
            if (string.IsNullOrEmpty(lt) ||
                string.Equals(lt, "BYLAYER", StringComparison.OrdinalIgnoreCase))
            {
                return cache.TryGetValue(ent.Layer, out string cached) ? cached : "Continuous";
            }
            return lt;
        }
    }
}
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Dimtools
{
    public class DimDelete
    {
        // ════════════════════════════════════════════════════════
        // Lệnh chính: DELETEDIM
        // ════════════════════════════════════════════════════════
        [CommandMethod("DELETEDIM")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            var kwo = new PromptKeywordOptions(
                "\nChọn chức năng [1:Chọn / 2:Theo layer / 3:MLEADER / 4:Theo layer+vùng / 5:Tất cả] <1>: ")
            { AllowNone = true };
            kwo.Keywords.Add("1");
            kwo.Keywords.Add("2");
            kwo.Keywords.Add("3");
            kwo.Keywords.Add("4");
            kwo.Keywords.Add("5");

            var kwr = ed.GetKeywords(kwo);
            string choice = (kwr.Status == PromptStatus.OK && !string.IsNullOrEmpty(kwr.StringResult))
                ? kwr.StringResult
                : "1";

            switch (choice)
            {
                case "1":
                    DeleteSelectedDim(ed, doc);
                    break;

                case "2":
                    {
                        var pso = new PromptStringOptions("\nNhập tên layer cần xóa DIMENSION: ")
                        { AllowSpaces = true };
                        var psr = ed.GetString(pso);
                        if (psr.Status != PromptStatus.OK) return;

                        string layName = psr.StringResult.Trim();
                        if (string.IsNullOrEmpty(layName))
                        {
                            AcApp.ShowAlertDialog("Tên layer không hợp lệ.");
                            return;
                        }
                        DeleteDimByLayer(ed, doc, layName);
                    }
                    break;

                case "3":
                    DeleteMLeader(ed, doc);
                    break;

                case "4":
                    {
                        var pso = new PromptStringOptions("\nNhập tên layer cần xóa DIMENSION: ")
                        { AllowSpaces = true };
                        var psr = ed.GetString(pso);
                        if (psr.Status != PromptStatus.OK) return;

                        string layName = psr.StringResult.Trim();
                        if (string.IsNullOrEmpty(layName))
                        {
                            AcApp.ShowAlertDialog("Tên layer không hợp lệ.");
                            return;
                        }
                        DeleteDimByLayerSelect(ed, doc, layName);
                    }
                    break;

                case "5":
                    DeleteAllDim(ed, doc);
                    break;

                default:
                    AcApp.ShowAlertDialog("Lựa chọn không hợp lệ. Mặc định xóa DIMENSION được chọn.");
                    DeleteSelectedDim(ed, doc);
                    break;
            }
        }

        // ════════════════════════════════════════════════════════
        // Chức năng 1: Xóa DIMENSION được chọn
        // ════════════════════════════════════════════════════════
        private static void DeleteSelectedDim(Editor ed, Autodesk.AutoCAD.ApplicationServices.Document doc)
        {
            ed.WriteMessage("\nChọn các DIMENSION cần xóa (quét chọn hoặc pick, Enter để kết thúc): ");

            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "DIMENSION") });
            var sel = ed.GetSelection(filter);

            if (sel.Status != PromptStatus.OK)
            {
                AcApp.ShowAlertDialog("Không có DIMENSION nào được chọn.");
                return;
            }

            int cnt = EraseAll(sel.Value);
            AcApp.ShowAlertDialog($"Đã xóa {cnt} DIMENSION được chọn.");
        }

        // ════════════════════════════════════════════════════════
        // Chức năng 2: Xóa DIMENSION theo layer (toàn bản vẽ)
        // ════════════════════════════════════════════════════════
        private static void DeleteDimByLayer(Editor ed, Autodesk.AutoCAD.ApplicationServices.Document doc, string layName)
        {
            var filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "DIMENSION"),
                new TypedValue((int)DxfCode.LayerName, layName)
            });
            var sel = ed.SelectAll(filter);

            if (sel.Status != PromptStatus.OK)
            {
                AcApp.ShowAlertDialog($"Không có DIMENSION nào trên layer {layName}.");
                return;
            }

            int cnt = EraseAll(sel.Value);
            AcApp.ShowAlertDialog($"Đã xóa {cnt} DIMENSION trên layer {layName}.");
        }

        // ════════════════════════════════════════════════════════
        // Chức năng 3: Xóa MLEADER (LEADER NOTE) toàn bản vẽ
        // ════════════════════════════════════════════════════════
        private static void DeleteMLeader(Editor ed, Autodesk.AutoCAD.ApplicationServices.Document doc)
        {
            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "MULTILEADER") });
            var sel = ed.SelectAll(filter);

            if (sel.Status != PromptStatus.OK)
            {
                AcApp.ShowAlertDialog("Không có LEADER NOTE (MLEADER) nào trong bản vẽ.");
                return;
            }

            int cnt = EraseAll(sel.Value);
            AcApp.ShowAlertDialog($"Đã xóa {cnt} LEADER NOTE (MLEADER).");
        }

        // ════════════════════════════════════════════════════════
        // Chức năng 4: Xóa DIMENSION theo layer + vùng chọn
        // ════════════════════════════════════════════════════════
        private static void DeleteDimByLayerSelect(Editor ed, Autodesk.AutoCAD.ApplicationServices.Document doc, string layName)
        {
            ed.WriteMessage($"\nChọn vùng DIMENSION trên layer {layName} (quét chọn hoặc pick, Enter để kết thúc): ");

            var filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "DIMENSION"),
                new TypedValue((int)DxfCode.LayerName, layName)
            });
            var sel = ed.GetSelection(filter);

            if (sel.Status != PromptStatus.OK)
            {
                AcApp.ShowAlertDialog($"Không có DIMENSION nào được chọn trên layer {layName}.");
                return;
            }

            int cnt = EraseAll(sel.Value);
            AcApp.ShowAlertDialog($"Đã xóa {cnt} DIMENSION trên layer {layName} trong vùng chọn.");
        }

        // ════════════════════════════════════════════════════════
        // Chức năng 5: Xóa TẤT CẢ DIMENSION trong bản vẽ
        // ════════════════════════════════════════════════════════
        private static void DeleteAllDim(Editor ed, Autodesk.AutoCAD.ApplicationServices.Document doc)
        {
            var filter = new SelectionFilter(new[]
            { new TypedValue((int)DxfCode.Start, "DIMENSION") });
            var sel = ed.SelectAll(filter);

            if (sel.Status != PromptStatus.OK)
            {
                AcApp.ShowAlertDialog("Không có DIMENSION nào trong bản vẽ.");
                return;
            }

            int cnt = EraseAll(sel.Value);
            AcApp.ShowAlertDialog($"Đã xóa {cnt} DIMENSION.");
        }

        // ════════════════════════════════════════════════════════
        // Helper: Xóa toàn bộ entity trong SelectionSet, trả về số lượng
        // ════════════════════════════════════════════════════════
        private static int EraseAll(SelectionSet ss)
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                int cnt = 0;
                foreach (SelectedObject so in ss)
                {
                    if (so == null) continue;
                    var ent = tr.GetObject(so.ObjectId, OpenMode.ForWrite, false) as Entity;
                    if (ent == null) continue;
                    ent.Erase();
                    cnt++;
                }
                tr.Commit();
                return cnt;
            }
        }
    }
}
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class DimChangeStyleWrite
    {
        [CommandMethod("DIMCHANGESTYLEWRITE")]
        public void ChangeDimStyle()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            // ── Bước 1: Nhập tên DimStyle cần đổi ──
            var pso = new PromptStringOptions("\nNhập tên Dim Style cần đổi: ")
            { AllowSpaces = true };
            var psr = ed.GetString(pso);
            if (psr.Status != PromptStatus.OK) return;
            string dimStyleName = psr.StringResult.Trim();

            if (string.IsNullOrEmpty(dimStyleName))
            {
                ed.WriteMessage("\n❌ Tên Dim Style không hợp lệ.");
                return;
            }

            // ── Bước 2: Kiểm tra DimStyle có tồn tại không ──
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var dst = (DimStyleTable)tr.GetObject(db.DimStyleTableId, OpenMode.ForRead);
                if (!dst.Has(dimStyleName))
                {
                    ed.WriteMessage($"\n❌ Dim Style '{dimStyleName}' không tồn tại!");
                    tr.Commit();
                    return;
                }

                // Lấy ObjectId của dimstyle để gán cho entity
                ObjectId dimStyleId = dst[dimStyleName];

                tr.Commit();

                // ── Bước 3: Chọn DIM cần đổi style ──
                var filter = new SelectionFilter(new[]
                { new TypedValue((int)DxfCode.Start, "DIMENSION") });
                var sel = ed.GetSelection(filter);
                if (sel.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n❌ Không chọn DIM nào.");
                    return;
                }

                // ── Bước 4: Gán dimstyle mới cho từng DIM ──
                int count = 0;
                using (var tr2 = db.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject so in sel.Value)
                    {
                        if (so == null) continue;
                        var dim = tr2.GetObject(so.ObjectId, OpenMode.ForWrite) as Dimension;
                        if (dim == null) continue;

                        // Gán style mới (giống subst (cons 3 ...) trong LISP)
                        dim.DimensionStyle = dimStyleId;
                        count++;
                    }
                    tr2.Commit();
                }

                ed.Regen();
                ed.WriteMessage($"\n✓ Đã đổi Dim Style '{dimStyleName}' cho {count} DIM được chọn.");
            }
        }
    }
}
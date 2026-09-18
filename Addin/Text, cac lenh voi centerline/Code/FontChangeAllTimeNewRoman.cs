using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace BlockTools
{
    public class FontChangeAllTimeNewRoman
    {
        [CommandMethod("FontChangeAllTimeNewRoman")]
        public void Run()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            Utils.Print("Đang ép font Times New Roman...");

            int count = 0;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var st = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                foreach (ObjectId id in st)
                {
                    var rec = tr.GetObject(id, OpenMode.ForWrite) as TextStyleTableRecord;
                    if (rec == null) continue;

                    try
                    {
                        // Times New Roman, không bold, không italic, charset 0, pitch 34 (variable)
                        var fd = new FontDescriptor("Times New Roman",
                            false, false, 0, 34);
                        rec.Font = fd;
                        count++;
                    }
                    catch { }
                }
                tr.Commit();
            }

            ed.Regen();
            Utils.Print($"✓ Hoàn thành: {count} style.");
        }
    }
}
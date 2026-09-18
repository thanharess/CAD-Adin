using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.Block
{
    public class BlockUnits
    {
        [CommandMethod("BLCHANGEALLUNITMM")]
        public void ChangeAllUnitsMm()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                foreach (ObjectId id in bt)
                {
                    var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (btr.Name.StartsWith("*")) continue;
                    btr.UpgradeOpen();
                    btr.Units = UnitsValue.Millimeters;
                }
                tr.Commit();
            }
            Utils.Print("✔ Tất cả block đã được đặt đơn vị = mm.");
        }
    }
}
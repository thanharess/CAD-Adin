using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CADAddin.Common              // ← ĐỔI THÀNH
{
    public static class Utils
    {
        public static Editor Ed => AcApp.DocumentManager.MdiActiveDocument?.Editor;
        public static void Print(string msg) => Ed?.WriteMessage("\n" + msg);

        public static bool IsBlockReference(Transaction tr, ObjectId id, out BlockReference br)
        {
            br = null;
            if (id.IsNull) return false;
            var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
            br = ent as BlockReference;
            return br != null;
        }

        public static SelectionFilter BlockNameFilter(string name) =>
            new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue((int)DxfCode.BlockName, name)
            });
    }
}
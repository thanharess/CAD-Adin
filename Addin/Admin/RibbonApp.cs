using System.IO;
using System.Reflection;
using Autodesk.AutoCAD.Runtime;

namespace Autocad_addin.Framework
{
    public class RibbonApp : IExtensionApplication
    {
        public void Initialize()
        {
            string dll = Assembly.GetExecutingAssembly().Location;
            string root = Path.GetDirectoryName(dll);
            string lisp = Path.Combine(root, "Lisp");
            string images = Path.Combine(root, "Image");

            // ⬇️ IN ĐƯỜNG DẪN ĐỂ DEBUG
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager
                .MdiActiveDocument?.Editor.WriteMessage(
                    $"\n[Plugin] DLL: {dll}\n[Plugin] Image folder: {images}");

            new RibbonBuilder(Assembly.GetExecutingAssembly(), lisp, images).Build();
        }

        public void Terminate() { }
    }
}
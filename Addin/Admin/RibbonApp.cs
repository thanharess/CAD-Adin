using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using System;

namespace Autocad_addin.Framework
{
    public class RibbonApp : IExtensionApplication
    {
        private RibbonBuilder _builder;
        private string _lispFolder;
        private string _imageFolder;

        public void Initialize()
        {
            // Đường dẫn thư mục Lisp & Image (điều chỉnh theo project của bạn)
            string asmPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string root = System.IO.Path.GetDirectoryName(asmPath);

            _lispFolder = System.IO.Path.Combine(root, "Lisp");
            _imageFolder = System.IO.Path.Combine(root, "Image");

            _builder = new RibbonBuilder(
                System.Reflection.Assembly.GetExecutingAssembly(),
                _lispFolder,
                _imageFolder);

            // Tạo Ribbon
            _builder.Build();

            // ===== TỰ ĐỘNG LOAD LISP KHI MỞ / CHUYỂN DRAWING =====
            Application.DocumentManager.DocumentCreated += OnDocumentCreated;
            Application.DocumentManager.DocumentActivated += OnDocumentActivated;

            // Load ngay cho drawing đang mở
            LoadLispForCurrentDoc();
        }

        public void Terminate()
        {
            Application.DocumentManager.DocumentCreated -= OnDocumentCreated;
            Application.DocumentManager.DocumentActivated -= OnDocumentActivated;
        }

        private void OnDocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            LoadLispForCurrentDoc();
        }

        private void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            LoadLispForCurrentDoc();
        }

        private void LoadLispForCurrentDoc()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            if (!System.IO.Directory.Exists(_lispFolder)) return;

            var files = System.IO.Directory.GetFiles(_lispFolder, "*.lsp",
                System.IO.SearchOption.AllDirectories);

            foreach (var f in files)
            {
                string path = f.Replace("\\", "/");
                doc.SendStringToExecute($"(load \"{path}\") ", true, false, false);
            }

            doc.Editor.WriteMessage($"\n[Plugin] Đã load {files.Length} file LISP vào drawing hiện tại.");
        }
    }
}
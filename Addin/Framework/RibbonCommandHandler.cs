using Autodesk.Windows;
using System;
using System.Reflection;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;   // ← THÊM DÒNG NÀY

namespace CADAddin.Framework

{
    public class RibbonCommandHandler : ICommand
    {
        public bool CanExecute(object parameter) => true;

        public event EventHandler CanExecuteChanged
        {
            add { }
            remove { }
        }

        public void Execute(object parameter)
        {
            if (parameter == null) return;

            string methodName = null;

            // Trường hợp 1: parameter là string (đúng như mình set)
            if (parameter is string str)
            {
                methodName = str.Trim();
            }
            // Trường hợp 2: AutoCAD truyền luôn RibbonButton
            else if (parameter is RibbonButton btn)
            {
                // Lấy CommandParameter từ chính nút
                methodName = btn.CommandParameter?.ToString()?.Trim();
            }

            if (string.IsNullOrEmpty(methodName))
            {
                Application.DocumentManager.MdiActiveDocument?
                    .Editor.WriteMessage("\n[Plugin] Không tìm thấy CommandParameter");
                return;
            }

            // ===== Tìm method =====
            MethodInfo method = null;
            var asm = Assembly.GetExecutingAssembly();

            foreach (var type in asm.GetTypes())
            {
                if (type.IsClass && type.Name.StartsWith("Button"))
                {
                    method = type.GetMethod(methodName,
                        BindingFlags.Public | BindingFlags.Static);

                    if (method != null)
                        break;
                }
            }

            if (method != null)
            {
                try
                {
                    method.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    Application.DocumentManager.MdiActiveDocument?
                        .Editor.WriteMessage($"\n[Plugin] Lỗi method: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
            else
            {
                // Không có method → chạy như lệnh LISP / AutoCAD
                var doc = Application.DocumentManager.MdiActiveDocument;
                doc?.SendStringToExecute(methodName + " ", true, false, false);
            }
        }
    }
}
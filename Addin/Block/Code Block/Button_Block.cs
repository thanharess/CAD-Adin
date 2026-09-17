using Autocad_addin.Framework;
using Autodesk.Windows;
using Autodesk.AutoCAD.ApplicationServices;
using System.IO;
using System.Reflection;

namespace Autocad_addin.Addin_Autocad.Button
{
    public static class Button_Block
    {
        [RibbonButton("Tool CAD", "Block Tool", "Layer Change Block",
            ToolTip = "Thay đổi layer của block",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "Block",
            Order = 1)]
        public static void LayerChangeBlock()
        {
            // Load đúng file LISP rồi chạy lệnh
            RunLispFile("LayerChangeBlock.lsp", "C:LAYERCHANGEBLOCK");
        }

        [RibbonButton("Tool CAD", "Block Tool", "Create New Block",
            ToolTip = "Tạo block mới",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "Block",
            Order = 2)]
        public static void CreateNewBlock()
        {
            // Ví dụ:
            // RunLispFile("Create new block từ block section.lsp", "C:BLSAVEASNEWBLOCK");
        }

        [RibbonButton("Tool CAD", "Block Tool", "Đếm Block Trùng Tên",
            ToolTip = "Đếm block trùng tên",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = true,
            Namespace = "Block",
            Order = 3)]
        public static void DemBlockTrungTen()
        {
            // RunLispFile("Dem tat ca block trung ten.lsp", "C:BLCOUNT");
        }

        // ===== Hàm chính: load file + chạy lệnh =====
        private static void RunLispFile(string fileName, string command)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            // Tìm thư mục Lisp (cùng cấp với DLL)
            string dllPath = Assembly.GetExecutingAssembly().Location;
            string root = Path.GetDirectoryName(dllPath);
            string lispPath = Path.Combine(root, "Lisp", "Block", fileName);

            // Nếu file nằm sâu hơn thì tìm đệ quy
            if (!File.Exists(lispPath))
            {
                var found = Directory.GetFiles(Path.Combine(root, "Lisp"), fileName, SearchOption.AllDirectories);
                if (found.Length > 0)
                    lispPath = found[0];
                else
                {
                    doc.Editor.WriteMessage($"\n[Plugin] Không tìm thấy file: {fileName}");
                    return;
                }
            }

            // Load + chạy (1 lần duy nhất khi bấm nút)
            string path = lispPath.Replace("\\", "/");
            string fullCmd = $"(if (not (boundp '{command.ToLower()})) (load \"{path}\")) ({command}) ";

            doc.SendStringToExecute(fullCmd, true, false, false);
        }
    }
}
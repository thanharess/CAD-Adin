using System.IO;
using System.Reflection;
using CADAddin.Framework;
using Autodesk.Windows;
using CADAddin.Common;                    // ← THÊM để gọi Utils
using Autodesk.AutoCAD.ApplicationServices;

namespace CADAddin.Khac

{
    public static class Button_VLX
    {
        // ═══════════════════════════════════════════════════════════
        // CẤU HÌNH — SỬA 2 DÒNG NÀY
        // ═══════════════════════════════════════════════════════════
        private const string VLX_FILE = "VV_TRICH_CHI_TIET_PHUC.VLX";
        private const string VLX_COMMAND = "IN";   // ← ĐIỀN TÊN LỆNH THẬT

        // ═══════════════════════════════════════════════════════════
        // NÚT RIBBON
        // ═══════════════════════════════════════════════════════════
        [RibbonButton("Tool CAD", "Tools Khác", "IN PDF Auto",
            ToolTip = "IN PDF Auto",
            Size = RibbonItemSize.Standard,
            Icon = "A1.png",
            NewRow = false,
            Order = 2)]
        public static void RunVlx()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            // Đường dẫn VLX: cùng folder DLL, subfolder "Lisp"
            string asmPath = Assembly.GetExecutingAssembly().Location;
            string root = Path.GetDirectoryName(asmPath);
            string vlxPath = Path.Combine(root, "Lisp", VLX_FILE).Replace("\\", "/");

            if (!File.Exists(vlxPath))
            {
                doc.Editor.WriteMessage($"\n[Plugin] Không tìm thấy VLX: {vlxPath}");
                return;
            }

            // Load VLX — dùng vl-load-all để không hiện dialog APPLOAD
            doc.SendStringToExecute($"(vl-load-all \"{vlxPath}\") ", true, false, false);

            // Gọi lệnh của VLX
            doc.SendStringToExecute($"{VLX_COMMAND} ", true, false, false);
        }
    }
}
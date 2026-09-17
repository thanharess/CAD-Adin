using System;
using Autodesk.Windows;

namespace Autocad_addin.Framework
{
    // ============================================================
    // ATTRIBUTE 1: Nút thường (đã có)
    // ============================================================
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RibbonButtonAttribute : Attribute
    {
        public string Tab { get; }
        public string Panel { get; }
        public string Text { get; }
        public string ToolTip { get; set; }
        public RibbonItemSize Size { get; set; } = RibbonItemSize.Standard;
        public int Order { get; set; } = 0;
        public string Icon { get; set; }
        public string LargeIcon { get; set; }
        public bool NewRow { get; set; } = false;
        public bool HasDialogLauncher { get; set; } = false;
        public string DialogCommand { get; set; }

        // ⬇️ THÊM NAMESPACE
        public string Namespace { get; set; }   // Nhóm LISP (Block, Layer, Text...)

        public RibbonButtonAttribute(string tab, string panel, string text)
        {
            Tab = tab; Panel = panel; Text = text;
        }
    }

    // ============================================================
    // ATTRIBUTE 2: Nút Dropdown (SplitButton)
    // ============================================================
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RibbonDropDownAttribute : Attribute
    {
        public string Tab { get; }
        public string Panel { get; }
        public string Text { get; }
        public string ToolTip { get; set; }
        public RibbonItemSize Size { get; set; } = RibbonItemSize.Large;
        public int Order { get; set; } = 0;
        public string Icon { get; set; }
        public string LargeIcon { get; set; }
        public string Namespace { get; set; }   // ⬅️ THÊM
        public RibbonDropDownAttribute(string tab, string panel, string text)
        {
            Tab = tab; Panel = panel; Text = text;
        }
    }

    // ============================================================
    // ATTRIBUTE 3: Item con trong Dropdown
    // ============================================================
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RibbonDropItemAttribute : Attribute
    {
        public string Parent { get; }          // Tên nút cha (khớp Text của RibbonDropDown)
        public string Text { get; }
        public string Namespace { get; set; }   // ⬅️ THÊM
        public string ToolTip { get; set; }
        public int Order { get; set; } = 0;
        public string Icon { get; set; }       // 16×16 cho item con

        public RibbonDropItemAttribute(string parent, string text)
        {
            Parent = parent; Text = text;
        }
    }

    // ============================================================
    // COMMAND HANDLER (giữ nguyên)
    // ============================================================
    public class RibbonCommandHandler : System.Windows.Input.ICommand
    {
        public bool CanExecute(object p) => true;
        public event EventHandler CanExecuteChanged { add { } remove { } }

        public void Execute(object p)
        {
            if (p is string cmd)
            {
                var doc = Autodesk.AutoCAD.ApplicationServices
                    .Application.DocumentManager.MdiActiveDocument;
                doc?.SendStringToExecute(cmd, true, false, false);
            }
        }
    }
}
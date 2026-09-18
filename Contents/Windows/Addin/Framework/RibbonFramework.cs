using Autodesk.Windows;
using System;

namespace CADAddin.Framework
{
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
        public string Namespace { get; set; }
        public int RowsPerColumn { get; set; } = 3;         // ← BẮT BUỘC

        public RibbonButtonAttribute(string tab, string panel, string text)
        {
            Tab = tab; Panel = panel; Text = text;
        }
    }

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
        public string Namespace { get; set; }
        public int RowsPerColumn { get; set; } = 3;         // ← BẮT BUỘC

        public RibbonDropDownAttribute(string tab, string panel, string text)
        {
            Tab = tab; Panel = panel; Text = text;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RibbonDropItemAttribute : Attribute
    {
        public string Parent { get; }
        public string Text { get; }
        public string Namespace { get; set; }
        public string ToolTip { get; set; }
        public int Order { get; set; } = 0;
        public string Icon { get; set; }

        public RibbonDropItemAttribute(string parent, string text)
        {
            Parent = parent; Text = text;
        }
    }
}
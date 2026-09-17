using System;
using Autodesk.Windows;

namespace Autocad_addin.Framework
{
    using System;
    using Autodesk.Windows;

    namespace Autocad_addin.Framework
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
            public bool NewRow { get; set; } = false;   // ⬅️ NGẮT HÀNG

            public RibbonButtonAttribute(string tab, string panel, string text)
            {
                Tab = tab; Panel = panel; Text = text;
            }
        

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

}

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
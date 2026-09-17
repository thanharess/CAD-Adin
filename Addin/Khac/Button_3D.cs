using Autocad_addin.Framework;
using Autodesk.Windows;

namespace Autocad_addin.Addin_Autocad.Button
{
    public static class Button_3D
    {
        [RibbonDropDown("MY TOOLS", "3D", "Box",
            ToolTip = "Vẽ khối 3D",
            Size = RibbonItemSize.Large,
            Icon = "A1.png",
            LargeIcon = "A2.png",
            Order = 1)]
        public static void Box() { }

        [RibbonDropItem("Box", "Cylinder", ToolTip = "Vẽ hình trụ", Icon = "A2.png", Order = 1)]
        public static void Cylinder() { }

        [RibbonDropItem("Box", "Cone", ToolTip = "Vẽ hình nón", Icon = "A2.png", Order = 2)]
        public static void Cone() { }

        [RibbonDropItem("Box", "Sphere", ToolTip = "Vẽ hình cầu", Icon = "A2.png", Order = 3)]
        public static void Sphere() { }

        [RibbonDropItem("Box", "Pyramid", ToolTip = "Vẽ hình chóp", Icon = "A2.png", Order = 4)]
        public static void Pyramid() { }

        [RibbonDropItem("Box", "Wedge", ToolTip = "Vẽ hình nêm", Icon = "A2.png", Order = 5)]
        public static void Wedge() { }

        [RibbonDropItem("Box", "Torus", ToolTip = "Vẽ hình xuyến", Icon = "A2.png", Order = 6)]
        public static void Torus() { }

        [RibbonDropItem("Box", "Polysolid", ToolTip = "Vẽ polysolid", Icon = "A2.png", Order = 7)]
        public static void Polysolid() { }


  
    }

}
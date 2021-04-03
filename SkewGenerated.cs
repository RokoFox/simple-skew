using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Reflection;
using System.Drawing.Text;
using System.Windows.Forms;
using System.IO.Compression;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Registry = Microsoft.Win32.Registry;
using RegistryKey = Microsoft.Win32.RegistryKey;
using PaintDotNet;
using PaintDotNet.AppModel;
using PaintDotNet.Clipboard;
using PaintDotNet.IndirectUI;
using PaintDotNet.Collections;
using PaintDotNet.PropertySystem;
using PaintDotNet.Effects;
using ColorWheelControl = PaintDotNet.ColorBgra;
using AngleControl = System.Double;
using PanSliderControl = PaintDotNet.Pair<double,double>;
using FilenameControl = System.String;
using ReseedButtonControl = System.Byte;
using RollControl = System.Tuple<double, double, double>;
using IntSliderControl = System.Int32;
using CheckboxControl = System.Boolean;
using TextboxControl = System.String;
using DoubleSliderControl = System.Double;
using ListBoxControl = System.Byte;
using RadioButtonControl = System.Byte;
using MultiLineTextboxControl = System.String;

[assembly: AssemblyTitle("Skew plugin for Paint.NET")]
[assembly: AssemblyDescription("Skew selected pixels")]
[assembly: AssemblyConfiguration("skew")]
[assembly: AssemblyCompany("Roko Lisica")]
[assembly: AssemblyProduct("Skew")]
[assembly: AssemblyCopyright("Copyright ©2021 by Roko Lisica")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.*")]

namespace SkewEffect
{
    public class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author
        {
            get
            {
                return base.GetType().Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
            }
        }

        public string Copyright
        {
            get
            {
                return base.GetType().Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
            }
        }

        public string DisplayName
        {
            get
            {
                return base.GetType().Assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
            }
        }

        public Version Version
        {
            get
            {
                return base.GetType().Assembly.GetName().Version;
            }
        }

        public Uri WebsiteUri
        {
            get
            {
                return new Uri("https://www.getpaint.net/redirect/plugins.html");
            }
        }
    }

    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "Skew")]
    public class SkewEffectPlugin : PropertyBasedEffect
    {
        public static string StaticName
        {
            get
            {
                return "Skew";
            }
        }

        public static Image StaticIcon
        {
            get
            {
                return null;
            }
        }

        public static string SubmenuName
        {
            get
            {
                return SubmenuNames.Distort;
            }
        }

        public SkewEffectPlugin()
            : base(StaticName, StaticIcon, SubmenuName, new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        public enum PropertyNames
        {
            Amount1,
            Amount2,
            Amount3,
            Amount4
        }


        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>();

            props.Add(new Int32Property(PropertyNames.Amount1, 0, 0, 100));
            props.Add(new Int32Property(PropertyNames.Amount2, 0, 0, 100));
            props.Add(new Int32Property(PropertyNames.Amount3, 0, 0, 100));
            props.Add(new Int32Property(PropertyNames.Amount4, 0, 0, 100));

            return new PropertyCollection(props);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyNames.Amount1, ControlInfoPropertyNames.DisplayName, "Top Left Offset (%)");
            configUI.SetPropertyControlValue(PropertyNames.Amount2, ControlInfoPropertyNames.DisplayName, "Bottom Left Offset (%)");
            configUI.SetPropertyControlValue(PropertyNames.Amount3, ControlInfoPropertyNames.DisplayName, "Top Right Offset (%)");
            configUI.SetPropertyControlValue(PropertyNames.Amount4, ControlInfoPropertyNames.DisplayName, "Bottom Right Offset (%)");

            return configUI;
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            // Add help button to effect UI
            props[ControlInfoPropertyNames.WindowHelpContentType].Value = WindowHelpContentType.PlainText;
            props[ControlInfoPropertyNames.WindowHelpContent].Value = "Skew v1,0\nCopyright ©2021 by Roko Lisica\nAll rights reserved.";
            base.OnCustomizeConfigUIWindowProperties(props);
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken token, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            Amount1 = token.GetProperty<Int32Property>(PropertyNames.Amount1).Value;
            Amount2 = token.GetProperty<Int32Property>(PropertyNames.Amount2).Value;
            Amount3 = token.GetProperty<Int32Property>(PropertyNames.Amount3).Value;
            Amount4 = token.GetProperty<Int32Property>(PropertyNames.Amount4).Value;

            base.OnSetRenderInfo(token, dstArgs, srcArgs);
        }

        protected override unsafe void OnRender(Rectangle[] rois, int startIndex, int length)
        {
            if (length == 0) return;
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                Render(DstArgs.Surface,SrcArgs.Surface,rois[i]);
            }
        }

        #region User Entered Code
        // Name: Skew
        // Submenu: Distort
        // Author: Roko Lisica
        // Title:
        // Version:
        // Desc:
        // Keywords:
        // URL:
        // Help:
        #region UICode
        IntSliderControl Amount1 = 0; // [0,100] Top Left Offset (%)
        IntSliderControl Amount2 = 0; // [0,100] Bottom Left Offset (%)
        IntSliderControl Amount3 = 0; // [0,100] Top Right Offset (%)
        IntSliderControl Amount4 = 0; // [0,100] Bottom Right Offset (%)
        #endregion
        
        void Render(Surface dst, Surface src, Rectangle rect)
        {
            Rectangle selection = EnvironmentParameters.SelectionBounds;
        
            int topLeftOffset = Amount1 * (selection.Bottom - selection.Top) / 100;
            int bottomLeftOffset = Amount2 * (selection.Bottom - selection.Top) / 100;
            int topRightOffset = Amount3 * (selection.Bottom - selection.Top) / 100;
            int bottomRightOffset = Amount4 * (selection.Bottom - selection.Top) / 100;
        
            int origHeight = selection.Bottom - selection.Top;
            int origWidth = selection.Right - selection.Left;
        
            if (topLeftOffset > origHeight - bottomLeftOffset)
            {
                topLeftOffset = origHeight - bottomLeftOffset - 1;
            }
        
            if (topRightOffset > origHeight - bottomRightOffset)
            {
                topRightOffset = origHeight - bottomRightOffset - 1;
            }
        
            ColorBgra currentPixel;
            for (int x = rect.Left; x < rect.Right; x++)
            {
                //int percentLeft = (rect.Right - x) / (double) origWidth;
                int currStart = selection.Top + (selection.Right - x) * topLeftOffset / origWidth
                    + (x - selection.Left) * topRightOffset / origWidth;
                int currEnd = selection.Bottom - (selection.Right - x) * bottomLeftOffset / origWidth
                    - (x - selection.Left) * bottomRightOffset / origWidth;
                int currHeight = currEnd - currStart;
        
                if (IsCancelRequested) return;
                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    int yOld = (y - currStart) * origHeight / currHeight + selection.Top;
                    if (yOld < 0 || yOld >= src.Size.Height)
                    {
                        dst[x,y] = ColorBgra.Transparent;
                    }
                    else {
                        dst[x,y] = src[x,yOld];
                    }
                    
                }
            }
        }
        
        #endregion
    }
}

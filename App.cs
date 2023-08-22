
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RevitParametersAddin
{
    class App : IExternalApplication
    {
        const string RIBBON_TAB = "ACC Parameters";
        const string RIBBON_PANEL = "ACC Parameters Panel";
        public Result OnStartup(UIControlledApplication a)
        {
            try
            {
                a.CreateRibbonTab(RIBBON_TAB);
            }
            catch (Exception)
            {

            }
            RibbonPanel panel = null;
            List<RibbonPanel> panels = a.GetRibbonPanels(RIBBON_TAB);
            foreach (RibbonPanel pnl in panels)
            {
                if (pnl.Name == RIBBON_PANEL)
                {
                    panel = pnl;
                    break;
                }
            }
            if (panel == null)
            {
                panel = a.CreateRibbonPanel(RIBBON_TAB, RIBBON_PANEL);
            }
            Image img = Properties.Resources._5737041_32px_app_home_line_media_icon;
            ImageSource imgSrc = GetImageSource(img);

            PushButtonData btnData = new PushButtonData(
                "ACC Parameters",
                "ACC Parameters",
                Assembly.GetExecutingAssembly().Location,
                "RevitParametersAddin.Command"
                )
            {
                ToolTip = "Button to initiate ACC Parameters",
                LongDescription = "Button to initiate Parameters API from ACC",
                Image = imgSrc,
                LargeImage = imgSrc
            };
            PushButton btn = panel.AddItem(btnData) as PushButton;
            btn.Enabled = true;

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }

        private BitmapSource GetImageSource(Image img)
        {
            BitmapImage bmp = new BitmapImage();
            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = null;
                bmp.StreamSource = ms;

                bmp.EndInit();
            }
            return bmp;
        }
    }
}

using System.Windows;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BetterNetwork
{
    public class CustomWindowManager : WindowManager
    {
        // http://www.mindscapehq.com/blog/index.php/2012/03/13/caliburn-micro-part-5-the-window-manager/
        protected override Window EnsureWindow(object model, object view, bool isDialog)
        {
            Window window = base.EnsureWindow(model, view, isDialog);

            //window.SizeToContent = SizeToContent.Manual;
            window.ResizeMode = ResizeMode.NoResize;
            window.Title = "Better Network";

            var url = new Uri("pack://application:,,,/BetterNetwork;component/Images/Icon.ico",UriKind.RelativeOrAbsolute);
            window.Icon = BitmapFrame.Create(url);

            return window;
        }
    }
}

using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterNetwork.ViewModels
{
    [Export(typeof(AboutViewModel))]
    public class AboutViewModel
    {
        private readonly IWindowManager _windowManager;
        [ImportingConstructor]
        public AboutViewModel(IWindowManager windowManager)
        {
            _windowManager = windowManager;
        }
    }
}

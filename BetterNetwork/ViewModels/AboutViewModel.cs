using Caliburn.Micro;
using System.ComponentModel.Composition;

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

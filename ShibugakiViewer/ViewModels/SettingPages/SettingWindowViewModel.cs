using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShibugakiViewer.ViewModels.SettingPages
{
    class SettingWindowViewModel : IDisposable
    {
        public void Dispose()
        {
            ((App)Application.Current).Core.SaveApplicationSettings();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Boredbone.Utility.Notification;
using Reactive.Bindings.Extensions;

namespace ShibugakiViewer.ViewModels.SettingPages
{
    class SettingWindowViewModel : NotificationBase
    {

        public ViewerSettingPageViewModel SettingViewModel
        {
            get { return _fieldSettingViewModel; }
            set
            {
                if (_fieldSettingViewModel != value)
                {
                    _fieldSettingViewModel = value;
                    RaisePropertyChanged(nameof(SettingViewModel));
                }
            }
        }
        private ViewerSettingPageViewModel _fieldSettingViewModel;


        public SettingWindowViewModel()
        {
            this.SettingViewModel = new ViewerSettingPageViewModel().AddTo(this.Disposables);

            Disposable.Create(() => ((App)Application.Current).Core.SaveApplicationSettings())
                .AddTo(this.Disposables);
        }
    }
}

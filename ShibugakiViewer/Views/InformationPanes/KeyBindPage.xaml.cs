using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Boredbone.Utility.Notification;
using Boredbone.XamlTools;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using WpfTools;

namespace ShibugakiViewer.Views.InformationPanes
{
    /// <summary>
    /// KeyBindPage.xaml の相互作用ロジック
    /// </summary>
    public partial class KeyBindPage : UserControl,IDisposable
    {
        public KeyBindPage()
        {
            InitializeComponent();
            this.rootGrid.DataContext = new KeyBindPageViewModel();
        }

        public void Dispose()
        {
            (this.DataContext as IDisposable)?.Dispose();
        }
    }

    public class KeyBindPageViewModel : DisposableBase
    {

        public ReactivePropertySlim<int> CursorKeyBind { get; }
        public ReadOnlyReactivePropertySlim<Visibility> CursorKeyToMoveVisibility { get; }
        public ReadOnlyReactivePropertySlim<Visibility> CursorKeyToFlipVisibility { get; }

        
        public KeyBindPageViewModel()
        {
            var core = ((App)Application.Current).Core;

            this.CursorKeyBind = core
                .ToReactivePropertySlimAsSynchronized(x => x.CursorKeyBind).AddTo(this.Disposables);

            this.CursorKeyToFlipVisibility = this.CursorKeyBind
                .Select(x => VisibilityHelper.Set(x != 1))
                .ToReadOnlyReactivePropertySlim().AddTo(this.Disposables);

            this.CursorKeyToMoveVisibility = this.CursorKeyBind
                .Select(x => VisibilityHelper.Set(x == 1))
                .ToReadOnlyReactivePropertySlim().AddTo(this.Disposables);
            
        }
    }
}

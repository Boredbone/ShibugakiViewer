using ImageLibrary.Tag;
using Microsoft.Xaml.Behaviors;
using ShibugakiViewer.Models.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ShibugakiViewer.Views.Behaviors
{
    public class TagComboBoxBehavior : Behavior<ComboBox>
    {
        bool isSubscribing = false;
        ScrollViewer innerScrollViewer = null;

        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.GotFocus += (_, __) =>
            {
                if (isSubscribing)
                {
                    return;
                }
                innerScrollViewer = this.AssociatedObject.Template
                    .FindName("DropDownScrollViewer", this.AssociatedObject) as ScrollViewer;

                if (innerScrollViewer != null)
                {
                    innerScrollViewer.PreviewKeyUp += OnKeyInput;
                    isSubscribing = true;
                }
            };
            this.AssociatedObject.Unloaded += (_, __) =>
            {
                if (innerScrollViewer != null && isSubscribing)
                {
                    innerScrollViewer.PreviewKeyUp -= OnKeyInput;
                    isSubscribing = false;
                }
            };
        }

        private void OnKeyInput(object obj, KeyEventArgs e)
        {
            if (!(obj is ScrollViewer sv))
            {
                return;
            }
            if (this.AssociatedObject.ItemsSource == null)
            {
                return;
            }
            if (!KeyBoardHelper.KeyToChar(e.Key, out var c))
            {
                return;
            }

            var desiredIndex = -1;
            int index = 0;
            foreach (var item in this.AssociatedObject.ItemsSource)
            {
                if (item is KeyValuePair<int, TagInformation> tag
                    && tag.Value != null
                    && tag.Value.Name.Length > 0
                    && char.ToLower(tag.Value.Name[0]) >= c)
                {
                    desiredIndex = index;
                    break;
                }
                index++;
            }
            if (desiredIndex >= 0)
            {
                sv.ScrollToVerticalOffset(desiredIndex);
            }

            //System.Diagnostics.Debug.WriteLine($"key={e.Key}");
        }
    }
}

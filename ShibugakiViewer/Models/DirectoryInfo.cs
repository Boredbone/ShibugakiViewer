using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageLibrary.Core;
using Reactive.Bindings;

namespace ShibugakiViewer.Models
{

    public class DirectoryInfo
    {
        private static int id;

        public List<KeyValuePair<string, TreeNode<string>>> Children { get; set; }
        public ReactiveProperty<int> SelectedIndex { get; private set; }
        public int ID { get; private set; }

        public DirectoryInfo(List<KeyValuePair<string, TreeNode<string>>> source)
        {
            this.Children = new List<KeyValuePair<string, TreeNode<string>>>();
            this.Children.Add(new KeyValuePair<string, TreeNode<string>>("", null));
            this.Children.AddRange(source);
            this.SelectedIndex = new ReactiveProperty<int>(-1);
            this.ID = id;
            id++;
        }

        public string GetSelectedLabel()
        {
            if (this.SelectedIndex.Value < 0 || this.SelectedIndex.Value >= this.Children.Count)
            {
                return null;
            }

            var str = this.Children[this.SelectedIndex.Value].Key;

            return str.Length > 0 ? str : null;
        }

        public void Choice(string key)
        {
            var k = key.ToLower();
            this.SelectedIndex.Value = this.Children.FindIndex(x => x.Key.ToLower().Equals(k));
        }
    }
}

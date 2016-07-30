using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;

namespace ImageLibrary.Core
{
    public class TreeNode<T>
    {
        private TreeNode<T> Parent { get; set; }
        private Dictionary<T, TreeNode<T>> Children { get; set; }

        public TreeNode(TreeNode<T> parent)
        {
            this.Parent = parent;
            this.Children = new Dictionary<T, TreeNode<T>>();
        }

        public bool TryGetChild(T key, out TreeNode<T> value)
        {
            return this.Children.TryGetValue(key, out value);
        }

        public TreeNode<T> AddChild(T item)
        {
            this.Children[item] = new TreeNode<T>(this);
            return this.Children[item];
        }

        public Dictionary<T, TreeNode<T>> GetChildren()
        {
            return new Dictionary<T, TreeNode<T>>(this.Children);
        }

        public T GetKey()
        {
            if (this.Parent == null)
            {
                return default(T);
            }
            var item = this.Parent.Children.FirstOrNull(x => x.Value == this);
            if (item == null)
            {
                return default(T);
            }
            return item.Value.Key;
        }
    }

    public class DirectoryTreeAnalyzer
    {

        public TreeNode<string> Analyze(IEnumerable<string> pathCollection)
        {
            var root = new Folder("");
            var separator = System.IO.Path.DirectorySeparatorChar;
            var header = separator.ToString();

            foreach (var path in pathCollection)
            {
                if (path == null || path.Length <= 0)
                {
                    continue;
                }
                var items = path
                    .Split(separator, System.IO.Path.AltDirectorySeparatorChar)
                    .ToList();

                if (items[items.Count - 1].Length > 0)
                {
                    items.Add("");
                }

                var target = root;

                for (int i = 0; i < items.Count; i++)
                {
                    var key = (i > 0) ? (header + items[i]) : items[i];

                    if (!target.Children.ContainsKey(key))
                    {
                        target.Children.Add(key, new Folder(key));
                    }
                    target = target.Children[key];

                }

                //最後のフォルダはファイルを持っている
                target.HasFile = true;
            }

            foreach (var directory in root.Children)
            {
                directory.Value.Compress();
            }


            var treeRoot = new TreeNode<string>(null);

            foreach (var child in root.Children)
            {
                child.Value.ToTreeNode(treeRoot);
            }

            return treeRoot;
        }



        private class Folder
        {
            public bool HasFile { get; set; }
            public Dictionary<string, Folder> Children { get; }
            public string Name { get; private set; }

            public Folder(string name)
            {
                this.Name = name;
                Children = new Dictionary<string, Folder>();
            }

            public void Compress()
            {
                while (!this.HasFile && this.Children.Count == 1)
                {
                    var child = this.Children.First().Value;
                    this.Name = this.Name + child.Name;
                    this.HasFile = child.HasFile;

                    this.Children.Clear();
                    foreach (var c in child.Children)
                    {
                        this.Children.Add(c.Key, c.Value);
                    }

                }

                foreach (var directory in this.Children)
                {
                    directory.Value.Compress();
                }
            }

            public TreeNode<string> ToTreeNode(TreeNode<string> parent)
            {
                var node = parent.AddChild(this.Name);

                foreach (var child in this.Children)
                {
                    child.Value.ToTreeNode(node);
                }
                return node;
            }
        }
    }

}

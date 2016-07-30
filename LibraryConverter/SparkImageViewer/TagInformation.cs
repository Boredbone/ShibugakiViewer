using System;
using System.Collections.Generic;
using System.Text;

namespace SparkImageViewer.DataModel
{
    public class TagInformation
    {
        public string Name { get; set; }
        public string Shortcut { get; set; }

        public TagInformation()
        {
            Name = "";
            Shortcut = "";
        }

    }
}

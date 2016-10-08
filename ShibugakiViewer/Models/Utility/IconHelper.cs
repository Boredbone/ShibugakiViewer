using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShibugakiViewer.Models.Utility
{
    public class IconHelper
    {
        public static System.Drawing.Icon CreateIcon(string path, int size)
        {
            try
            {
                Uri uri;
                if (!Uri.TryCreate(path, UriKind.Absolute, out uri))
                {
                    return null;
                }

                return CreateIcon(uri, size);
            }
            catch
            {
                return null;
            }
        }

        public static System.Drawing.Icon CreateIcon(Uri uri, int size)
        {
            try
            {
                var streamResourceInfo = Application.GetResourceStream(uri);
                if (streamResourceInfo == null)
                {
                    return null;
                }

                System.Drawing.Icon icon;

                using (var stream = streamResourceInfo.Stream)
                {
                    icon = new System.Drawing.Icon(stream, new System.Drawing.Size(size, size));
                }

                return icon;
            }
            catch
            {
                return null;
            }
        }
    }
}

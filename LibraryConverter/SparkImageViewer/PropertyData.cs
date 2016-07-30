using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikanViewer.PropertySearch
{
    public class PropertyData<T>
    {
        public Func<T, IComparable> GetComparableProperty { get; private set; }
        //public Func<FileInformation, object> GetProperty { get; private set; }
        public Func<T, object, bool> IsCorrect { get; private set; }
        public bool IsComparable { get; private set; }

        public PropertyData(Func<T, object, bool> func)
        {
            this.GetComparableProperty = null;
            this.IsCorrect = func;
            this.IsComparable = false;

        }
        public PropertyData(Func<T, IComparable> func)
        {

            this.GetComparableProperty = func;
            //this.GetProperty = func;
            this.IsCorrect = null;
            this.IsComparable = true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MikanViewer.PropertySearch
{
    public class SortMethod<T>
    {
        public bool ByDescending { get; set; }
        private PropertyData<T> usingProperty;


        public SortMethod(PropertyData<T> property, bool byDescending)
        {
            this.usingProperty = property;
            this.ByDescending = byDescending;
        }


        public IComparable GetComparableProperty(T obj)
        {
            return this.usingProperty.GetComparableProperty(obj);
        }
    }
}

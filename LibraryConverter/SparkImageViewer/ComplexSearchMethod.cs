//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace MikanViewer.PropertySearch
//{

//    public class ComplexSearchMethod<T> : ISearchMethod<T>
//    {
//        private List<ISearchMethod<T>> methods;
//        public bool IsOr { get; set; }

//        public ComplexSearchMethod(bool or)
//        {
//            this.methods = new List<ISearchMethod<T>>();
//            this.IsOr = or;
//        }


//        public void Add(ISearchMethod<T> method)
//        {
//            this.methods.Add(method);
//        }

//        public bool IsCorrect(T obj)
//        {
//            if (methods.Count <= 0)
//            {
//                return true;
//            }

//            if (this.IsOr)
//            {
//                foreach (var elm in methods)
//                {
//                    if (elm.IsCorrect(obj))
//                    {
//                        return true;
//                    }
//                }
//                return false;
//            }
//            else
//            {
//                foreach (var elm in methods)
//                {
//                    if (!elm.IsCorrect(obj))
//                    {
//                        return false;
//                    }
//                }
//                return true;
//            }
//        }
//    }
//}

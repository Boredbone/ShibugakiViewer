using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImageLibrary.File
{


    class SequenceNumber
    {

        public string PreNameLong { get; set; }
        //public string NameNumberRightString { get; set; }
        public string PostNameShort { get; set; }
        public int NameNumberRight { get; set; }

        public string PreNameShort { get; set; }
        //public string NameNumberLeftString { get; set; }
        public string PostNameLong { get; set; }
        public int NameNumberLeft { get; set; }

        public string Extension { get; set; }
        public int NameLength { get; set; }

        public string Name { get; set; }


        private static Regex regexRight = new Regex(@"\d+", RegexOptions.RightToLeft);
        private static Regex regexLeft = new Regex(@"\d+");

        public SequenceNumber(string str)
        {
            this.Name = str;

            //var sname = Path.GetFileNameWithoutExtension(str);
            this.Extension = Path.GetExtension(str);

            var name = str.Substring(0, str.Length - this.Extension.Length);

            this.NameLength = name.Length;

            //Right
            {
                int number;
                var nameNumberRightString = regexRight.Match(name).ToString();
                Int32.TryParse(nameNumberRightString, out number);
                this.NameNumberRight = number;

                var sp = regexRight.Split(name, 2);


                if (sp.Length > 0)
                {
                    PreNameLong = sp[0];
                }
                else
                {
                    PreNameLong = "";
                }

                if (sp.Length > 2)
                {
                    PostNameShort = string.Join("", sp.Skip(1));
                }
                else if (sp.Length > 1)
                {
                    PostNameShort = sp[1];
                }
                else
                {
                    PostNameShort = "";
                }
            }

            //Left
            {
                int number;
                var nameNumberLeftString = regexLeft.Match(name).ToString();
                Int32.TryParse(nameNumberLeftString, out number);
                this.NameNumberLeft = number;

                var sp = regexLeft.Split(name, 2);


                if (sp.Length > 0)
                {
                    PreNameShort = sp[0];
                }
                else
                {
                    PreNameShort = "";
                }

                if (sp.Length > 2)
                {
                    PostNameLong = string.Join("", sp.Skip(1));
                }
                else if (sp.Length > 1)
                {
                    PostNameLong = sp[1];
                }
                else
                {
                    PostNameLong = "";
                }
            }
        }
        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary.Search
{

    [DataContract]
    public class SearchReferences
    {

        [DataMember(EmitDefaultValue = false)]
        public double? Num
        {
            get { return _fieldNum; }
            set
            {
                if (_fieldNum != value)
                {
                    _fieldNum = value;
                    this.IsEdited = true;
                }
            }
        }
        private double? _fieldNum = null;

        [DataMember(EmitDefaultValue = false)]
        public string? Str
        {
            get { return _fieldStr; }
            set
            {
                if (_fieldStr != value)
                {
                    _fieldStr = value;
                    this.IsEdited = true;
                }
            }
        }
        private string? _fieldStr = null;

        public bool IsNull => (!this.Num.HasValue && (this.Str is null));

        public int Num32 => (this.Num.HasValue) ? ((int)this.Num.Value) : 0;
        public long Num64 => (this.Num.HasValue) ? ((long)this.Num.Value) : 0;
        public double NumDouble => (this.Num.HasValue) ? (this.Num.Value) : 0.0;

        public DateTimeOffset DateTime => DateTimeOffset.FromUnixTimeSeconds(this.Num64).ToLocalTime();

        public bool IsEdited { get; private set; } = false;

        public override string? ToString()
        {
            if (this.Str is not null)
            {
                return this.Str;
            }
            if (this.Num.HasValue)
            {
                return this.Num.Value.ToString("0.###");
            }
            return null;
        }

        public void CopyFrom(SearchReferences source)
        {
            this.Num = source.Num;
            this.Str = source.Str;
        }

        public static SearchReferences From(int val)
        {
            return From((double)val);
        }

        public static SearchReferences From(long val)
        {
            return From((double)val);
        }

        public static SearchReferences From(double val)
        {
            var obj = new SearchReferences()
            {
                Num = val,
            };
            obj.IsEdited = false;
            return obj;
        }

        public static SearchReferences From(DateTimeOffset val)
        {
            var obj = new SearchReferences()
            {
                Num = val.ToUnixTimeSeconds(),
            };
            obj.IsEdited = false;
            return obj;
        }
        public static SearchReferences FromUnixTime(long val)
        {
            return From(DateTimeOffset.FromUnixTimeSeconds(val));
        }
        public static SearchReferences From(string val)
        {
            var obj = new SearchReferences()
            {
                Str = val,
            };
            obj.IsEdited = false;
            return obj;
        }


        public static SearchReferences ConvertFrom(object? val)
        {
            switch (val)
            {
                case double dn:
                    return SearchReferences.From(dn);
                case float fn:
                    return SearchReferences.From(fn);
                case int i32:
                    return SearchReferences.From(i32);
                case long i64:
                    return SearchReferences.From(i64);
                case uint ui32:
                    return SearchReferences.From(ui32);
                case ulong ui64:
                    return SearchReferences.From(ui64);
                case string str:
                    return SearchReferences.From(str);
                case DateTimeOffset dto:
                    return SearchReferences.From(dto);
                case DateTime dt:
                    return SearchReferences.From((DateTimeOffset)dt);
            }
            
            var txt = val?.ToString();
            return (txt != null) ? From(txt) : (new SearchReferences());
        }


    }
}

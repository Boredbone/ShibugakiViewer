using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Table
{

    internal class TableColumnDefinition
    {
        /// <summary>
        /// cid: id of the column
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// name: the name of the column
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// type: the type of the column
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// notnull: 0 or 1 if the column can contains null values
        /// </summary>
        public bool IsNotNull { get; set; }

        /// <summary>
        /// dflt_value: the default value
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// pk: 0 or 1 if the column partecipate to the primary key
        /// </summary>
        public bool IsPrimaryKey { get; set; }



        public static TableColumnDefinition FromArray(object[] array)
        {
            if (array == null || array.Length < 3)
            {
                throw new ArgumentException("Invalid source");
            }

            var id = System.Convert.ToInt64(array[0]);
            var name = System.Convert.ToString(array[1]);
            var type = System.Convert.ToString(array[2]);
            var isNotNull = (array.Length >= 4) ? (System.Convert.ToInt64(array[3]) > 0) : false;
            var defaultValue = (array.Length >= 5) ? array[4] : null;
            var isPrimaryKey = (array.Length >= 6) ? (System.Convert.ToInt64(array[5]) > 0) : false;

            return new TableColumnDefinition()
            {
                Id = id,
                Name = name,
                Type = type,
                IsNotNull = isNotNull,
                DefaultValue = defaultValue,
                IsPrimaryKey = isPrimaryKey,
            };
        }

        public override string ToString()
        {
            return $"ID = {this.Id}, Name = {this.Name}, Type = {this.Type}"
                + $", IsNotNull = {this.IsNotNull}, Default = {this.DefaultValue}"
                + $", PrimaryKey = {this.IsPrimaryKey}";
        }
    }
}

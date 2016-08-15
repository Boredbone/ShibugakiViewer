using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Table
{
    public class TableInformation : IRecord<int>
    {
        [RecordMember]
        public int Id { get; set; }

        [RecordMember]
        public int Version { get; set; }

        [RecordMember]
        public string TableName { get; set; }

        [RecordMember]
        public DateTimeOffset Created { get; set; }

        [RecordMember]
        public DateTimeOffset Modified { get; set; }
        
    }
}

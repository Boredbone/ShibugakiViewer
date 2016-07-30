using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Table
{
    public interface IRecord<T>
    {
        T Id { get; }
    }
}

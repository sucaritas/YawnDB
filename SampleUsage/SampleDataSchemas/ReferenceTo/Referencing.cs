using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YawnDB
{
    partial class Referencing<T> : YawnDB.ReferenceTo<T> where T : YawnDB.YawnSchema
    {
    }
}

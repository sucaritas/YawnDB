using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YawnDB.Interfaces
{
    public interface IIdexArguments
    {
        IList<string> IndexParams { get; }
        IDictionary<string, object> IndexStartValue { get; }
        IDictionary<string, object> IndexEndValue { get; }
    }
}

// <copyright file="IIdexArguments.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Index
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public enum IndexArgumentOperation
    {
        Equals,
        NotEquals,
        LessThan,
        MoreThan,
        ContainedIn,
        NotContainedIn
    }

    public interface IIdexArguments
    {
        IList<string> IndexParams { get; }

        IDictionary<string, object> Value1 { get; }

        IDictionary<string, object> Value2 { get; }
    }
}

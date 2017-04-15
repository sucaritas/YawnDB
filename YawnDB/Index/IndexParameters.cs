// <copyright file="IndexParameters.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Index
{
    using System.Collections.Generic;
    using YawnDB.Storage;

    public class IndexParameters : IIdexArguments
    {
        public IList<string> IndexParams { get; set; }

        public IDictionary<string, object> Value1 { get; set; }

        public IDictionary<string, object> Value2 { get; set; }
    }
}

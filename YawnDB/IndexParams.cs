namespace YawnDB
{
    using System.Collections.Generic;
    using YawnDB.Interfaces;

    public class IndexParameters : IIdexArguments
    {
        public IList<string> IndexParams { get; set; }
        public IDictionary<string, object> Value1 { get; set; }
        public IDictionary<string, object> Value2 { get; set; }
    }
}

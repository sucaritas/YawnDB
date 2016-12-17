namespace YawnDB
{
    using System.Collections.Generic;
    using YawnDB.Interfaces;

    public class IndexParameters : IIdexArguments
    {
        public IList<string> IndexParams { get; }
        public IDictionary<string, object> IndexStartValue { get; }
        public IDictionary<string, object> IndexEndValue { get; }
    }
}

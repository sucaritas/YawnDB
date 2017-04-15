// <copyright file="RecordLockPair.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Locking
{
    using System.Threading;
    using YawnDB.Locking;

    public class RecordLockPair : IRecordLockPair
    {
        public string Id { get; set; }

        public object Reader { get; set; } = new object();

        public object Writer { get; set; } = new object();
    }
}

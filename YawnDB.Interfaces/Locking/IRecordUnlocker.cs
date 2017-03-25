// <copyright file="IRecordUnlocker.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Locking
{
    using System;

    public interface IRecordUnlocker : IDisposable
    {
        IRecordLockPair SyncCounter { get; }

        IRecordLocker RecordLocker { get; }

        RecordLockType RecordLockType { get; }
    }
}

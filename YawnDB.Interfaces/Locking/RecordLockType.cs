// <copyright file="RecordLockType.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Locking
{
    using System;

    [Flags]
    public enum RecordLockType
    {
        Read = 0x01,
        Write = 0x02,
        ReadWrite = 0x04
    }
}

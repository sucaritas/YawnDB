// <copyright file="IRecordLockPair.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Locking
{
    public interface IRecordLockPair
    {
        string Id { get; set; }

        object Reader { get; set; }

        object Writer { get; set; }
    }
}

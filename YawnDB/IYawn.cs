// <copyright file="IYawn.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using YawnDB.Locking;
    using YawnDB.Storage;
    using YawnDB.Transactions;

    public interface IYawn
    {
        bool TransactionsEnabled { get; }

        string DatabaseName { get; }

        string DefaultStoragePath { get; }

        ConcurrentDictionary<Type, IReference> RegisteredTypes { get; }

        ConcurrentDictionary<Type, IStorage> RegisteredStorageTypes { get; }

        IRecordLocker RecordLocker { get; }

        bool RegisterSchema<T>(IStorage storage) where T : YawnSchema;

        bool UnRegisterSchema(Type schemaToUnregister);

        bool TryGetSchemaReference(Type schemaType, out IReference schemaRef);

        bool TryGetStorage(Type schemaType, out IStorage storage);

        void Close();

        void Open(bool enableTransactions);

        ITransaction CreateTransaction();

        void ReplayTransactionLog();

        void PurgeTransactionLog();

        StorageLocation SaveRecord(YawnSchema instance);

        StorageLocation SaveRecord(YawnSchema instance, ITransaction transaction);

        bool DeleteRecord(YawnSchema instance);

        bool DeleteRecord(YawnSchema instance, ITransaction transaction);

        T GetRecord<T>(long id) where T : YawnSchema;

        IRecordUnlocker LockRecord<T>(long id, RecordLockType lockType) where T : YawnSchema;

        IRecordUnlocker LockRecord(long id, RecordLockType lockType, Type schemaType);

        IRecordUnlocker LockRecord(string id, RecordLockType lockType);

        string GetLockName<T>(long id);

        string GetLockName(long id, Type type);
    }
}

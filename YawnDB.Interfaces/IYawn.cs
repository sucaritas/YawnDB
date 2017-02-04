namespace YawnDB.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IYawn
    {
        string DatabaseName { get; }

        string DefaultStoragePath { get; }

        ConcurrentDictionary<Type, IReference> RegisteredTypes { get; }

        ConcurrentDictionary<Type, IStorage> RegisteredStorageTypes { get; }

        bool RegisterSchema<T>(IStorageOf<T> storage) where T : YawnSchema;

        bool UnRegisterSchema(Type schemaToUnregister);

        bool TryGetSchemaReference(Type schemaType, out IReference schemaRef);

        bool TryGetStorage(Type schemaType, out IStorage storage);

        void Close();

        void Open();
    }
}

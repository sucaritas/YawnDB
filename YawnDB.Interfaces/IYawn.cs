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

        bool RegisterSchema<T>(IReferenceTo<T> referenceInstance) where T: YawnSchema;

        bool UnRegisterSchema(Type schemaToUnregister);

        bool TryGetSchemaReference(Type schemaType, out IReference schemaRef);

        bool TryGetStorage(Type schemaType, out IStorage storage);

        void Close();
    }
}

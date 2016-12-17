﻿namespace YawnDB
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using YawnDB.Interfaces;
    using YawnDB.Storage.BlockStorage;

    public class Yawn : IYawn
    {
        public string DatabaseName { get; } = string.Empty;
        public string DefaultStoragePath { get; } = @".\";

        public Yawn(string databaseName, string defaultStoragePath)
        {
            this.DatabaseName = databaseName;
            this.DefaultStoragePath = defaultStoragePath;
        }

        public ConcurrentDictionary<Type, IReference> RegisteredTypes { get; } = new ConcurrentDictionary<Type, IReference>();
        public ConcurrentDictionary<Type, IStorage> RegisteredStorageTypes { get; } = new ConcurrentDictionary<Type, IStorage>();

        public bool RegisterSchema<T>() where T : YawnSchema
        {
            return this.RegisterSchema<T>(new ReferenceTo<T>());
        }

        public bool RegisterSchema<T>(IReferenceTo<T> referenceInstance) where T : YawnSchema
        {
            IStorageOf<T> storage = new BlockStorage<T>(this);
            return this.RegisterSchema(referenceInstance, storage);
        }

        public bool RegisterSchema<T>(IReferenceTo<T> referenceInstance, IStorageOf<T> storage) where T : YawnSchema
        {
            var schemaType = typeof(T);
            if (RegisteredTypes.TryAdd(schemaType, referenceInstance))
            {
                if (this.RegisteredStorageTypes.TryAdd(schemaType, storage))
                {
                    referenceInstance.SetYawnSite(this);
                    return true;
                }
                else
                {
                    IReference temp;
                    RegisteredTypes.TryRemove(schemaType, out temp);
                }
            }

            return false;
        }

        public bool UnRegisterSchema<T>()
        {
            return UnRegisterSchema(typeof(T));
        }

        public bool UnRegisterSchema(Type schemaToUnregister)
        {
            IReference schemaRef;
            if (RegisteredTypes.TryRemove(schemaToUnregister, out schemaRef))
            {
                schemaRef.ResetYawnSite();
                return true;
            }

            return false;
        }

        public bool TryGetSchemaReference(Type schemaType, out IReference schemaRef)
        {
            return RegisteredTypes.TryGetValue(schemaType, out schemaRef);
        }

        public bool TryGetStorage(Type schemaType, out IStorage storage)
        {
            return RegisteredStorageTypes.TryGetValue(schemaType, out storage);
        }

        public T CreateRecord<T>() where T : YawnSchema
        {
            IStorage storage;
            RegisteredStorageTypes.TryGetValue(typeof(T), out storage);
            return (storage as IStorageOf<T>).CreateRecord().Result;
        }

        public Task<IStorageLocation> SaveRecord<T>(T instance) where T : YawnSchema
        {
            IStorage storage;
            RegisteredStorageTypes.TryGetValue(typeof(T), out storage);
            return (storage as IStorageOf<T>).SaveRecord(instance);
        }

        public void Close()
        {
            foreach(var storage in RegisteredStorageTypes.Values)
            {
                if (storage != null)
                {
                    storage.Close();
                }
            }
        }
    }
}
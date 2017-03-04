﻿namespace YawnDB.Index.HashKey
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Reflection;
    using System.IO;

    using Bond;
    using Bond.Protocols;
    using Bond.IO.Unsafe;

    using YawnDB.Interfaces;
    using YawnDB.Storage;

    public partial class HashKeyIndex : IIndex
    {
        public string Name { get; set; }
        public Type StorageLocationType { get; set; }
        public IList<IndexParameter> IndexParameters { get; } = new List<IndexParameter>();
        public ConcurrentDictionary<string, IStorageLocation> IndexData = new ConcurrentDictionary<string, IStorageLocation>();
        private string FolderPath;
        private string FilePath;

        public bool SetIndex(YawnSchema objectToIndex, IStorageLocation dataToIndex)
        {
            if (objectToIndex == null || dataToIndex == null)
            {
                return false;
            }

            var key = this.Getkey(objectToIndex);
            if (key != null)
            {
                this.IndexData[key] = dataToIndex;
                return true;
            }

            return false;
        }

        public bool DeleteIndex(YawnSchema objectToIndex)
        {
            
            if (objectToIndex == null)
            {
                return true;
            }

            var key = this.Getkey(objectToIndex);
            if (key != null)
            {
                IStorageLocation val;
                return this.IndexData.TryRemove(key, out val);
            }
            
            return false;
        }

        public IEnumerable<IStorageLocation> GetStorageLocations(IIdexArguments inputParams)
        {
            IStorageLocation result;
            foreach (var key in this.GetKeyFromIndexArguments(inputParams))
            {
                if (IndexData.TryGetValue(key, out result))
                {
                    yield return result;
                }
                else
                {
                    continue;
                }
            }

            yield break;
        }

        public string Getkey(YawnSchema instance)
        {
            var key = string.Empty;
            foreach (var paramGetter in this.IndexParameters)
            {
                key += paramGetter.ParameterGetter.GetValue(instance).ToString();
            }

            return key;
        }

        public IStorageLocation GetLocationForInstance(YawnSchema instance)
        {
            var key = Getkey(instance);
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            IStorageLocation value;
            this.IndexData.TryGetValue(key, out value);
            return value;
        }

        private IEnumerable<string> GetKeyFromIndexArguments(IIdexArguments arguments)
        {
            var count = this.IndexParameters.Select(x => x.Name).Except(arguments.IndexParams);
            if (count.Any())
            {
                return Enumerable.Empty<string>();
            }

            List<string> keys = new List<string>(Enumerable.Range(0, this.GetMaxKeyCount(arguments)).Select(x=>string.Empty));

            foreach (var indexParam in this.IndexParameters)
            {
                var val1 = arguments.Value1[indexParam.Name] as IEnumerable;
                if (val1 != null)
                {
                    int i = 0;
                    foreach (var p in val1)
                    {
                        keys[i] = keys[i] + p.ToString();
                        i++;
                    }
                }
                else
                {
                    for (int i = 0; i < keys.Count; i++)
                    {
                        keys[i] = keys[i] + arguments.Value1[indexParam.Name].ToString();
                    }
                }
            }

            return keys;
        }

        private int GetMaxKeyCount(IIdexArguments arguments)
        {
            int max = 1;
            foreach(var arg in arguments.Value1)
            {
                int cnt = 0;
                var ien = (arg.Value as IEnumerable);
                foreach (var p in ien)
                {
                    cnt++;
                }

                if (cnt > max)
                {
                    max = cnt;
                }
            }
            return max;
        }

        public IEnumerable<IStorageLocation> EnumerateAllLocations()
        {
            var enumerator = this.IndexData.Values.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current == null)
                {
                    continue;
                }

                yield return enumerator.Current;
            }
        }

        public bool Initialize(string folderPath, bool shouldLoadFromDisk)
        {
            this.FolderPath = folderPath;
            this.FilePath = Path.Combine(this.FolderPath, this.Name + ".bin");
            if (shouldLoadFromDisk)
            {
                return LoadDataFromPath(this.FilePath);
            }

            return true;
        }

        public void Close(bool shouldSaveToDisk)
        {
            if (shouldSaveToDisk)
            {
                SaveDataToPath(this.FilePath);
            }
        }

        public bool SaveDataToPath(string path)
        {
            HashKeyStorageFormat storageFormat = new HashKeyStorageFormat();
            Serializer<CompactBinaryWriter<OutputBuffer>> schemaDeserializer = new Serializer<CompactBinaryWriter<OutputBuffer>>(this.StorageLocationType);

            foreach (var kv in this.IndexData)
            {
                var output = new OutputBuffer();
                var writer = new CompactBinaryWriter<OutputBuffer>(output);
                schemaDeserializer.Serialize(kv.Value, writer);
                byte[] bits = new byte[output.Data.Count];
                Buffer.BlockCopy(output.Data.Array, output.Data.Offset, bits, 0, output.Data.Count);
                storageFormat.StoredData.Add(kv.Key, new ArraySegment<byte>(bits));
            }

            var diskOutput = new OutputBuffer();
            var diskWriter = new CompactBinaryWriter<OutputBuffer>(diskOutput);
            Serializer<CompactBinaryWriter<OutputBuffer>> schemaSerializer = new Serializer<CompactBinaryWriter<OutputBuffer>>(typeof(HashKeyStorageFormat));
            schemaSerializer.Serialize(storageFormat, diskWriter);
            byte[] diskBits = new byte[diskOutput.Data.Count];
            Buffer.BlockCopy(diskOutput.Data.Array, diskOutput.Data.Offset, diskBits, 0, diskOutput.Data.Count);
            File.WriteAllBytes(path, diskBits);

            var diskInput = new InputBuffer(diskBits);
            var diskReader = new CompactBinaryReader<InputBuffer>(diskInput);
            Deserializer<CompactBinaryReader<InputBuffer>> deserializer = new Deserializer<CompactBinaryReader<InputBuffer>>(this.StorageLocationType);
            HashKeyStorageFormat diskData = deserializer.Deserialize<HashKeyStorageFormat>(diskReader);

            return true;
        }

        public bool LoadDataFromPath(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            var diskInput = new InputStream(new FileStream(this.FilePath, FileMode.Open));
            var diskReader = new CompactBinaryReader<InputStream>(diskInput);
            Deserializer<CompactBinaryReader<InputStream>> deserializer = new Deserializer<CompactBinaryReader<InputStream>>(this.StorageLocationType);

            HashKeyStorageFormat diskData = deserializer.Deserialize<HashKeyStorageFormat>(diskReader);

            Deserializer<CompactBinaryReader<InputBuffer>> schemaDeserializer = new Deserializer<CompactBinaryReader<InputBuffer>>(this.StorageLocationType);

            foreach (var kv in diskData.StoredData)
            {
                var input = new InputBuffer(kv.Value);
                var reader = new CompactBinaryReader<InputBuffer>(input);
                IStorageLocation location = schemaDeserializer.Deserialize(reader) as IStorageLocation;
                if (location != null)
                {
                    this.IndexData.TryAdd(kv.Key, location);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public bool UpdateIndex(YawnSchema oldRecord, YawnSchema newRecord, IStorageLocation storageLocation)
        {
            DeleteIndex(oldRecord);
            SetIndex(newRecord, storageLocation);
            return true;
        }
    }
}

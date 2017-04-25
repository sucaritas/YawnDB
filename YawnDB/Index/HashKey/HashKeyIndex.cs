// <copyright file="HashKeyIndex.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Index.HashKey
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Bond;
    using Bond.IO.Unsafe;
    using Bond.Protocols;
    using YawnDB.Storage;

    public partial class HashKeyIndex : IIndex
    {
        public string Name { get; set; }

        public Type StorageLocationType { get; set; }

        public IList<IndexParameter> IndexParameters { get; } = new List<IndexParameter>();

        private string folderPath;

        private string filePath;

        public bool SetIndex<T>(YawnSchema objectToIndex, T dataToIndex) where T : IBonded<StorageLocation>
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
                IBonded<StorageLocation> val;
                return this.IndexData.TryRemove(key, out val);
            }

            return false;
        }

        public IEnumerable<IBonded<StorageLocation>> GetStorageLocations(IIdexArguments inputParams)
        {
            IBonded<StorageLocation> result;
            foreach (var key in this.GetKeyFromIndexArguments(inputParams))
            {
                if (this.IndexData.TryGetValue(key, out result))
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

        public IBonded<StorageLocation> GetLocationForInstance(YawnSchema instance)
        {
            var key = this.Getkey(instance);
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            IBonded<StorageLocation> value;
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

            List<string> keys = new List<string>(Enumerable.Range(0, this.GetMaxKeyCount(arguments)).Select(x => string.Empty));

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
            foreach (var arg in arguments.Value1)
            {
                int cnt = 0;
                var ien = arg.Value as IEnumerable;
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

        public IEnumerable<IBonded<StorageLocation>> EnumerateAllLocations()
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
            this.folderPath = folderPath;
            this.filePath = Path.Combine(this.folderPath, this.Name + ".bin");
            if (shouldLoadFromDisk)
            {
                return this.LoadDataFromPath(this.filePath);
            }

            return true;
        }

        public void Close(bool shouldSaveToDisk)
        {
            if (shouldSaveToDisk)
            {
                this.SaveDataToPath(this.filePath);
            }
        }

        public bool SaveDataToPath(string path)
        {
            try
            {
                var output = new OutputBuffer();
                var writer = new CompactBinaryWriter<OutputBuffer>(output);
                Serialize.To(writer, this);
                File.WriteAllBytes(path, output.Data.ToArray());
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool LoadDataFromPath(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                using (var fileStream = File.OpenRead(path))
                {
                    var input = new InputStream(fileStream);
                    var reader = new CompactBinaryReader<InputStream>(input);
                    var temp = Deserialize<HashKeyIndex>.From(reader);
                    this.IndexData = temp.IndexData;
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool UpdateIndex(YawnSchema oldRecord, YawnSchema newRecord, IBonded<StorageLocation> storageLocation)
        {
            this.DeleteIndex(oldRecord);
            this.SetIndex(newRecord, storageLocation);
            return true;
        }
    }
}

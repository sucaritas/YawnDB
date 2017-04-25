// <copyright file="IIndex.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.Index
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Bond;

    using YawnDB.Storage;

    public interface IIndex
    {
        string Name { get; set; }

        Type StorageLocationType { get; set; }

        IList<IndexParameter> IndexParameters { get; }

        IEnumerable<IBonded<StorageLocation>> GetStorageLocations(IIdexArguments inputArguments);

        IBonded<StorageLocation> GetLocationForInstance(YawnSchema instance);

        IEnumerable<IBonded<StorageLocation>> EnumerateAllLocations();

        bool SetIndex<T>(YawnSchema objectToIndex, T dataToIndex) where T : IBonded<StorageLocation>;

        bool UpdateIndex(YawnSchema oldRecord, YawnSchema newRecord, IBonded<StorageLocation> storageLocation);

        bool DeleteIndex(YawnSchema instance);

        bool SaveDataToPath(string path);

        bool LoadDataFromPath(string path);

        bool Initialize(string folderPath, bool shouldLoadFromDisk);

        void Close(bool shouldSaveToDisk);
    }

    public struct IndexParameter
    {
        public string Name;
        public PropertyInfo ParameterGetter;
    }
}

namespace YawnDB.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Reflection;
    using Bond;
    
    public interface IIndex
    {
        string Name { get; set; }

        Type StorageLocationType { get; set; }

        IList<IndexParameter> IndexParameters { get; }

        IEnumerable<IStorageLocation> GetStorageLocations(IIdexArguments inputArguments);

        IStorageLocation GetLocationForInstance(YawnSchema instance);

        IEnumerable<IStorageLocation> EnumerateAllLocations();

        bool SetIndex(YawnSchema instance, IStorageLocation storageLocation);

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

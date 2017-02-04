﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YawnDB.Interfaces
{
    public interface IStorage
    {
        StorageState State { get; }
        IEnumerable<TE> GetRecords<TE>(IEnumerable<IStorageLocation> recordsToPull) where TE : YawnSchema;
        IEnumerable<TE> GetAllRecords<TE>() where TE : YawnSchema;
        Task<IEnumerable<TE>> GetAllRecordsAsync<TE>() where TE : YawnSchema;
        Task<IEnumerable<TE>> GetRecordsAsync<TE>(IEnumerable<IStorageLocation> recordsToPull) where TE : YawnSchema;
        IEnumerable<IStorageLocation> GetStorageLocations(IIdexArguments queryParams);
        void Open();
        void Close();
    }
}

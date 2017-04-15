// <copyright file="StorageEvents.cs" company="YawnDB">
//  By Julio Cesar Saenz
// </copyright>

namespace YawnDB.EventSources
{
    public static class StorageEvents
    {
        public const int InitializeStart = 1;
        public const int InitializeFinish = 2;

        public const int RecordWriteStart = 3;
        public const int RecordWriteFinish = 4;

        public const int RecordReadFromCache = 5;
        public const int RecordReadStart = 6;
        public const int RecordReadFinish = 7;

        public const int RecordSerializeStart = 8;
        public const int RecordSerializeFinish = 9;

        public const int RecordDeserializeStart = 10;
        public const int RecordDeserializeFinish = 11;

        public const int RecordDeleteStart = 11;
        public const int RecordDeleteFinish = 12;

        public const int IndexingStart = 13;
        public const int IndexingFinish = 14;
    }
}

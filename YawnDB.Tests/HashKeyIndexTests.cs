namespace YawnDB.Tests
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;
    using YawnDB.Storage;
    using YawnDB.Storage.BlockStorage;
    using YawnDB.Testing;
    using YawnDB.Exceptions;
    using YawnDB.Index.HashKey;
    using YawnDB.Tests.TestDB;
    using System.Threading.Tasks;
    using static TestsUtilities;

    [TestFixture]
    public class HashKeyIndexTests
    {
        private string basePath = Path.Combine(Path.GetDirectoryName(typeof(StorageTests).Assembly.Location), "HashKeyIndexTestsFiles");

        [OneTimeTearDown]
        public void cleanup()
        {
            System.IO.Directory.Delete(basePath, true);
        }

        [TestCase]
        public void HashIndexSavesTodisk()
        {
            var dbName = NUnit.Framework.TestContext.CurrentContext.Test.Name;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var database = new TestDatabase(dbName, path);
            database.Open(false);

            for (int i = 0; i < 5; i++)
            {
                var p = database.CreateRecord<Person>();
                p.Age = 1;
                p.FirstName = "Julio";
                p.LastName = "Saenz";
                database.SaveRecord(p);
            }

            database.Close();
            database = new TestDatabase(dbName, path);

            database.Open(false);
        }
    }
}

namespace YawnDB.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using YawnDB.Interfaces;
    using YawnDB.Storage.BlockStorage;
    using YawnDB.Testing;
    using YawnDB.Exceptions;
    using YawnDB.Index.HashKey;
    using static TestsUtilities;

    [TestFixture]
    public class StorageTests
    {
        private string basePath = Path.Combine(Path.GetDirectoryName(typeof(StorageTests).Assembly.Location), "BlockStorageTestsFiles");

        [OneTimeTearDown]
        public void cleanup()
        {
            System.IO.Directory.Delete(basePath, true);
        }

        [TestCase]
        public void StartBlockStoreageEmptyTest()
        {
            var dbName = nameof(StartBlockStoreageEmptyTest);
            var bufferBlocks = 10;
            var blockSize = 128;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var yawnDB = new Yawn(dbName, path);
            var storage = new BlockStorage<Person>(yawnDB, blockSize, bufferBlocks);

            yawnDB.RegisterSchema<Person>(storage);
            yawnDB.Open(false);
            Assert.IsTrue(Directory.Exists($"{path}\\YawnDB.Testing.Person"));
            FileInfo info = new FileInfo($"{path}\\YawnDB.Testing.Person\\YawnDB.Testing.Person.ydb");
            Assert.AreEqual(blockSize * bufferBlocks, info.Length);
            yawnDB.Close();
        }

        [TestCase]
        public void WriteReadRecorTest()
        {
            var dbName = nameof(WriteReadRecorTest);
            var bufferBlocks = 10;
            var blockSize = 128;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var yawnDB = new Yawn(dbName, path);
            var storage = new BlockStorage<Person>(yawnDB, blockSize, bufferBlocks);

            yawnDB.RegisterSchema<Person>(storage);
            yawnDB.Open(false);
            Assert.IsTrue(Directory.Exists($"{path}\\YawnDB.Testing.Person"));
            FileInfo info = new FileInfo($"{path}\\YawnDB.Testing.Person\\YawnDB.Testing.Person.ydb");
            Assert.AreEqual(blockSize * bufferBlocks, info.Length);

            var writeResult = writeRandomPersonToStorage(storage);
            var personRead = storage.ReadRecord(writeResult.Item2);
            Assert.AreEqual(writeResult.Item1.FirstName, personRead.FirstName);
            Assert.AreEqual(writeResult.Item1.LastName, personRead.LastName);
            Assert.AreEqual(writeResult.Item1.Age, personRead.Age);

            yawnDB.Close();
        }

        [TestCase]
        public void WriteRecorOnClosedStorageTest()
        {
            var dbName = nameof(WriteRecorOnClosedStorageTest);
            var bufferBlocks = 10;
            var blockSize = 128;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var yawnDB = new Yawn(dbName, path);
            var storage = new BlockStorage<Person>(yawnDB, blockSize, bufferBlocks);

            yawnDB.RegisterSchema<Person>(storage);
            yawnDB.Open(false);
            Assert.IsTrue(Directory.Exists($"{path}\\YawnDB.Testing.Person"));
            FileInfo info = new FileInfo($"{path}\\YawnDB.Testing.Person\\YawnDB.Testing.Person.ydb");
            Assert.AreEqual(blockSize * bufferBlocks, info.Length);
            yawnDB.Close();

            try
            {
                var writeResult = writeRandomPersonToStorage(storage);
                Assert.Fail("Should have thrown exception");
            }
            catch (Exception e)
            {
                Assert.AreEqual(typeof(DatabaseIsClosedException), e.GetType());
            }
        }

        [TestCase]
        public void ReadRecorOnClosedStorageTest()
        {
            var dbName = nameof(ReadRecorOnClosedStorageTest);
            var bufferBlocks = 10;
            var blockSize = 128;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var yawnDB = new Yawn(dbName, path);
            var storage = new BlockStorage<Person>(yawnDB, blockSize, bufferBlocks);

            yawnDB.RegisterSchema<Person>(storage);
            yawnDB.Open(false);
            Assert.IsTrue(Directory.Exists($"{path}\\YawnDB.Testing.Person"));
            FileInfo info = new FileInfo($"{path}\\YawnDB.Testing.Person\\YawnDB.Testing.Person.ydb");
            Assert.AreEqual(blockSize * bufferBlocks, info.Length);
            var writeResult = writeRandomPersonToStorage(storage);
            yawnDB.Close();

            try
            {
                var personRead = storage.ReadRecord(writeResult.Item2);
                Assert.Fail("Should have thrown exception");
            }
            catch (Exception e)
            {
                Assert.AreEqual(typeof(DatabaseIsClosedException), e.GetType());
            }
        }

        [TestCase]
        public void WriteInParallelRecorTest()
        {
            var dbName = "WriteInParallelRecorTest";
            var bufferBlocks = 100;
            var blockSize = 128;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var yawnDB = new Yawn(dbName, path);
            var storage = new BlockStorage<Person>(yawnDB, blockSize, bufferBlocks);

            yawnDB.RegisterSchema<Person>(storage);
            yawnDB.Open(false);
            Assert.IsTrue(Directory.Exists($"{path}\\YawnDB.Testing.Person"));
            FileInfo info = new FileInfo($"{path}\\YawnDB.Testing.Person\\YawnDB.Testing.Person.ydb");
            Assert.AreEqual(blockSize * bufferBlocks, info.Length);

            var noThreads = 1000;
            Task[] threads = new Task[noThreads];
            for (int i = 0; i < noThreads; i++)
            {
                threads[i] = new Task(() => writeRandomPersonToStorage(storage));
                threads[i].Start();
            }

            Task.WaitAll(threads);

            if (threads.Any(x => x.Status != TaskStatus.RanToCompletion))
            {
                Assert.Fail("A thread failed");
            }

            Assert.AreEqual(noThreads, storage.GetAllRecords<Person>().Count());
            yawnDB.Close();
        }

        [TestCase]
        public void UpdateInParallelRecorTest()
        {
            var dbName = nameof(UpdateInParallelRecorTest);
            var bufferBlocks = 100000;
            var blockSize = 128;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var yawnDB = new Yawn(dbName, path);
            var storage = new BlockStorage<Person>(yawnDB, blockSize, bufferBlocks);

            yawnDB.RegisterSchema<Person>(storage);
            yawnDB.Open(false);
            Assert.IsTrue(Directory.Exists($"{path}\\YawnDB.Testing.Person"));
            FileInfo info = new FileInfo($"{path}\\YawnDB.Testing.Person\\YawnDB.Testing.Person.ydb");
            Assert.AreEqual(blockSize * bufferBlocks, info.Length);

            var noThreads = 100;
            var noOperationsPerThread = 10000;
            var noOfRecords = 10;
            Task[] threads = new Task[noThreads];
            for (int i = 0; i < noOfRecords; i++)
            {
                writeRandomPersonToStorage(storage);
            }

            var personRef = new ReferenceTo<Person>();
            personRef.YawnSite = yawnDB;
            var personArray = personRef.ToArray();
            threads = new Task[noThreads];

            for (int i = 0; i < noThreads; i++)
            {
                threads[i] = new Task(() =>
                {
                    for (int j = 0; j < noOperationsPerThread; j++)
                    {
                        var prn = personArray[i % noOfRecords];
                        prn.FirstName = i.ToString();
                        yawnDB.SaveRecord(prn);
                    }
                });

                threads[i].Start();
            }

            Task.WaitAll(threads);

            Assert.AreEqual(noOfRecords, storage.GetAllRecords<Person>().Count());
            yawnDB.Close();
        }

        [TestCase]
        public void ItemExistsOnInsert()
        {
            var dbName = nameof(ItemExistsOnInsert);
            var bufferBlocks = 10;
            var blockSize = 128;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var yawnDB = new Yawn(dbName, path);
            var storage = new BlockStorage<Person>(yawnDB, blockSize, bufferBlocks);
            yawnDB.RegisterSchema<Person>(storage);
            yawnDB.Open(false);
            var writeResult = writeRandomPersonToStorage(storage);
            var index = storage.Indicies["YawnKeyIndex"] as HashKeyIndex;
            var locationInIndex = index.GetLocationForInstance(writeResult.Item1);
            Assert.AreEqual(writeResult.Item2, locationInIndex);
            yawnDB.Close();
        }

        [TestCase]
        public void ItemDoesNotExistsOnDelete()
        {
            var dbName = nameof(ItemDoesNotExistsOnDelete);
            var bufferBlocks = 10;
            var blockSize = 128;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var yawnDB = new Yawn(dbName, path);
            var storage = new BlockStorage<Person>(yawnDB, blockSize, bufferBlocks);
            yawnDB.RegisterSchema<Person>(storage);
            yawnDB.Open(false);
            var writeResult = writeRandomPersonToStorage(storage);
            yawnDB.DeleteRecord(writeResult.Item1);
            var index = storage.Indicies["YawnKeyIndex"] as HashKeyIndex;
            var locationInIndex = index.GetLocationForInstance(writeResult.Item1);
            Assert.AreEqual(null, locationInIndex);
            yawnDB.Close();
        }

        [TestCase]
        public void OneItemExistsAfterRecordUpdate()
        {
            var dbName = nameof(OneItemExistsAfterRecordUpdate);
            var bufferBlocks = 10;
            var blockSize = 128;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var yawnDB = new Yawn(dbName, path);
            var storage = new BlockStorage<Person>(yawnDB, blockSize, bufferBlocks);
            yawnDB.RegisterSchema<Person>(storage);
            yawnDB.Open(false);
            var writeResult = writeRandomPersonToStorage(storage);
            var anotherPerson = yawnDB.CreateRecord<Person>();
            anotherPerson.Id = writeResult.Item1.Id;
            anotherPerson.FirstName = "AChangedName";
            yawnDB.SaveRecord(anotherPerson);

            var index = storage.Indicies["YawnKeyIndex"] as HashKeyIndex;
            var locationInIndex = index.GetLocationForInstance(writeResult.Item1) as BlockStorageLocation;
            var anotherLocationInIndex = index.GetLocationForInstance(anotherPerson) as BlockStorageLocation;
            Assert.AreNotEqual((writeResult.Item2 as BlockStorageLocation).Address, anotherLocationInIndex.Address);
            yawnDB.Close();
        }
    }
}

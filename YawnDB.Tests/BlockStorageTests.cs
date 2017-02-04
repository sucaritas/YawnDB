namespace YawnDB.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using YawnDB.Interfaces;
    using YawnDB.Storage.BlockStorage;
    using YawnDB.Testing;
    using YawnDB.Exceptions;
    using static TestsUtilities;

    [TestClass]
    public class StorageTests
    {
        [TestMethod]
        public void StartBlockStoreageEmptyTest()
        {
            var dbName = "StartBlockStorage";
            var bufferBlocks = 10;
            var blockSize = 128;
            SetupTestDirectory($".\\{dbName}\\");
            var yawnDB = new Yawn(dbName, $".\\{dbName}\\");
            var storage = new BlockStorage<Person>(yawnDB, blockSize, bufferBlocks);

            yawnDB.RegisterSchema<Person>(storage);
            yawnDB.Open();
            Assert.IsTrue(Directory.Exists($"{dbName}\\YawnDB.Testing.Person"));
            FileInfo info = new FileInfo($"{dbName}\\YawnDB.Testing.Person\\YawnDB.Testing.Person.ydb");
            Assert.AreEqual(blockSize * bufferBlocks, info.Length);
            yawnDB.Close();
        }

        [TestMethod]
        public void WriteReadRecorTest()
        {
            var dbName = "WriteReadRecord";
            var bufferBlocks = 10;
            var blockSize = 128;
            SetupTestDirectory($".\\{dbName}\\");
            var yawnDB = new Yawn(dbName, $".\\{dbName}\\");
            var storage = new BlockStorage<Person>(yawnDB, blockSize, bufferBlocks);

            yawnDB.RegisterSchema<Person>(storage);
            yawnDB.Open();
            Assert.IsTrue(Directory.Exists($"{dbName}\\YawnDB.Testing.Person"));
            FileInfo info = new FileInfo($"{dbName}\\YawnDB.Testing.Person\\YawnDB.Testing.Person.ydb");
            Assert.AreEqual(blockSize * bufferBlocks, info.Length);
           
            var writeResult = writeRandomStudentToStorage(storage);
            var personRead = storage.ReadRecord(writeResult.Item2).Result;
            Assert.AreEqual(writeResult.Item1.FirstName, personRead.FirstName);
            Assert.AreEqual(writeResult.Item1.LastName, personRead.LastName);
            Assert.AreEqual(writeResult.Item1.Age, personRead.Age);

            yawnDB.Close();
        }

        [TestMethod]
        public void WriteRecorOnClosedStorageTest()
        {
            var dbName = "WriteRecorOnClosedStorageTest";
            var bufferBlocks = 10;
            var blockSize = 128;
            SetupTestDirectory($".\\{dbName}\\");
            var yawnDB = new Yawn(dbName, $".\\{dbName}\\");
            var storage = new BlockStorage<Person>(yawnDB, blockSize, bufferBlocks);

            yawnDB.RegisterSchema<Person>(storage);
            yawnDB.Open();
            Assert.IsTrue(Directory.Exists($"{dbName}\\YawnDB.Testing.Person"));
            FileInfo info = new FileInfo($"{dbName}\\YawnDB.Testing.Person\\YawnDB.Testing.Person.ydb");
            Assert.AreEqual(blockSize * bufferBlocks, info.Length);
            yawnDB.Close();

            try
            {
                var writeResult = writeRandomStudentToStorage(storage);
                Assert.Fail("Should have thrown exception");
            }
            catch (Exception e)
            {
                Assert.AreEqual(typeof(AggregateException), e.GetType()); // <== Because of async
                Assert.AreEqual(typeof(DatabaseIsClosedException), e.InnerException.GetType()); // Actual exception from storage
            }
        }

        [TestMethod]
        public void ReadRecorOnClosedStorageTest()
        {
            var dbName = "ReadRecorOnClosedStorageTest";
            var bufferBlocks = 10;
            var blockSize = 128;
            SetupTestDirectory($".\\{dbName}\\");
            var yawnDB = new Yawn(dbName, $".\\{dbName}\\");
            var storage = new BlockStorage<Person>(yawnDB, blockSize, bufferBlocks);

            yawnDB.RegisterSchema<Person>(storage);
            yawnDB.Open();
            Assert.IsTrue(Directory.Exists($"{dbName}\\YawnDB.Testing.Person"));
            FileInfo info = new FileInfo($"{dbName}\\YawnDB.Testing.Person\\YawnDB.Testing.Person.ydb");
            Assert.AreEqual(blockSize * bufferBlocks, info.Length);
            var writeResult = writeRandomStudentToStorage(storage);
            yawnDB.Close();

            try
            {
                var personRead = storage.ReadRecord(writeResult.Item2).Result;
                Assert.Fail("Should have thrown exception");
            }
            catch (Exception e)
            {
                Assert.AreEqual(typeof(AggregateException), e.GetType()); // <== Because of async
                Assert.AreEqual(typeof(DatabaseIsClosedException), e.InnerException.GetType()); // Actual exception from storage
            }
        }
    }
}

namespace YawnDB.Tests
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using YawnDB.Interfaces;
    using YawnDB.Storage.BlockStorage;
    using YawnDB.Testing;
    using YawnDB.Exceptions;
    using YawnDB.Index.HashKey;
    using static TestsUtilities;

    [TestClass]
    public class HashKeyIndexTests
    {
       [TestMethod]
        public void ItemExistsOnInsert()
        {
            var dbName = nameof(ItemExistsOnInsert);
            var bufferBlocks = 10;
            var blockSize = 128;
            SetupTestDirectory($".\\{dbName}\\");
            var yawnDB = new Yawn(dbName, $".\\{dbName}\\");
            var storage = new BlockStorage<Person>(yawnDB, blockSize, bufferBlocks);
            yawnDB.RegisterSchema<Person>(storage);
            yawnDB.Open();
            var writeResult = writeRandomStudentToStorage(storage);
            var index = storage.Indicies["FirstNameIndex"] as HashKeyIndex;
            var locationInIndex = index.GetLocationForInstance(writeResult.Item1);
            Assert.AreEqual(writeResult.Item2, locationInIndex);
            yawnDB.Close();
        }

        [TestMethod]
        public void ItemDoesNotExistsOnDelete()
        {
            var dbName = nameof(ItemDoesNotExistsOnDelete);
            var bufferBlocks = 10;
            var blockSize = 128;
            SetupTestDirectory($".\\{dbName}\\");
            var yawnDB = new Yawn(dbName, $".\\{dbName}\\");
            var storage = new BlockStorage<Person>(yawnDB, blockSize, bufferBlocks);
            yawnDB.RegisterSchema<Person>(storage);
            yawnDB.Open();
            var writeResult = writeRandomStudentToStorage(storage);
            yawnDB.DeleteRecord(writeResult.Item1);
            var index = storage.Indicies["FirstNameIndex"] as HashKeyIndex;
            var locationInIndex = index.GetLocationForInstance(writeResult.Item1);
            Assert.AreEqual(null, locationInIndex);
            yawnDB.Close();
        }

        [TestMethod]
        public void OneItemExistsAfterRecordUpdate()
        {
            var dbName = nameof(OneItemExistsAfterRecordUpdate);
            var bufferBlocks = 10;
            var blockSize = 128;
            SetupTestDirectory($".\\{dbName}\\");
            var yawnDB = new Yawn(dbName, $".\\{dbName}\\");
            var storage = new BlockStorage<Person>(yawnDB, blockSize, bufferBlocks);
            yawnDB.RegisterSchema<Person>(storage);
            yawnDB.Open();
            var writeResult = writeRandomStudentToStorage(storage);
            var anotherPerson = yawnDB.CreateRecord<Person>();
            anotherPerson.Id = writeResult.Item1.Id;
            anotherPerson.FirstName = "AChangedName";
            yawnDB.SaveRecord(anotherPerson);

            var index = storage.Indicies["FirstNameIndex"] as HashKeyIndex;
            var locationInIndex = index.GetLocationForInstance(writeResult.Item1) as BlockStorageLocation;
            var anotherLocationInIndex = index.GetLocationForInstance(anotherPerson) as BlockStorageLocation;
            Assert.AreNotEqual((writeResult.Item2 as BlockStorageLocation).Address, anotherLocationInIndex.Address);
            Assert.AreEqual(null, locationInIndex);
            yawnDB.Close();
        }
    }
}

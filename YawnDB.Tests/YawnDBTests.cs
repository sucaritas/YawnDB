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
    using YawnDB.Tests.TestDB;
    using YawnDB.Transactions;
    using static TestsUtilities;

    [TestFixture]
    public class YawnDBTests
    {
        private string basePath = Path.Combine(Path.GetDirectoryName(typeof(StorageTests).Assembly.Location), "YawnDbTests");

        [TestCase]
        public void YawnDeleteWithTransaction()
        {
            var dbName = nameof(YawnDeleteWithTransaction);
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var database = new TestDatabase(dbName, path);
            database.Open(true);

            var p = database.CreateRecord<Person>();
            p.Age = 1;
            p.FirstName = "Julio";
            p.LastName = "Saenz";
            database.SaveRecord(p);

            Assert.AreEqual(1, database.Persons.ToArray().Length);

            // Implicit rollback (commit was not called)
            using (var transaction = database.CreateTransaction())
            {
                database.DeleteRecord(p, transaction);
                Assert.AreEqual(1, database.Persons.ToArray().Length);
            }

            Assert.AreEqual(1, database.Persons.ToArray().Length);

            // Delete and commit the transaction
            using (var transaction = database.CreateTransaction())
            {
                database.DeleteRecord(p, transaction);
                Assert.AreEqual(1, database.Persons.ToArray().Length);
                transaction.Commit();
                Assert.AreEqual(0, database.Persons.ToArray().Length);
            }

            database.Close();
        }

        [TestCase]
        public void YawnInsertWithTransaction()
        {
            var dbName = nameof(YawnInsertWithTransaction);
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var database = new TestDatabase(dbName, path);
            database.Open(true);

            var p = database.CreateRecord<Person>();
            p.Age = 1;
            p.FirstName = "Julio";
            p.LastName = "Saenz";

            // Implicit rollback (commit was not called)
            using (var transaction = database.CreateTransaction())
            {
                database.SaveRecord(p, transaction);
                Assert.AreEqual(0, database.Persons.ToArray().Length);
            }

            Assert.AreEqual(0, database.Persons.ToArray().Length);

            // insert and commit the transaction
            using (var transaction2 = database.CreateTransaction())
            {
                database.SaveRecord(p, transaction2);
                Assert.AreEqual(0, database.Persons.ToArray().Length);
                transaction2.Commit();
                Assert.AreEqual(1, database.Persons.ToArray().Length);
            }

            database.Close();
        }

        [TestCase]
        public void YawnTransactionStore()
        {
            var dbName = nameof(YawnTransactionStore);
            var bufferBlocks = 10;
            var blockSize = 128;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var yawnDB = new Yawn(dbName, path);
            var storage = new BlockStorage<Transaction>(yawnDB, blockSize, bufferBlocks);
            yawnDB.RegisterSchema<Transaction>(storage);
            yawnDB.Open(false);
            var trans = storage.CreateRecord().Result as Transaction;
            trans.Id = 13;
            trans.State = TransactionState.Created;
            trans.TransactionItems = new LinkedList<TransactionItem>();
            var item = new BlockTransactionItem();

            trans.TransactionItems.AddLast(item);

            var location = storage.SaveRecord(trans).Result;
            var index = storage.Indicies["YawnKeyIndex"] as HashKeyIndex;
            var locationInIndex = index.GetLocationForInstance(trans);
            Assert.AreEqual(location, locationInIndex);

            var read = storage.ReadRecord(location).Result;

            Assert.AreEqual(trans.Id, read.Id);
            Assert.AreEqual(trans.State, read.State);

            yawnDB.Close();
        }
    }
}

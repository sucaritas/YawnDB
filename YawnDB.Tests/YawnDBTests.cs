namespace YawnDB.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using YawnDB.Storage;
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

        [OneTimeTearDown]
        public void cleanup()
        {
            try
            {
                System.IO.Directory.Delete(basePath, true);
            }
            catch { }
        }

        [TestCase]
        public void YawnDeleteWithTransaction()
        {
            var dbName = NUnit.Framework.TestContext.CurrentContext.Test.Name;
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
            var dbName = NUnit.Framework.TestContext.CurrentContext.Test.Name;
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
            var dbName = NUnit.Framework.TestContext.CurrentContext.Test.Name;
            var bufferBlocks = 10;
            var blockSize = 128;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var yawnDB = new Yawn(dbName, path);
            var storage = new BlockStorage<Transaction>(yawnDB, blockSize, bufferBlocks);
            yawnDB.RegisterSchema<Transaction>(storage);
            yawnDB.Open(false);
            var trans = storage.CreateRecord() as Transaction;
            trans.Id = 13;
            trans.State = TransactionState.Created;
            trans.TransactionItems = new LinkedList<Bond.IBonded<TransactionItem>>();
            var item = new BlockTransactionItem();

            trans.TransactionItems.AddLast(new Bond.Bonded<TransactionItem>(item));

            var location = storage.SaveRecord(trans);
            var index = storage.Indicies["YawnKeyIndex"] as HashKeyIndex;
            var locationInIndex = index.GetLocationForInstance(trans);

            var expected = location as BlockStorageLocation;
            var actual = locationInIndex.Deserialize<BlockStorageLocation>();
            Assert.AreEqual(expected.Address, actual.Address);
            Assert.AreEqual(expected.Id, actual.Id);

            var read = storage.ReadRecord(location);

            Assert.AreEqual(trans.Id, read.Id);
            Assert.AreEqual(trans.State, read.State);

            yawnDB.Close();
        }

        [TestCase]
        public void YawnInsertWaitsForLockedRecord()
        {
            var dbName = NUnit.Framework.TestContext.CurrentContext.Test.Name;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var database = new TestDatabase(dbName, path);
            database.Open(true);

            var p = database.CreateRecord<Person>();
            p.Age = 1;
            p.FirstName = "Julio";
            p.LastName = "Saenz";

            var lockingTask = new Task(() =>
            {
                using (var unlocker = database.LockRecord<Person>(1, YawnDB.Locking.RecordLockType.Write))
                {
                    System.Threading.Thread.Sleep(1000);
                }

            });
            lockingTask.Start();

            System.Threading.Thread.Sleep(500);
            var writingTask = new Task(() =>
            {
                database.SaveRecord(p);
            });
            writingTask.Start();

            Task.WaitAll(lockingTask, writingTask);

            Assert.AreEqual(1, database.Persons.ToArray().Length);
            database.Close();
        }

        [TestCase]
        public void YawnReadWaitsForLockedRecord()
        {
            var dbName = NUnit.Framework.TestContext.CurrentContext.Test.Name;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var database = new TestDatabase(dbName, path);
            database.Open(true);

            var p = database.CreateRecord<Person>();
            p.Age = 1;
            p.FirstName = "Julio";
            p.LastName = "Saenz";
            database.SaveRecord(p);

            var lockingTask = new Task(() =>
            {
                using (var unlocker = database.LockRecord<Person>(1, YawnDB.Locking.RecordLockType.Read))
                {
                    System.Threading.Thread.Sleep(1000);
                }

            });
            lockingTask.Start();

            System.Threading.Thread.Sleep(500);
            Person read = database.CreateRecord<Person>();
            var writingTask = new Task(() =>
            {
                read = database.GetRecord<Person>(1);
            });
            writingTask.Start();

            Task.WaitAll(lockingTask, writingTask);

            Assert.AreEqual(1, read.Id);
            database.Close();
        }

        [TestCase]
        public void YawnDeleteWaitsForLockedRecord()
        {
            var dbName = NUnit.Framework.TestContext.CurrentContext.Test.Name;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var database = new TestDatabase(dbName, path);
            database.Open(true);

            var p = database.CreateRecord<Person>();
            p.Age = 1;
            p.FirstName = "Julio";
            p.LastName = "Saenz";
            database.SaveRecord(p);

            var lockingTask = new Task(() =>
            {
                using (var unlocker = database.LockRecord<Person>(1, YawnDB.Locking.RecordLockType.Write))
                {
                    System.Threading.Thread.Sleep(1000);
                }

            });
            lockingTask.Start();

            System.Threading.Thread.Sleep(500);

            var writingTask = new Task(() =>
            {
                database.DeleteRecord(p);
            });
            writingTask.Start();

            Task.WaitAll(lockingTask, writingTask);

            Assert.AreEqual(0, database.Persons.ToArray().Length);
            database.Close();
        }

        [TestCase]
        public void YawnTransactionIsolation()
        {
            var dbName = NUnit.Framework.TestContext.CurrentContext.Test.Name;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var database = new TestDatabase(dbName, path);
            database.Open(true);

            var p = database.CreateRecord<Person>();
            p.Age = 1;
            p.FirstName = "Julio";
            p.LastName = "Saenz";
            database.SaveRecord(p);

            var lockingTask = new Task(() =>
            {
                using (var unlocker = database.LockRecord<Person>(1, YawnDB.Locking.RecordLockType.Write))
                {
                    System.Threading.Thread.Sleep(1000);
                }

            });
            lockingTask.Start();

            System.Threading.Thread.Sleep(500);

            var writingTask = new Task(() =>
            {
                database.DeleteRecord(p);
            });
            writingTask.Start();

            Task.WaitAll(lockingTask, writingTask);

            Assert.AreEqual(0, database.Persons.ToArray().Length);
            database.Close();
        }
    }
}

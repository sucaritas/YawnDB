namespace YawnDB.Tests.Locking
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using YawnDB.Locking;
    using YawnDB.Testing;
    using static TestsUtilities;
    using System.Diagnostics;

    [TestFixture]
    public class RecordLockingTests
    {
        private string basePath = Path.Combine(Path.GetDirectoryName(typeof(StorageTests).Assembly.Location), "BlockStorageTestsFiles");

        [OneTimeTearDown]
        public void cleanup()
        {
            System.IO.Directory.Delete(basePath, true);
        }

        [TestCase]
        public void LockRecordsReadOnly()
        {
            var dbName = NUnit.Framework.TestContext.CurrentContext.Test.Name;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var yawnDB = new Yawn(dbName, path);
            yawnDB.RegisterSchema<Person>();
            yawnDB.Open(false);
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int noThreads = 5;
            int sleepTime = 100;
            Task[] threads = new Task[noThreads];
            for (int i = 0; i < noThreads; i++)
            {
                threads[i] = new Task(()=>
                {
                    using (var unloker = yawnDB.LockRecord<Person>(1, RecordLockType.Read))
                    {
                        Thread.Sleep(sleepTime);
                    }
                });

                threads[i].Start();
            }

            Task.WaitAll(threads);
            stopWatch.Stop();

            Assert.IsTrue(stopWatch.ElapsedMilliseconds >= (noThreads * sleepTime));
            yawnDB.Close();
        }

        [TestCase]
        public void LockRecordsWriteOnly()
        {
            var dbName = NUnit.Framework.TestContext.CurrentContext.Test.Name;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var yawnDB = new Yawn(dbName, path);
            yawnDB.RegisterSchema<Person>();
            yawnDB.Open(false);
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int noThreads = 1;
            int sleepTime = 100;
            Task[] threads = new Task[noThreads];
            for (int i = 0; i < noThreads; i++)
            {
                threads[i] = new Task(() =>
                {
                    using (var unloker = yawnDB.LockRecord<Person>(1, RecordLockType.Write))
                    {
                        Thread.Sleep(sleepTime);
                    }
                });

                threads[i].Start();
            }

            Task.WaitAll(threads);
            stopWatch.Stop();

            Assert.IsTrue(stopWatch.ElapsedMilliseconds >= (noThreads * sleepTime));
            yawnDB.Close();
        }

        [TestCase]
        public void LockRecordsWritesDontBlockReads()
        {
            var dbName = NUnit.Framework.TestContext.CurrentContext.Test.Name;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var yawnDB = new Yawn(dbName, path);
            yawnDB.RegisterSchema<Person>();
            yawnDB.Open(false);
            int a = 0;

            using (var unloker = yawnDB.LockRecord<Person>(1, RecordLockType.Write))
            {
                using (var unloker2 = yawnDB.LockRecord<Person>(1, RecordLockType.Read))
                {
                    a++;
                }
            }

            Assert.AreEqual(1, a);
            yawnDB.Close();
        }

        [TestCase]
        public void LockRecordsReadsDontBlockWrites()
        {
            var dbName = NUnit.Framework.TestContext.CurrentContext.Test.Name;
            var path = Path.Combine(basePath, dbName);
            SetupTestDirectory(path);
            var yawnDB = new Yawn(dbName, path);
            yawnDB.RegisterSchema<Person>();
            yawnDB.Open(false);
            int a = 0;

            using (var unloker = yawnDB.LockRecord<Person>(1, RecordLockType.Write))
            {
                using (var unloker2 = yawnDB.LockRecord<Person>(1, RecordLockType.Read))
                {
                    a++;
                }
            }

            Assert.AreEqual(1, a);
            yawnDB.Close();
        }
    }
}

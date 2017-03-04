namespace YawnDB.Tests
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;
    using YawnDB.Interfaces;
    using YawnDB.Storage.BlockStorage;
    using YawnDB.Testing;
    using YawnDB.Exceptions;
    using YawnDB.Index.HashKey;
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
    }
}

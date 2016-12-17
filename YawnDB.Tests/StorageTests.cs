namespace YawnDB.Tests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using YawnDB.Interfaces;
    using YawnDB.Storage.BlockStorage;

    using School;

    [TestClass]
    public class StorageTests
    {
        [TestMethod]
        public void StartBlockStoreageEmpty()
        {
            SetupTestDirectory(@".\StartBlockStorage\");

            var yawnDB = new Yawn("StartBlockStorage", @".\StartBlockStorage\");
            var storage = new BlockStorage<School.Student>(yawnDB,128,10000);
        }
        private void SetupTestDirectory(string folder)
        {
            if (System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.Delete(folder, true);
            }

            System.IO.Directory.CreateDirectory(folder);
        }

        private List<Student> writeStudentsToStorage(IStorageOf<Student> storage, int numberOfRecords)
        {
            string[] names = new[] { "Julio", "Miguel", "Marco", "Omar", "Rene" };
            string[] lastNames = new[] { "Saenz", "Telles", "Ruelas", "Quirino", "Sandoval" };
            int[] ages = new[] { 37, 38, 39, 43, 17 };
            Random rnd = new Random();
            List<Student> students = new List<Student>();

            for (int i = 0; i < 100000; i++)
            {
                var student = storage.CreateRecord().Result;
                student.Age = ages[rnd.Next(5)];
                student.FirstName = names[rnd.Next(5)];
                student.LastName = lastNames[rnd.Next(5)];

                storage.SaveRecord(student);
                students.Add(student);
            }

            return students;
        }
    }
}

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

    public static class TestsUtilities
    {
        public static void SetupTestDirectory(string folder)
        {
            if (System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.Delete(folder, true);
            }

            System.IO.Directory.CreateDirectory(folder);
        }

        public static Tuple<Person, IStorageLocation> writeRandomStudentToStorage(IStorageOf<Person> storage)
        {
            string[] names = new[] { "Julio", "Miguel", "Marco", "Omar", "Rene" };
            string[] lastNames = new[] { "Saenz", "Telles", "Ruelas", "Quirino", "Sandoval" };
            int[] ages = new[] { 37, 38, 39, 43, 17 };

            Random rnd = new Random();
            var student = storage.CreateRecord().Result;
            student.Age = ages[rnd.Next(5)];
            student.FirstName = names[rnd.Next(5)];
            student.LastName = lastNames[rnd.Next(5)];
            var location = storage.SaveRecord(student).Result;
            return new Tuple<Person, IStorageLocation>(student, location);
        }
    }
}

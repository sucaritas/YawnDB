namespace YawnDB.Tests.TestDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using YawnDB;
    using YawnDB.Storage;
    using YawnDB.Testing;
    using YawnDB.Transactions;

    public class TestDatabase : Yawn
    {
        public IReferenceTo<Person> Persons
        {
            get
            {
                return this.RegisteredTypes[typeof(Person)] as IReferenceTo<Person>;
            }
        }

        public IReferenceTo<Transaction> Transactions
        {
            get
            {
                return this.RegisteredTypes[typeof(Transaction)] as IReferenceTo<Transaction>;
            }
        }

        public TestDatabase(string databaseName, string defaultStoragePath) : base(databaseName, defaultStoragePath)
        {

            this.RegisterSchema<Person>();
            this.RegisterSchema<Transaction>();
        }
    }
}

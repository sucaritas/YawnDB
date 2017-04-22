namespace SampleYawnDB
{

    using YawnDB;
    using YawnDB.Storage.BlockStorage;
    using YawnDB.Storage.MemStorage;
    using School;

    public class MyDataBase : Yawn
    {
        private string DatabasePath { get; set; }

        public IReferenceTo<Student> Students
        {
            get
            {
                return this.RegisteredTypes[typeof(Student)] as IReferenceTo<Student>;
            }
        }

        public IReferenceTo<Classes> Classes
        {
            get
            {
                return this.RegisteredTypes[typeof(Classes)] as IReferenceTo<Classes>;
            }
        }

        public MyDataBase(string databasePath) : base("SampleDatabase", databasePath)
        {
            this.DatabasePath = databasePath;
            this.RegisterSchema<Student>(new BlockStorage<Student>(this, 256, 100000));
            //this.RegisterSchema<Student>(new MemStorage<Student>(this));
            this.RegisterSchema<Classes>();
        }
    }
}

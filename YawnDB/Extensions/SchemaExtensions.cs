namespace YawnDB.Extensions
{
    using YawnDB.Interfaces;

    public static class SchemaExtensions
    {
        public static bool Save(this YawnSchema schemaInstance, IYawn database)
        {
            if (database == null)
            {
                return false;
            }

            return true;
        }

        public static bool Delete(this YawnSchema schemaInstance, IYawn database)
        {
            if (database == null)
            {
                return false;
            }

            return true;
        }
    }
}

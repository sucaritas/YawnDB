namespace YawnDB.Interfaces
{
    public interface ITransactionItem
    {
        IStorage Storage { get; set; }

        bool Commit();

        bool Rollback();
    }
}

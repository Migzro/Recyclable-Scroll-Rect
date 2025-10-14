namespace RecyclableSR
{
    public interface IRSRSource : IDataSource
    {
        int ExtraItemsVisible { get; }
    }
}
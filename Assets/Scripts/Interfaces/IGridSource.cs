namespace RecyclableSR
{
    public interface IGridSource : IDataSource
    {
        int ExtraRowsColumnsVisible { get; }
    }
}
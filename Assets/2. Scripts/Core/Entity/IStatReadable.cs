public interface IStatReadable
{
    bool TryGetStat<T>(StatType type, out T stat); 
    T GetStat<T>(StatType type);
}

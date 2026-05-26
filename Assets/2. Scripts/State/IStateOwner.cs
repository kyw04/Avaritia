public interface IStateOwner<T>
{
    T Owner { get; }
    IStateMachine Machine { get; }
}
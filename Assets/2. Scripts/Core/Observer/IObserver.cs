public interface IObserver<T> where T : ISubject
{
    void OnNotify(T gameEvent);
}

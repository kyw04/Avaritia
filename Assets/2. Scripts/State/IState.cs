public interface IState
{
    public void Enter(); // 들어왔을 때 한번 실행

    public void Execute(); // Update()에서 계속 실행
    public void FixedExecute(); // FixedUpdate()에서 계속 실행
    
    public void Exit(); // 나갈 때 한번 실행
}

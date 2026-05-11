using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerStateMachine stateMachine { get; private set; }

    private void Awake()
    {
        stateMachine = new PlayerStateMachine(this);
    }
}
using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerStateMachine StateMachine { get; private set; }
    public Rigidbody2D Rb { get; private set; }
    
    public float moveSpeed;

    private void Awake()
    {
        StateMachine = new PlayerStateMachine(this);
        Rb =  GetComponent<Rigidbody2D>();
    }
    
    public void Move(Vector2 input)
    {
        Vector2 dir  = new Vector3(input.x, 0, 0).normalized;
        Vector2 move = dir * moveSpeed * Time.fixedDeltaTime;
        Rb.MovePosition(Rb.position + move);

        // if (dir != Vector2.zero)
        //     Rb.rotation = Quaternion.LookRotation(dir);
    }
}
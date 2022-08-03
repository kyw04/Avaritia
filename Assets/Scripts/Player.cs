using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Transform attackBoxPos;
    public Vector2 boxSize;
    private Rigidbody2D rb;
    public float damage;
    public float moveSpeed;
    public float jumpPower;
    public float coolTime;
    private int jumpCount;
    private float curTime;
    void Start()
    {
        jumpCount = 0;
        curTime = 0;
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        Move();
        Attack();

        curTime -= Time.deltaTime;
    }

    private void Move()
    {
        float x = Input.GetAxis("Horizontal");
        transform.Translate(new Vector3(x, 0f, 0f) * moveSpeed * Time.deltaTime);
        if (x != 0)
            transform.localScale = x > 0 ? new Vector3(1, 1, 1) : new Vector3(-1, 1, 1);

        if (Input.GetButtonDown("Jump") && jumpCount < 2)
        {
            jumpCount++;
            rb.velocity = Vector3.zero;
            rb.AddForce(new Vector2(0, jumpPower), ForceMode2D.Impulse);
        }
    }

    private void Attack()
    {
        if (Input.GetButtonDown("Fire1") && curTime <= 0)
        {
            Collider2D[] collider2Ds = Physics2D.OverlapBoxAll(attackBoxPos.position, boxSize, 0);
            for (int i = 0; i < collider2Ds.Length; i++)
            {
                if (collider2Ds[i].CompareTag("Enemy"))
                {
                    TakeDamaged(collider2Ds[i]);
                }
            }
            curTime = coolTime;
        }
    }

    private void TakeDamaged(Collider2D collider2D)
    {
        collider2D.GetComponent<Enemy>().hp -= damage;

        float direction = transform.localScale.x > 0 ? 2 : -2;
        collider2D.GetComponent<Rigidbody2D>().AddForce(new Vector2(direction, 0), ForceMode2D.Impulse);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
        {
            jumpCount = 0;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(attackBoxPos.position, boxSize);
    }
}

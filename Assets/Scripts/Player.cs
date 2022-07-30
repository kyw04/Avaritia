using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    public float moveSpeed;
    public float jumpPower;
    private int jumpCount;
    void Start()
    {
        jumpCount = 0;
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        Move();
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
            rb.AddForce(new Vector3(0, jumpPower, 0), ForceMode2D.Impulse);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
        {
            jumpCount = 0;
        }
    }
}

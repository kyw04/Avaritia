using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float hp;
    public bool isDead;
    
    void Start()
    {
        isDead = false;
    }

    void Update()
    {
        if (hp <= 0)
        {
            hp = 0;
            if (!isDead)
            {
                gameObject.layer = 8;
                isDead = true;
            }
        }
    }
}

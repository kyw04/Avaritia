using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillChange : MonoBehaviour
{
    [HideInInspector] public GameObject taget;
    public GameObject chnageButtonImage;
    public float distance;

    private void Update()
    {
        int mask = 1 << LayerMask.NameToLayer("Dead");
        RaycastHit2D rightObject = Physics2D.Raycast(transform.position, transform.right, distance, mask);
        RaycastHit2D leftObject = Physics2D.Raycast(transform.position, -transform.right, distance, mask);
        float rightDistance = distance + 1;
        float leftDistance = distance + 1;

        Debug.DrawRay(transform.position, transform.right * distance, Color.red);
        Debug.DrawRay(transform.position, -transform.right * distance, Color.blue);

        if (rightObject)
        {
            rightDistance = rightObject.distance;
        }
        if (leftObject)
        {
            leftDistance = leftObject.distance;
        }

        if (rightDistance != distance + 1 || leftDistance != distance + 1)
        {
            if (rightDistance > leftDistance)
            {
                taget = leftObject.collider.gameObject;
                Debug.Log("left : " + taget.name);
            }
            else
            {
                taget = rightObject.collider.gameObject;
                Debug.Log("right : " + taget.name);
            }
            chnageButtonImage.gameObject.transform.position = taget.transform.position + Vector3.up;
            chnageButtonImage.SetActive(true);
        }
        else if (taget)
        {
            taget = null;
            chnageButtonImage.SetActive(false);
        }
        
    }
}

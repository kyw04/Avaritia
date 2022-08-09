using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillChange : MonoBehaviour
{
    [HideInInspector] public GameObject taget;
    public GameObject changeCanvas;
    private GameObject previousTaget;
    private Image[] images = new Image[2];
    private Vector3 startPos;
    private Vector3 endPos;
    public float distance;
    public float changeSpeed;
    public float changeCountMax;
    private float changeCount;
    private float keyDownTimeOne;
    private float keyDownTimeTwo;

    private void Start()
    {
        endPos = Vector3.zero;
        images[0] = changeCanvas.transform.GetChild(0).GetComponent<Image>();
        images[1] = changeCanvas.transform.GetChild(1).GetComponent<Image>();
    }

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
            }
            else
            {
                taget = rightObject.collider.gameObject;
            }
            startPos = taget.transform.position + Vector3.up * 1.5f;
            changeCanvas.gameObject.transform.position = startPos;
            changeCanvas.SetActive(true);

            if (startPos != endPos)
            {
                endPos = startPos;
                ClearCount();
            }
        }
        else if (taget)
        {
            taget = null;
            changeCanvas.SetActive(false);
            ClearCount();
        }
        
        if (taget)
        {
            if (Input.GetButtonDown("Skill1"))
            {
                keyDownTimeOne = Time.time;
                ClearCount();
            }
            if (Input.GetButtonDown("Skill2"))
            {
                keyDownTimeTwo = Time.time;
                ClearCount();
            }
            int index = keyDownTimeOne < keyDownTimeTwo ? 1 : 0;

            if (Input.GetButtonUp("Skill1"))
            {
                keyDownTimeOne = 0;
                if (index == 0)
                {
                    ClearCount();
                }
            }
            if (Input.GetButtonUp("Skill2"))
            {
                keyDownTimeTwo = 0;
                if (index == 1)
                {
                    ClearCount();
                }
            }

            if (Input.GetButton("Skill1") || Input.GetButton("Skill2"))
            {
                AddCount(index);
            }
        }
    }

    private void AddCount(int index)
    {
        int temp = index == 1 ? 0 : 1;
        images[temp].fillAmount = 0;
        changeCount += Time.deltaTime * changeSpeed;
        images[index].fillAmount = changeCount / changeCountMax;
    }

    private void ClearCount()
    {
        changeCount = 0;
        images[0].fillAmount = 0;
        images[1].fillAmount = 0;
    }
}

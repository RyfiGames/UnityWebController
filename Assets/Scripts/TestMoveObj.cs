using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMoveObj : MonoBehaviour
{
    public Vector3 leftSide;
    public Vector3 rightSide;
    public float speed;
    public bool goingRight;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (goingRight)
        {
            transform.position = Vector3.MoveTowards(transform.position, rightSide, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, rightSide) < 0.1f)
            {
                goingRight = false;
            }
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, leftSide, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, leftSide) < 0.1f)
            {
                goingRight = true;
            }
        }
    }
}

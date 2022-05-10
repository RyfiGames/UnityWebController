using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestJoystick : MonoBehaviour
{

    public Transform stick;
    public float stickX;
    public float stickY;

    // Start is called before the first frame update
    void Start()
    {

    }

    public void SetStick(float x, float y)
    {
        stickX = x;
        stickY = y;
    }

    // Update is called once per frame
    void Update()
    {
        stick.localPosition = new Vector3(stickX, stickY, -2f) * 0.5f;
    }
}

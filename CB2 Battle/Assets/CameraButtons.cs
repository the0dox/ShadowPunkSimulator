using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraButtons : MonoBehaviour
{
    public float moveSpeed = 0.03f;
    public float panSpeed = 5f;
    public float panBorderRange = 5f;
    private static bool UIactive = false;
    private static bool GracePeriod = false;
    void Update()
    {
        if(GracePeriod)
        {
            StartCoroutine(FreezeDelay());
        }
        if(!UIactive)
        {
            Vector3 pos = transform.position;
            if(Input.mousePosition.y >= Screen.height - panBorderRange)
            {
                pos += transform.forward * panSpeed * Time.deltaTime;
            }
            if(Input.mousePosition.y <= panBorderRange)
            {
                pos += -transform.forward * panSpeed * Time.deltaTime;
            }
            if(Input.mousePosition.x >= Screen.width - panBorderRange)
            {
                pos += transform.right * panSpeed * Time.deltaTime;
            }
            if(Input.mousePosition.x <= panBorderRange)
            {
                pos += -transform.right * panSpeed * Time.deltaTime;
            }
            transform.position = pos;
        }
    }
    // Start is called before the first frame update
    public void RotateLeft()
    {
        transform.Rotate(Vector3.up, 90, Space.Self);
    }

    public void RotateRight()
    {
        transform.Rotate(Vector3.up, -90, Space.Self);
    }

    public static void UIFreeze(bool input)
    {
        UIactive = input;
        GracePeriod = true;
    } 

    public static bool UIActive()
    {
        if(GracePeriod)
        {
            return GracePeriod;
        }
        return UIactive;
    }

    private static IEnumerator FreezeDelay()
    {
        yield return new WaitForSeconds (0.01f);
        GracePeriod = false;
    }
}

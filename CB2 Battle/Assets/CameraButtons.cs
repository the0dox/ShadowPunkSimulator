using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraButtons : MonoBehaviour
{
    public float moveSpeed = 0.03f;
    public float panSpeed = 5f;
    public float panBorderRange = 5f;
    void Update()
    {
        if(!CharacterSheet.active)
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
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// controls camera movement, and enables for a full screen freeze 
public class CameraButtons : MonoBehaviour
{
    // used if camera doesn't need to be controlled by the arrow keys/panned
    public bool Controllable = true;
    // movement speed of camera
    public float panSpeed = 5f;
    // Range of the screen that will trigger panning
    public float panBorderRange = 5f;
    // Speed at which camera rotations take;
    public float tics = 45f;

    // If true, freeze all interaction with the screen outside of ui
    private static bool UIactive = false;
    // A short delay that prevents interaction after ui is set inactive again
    private static bool GracePeriod = false;
    // reference for regular camera position
    private Vector3 StandardLocalCameraPos = new Vector3(0,30,-30);
    // reference for regular camera position
    private Vector3 StandardLocalRotation = new Vector3(45,0,0);
    // indicates if birds eye view is in effect
    private bool BirdsEyeView = false;
    // static reference so camera can be altered by another script
    private static GameObject objectRef;
    // Camera that renders all normal game objects
    void Start()
    {
        objectRef = gameObject;
    }
    void Update()
    {
        // Prevents the mouse click that disabled ui from being interpreted as a different action
        if(GracePeriod)
        {
            StartCoroutine(FreezeDelay());
        }
        // If Ui is not active and mouse position passes the border range, move the camera
        if(!UIactive && Controllable)
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
            if(Input.GetKeyDown(KeyCode.Q))
            {
                RotateLeft();
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                RotateRight();
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                BirdsEye();
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                Recenter();
            }
        }
    }
    // Rotates the camera left
    public void RotateLeft()
    {
        StartCoroutine(DelayedRotation(90));
    }
    // Rotates the camera right
    public void RotateRight()
    {
        StartCoroutine(DelayedRotation(-90));
    }
    // Toggles overhead view 
    public void BirdsEye()
    {
        GameObject cameraObject = GetComponentInChildren<Camera>().gameObject;
        //returns to regular view from birds eye
        if(BirdsEyeView)
        {
            cameraObject.transform.localPosition = StandardLocalCameraPos;
            cameraObject.transform.localEulerAngles = StandardLocalRotation;
        }
        //enables new view from regular angle
        else
        {
            cameraObject.transform.localPosition = new Vector3(0,30,0);
            cameraObject.transform.localEulerAngles = new Vector3(80,0,0);
        }
        BirdsEyeView = !BirdsEyeView;
    }

    public void Recenter()
    {
        gameObject.transform.position = new Vector3(-1,0,-4);
    }

    // input: the new state of the ui
    // Can be used to either freeze or unfreeze the camera
    public static void UIFreeze(bool input)
    {
        UIactive = input;
        GracePeriod = true;
    } 
    // Referenced by other gamecontrollers to know if they should take input
    // Returns if the screen is/was recently frozen
    public static bool UIActive()
    {
        if(GracePeriod)
        {
            return GracePeriod;
        }
        return UIactive;
    }

    // Focuses the camera on the transfom of a given gameobject
    public static void SetFocus(Vector3 newPos)
    {
        objectRef.transform.position = newPos;
    }

    // After a few frames, end the grace period and resume normal input
    private static IEnumerator FreezeDelay()
    {
        yield return new WaitForSeconds (0.01f);
        GracePeriod = false;
    }

    // Smoothes out the rotation movement depending on tics
    private IEnumerator DelayedRotation(float dir)
    {
        UIFreeze(true);
        for(int i = 0; i < tics; i++)
        {
            transform.Rotate(Vector3.up, dir/tics, Space.Self);
            yield return new WaitForSeconds (0.01f);
        }
        UIFreeze(false);
    }
}

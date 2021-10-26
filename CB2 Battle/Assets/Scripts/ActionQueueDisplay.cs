using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// Manages all time-based activities in the overworld
public class ActionQueueDisplay : MonoBehaviourPunCallbacks
{
    // Used to store the activity entry prefab in editor
    [SerializeField] GameObject ActivityObjectReference;
    // Reference to all active actions to ensure proper stacking 
    private static Stack<GameObject> ActionStack;
    // Used to place actions relative to this position 
    private static Transform mytransform;
    // How much each action is spaced out between each other
    private static Vector3 activityDisplacement = new Vector3(0, 200, 0);
    //static reference of empty activity entry
    private static GameObject SActivityObjectReference;
    private static PhotonView pv;
    // Assigns transform
    void Awake()
    {
        pv = GetComponent<PhotonView>();
        ActionStack = new Stack<GameObject>();
        mytransform = gameObject.transform;
        SActivityObjectReference = ActivityObjectReference;
    }

    // used to add an activity from memory 
    public static void AddActivity(string name, float progress, float time)
    {
        pv.RPC("RPC_AddActivity",RpcTarget.MasterClient,name,progress,time);
    }

    [PunRPC]
    void RPC_AddActivity(string name, float progress, float time)
    {
        GameObject newActivity = PhotonNetwork.Instantiate("Lead",new Vector3(), Quaternion.identity);
        newActivity.GetComponent<LeadScript>().SetLead(name,progress,time,ActionStack.Count);
        ActionStack.Push(newActivity);
        mytransform.transform.localPosition += activityDisplacement;
    }

    // oldActivity: a Gameobject to be removed from the display
    // Removes oldActivity from the display and shifts the positions of all others accordingly.
    public static void RemoveActivity(GameObject oldActivity)
    {
        Queue<GameObject> tempActionQueue = new Queue<GameObject>();
        while(ActionStack.Peek() != oldActivity)
        {
            ActionStack.Peek().transform.localPosition += activityDisplacement;
            tempActionQueue.Enqueue(ActionStack.Pop());
        }
        mytransform.localPosition -= activityDisplacement;
        Destroy(ActionStack.Pop());
        while(ActionStack.Count > 0)
        {
            tempActionQueue.Enqueue(ActionStack.Pop());
        }
    
        while(tempActionQueue.Count > 0)
        {
            ActionStack.Push(tempActionQueue.Dequeue());
        }
        while(ActionStack.Count > 0)
        {
            tempActionQueue.Enqueue(ActionStack.Pop());
        }
        while(tempActionQueue.Count > 0)
        {
            ActionStack.Push(tempActionQueue.Dequeue());
        }
    }
}

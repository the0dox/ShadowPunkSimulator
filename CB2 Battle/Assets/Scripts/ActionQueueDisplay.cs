using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Manages all time-based activities in the overworld
public class ActionQueueDisplay : MonoBehaviour
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
    // Assigns transform
    void Awake()
    {
        ActionStack = new Stack<GameObject>();
        mytransform = gameObject.transform;
        SActivityObjectReference = ActivityObjectReference;
    }

    // used to add an activity from memory 
    public static void LoadActivity(string name, float Progress, float Time)
    {
        GameObject newActivity = Instantiate(SActivityObjectReference) as GameObject;
        newActivity.GetComponent<LeadScript>().SetLead(name,Progress,Time);
        ActionStack.Push(newActivity);
        newActivity.transform.SetParent(mytransform);
        newActivity.transform.localPosition = -(activityDisplacement * ActionStack.Count);
        mytransform.transform.localPosition += activityDisplacement;
    }

    // newActivity: a Gameobject to be added to the display
    // Used to add an new activity that hasn't been modified by a player check
    public static void AddActivity(int Modifier, int Time, PlayerStats[] PlayerSelection, string SkillChoice, string name)
    {
        GameObject newActivity = Instantiate(SActivityObjectReference) as GameObject;
        newActivity.GetComponent<LeadScript>().UpdateLead(Modifier,Time,PlayerSelection,SkillChoice, name);
        ActionStack.Push(newActivity);
        newActivity.transform.SetParent(mytransform);
        newActivity.transform.localPosition = -(activityDisplacement * ActionStack.Count);
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Manages all time-based activities in the overworld
public class ActionQueueDisplay : MonoBehaviour
{
    // Reference to all active actions to ensure proper stacking 
    private static Stack<GameObject> ActionStack = new Stack<GameObject>();
    // Used to place actions relative to this position 
    private static Transform mytransform;
    // How much each action is spaced out between each other
    private static Vector3 activityDisplacement = new Vector3(0, 200, 0);

    // Assigns transform
    void Start()
    {
        mytransform = gameObject.transform;
    }

    // newActivity: a Gameobject to be added to the display
    // Adds newActivity to the display according to myTransform and activityDisplacement
    public static void AddActivity(GameObject newActivity)
    {
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

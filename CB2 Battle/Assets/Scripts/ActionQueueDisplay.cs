using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionQueueDisplay : MonoBehaviour
{
    private static Stack<GameObject> ActionStack = new Stack<GameObject>();
    private static Transform mytransform;
    private static Vector3 activityDisplacement = new Vector3(0, 200, 0);

    void Start()
    {
        mytransform = gameObject.transform;
    }

    public static void AddActivity(GameObject newActivity)
    {
        ActionStack.Push(newActivity);
        newActivity.transform.SetParent(mytransform);
        newActivity.transform.localPosition = -(activityDisplacement * ActionStack.Count);
        mytransform.transform.localPosition += activityDisplacement;
    }

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

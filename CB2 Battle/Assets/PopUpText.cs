using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopUpText : MonoBehaviour
{
   public static GameObject DisplayText;

   public GameObject input;
   public static PopUpText instance;

   //public static Queue<GameObject> DisplayQueue;

   public static GameObject QueueSelection;

   private static List<GameObject> ActiveQueue = new List<GameObject>();

   private static Dictionary<GameObject, Queue<GameObject>> DisplayQueue = new Dictionary<GameObject, Queue<GameObject>>();

    void Start()
    {
        DisplayText = input;
        instance = this;
        //DisplayQueue = new Queue<GameObject>();
        instance.StartCoroutine(InstantiatorCoroutine());
    }

    public static void CreateText(string input, Color c, GameObject location)
    {
        if(!DisplayQueue.ContainsKey(location))
        {
            DisplayQueue.Add(location, new Queue<GameObject>());
        }
        GameObject current = GameObject.Instantiate(DisplayText, new Vector3(0,0,0), Quaternion.identity);
        current.GetComponent<DisplayTextScript>().SetInfo(c, input, location);
        current.SetActive(false);
        DisplayQueue[location].Enqueue(current);
    }

    static IEnumerator InstantiatorCoroutine()
    {
        while (true)
        {
            if(!FinishedPrinting()){
            foreach(GameObject location in DisplayQueue.Keys)
            {
                if(DisplayQueue[location].Count != 0)
                {
                    DisplayQueue[location].Peek().SetActive(true);
                }
            }
            yield return new WaitForSeconds (2f);
            }
            else
            {
                yield return new WaitForSeconds (0.2f);
            }
        }
    }

    public static bool FinishedPrinting()
    {
        foreach(GameObject location in DisplayQueue.Keys)
            {
                if(DisplayQueue[location].Count > 0 && !DisplayQueue[location].Peek().activeInHierarchy)
                {
                    return false;
                }
            }
        return true;
    }

    public static void Dequeue(GameObject location)
    {
        DisplayQueue[location].Dequeue();
    }
}

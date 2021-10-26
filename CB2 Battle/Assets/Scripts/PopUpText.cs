using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PopUpText : MonoBehaviourPunCallbacks
{
   public static GameObject DisplayText;
   public GameObject input;
   public static PopUpText instance;

   //public static Queue<GameObject> DisplayQueue;

   public static GameObject QueueSelection;

   private static List<GameObject> ActiveQueue = new List<GameObject>();

   private static Dictionary<Vector3, Queue<GameObject>> DisplayQueue = new Dictionary<Vector3, Queue<GameObject>>();
    [SerializeField] private PhotonView pv;
    private static PhotonView spv;
    void Start()
    {
        spv = pv;
        DisplayText = input;
        instance = this;
        //DisplayQueue = new Queue<GameObject>();
        instance.StartCoroutine(InstantiatorCoroutine());
    }

    public static void CreateText(string input, Color c, GameObject location)
    {
        float[] colorcode = new float[4];
        colorcode[0] = c.r;
        colorcode[0] = c.b;
        colorcode[0] = c.g;
        colorcode[0] = c.a;
        
        spv.RPC("RPC_SaveText", RpcTarget.All, input, colorcode, location.transform.position);
    }

    [PunRPC]
    void RPC_SaveText(string input, float[] colorcode, Vector3 location)
    {
        Color c = Color.yellow; //new Color(colorcode[0],colorcode[1],colorcode[2],colorcode[3]);
        if(!DisplayQueue.ContainsKey(location))
        {
            DisplayQueue.Add(location, new Queue<GameObject>());
        }
        GameObject current = Instantiate(DisplayText, new Vector3(0,0,0), Quaternion.identity);
        current.name = "findme";
        current.GetComponent<DisplayTextScript>().SetInfo(c, input, location);
        current.SetActive(false);
        DisplayQueue[location].Enqueue(current);
        Debug.Log("making text" + DisplayQueue.Count + current.name);
    }

    static IEnumerator InstantiatorCoroutine()
    {
        while (true)
        {
            if(!FinishedPrinting()){
            foreach(Vector3 location in DisplayQueue.Keys)
            {
                if(DisplayQueue[location].Count != 0)
                {
                    Debug.Log("setting active");
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
        foreach(Vector3 location in DisplayQueue.Keys)
        {
            if(DisplayQueue[location].Count > 0 && !DisplayQueue[location].Peek().activeInHierarchy)
            {
                return false;
            }
        }
        return true;
    }

    public static void Dequeue(Vector3 location)
    {
        DisplayQueue[location].Dequeue();
    }
}

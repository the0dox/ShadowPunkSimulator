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
    private static Dictionary<Color, string> colorCodes = new Dictionary<Color, string>
    {
        {Color.yellow, "y"},
        {Color.green, "g"},
        {Color.red, "r"},
        {Color.cyan, "c"}
    };
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
        string punColorCode = colorCodes[c];
        
        spv.RPC("RPC_SaveText", RpcTarget.All, input, punColorCode, location.transform.position);
    }

    [PunRPC]
    void RPC_SaveText(string input, string punColorDode, Vector3 location)
    {
        Color myColor = Color.yellow;
        foreach(Color key in colorCodes.Keys)
        {
            if(colorCodes[key].Equals(punColorDode))
            {
                myColor = key;
            }
        }
        if(!DisplayQueue.ContainsKey(location))
        {
            DisplayQueue.Add(location, new Queue<GameObject>());
        }
        GameObject current = Instantiate(DisplayText, new Vector3(0,0,0), Quaternion.identity);
        current.GetComponent<DisplayTextScript>().SetInfo(myColor, input, location);
        current.SetActive(false);
        DisplayQueue[location].Enqueue(current);
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

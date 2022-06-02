using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

// UI element that displays the events of the game, any script can access and make this display print a message to the player
public class CombatLog : MonoBehaviourPunCallbacks
{
    [SerializeField] private int MaxEntries = 10;
    [SerializeField] private RectTransform content;
    [SerializeField] private GameObject entryReference;
    private static PhotonView pv;
    // lines are entered into queue so that when the text box overflows, the first entry is deleted.
    private static Queue<GameObject> ContentsQueue = new Queue<GameObject>();

    // text: string input to be read to the player
    // types out text and formats it for the player

    void Start()
    {
        pv = GetComponent<PhotonView>();
    }
    public static void Log(string text)
    {
        pv.RPC("RPC_Log",RpcTarget.All,text);
    }

    [PunRPC]
    void RPC_Log(string text)
    {
        // create new entry with this text
        Debug.Log("creating object");
        GameObject newEntry = GameObject.Instantiate(entryReference, Vector3.zero, Quaternion.identity);
        newEntry.GetComponentInChildren<Text>().text = text;
        newEntry.transform.SetParent(content);
        newEntry.transform.localScale = Vector3.one;
        newEntry.SetActive(true);
        ContentsQueue.Enqueue(newEntry);
        TrimEntries();
    }


    private void TrimEntries()
    {
        while(ContentsQueue.Count > MaxEntries)
        {
            Destroy(ContentsQueue.Dequeue());
        }
    }
}

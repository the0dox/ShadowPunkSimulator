using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

// Displays the initative order of the board scene purely for display
// although a way to manipulate initative is planned in the future
public class InitativeTrackerScript : MonoBehaviour
{

    // Reference to the text object each text object only represents a single player
    [SerializeField] private GameObject ActivePlayerPrefab;
    [SerializeField] private GameObject InactivePlayerPrefab;
    [SerializeField] private RectTransform content;
    private int rounds = 0;
    [SerializeField] private Text roundText;
    [SerializeField] private PhotonView pv;
    private Stack<GameObject> Entries = new Stack<GameObject>();

    // L is the string of initative order provided by gamecontroller
    // displays the iniative order with the current player on top
    public void UpdateList(string[] initative)
    {
        if(initative.Length == 1)
        {
            pv.RPC("RPC_List_Single",RpcTarget.All,initative);
        }
        else
        {
            pv.RPC("RPC_List",RpcTarget.All,initative);
        }
    }

    public void UpdateRound()
    {
        CombatLog.Log("Starting Round " + (rounds + 1));
        pv.RPC("RPC_UpdateRound", RpcTarget.All);
    }

    [PunRPC]
    void RPC_UpdateRound()
    {
        rounds++;
        roundText.text = "Round " + rounds;
    }   

    // Sends l to all clients
    [PunRPC]
    void RPC_List_Single(string initative)
    {
        ClearList();
        CreateEntry(initative, true);
    }

    // Sends l to all clients
    [PunRPC]
    void RPC_List(string[] initative)
    {
        ClearList();
        bool active = true;
        for(int i = 0; i < initative.Length; i++)
        {
            string value = initative[i];
            // string inactive indicates everything behind it should be created with the inactive prefab
            if(value.Equals("!inactive"))
            {
                active = false;
            }
            else
            {
                CreateEntry(value, active);
            }
        }
        
    }
    // Clears previous order    
    public void ClearList()
    {
        while(Entries.Count > 0)
        {
            Destroy(Entries.Pop());
        }
    }

    public void CreateEntry(string value, bool active)
    {
        GameObject newEntry;
        if(active)
        {
            newEntry = GameObject.Instantiate(ActivePlayerPrefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            newEntry = GameObject.Instantiate(InactivePlayerPrefab, Vector3.zero, Quaternion.identity);
        }
        newEntry.GetComponentInChildren<Text>().text = value;
        newEntry.transform.SetParent(content);
        newEntry.transform.localScale = Vector3.one;
        newEntry.SetActive(true);
        Entries.Push(newEntry);
    }
}

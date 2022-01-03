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
    [SerializeField] private GameObject TextEntry;
    [SerializeField] private PhotonView pv;
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

    // Sends l to all clients
    [PunRPC]
    void RPC_List_Single(string initative)
    {
        ClearList();
        int verticalDisplacement = 0;
        verticalDisplacement -= 50;
        GameObject newEntry = Instantiate(TextEntry) as GameObject;
        newEntry.transform.SetParent(gameObject.transform, false);
        newEntry.transform.localPosition = new Vector3(0,verticalDisplacement,0);
        newEntry.GetComponent<Text>().text = initative;
    }

    // Sends l to all clients
    [PunRPC]
    void RPC_List(string[] initative)
    {
        ClearList();
        int verticalDisplacement = 0;
        for(int i = 0; i < initative.Length; i++)
        {
            verticalDisplacement -= 50;
            GameObject newEntry = Instantiate(TextEntry) as GameObject;
            newEntry.transform.SetParent(gameObject.transform, false);
            newEntry.transform.localPosition = new Vector3(0,verticalDisplacement,0);
            newEntry.GetComponent<Text>().text = initative[i];
        }
        
    }
    // Clears previous order    
    public void ClearList()
    {
        GameObject[] g = GameObject.FindGameObjectsWithTag("Init");
        foreach(GameObject current in g)
        {
            Destroy(current);
        }
    }
}

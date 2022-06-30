using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// ButtonManager is the basic logic component that every scene with game logic should have some variation of. 
// ButtonManager is extended by other classes for specific scenes that require different kinds of buttons 

public class UIButtonManager : MonoBehaviourPunCallbacks
{
    // used for multiplayer syncing
    [SerializeField] protected private PhotonView pv;

    // creates a set of interactable buttons can key = text value = method called
    public void ConstructActions(Dictionary<string, string> d)
    {
        pv.RPC("RPC_ConstructActions",RpcTarget.MasterClient,d);
    }

    // Instead of construction smart buttons on all clients, button manager creates a set of photon objects on the masters side
    // these buttons have a simple code that can be viewed on client side
    [PunRPC]
    public void RPC_ConstructActions(Dictionary<string, string> d)
    {
        pv.RPC("RPC_ClearActions",RpcTarget.All);
        if(d != null)
        {
            int displacement = 0;
            foreach (KeyValuePair<string, string> kvp in d)
            {
                GameObject newButton = PhotonNetwork.Instantiate("ActionButton", Vector3.one, Quaternion.identity);
                newButton.GetComponent<ActionButtonScript>().DownloadButton(kvp.Key, kvp.Value);
                displacement += 150;
            }
        }
    }

    // Clears all active buttons
    [PunRPC]
    public void RPC_ClearActions()
    {
        GameObject[] oldButtons = GameObject.FindGameObjectsWithTag("ActionInput");
        foreach(GameObject g in oldButtons)
        {
            g.GetComponent<ActionButtonScript>().DestroyMe();
        }
    }

    //is passed the action value of a button as an input
    virtual public void OnButtonPressed(string input)
    {   
        Invoke(input,0);
        //pv.RPC("RPC_OnButtonPressed",RpcTarget.MasterClient,input);
    }
    /*
    [PunRPC]
    public void RPC_OnButtonPressed(string input)
    {
        Invoke(input,0);
    }
    */
}

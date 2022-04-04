using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class UIButtonManager : MonoBehaviourPunCallbacks
{
    [SerializeField] protected private PhotonView pv;
    private Vector3 StartingLine = new Vector3(-300,80,0);

    //creates a set of interactable buttons can key = text value = method called
    public void ConstructActions(Dictionary<string, string> d)
    {
        pv.RPC("RPC_ConstructActions",RpcTarget.MasterClient,d);
    }

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

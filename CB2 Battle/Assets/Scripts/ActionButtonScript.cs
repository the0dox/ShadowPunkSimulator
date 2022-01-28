using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

// simple UI button that holds a single string that can be invoked
public class ActionButtonScript : MonoBehaviour
{
    // Invoke string, the name of the method thats called
    [SerializeField] private string action;
    // Display name of the button shown to the player, can be different than action
    [SerializeField] private GameObject Text;
    private PhotonView pv;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        transform.SetParent(UIActionbar.current.transform);
        transform.localScale = Vector3.one;
    }

    // Called by whatevered instantiated the button to set its saved string and display name
    public void SetAction(string action)
    {
        pv = GetComponent<PhotonView>();
        pv.RPC("RPC_SetAction",RpcTarget.All,action);
    }

    [PunRPC]
    void RPC_SetAction(string action)
    {
        Text.GetComponent<Text>().text = action;
        this.action = action;
    }

    // Same as SetAction, but only changes display name 
    public void SetText(string text)
    {
        pv.RPC("RPC_SetText",RpcTarget.All,text);
    }

    [PunRPC]
    void RPC_SetText(string text)
    {
        Text.GetComponent<Text>().text = text;
    }
    // On button press send my action string to the game controller
    public void GetAction()
    {
        Debug.Log("clicked");
        pv.RPC("RPC_GetAction",RpcTarget.MasterClient, action);
    }

    [PunRPC]
    void RPC_GetAction(string sendAction)
    {
        Debug.Log("recieved");
        if(!CameraButtons.UIActive())
        {
            if(GameObject.FindGameObjectWithTag("GameController").TryGetComponent<TurnManager>(out TurnManager tm))
            {
                tm.OnButtonPressed(sendAction);
            }
            else
            {
                GameObject.FindGameObjectWithTag("GameController").GetComponent<UIButtonManager>().OnButtonPressed(sendAction);
            }
        }
    }
    // Called by the game controller to clear button options
    public void DestroyMe()
    {
        Destroy(gameObject);
    }
}

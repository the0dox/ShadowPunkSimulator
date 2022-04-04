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
    [SerializeField] private Text Text;
    [SerializeField] private Image myIcon;
    [SerializeField] TooltipTrigger myTooltip;
    [SerializeField] private PhotonView pv;

    void Awake()
    {
        transform.SetParent(UIActionbar.current.transform);
        transform.localScale = Vector3.one;
    }

    // Called by whatevered instantiated the button to set its saved string and display name
    public void DownloadButton(string displayName, string action)
    {
        pv.RPC("RPC_SetAction",RpcTarget.All, displayName, action);
    }

    [PunRPC]
    void RPC_SetAction(string displayName, string action)
    {
        Text.text = displayName;
        this.action = action;
        ActionTemplate myTemplate = ActionReference.GetActionTemplate(action);
        if(myTemplate != null)
        {
            Text.enabled = false;
            myIcon.sprite = myTemplate.icon;
            myTooltip.content = myTemplate.description;
            myTooltip.header = displayName; 
        }
        else
        {
            myIcon.enabled = false;
            myTooltip.enabled = false;
        }
    }

    // On button press send my action string to the game controller
    public void GetAction()
    {
        pv.RPC("RPC_GetAction",RpcTarget.MasterClient, action);
    }

    [PunRPC]
    void RPC_GetAction(string sendAction)
    {
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

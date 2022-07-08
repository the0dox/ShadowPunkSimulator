using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
// UI screen that displays the info of the active player
public class UIPlayerInfo : MonoBehaviourPunCallbacks
{
    // displays the weapon held in the active player's primary hand
    [SerializeField] private Text WeaponOne;
    // displays the weapon held in the active player's secondary hand
    [SerializeField] private Text WeaponTwo;
    // Photonview used to communicate with other clients 
    [SerializeField] private Text PlayerName;
    // reference for communication through photonnetwork
    [SerializeField] private PhotonView pv;
    // reference to health text
    [SerializeField] private TextMeshProUGUI Health;
    // reference to stun text
    [SerializeField] private TextMeshProUGUI Stun;
    // reference to remaining moves text
    [SerializeField] private TextMeshProUGUI Moves;
    // reference to the left hand action button
    [SerializeField] private Image Action1;
    // reference to the right hand action button
    [SerializeField] private Image Action2;
    // reference to the free action button
    [SerializeField] private Image ActionFree;
    // reference to health fill bar
    [SerializeField] private Image HealthBar;
    // reference to stress fill bar
    [SerializeField] private Image StunBar;
    // reference to health move bar
    [SerializeField] private Image MoveBar;
    // reference to ui object, disabled when you want to hide main ui panel 
    [SerializeField] private GameObject display;
    // reference to the action wheel below the main panel
    [SerializeField] private GameObject actiondisplay;
    // reference to the conditions wheel
    [SerializeField] private Transform conditionDisplay;
    private List<GameObject> conditionChildren;
    [SerializeField] private GameObject conditionObject;
    // reference to the prompt screen above the main panel
    [SerializeField] private GameObject customMessageDisplay;
    // reference to the text that displays custom messages
    [SerializeField] private Text customMessage;
    // static reference to the instance
    private static UIPlayerInfo instance;
    
    void Awake()
    {
        instance = this;
    }

    public static void UpdateDisplay(PlayerStats ps, int actions, int freeActions)
    {  
        instance.UpdateDisplayInstance(ps,actions,freeActions);
    }

    public static void UpdateCustomCommand(string message)
    {
        instance.pv.RPC("RPC_CustomCommand", RpcTarget.All, message);
    }

    [PunRPC]
    void RPC_CustomCommand(string message)
    {
        if(!string.IsNullOrEmpty(message))
        {
            customMessageDisplay.SetActive(true);
            customMessage.text = message;
        }
        else
        {
            customMessageDisplay.SetActive(false);
        }
    }

    // ps: the active player whos turn it is
    // actions: the remaining actions in this turn
    // takes all the information of the active player
    public void UpdateDisplayInstance(PlayerStats ps, int actions, int freeActions)
    {
        string Pname = ps.GetName();
        int moveMax = 0;
        if(ps.hasCondition(Condition.Running))
        {
            moveMax = ps.myData.GetAttribute(AttributeKey.MoveRun);
        }
        else
        {
            moveMax = ps.myData.GetAttribute(AttributeKey.MoveWalk);
        }
        string WeaponOneText = "---";
        string WeaponTwoText = "---";
        if(ps.PrimaryWeapon != null)
        {
            WeaponOneText = ps.PrimaryWeapon.ToString();
        }
        if(ps.SecondaryWeapon != null)
        {
            WeaponTwoText = ps.SecondaryWeapon.ToString();
        }
        pv.RPC("RPC_UpdateDisplay",RpcTarget.All,Pname,ps.getWounds(), ps.myData.GetAttribute(AttributeKey.PhysicalHealth), ps.getStun(), ps.myData.GetAttribute(AttributeKey.StunHealth),actions, freeActions, ps.remainingMove, moveMax, WeaponOneText,WeaponTwoText);
    }

    // used to change the display on all clients 
    [PunRPC]
    void RPC_UpdateDisplay(string Pname, int wounds, int woundMax, int stun, int stunMax, int Actions, int freeAction, int move, int moveMax, string WeaponOneText, string WeaponTwoText)
    {
        PlayerName.text = Pname;
        // HP
        string woundsText = "Wounds: " +  (woundMax - wounds)  + "/" + woundMax;
        float percentageHealth = (float) (woundMax-wounds)/woundMax;
        Health.text = woundsText;
        HealthBar.fillAmount = percentageHealth;
        // Stun
        string StunText = "Edge: " +  (stunMax - stun)  + "/" + stunMax;
        float percentageStun = (float) (stunMax-stun)/stunMax;
        Stun.text = StunText;
        StunBar.fillAmount = percentageStun;
        // Move
        string moveText = "Moves: " + move + "/" + moveMax;
        Moves.text = moveText;
        float percentageMove = (float)move/moveMax;
        MoveBar.fillAmount = percentageMove;
        // Actions
        Action1.color = Color.white;
        Action2.color = Color.white;
        ActionFree.color = Color.white;
        if(Actions < 2)
        {
            Action1.color = Color.gray;
        }
        if(Actions < 1)
        {
            Action2.color = Color.gray;
        }
        if(freeAction < 1)
        {
            ActionFree.color = Color.gray;
        }
        // Weapons
        WeaponOne.text = WeaponOneText;
        WeaponTwo.text = WeaponTwoText;
        // On cancel, custom messages disabled
        customMessage.text = "";
        customMessageDisplay.SetActive(false);
    }

    public static void ShowAllInfo(PlayerStats activePlayer)
    {
        Photon.Realtime.Player activePlayerOwner = DmMenu.GetOwner(activePlayer);
        instance.pv.RPC("RPC_ShowAllInfo",RpcTarget.All, activePlayerOwner.ActorNumber);
    }

    public static void ShowActionsOnly(PlayerStats activePlayer)
    {
        Photon.Realtime.Player activePlayerOwner = DmMenu.GetOwner(activePlayer);
        instance.pv.RPC("RPC_ShowActionsOnly",RpcTarget.All, activePlayerOwner.ActorNumber);
    }

    private void UpdateConditions(Dictionary<int,int> conditions)
    {
        foreach(GameObject previousObject in conditionChildren)
        {
            Destroy(previousObject);
        }
        foreach(KeyValuePair<int,int> kvp in conditions)
        {
            ConditionTemplate currentCondition = ConditionsReference.GetTemplate((Condition)kvp.Key);
            int conditionlength = kvp.Value;
            GameObject newConditionObject = Instantiate(conditionObject) as GameObject;
            newConditionObject.transform.SetParent(conditionDisplay);
        }
    }
    
    [PunRPC]
    void RPC_ShowActionsOnly(int PlayerID)
    {
        Photon.Realtime.Player defendingPlayer = PhotonNetwork.CurrentRoom.GetPlayer(PlayerID);
        if(pv.IsMine || PhotonNetwork.LocalPlayer == defendingPlayer)
        {
            instance.display.SetActive(false);
            instance.actiondisplay.SetActive(true);
        }
        else
        {
            instance.display.SetActive(false);
            instance.actiondisplay.SetActive(false);
        }
    }

    [PunRPC]
    void RPC_ShowAllInfo(int PlayerID)
    {
        Photon.Realtime.Player activePlayer = PhotonNetwork.CurrentRoom.GetPlayer(PlayerID);
        if(pv.IsMine || PhotonNetwork.LocalPlayer == activePlayer)
        {   
            instance.display.SetActive(true);
            instance.actiondisplay.SetActive(true);
        }
        else
        {
            instance.display.SetActive(false);
            instance.actiondisplay.SetActive(false);
        }
    }
}

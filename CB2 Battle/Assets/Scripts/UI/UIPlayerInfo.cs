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
    [SerializeField] private PhotonView pv;
    [SerializeField] private TextMeshProUGUI Health;
    [SerializeField] private TextMeshProUGUI Moves;
    [SerializeField] private Image Action1;
    [SerializeField] private Image Action2;
    [SerializeField] private Image ActionFree;
    [SerializeField] private Image HealthBar;
    [SerializeField] private Image MoveBar;
    [SerializeField] private GameObject display;
    [SerializeField] private GameObject actiondisplay;
    private static UIPlayerInfo instance;
    
    void Awake()
    {
        instance = this;
    }

    public static void UpdateDisplay(PlayerStats ps, int actions, int freeActions)
    {  
        instance.UpdateDisplayInstance(ps,actions,freeActions);
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
        pv.RPC("RPC_UpdateDisplay",RpcTarget.All,Pname,ps.getWounds(), ps.myData.GetAttribute(AttributeKey.PhysicalHealth),actions, freeActions, ps.remainingMove, moveMax, WeaponOneText,WeaponTwoText);
    }

    // used to change the display on all clients 
    [PunRPC]
    void RPC_UpdateDisplay(string Pname, int wounds, int woundMax, int Actions, int freeAction, int move, int moveMax, string WeaponOneText, string WeaponTwoText)
    {
        PlayerName.text = Pname;
        // HP
        string woundsText = "Wounds: " +  (woundMax - wounds)  + "/" + woundMax;
        float percentageHealth = (float) (woundMax-wounds)/woundMax;
        Health.text = woundsText;
        HealthBar.fillAmount = percentageHealth;
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

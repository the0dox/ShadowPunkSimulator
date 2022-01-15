using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
// UI screen that displays the info of the active player
public class UIPlayerInfo : MonoBehaviourPunCallbacks
{
    // displays the name of the active player
    public GameObject PlayerName;
    // displays health of the active player
    public GameObject Health;
    // displays the remaining actions of the active player
    public GameObject Actions;
    // displays the weapon held in the active player's primary hand
    public GameObject WeaponOne;
    // displays the weapon held in the active player's secondary hand
    public GameObject WeaponTwo;
    // Photonview used to communicate with other clients 
    [SerializeField] private PhotonView pv;
    [SerializeField] private Text Moves;
    
    // ps: the active player whos turn it is
    // actions: the remaining actions in this turn
    // takes all the information of the active player
    public void UpdateDisplay(PlayerStats ps, int actions)
    {
        string Pname = ps.GetName();
        string WoundsText = ps.HealthToString();
        string ActionsText = "Half Actions: " + actions +"/2";
        string MoveText = ps.MoveToString();
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
        pv.RPC("RPC_UpdateDisplay",RpcTarget.All,Pname,WoundsText,ActionsText, MoveText,WeaponOneText,WeaponTwoText);
    }

    // used to change the display on all clients 
    [PunRPC]
    void RPC_UpdateDisplay(string Pname, string WoundsText, string ActionsText, string MoveText, string WeaponOneText, string WeaponTwoText)
    {
        PlayerName.GetComponent<Text>().text = Pname;
        Health.GetComponent<Text>().text = WoundsText;
        Actions.GetComponent<Text>().text = ActionsText;
        Moves.text = MoveText;
        Text wpOne = WeaponOne.GetComponent<Text>();
        Text wpTwo = WeaponTwo.GetComponent<Text>();
        wpOne.text = WeaponOneText;
        wpTwo.text = WeaponTwoText;
    }
}

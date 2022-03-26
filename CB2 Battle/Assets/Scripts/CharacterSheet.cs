using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

// UI window used for editing characters, can take save data or active map tokens
public class CharacterSheet : MonoBehaviourPunCallbacks
{
    // Reference to all Inputfieldscripts so that the charactersheet only accesses its own inputs
    [SerializeField] private List<InputFieldScript> EntryList;
    // main page reference
    [SerializeField] private GameObject page1;
    // equipment page reference
    [SerializeField] private GameObject page2;
    // editable fields for basic stats like BS,WS etc
    private Dictionary<string, InputFieldScript> TextEntries;
    // reference for editing savedata
    private static CharacterSaveData ActivePlayer; 
    // an indvidual field for displaying weapon stats
    [SerializeField] private GameObject WeaponDisplay;
    // unique inputfield that saves the players name
    [SerializeField] private InputField NameField;
    [SerializeField] private PhotonView pv;
    [SerializeField] private TrackerSheet HealthMonitor;
    [SerializeField] private TrackerSheet StunMonitor;
    [SerializeField] private TrackerSheet EdgeMonitor;
    private PlayerStats CurrentToken;
    private int NPCHealth = -1;
    // Called when created downloads player data onto the sheet and freezes the screen
    public void Init(){
        DmMenu.ActiveCharacterSheet = this;
        CameraButtons.UIFreeze(true);
        transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform);
        transform.localPosition = new Vector3();
        transform.localScale = new Vector3(1,1,1);
        TextEntries = new Dictionary<string, InputFieldScript>();
    }

    // Uploading data is different depending on if we are editing save data of a map token
    public void UpdateStatsOut()
    {
        CameraButtons.UIFreeze(false);
        // if this is called client side send my changes to the server
        if(!pv.IsMine)
        {
            pv.RPC("RPC_SyncStatsOut",RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, NameField.text, ActivePlayer.CompileStats(), ActivePlayer.skillSpecialization, ActivePlayer.compileEquipment().ToArray(), NPCHealth);
        }
        // if this is server side apply the changes
        else 
        {
            // apply changes to npcs
            if(CurrentToken != null && CurrentToken.team != 0)
            {
                CurrentToken.NPCHealth = this.NPCHealth;
                CurrentToken.OnDownload();
            }
            // apply changes to an individual player
            else
            {
                GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
                foreach(GameObject p in players)
                {
                    PlayerStats currentPlayer = p.GetComponent<PlayerStats>();
                    if(currentPlayer.myData.Equals(ActivePlayer))
                    {
                        currentPlayer.OnDownload();
                    }
                }
            }
            PhotonNetwork.Destroy(gameObject);
        }
    }

    // playerstats activeplayer in the initative queue
    // callingplayer id: reference to the player that called for a charactersheet
    // Always called first and on the master client, gets the activeplayer and sends their stats back
    // to the player that called for a charactersheet
    public void UpdateStatsIn(PlayerStats input, int callingPlayerID)
    {
        CurrentToken = input;
        UpdateStatsIn(input.myData,callingPlayerID);
    }
    // Doownloads data from savedata
    public void UpdateStatsIn(CharacterSaveData input, int callingPlayerID)
    {
        // NPCS track health seperately per token
        if(CurrentToken != null && input.team != 0)
        {
            NPCHealth = CurrentToken.getWounds();
        }
        ActivePlayer = input;
        Photon.Realtime.Player CallingPlayer = PhotonNetwork.CurrentRoom.GetPlayer(callingPlayerID);
        //server side edit, no need for transfer notice this is always called by master so isMine doesn't work here
        if(CallingPlayer == pv.Owner)
        {
            UpdateStatsIn();
        }
        // client side edit, transfer charactershetet
        else
        { 
            pv.RPC("RPC_SyncStats", CallingPlayer, input.playername, input.CompileStats(), input.skillSpecialization, input.compileEquipment().ToArray(), NPCHealth);
        }
    }
    // Generic info that both kinds of download needs to know
    public void UpdateStatsIn(){
        Init();
        NameField.text = ActivePlayer.playername;
        
        ActivePlayer.CalculateCharacteristics();
        HealthMonitor.Init();
        StunMonitor.Init();
        EdgeMonitor.Init();
        StunMonitor.SetResource(ActivePlayer.GetAttribute(AttributeKey.SDamage));
        EdgeMonitor.SetResource(ActivePlayer.GetAttribute(AttributeKey.Edge));

        if(NPCHealth >= 0)
        {
            HealthMonitor.SetResource(NPCHealth);
        }
        else
        {
            HealthMonitor.SetResource(ActivePlayer.GetAttribute(AttributeKey.PDamage));
        }
        UpdateInputFields();
        //AddItem("Cyberdeck");
        SkillAdder.DownloadOwner(ActivePlayer);
        ItemAdder.DownloadOwner(ActivePlayer);
        page2.SetActive(false);
    }

    public void AddHealth()
    {
        if(NPCHealth >= 0)
        {
            if(NPCHealth < 18)
            {
                HealthMonitor.IncrementValue();
                NPCHealth++;
            }
        }
        else
        {
            if(ActivePlayer.attribues[(int)AttributeKey.PDamage] < 18)
            {
                HealthMonitor.IncrementValue();
                ActivePlayer.attribues[(int)AttributeKey.PDamage]++;
            }
        }
    }

    public void DecreaseHealth()
    {
        if(NPCHealth >= 0)
        {
            if(NPCHealth > 0)
            {
                HealthMonitor.SubtractValue();
                NPCHealth--;
            }
        }
        else
        {
            if(ActivePlayer.attribues[(int)AttributeKey.PDamage] > 0)
            {
                HealthMonitor.SubtractValue();
                ActivePlayer.attribues[(int)AttributeKey.PDamage]--;
            }
        }
    }

    public void AddStun()
    {
        if(ActivePlayer.attribues[(int)AttributeKey.SDamage] < 12)
        {
            StunMonitor.IncrementValue();
            ActivePlayer.attribues[(int)AttributeKey.SDamage]++;
        }
    }

    public void DecreaseStun()
    {
        if(ActivePlayer.attribues[(int)AttributeKey.SDamage] > 0)
        {
            StunMonitor.SubtractValue();
            ActivePlayer.attribues[(int)AttributeKey.SDamage]--;
        }
    }

    public void AddEdge()
    {
        if(ActivePlayer.attribues[(int)AttributeKey.CurrentEdge] < 7)
        {
            EdgeMonitor.IncrementValue();
            ActivePlayer.attribues[(int)AttributeKey.CurrentEdge]++;
        }
    }

    public void DecreaseEdge()
    {
        if(ActivePlayer.attribues[(int)AttributeKey.CurrentEdge] > 0)
        {
            EdgeMonitor.SubtractValue();
            ActivePlayer.attribues[(int)AttributeKey.CurrentEdge]--;
        }
    }

    public void UpdateName()
    {
        ActivePlayer.playername = NameField.text;
    }

    public void UpdatedAttribute(AttributeKey key, int value)
    {
        ActivePlayer.SetAttribute(key,value,true);
        UpdateInputFields();
        SkillAdder.UpdateSkillFields();
    } 

    private void UpdateInputFields()
    {
        foreach (InputFieldScript t in EntryList)
        {
            t.UpdateValue(ActivePlayer,this);
        }
        HealthMonitor.SetMaximum(ActivePlayer.GetAttribute(AttributeKey.PhysicalHealth));
        StunMonitor.SetMaximum(ActivePlayer.GetAttribute(AttributeKey.StunHealth));
        EdgeMonitor.SetMaximum(ActivePlayer.GetAttribute(AttributeKey.Edge));
    }

    // w: a weapon gameobject that was added itemadder 
    // location: where the weapon ought to be placed
    // creates a weapon display seperate from the regular equipment area
    private GameObject CreateWeapon(Weapon w, Vector3 location)
    {
        GameObject newEntry = Instantiate(WeaponDisplay) as GameObject;
        newEntry.transform.SetParent(gameObject.transform, false);
        newEntry.transform.localPosition = location;
        newEntry.GetComponent<WeaponInputScript>().UpdateIn(w);
        return newEntry;
    }

    // Master sends this to the client to be edited
    [PunRPC]
    void RPC_SyncStats(string name, string[] characteristics, Dictionary<string,int> specalizations, string[] equipment, int newNPCHealth)
    {
        List<string> convertedArray = new List<string>();
        for(int i = 0; i < equipment.Length; i++)
        {
            convertedArray.Add(equipment[i]);
        }
        ActivePlayer = new CharacterSaveData(name, characteristics, specalizations, convertedArray);
        if(newNPCHealth >= 0)
        {
            Debug.Log("Unique npc health" + newNPCHealth);
            NPCHealth = newNPCHealth;
        }
        UpdateStatsIn();
    }

    // Client sends this to the master to be saved 
    [PunRPC]
    void RPC_SyncStatsOut(int callingPlayerID, string newName, string[] newCharacteristics, Dictionary<string,int> newSpecalizations, string[] newequipment, int newNPCHealth)
    {
        DmMenu.RemoveClientSheet(callingPlayerID);
        ActivePlayer.playername = newName;
        ActivePlayer.skillSpecialization = newSpecalizations;
        List<string> convertedArray = new List<string>();
        for(int i = 0; i < newequipment.Length; i++)
        {
            convertedArray.Add(newequipment[i]);
        }
        ActivePlayer.decompileEquipment(convertedArray);
        ActivePlayer.DecompileStats(newCharacteristics);
        NPCHealth = newNPCHealth;
        UpdateStatsOut();
    }

}

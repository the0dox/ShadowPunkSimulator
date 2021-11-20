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
    // Reference to all skillinputfields so that the charactersheet can download its save data into it
    [SerializeField] private List<SkillScript> SkillEntryList;
    // main page reference
    [SerializeField] private GameObject page1;
    // equipment page reference
    [SerializeField] private GameObject page2;
    // editable fields for basic stats like BS,WS etc
    private Dictionary<string, InputFieldScript> TextEntries;
    private Dictionary<string, ItemInputField> ItemEntries;
    private List<GameObject> WeaponEntries;
    // reference for editing savedata
    private static CharacterSaveData ActivePlayer; 
    // an indvidual field for displaying weapon stats
    [SerializeField] private GameObject WeaponDisplay;
    // unique inputfield that saves the players name
    [SerializeField] private InputField NameField;
    // reference to the skillinputfield prefab
    [SerializeField] private GameObject SkillInputButton;
    // Stack used to preserve order all skills when one is removed
    private List<GameObject> BasicSkills;
    private Stack<GameObject> LastSkills;
    // Individual displays of the names/quantities of each item
    [SerializeField] private GameObject ItemDisplay;
    // Vectors are used to ensure proper spacing of ui elements when new skills/items are added
    [SerializeField] private PhotonView pv;
    // dicitionary form of map tokens stats for translating into the sheet
    private Dictionary<string, int> PlayerStats;
    
    // list of original players skills for translating into the sheet
    private Dictionary<string, int> PlayerSkills;
    // same but for equipment
    private Dictionary<string,int> PlayerEquipment;
    [SerializeField] private TrackerSheet HealthMonitor;
    [SerializeField] private TrackerSheet StunMonitor;
    [SerializeField] private TrackerSheet EdgeMonitor;
    // Called when created downloads player data onto the sheet and freezes the screen
    public void Init(){
        CameraButtons.UIFreeze(true);
        transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform);
        transform.localPosition = new Vector3();
        transform.localScale = new Vector3(1,1,1);
        LastSkills = new Stack<GameObject>();
        BasicSkills = new List<GameObject>();
        ItemEntries = new Dictionary<string, ItemInputField>();
        TextEntries = new Dictionary<string, InputFieldScript>();
        foreach (InputFieldScript t in EntryList)
        {
            TextEntries.Add(t.GetStat(), t);
        }
    }

    // Uploading data is different depending on if we are editing save data of a map token
    public void UpdateStatsOut()
    {
        CameraButtons.UIFreeze(false);
        if(!pv.IsMine)
        {
            pv.RPC("RPC_SyncStatsOut",RpcTarget.MasterClient, NameField.text, PlayerStats,PlayerSkills, ActivePlayer.skillSpecialization, ActivePlayer.compileEquipment());
        }
        Destroy(gameObject);
    }

    // playerstats activeplayer in the initative queue
    // callingplayer id: reference to the player that called for a charactersheet
    // Always called first and on the master client, gets the activeplayer and sends their stats back
    // to the player that called for a charactersheet
    public void UpdateStatsIn(PlayerStats input, int callingPlayerID)
    {
        UpdateStatsIn(input.myData,callingPlayerID);
    }
    // Doownloads data from savedata
    public void UpdateStatsIn(CharacterSaveData input, int callingPlayerID)
    {
        ActivePlayer = input;
        if(pv.IsMine)
        {
            UpdateStatsIn();
        }
        else
        { 
            Photon.Realtime.Player CallingPlayer = PhotonNetwork.CurrentRoom.GetPlayer(callingPlayerID);
            pv.RPC("RPC_SyncStats", CallingPlayer, input.playername, input.GetStats(), input.GetSkills(), input.skillSpecialization, input.compileEquipment());
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
        HealthMonitor.SetResource(ActivePlayer.GetAttribute(AttributeKey.PDamage));
        StunMonitor.SetResource(ActivePlayer.GetAttribute(AttributeKey.SDamage));
        EdgeMonitor.SetResource(ActivePlayer.GetAttribute(AttributeKey.Edge));
        UpdateInputFields();
        //AddItem("Cyberdeck");
        SkillAdder.DownloadOwner(ActivePlayer);
        ItemAdder.DownloadOwner(ActivePlayer);
        MainButton();
    }

    public void AddHealth()
    {
        if(ActivePlayer.attribues[AttributeKey.PDamage] < 18)
        {
            HealthMonitor.IncrementValue();
            ActivePlayer.attribues[AttributeKey.PDamage]++;
        }
    }

    public void DecreaseHealth()
    {
        if(ActivePlayer.attribues[AttributeKey.PDamage] > 0)
        {
            HealthMonitor.SubtractValue();
            ActivePlayer.attribues[AttributeKey.PDamage]--;
        }
    }

    public void AddStun()
    {
        if(ActivePlayer.attribues[AttributeKey.SDamage] < 12)
        {
            StunMonitor.IncrementValue();
            ActivePlayer.attribues[AttributeKey.SDamage]++;
        }
    }

    public void DecreaseStun()
    {
        if(ActivePlayer.attribues[AttributeKey.SDamage] > 0)
        {
            StunMonitor.SubtractValue();
            ActivePlayer.attribues[AttributeKey.SDamage]--;
        }
    }

    public void AddEdge()
    {
        if(ActivePlayer.attribues[AttributeKey.CurrentEdge] < 7)
        {
            EdgeMonitor.IncrementValue();
            ActivePlayer.attribues[AttributeKey.CurrentEdge]++;
        }
    }

    public void DecreaseEdge()
    {
        if(ActivePlayer.attribues[AttributeKey.CurrentEdge] > 0)
        {
            EdgeMonitor.SubtractValue();
            ActivePlayer.attribues[AttributeKey.CurrentEdge]--;
        }
    }

    public void UpdateName()
    {
        ActivePlayer.playername = NameField.text;
    }

    public void UpdatedAttribute(string key, int value)
    {
        ActivePlayer.SetAttribute(key,value,true);
        UpdateInputFields();
        SkillAdder.UpdateSkillFields();
    } 

    private void UpdateInputFields()
    {
        foreach (KeyValuePair<string, InputFieldScript> kvp in TextEntries)
        {
            kvp.Value.UpdateValue(ActivePlayer.GetAttribute(kvp.Key), this);   
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

    public void AddItem(string name)
    {
        string[] dummyTest = new string[1];
        dummyTest[0] = "1";
        ActivePlayer.AddItem(ItemReference.GetItem(name,1,dummyTest));
    }

    public void MainButton()
    {
        page1.SetActive(true);
        page2.SetActive(false);
    }

    public void EquipmentButton()
    {
        page1.SetActive(false);
        page2.SetActive(true);
    }

    public void Remove(string removedItem)
    {
        PlayerEquipment.Remove(removedItem);
    }

    // Master sends this to the client to be edited
    [PunRPC]
    void RPC_SyncStats(string name, Dictionary<string,int> characteristics, Dictionary<string, int> skills, Dictionary<string,int> specalizations, List<string> equipment)
    {
        ActivePlayer = new CharacterSaveData(name, characteristics, skills, specalizations, equipment);
        UpdateStatsIn();
    }

    // Client sends this to the master to be saved 
    [PunRPC]
    void RPC_SyncStatsOut(string newName,  Dictionary<string, int> newCharacteristics, Dictionary<string,int> newskills, Dictionary<string,int> newSpecalizations, List<string> newequipment)
    {
        ActivePlayer.playername = newName;
        ActivePlayer.attribues = newCharacteristics;
        ActivePlayer.skills = newskills;
        ActivePlayer.skillSpecialization = newSpecalizations;
        ActivePlayer.decompileEquipment(newequipment);
        UpdateStatsOut();
    }
}

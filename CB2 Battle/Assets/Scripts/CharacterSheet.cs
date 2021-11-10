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
    // Reference to the Item Adder button so items can be created
    [SerializeField] private GameObject ItemAdder;
    // Individual displays of the names/quantities of each item
    [SerializeField] private GameObject ItemDisplay;
    // Vectors are used to ensure proper spacing of ui elements when new skills/items are added
    [SerializeField] private PhotonView pv;
    //references for ui Movement
    private Vector3 weaponDisplacementRight = new Vector3(0,98.5f,0);
    private Vector3 weaponDisplacementLeft = new Vector3(0,82.5f,0);
    private Vector3 SkillDisplacement = new Vector3(0,14.5f,0);
    private Vector3 startingPos = new Vector3(-300,120,0);
    private Vector3 ItemDisplacement = new Vector3(0,-16,0);
    Vector3 PlacementPosAdvanced;
    Vector3 PlacementPosBasic;
    Vector3 PlacementWeaponRanged;
    Vector3 PlacementWeaponMelee;
    Vector3 PlacementItems;
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
        ItemAdder.GetComponent<WeaponAdder>().Init();
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
            pv.RPC("RPC_SyncStatsOut",RpcTarget.MasterClient, NameField.text, PlayerStats,PlayerSkills, ActivePlayer.skillSpecialization, PlayerEquipment);
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
            pv.RPC("RPC_SyncStats", CallingPlayer, input.playername, input.GetStats(), input.GetSkills(), input.skillSpecialization);
        }
    }
    // Generic info that both kinds of download needs to know
    public void UpdateStatsIn(){
        Init();
        NameField.text = ActivePlayer.playername;
        PlacementPosAdvanced = new Vector3(149.5f, 166,0);
        PlacementPosBasic = new Vector3(-258, 193,0);
        PlacementWeaponRanged = new Vector3(783, 212,0);
        PlacementWeaponMelee = new Vector3(375, 209,0);
        PlacementItems = new Vector3(780,-202,0);
        
        ActivePlayer.CalculateCharacteristics();
        HealthMonitor.Init();
        StunMonitor.Init();
        EdgeMonitor.Init();

        UpdateInputFields();
        SkillAdder.DownloadOwner(ActivePlayer);
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

    public void CreateItem(string name, int stack)
    {
        GameObject newText = Instantiate(ItemDisplay) as GameObject;
        newText.GetComponent<ItemInputField>().UpdateIn(name, stack, this);
        ItemEntries.Add(name, newText.GetComponent<ItemInputField>());
        newText.transform.SetParent(gameObject.transform);
        newText.transform.localPosition = PlacementItems;
        PlacementItems += ItemDisplacement;
    }

    // Takes the current selection in the item adder button and creates an item out of it
    public void CreateItem()
    {
        CreateItem(ItemAdder.GetComponent<WeaponAdder>().GetItem().GetName());
    }

    // given an Item from either the item adder or savedata, places it on the equipment display
    public void CreateItem(string itemName)
    {
        if(!PlayerEquipment.ContainsKey(itemName))
        {
            PlayerEquipment.Add(itemName, 0);
            CreateItem(itemName, 0);
        }
        PlayerEquipment[itemName] += 1;
        ItemEntries[itemName].UpdateStacks(PlayerEquipment[itemName]);
        /* stackable items just automatically stack onto exisiting items of the same type
        if(input.Stackable())
        {
            foreach(Item i in Equipment)
            {
                if(i.GetName().Equals(input.GetName()))
                {
                    i.AddStack();
                    stacked = true;
                }
            }
        }
        // unique items like weapons that can be dual wielded can't be stacked
        if(!stacked)
        {
            Equipment.Add(input);

            if(input.GetType() == typeof(Weapon))
            {
                Weapon newWeapon = (Weapon) input;
                if(newWeapon.GetClass().Equals("Melee"))
                {
                    CreateWeapon(newWeapon, PlacementWeaponMelee);
                    PlacementWeaponMelee -= weaponDisplacementLeft;
                }
                else
                {
                    CreateWeapon(newWeapon, PlacementWeaponRanged);
                    PlacementWeaponRanged -= weaponDisplacementRight;
                }
            }
            GameObject newText = Instantiate(ItemDisplay) as GameObject;
            newText.GetComponent<ItemInputField>().UpdateIn(input,this);
            newText.transform.SetParent(gameObject.transform);
            newText.transform.localPosition = PlacementItems;
            PlacementItems += ItemDisplacement;
        }
        */
    }

    public void Remove(string removedItem)
    {
        PlayerEquipment.Remove(removedItem);
    }

    // Master sends this to the client to be edited
    [PunRPC]
    void RPC_SyncStats(string name, Dictionary<string,int> characteristics, Dictionary<string, int> skills, Dictionary<string,int> specalizations)
    {
        ActivePlayer = new CharacterSaveData(name, characteristics, skills, specalizations);
        UpdateStatsIn();
    }

    // Client sends this to the master to be saved 
    [PunRPC]
    void RPC_SyncStatsOut(string newName,  Dictionary<string, int> newCharacteristics, Dictionary<string,int> newskills, Dictionary<string,int> newSpecalizations, Dictionary<string,int> newEquipment)
    {
        ActivePlayer.playername = newName;
        ActivePlayer.attribues = newCharacteristics;
        ActivePlayer.skills = newskills;
        ActivePlayer.skillSpecialization = newSpecalizations;
        UpdateStatsOut();
    }
}

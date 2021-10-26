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
    // editable fields for basic stats like BS,WS etc
    private Dictionary<string, InputFieldScript> TextEntries;
    private Dictionary<string, ItemInputField> ItemEntries;
    private List<GameObject> WeaponEntries;
    // reference for editing savedata
    private static CharacterSaveData ActivePlayer; 
    // reference for editing map tokens
    private static PlayerStats ActivePlayerStats; 
    // an indvidual field for displaying weapon stats
    [SerializeField] private GameObject WeaponDisplay;
    // unique inputfield that saves the players name
    [SerializeField] private InputField NameField;
    // reference to the skillinputfield prefab
    [SerializeField] private GameObject SkillInputButton;
    // reference to the skill adder button so that its position can be modified
    [SerializeField] private GameObject SkillAdderButton;
    // Stack used to preserve order all skills when one is removed
    private List<GameObject> BasicSkills;
    private Stack<GameObject> LastSkills;
    // Dropdown used to select what skill is created by the skill adder button
    [SerializeField] private Dropdown SkillAdderName;
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
    // players original name
    private string PlayerDisplayName;
    // Called when created downloads player data onto the sheet and freezes the screen
    public void Init(){
        CameraButtons.UIFreeze(true);
        transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform);
        transform.localPosition = startingPos;
        transform.localScale = new Vector3(1,1,1);
        LastSkills = new Stack<GameObject>();
        BasicSkills = new List<GameObject>();
        ItemEntries = new Dictionary<string, ItemInputField>();
        ItemAdder.GetComponent<WeaponAdder>().Init();
        UpdateSkillDropdown();
        TextEntries = new Dictionary<string, InputFieldScript>();
        foreach (InputFieldScript t in EntryList)
        {
            TextEntries.Add(t.GetStat(), t);
        }
    }

    public void OnButtonPressed()
    {
        PlayerStats = new Dictionary<string, int>();
        foreach (KeyValuePair<string, InputFieldScript> kvp in TextEntries){
            int newValue = kvp.Value.GetValue();
            PlayerStats.Add(kvp.Key, newValue);
        }
        PlayerSkills = new Dictionary<string, int>();
        GameObject[] EntryListSkillsBasic = BasicSkills.ToArray(); 
        GameObject[] EntryListSkills = LastSkills.ToArray(); 
        foreach (GameObject g in EntryListSkillsBasic)
        {
            Skill currentSkill = g.GetComponent<SkillScript>().GetSkill();
            PlayerSkills.Add(currentSkill.name,currentSkill.levels);
            Destroy(g);
        }
        //skills
        foreach (GameObject g in EntryListSkills)
        {
            Skill currentSkill = g.GetComponent<SkillScript>().GetSkill();
            PlayerSkills.Add(currentSkill.name,currentSkill.levels);
            Destroy(g);
        }
        UpdateStatsOut();
    }

    // Uploading data is different depending on if we are editing save data of a map token
    public void UpdateStatsOut()
    {
        CameraButtons.UIFreeze(false);
        if(pv.IsMine)
        {
            if(ActivePlayerStats != null)
            {
                UpdateStatsOut(ActivePlayerStats);
            }
            else if(ActivePlayer != null)
            {
                UpdateStatsOut(ActivePlayer);
            }
        }
        else
        {
            pv.RPC("RPC_SyncStatsOut",RpcTarget.MasterClient, NameField.text, PlayerStats,PlayerSkills,PlayerEquipment);
            Destroy(gameObject);
        }
    }

    //transfers sheet info to player for a map token
    public void UpdateStatsOut(PlayerStats output){
        output.playername = NameField.text;
        Dictionary<string, int> newCharacteristics = new Dictionary<string, int>();
        output.Stats.Clear();
        Debug.Log(PlayerStats.Count);
        foreach (KeyValuePair<string, int> kvp in PlayerStats){
            output.SetStat(kvp.Key, kvp.Value);
        }
        output.Skills.Clear();
        foreach(KeyValuePair<string,int> kvp in PlayerSkills)
        {
            Skill newSkill = new Skill(SkillReference.GetSkill(kvp.Key), kvp.Value);
            output.Skills.Add(newSkill);
        }
        output.equipment = ItemReference.DownloadEquipment(PlayerEquipment);
        Destroy(gameObject);
        /*
        GameObject[] EntryListSkills = GameObject.FindGameObjectsWithTag("Skill");
        PlayerSkills.Clear();    
        //skills
        foreach (GameObject g in EntryListSkills)
        {
            SkillScript input = g.GetComponent<SkillScript>();
            PlayerSkills.Add(input.GetSkill());
            Destroy(g);
        }
        PlayerEquipment.Clear();
        //weapons
        foreach(Item i in Equipment)
        {
            PlayerEquipment.Add(i);
        }
        */
    }

    //transfers sheet info to player for save data
    public void UpdateStatsOut(CharacterSaveData output){
        output.playername = NameField.text;
        output.ClearSkills();
        output.ClearEquipment();
        //characterisitcs
        foreach (KeyValuePair<string, int> kvp in PlayerStats){
            ActivePlayer.SetStat(kvp.Key, kvp.Value);
        }
        GameObject[] EntryListSkills = GameObject.FindGameObjectsWithTag("Skill"); 
        //skills
        foreach(KeyValuePair<string,int> kvp in PlayerSkills)
        {
            Skill newSkill = new Skill(SkillReference.GetSkill(kvp.Key), kvp.Value);
            ActivePlayer.addSkill(newSkill);   
        }
        //weapons
        ActivePlayer.AddEquipment(PlayerEquipment);
        Destroy(gameObject);
    }

    // playerstats activeplayer in the initative queue
    // callingplayer id: reference to the player that called for a charactersheet
    // Always called first and on the master client, gets the activeplayer and sends their stats back
    // to the player that called for a charactersheet
    public void UpdateStatsIn(PlayerStats input, int callingPlayerID)
    {
        ActivePlayerStats = input;
        PlayerSkills = new Dictionary<string, int>();
        foreach(Skill s in input.Skills)
        {
            PlayerSkills.Add(s.name, s.levels);
        }
        PlayerEquipment = new Dictionary<string, int>();
        foreach(Item i in input.equipment)
        {
            PlayerEquipment.Add(i.GetName(),i.GetStacks());
        }
        Photon.Realtime.Player CallingPlayer = PhotonNetwork.CurrentRoom.GetPlayer(callingPlayerID);
        pv.RPC("RPC_SyncStats", CallingPlayer, input.playername, input.Stats, PlayerSkills, PlayerEquipment);
        /*
        ActivePlayerStats = input;
        PlayerStats = input.Stats;
        PlayerSkills = input.Skills;
        PlayerEquipment = input.equipment;
        PlayerDisplayName = input.playername;
        UpdateStatsIn();
        */
    }
    // Doownloads data from savedata
    public void UpdateStatsIn(CharacterSaveData input, int callingPlayerID)
    {
        ActivePlayer = input;
        PlayerStats = input.GetStats();
        PlayerSkills = input.GetSkills();
        PlayerEquipment = input.GetEquipment();
        PlayerDisplayName = input.playername;
        Photon.Realtime.Player CallingPlayer = PhotonNetwork.CurrentRoom.GetPlayer(callingPlayerID);
        pv.RPC("RPC_SyncStats", CallingPlayer, input.playername, PlayerStats, PlayerSkills, PlayerEquipment);
    }
    // Generic info that both kinds of download needs to know
    public void UpdateStatsIn(){
        Init();
        NameField.text = PlayerDisplayName;
        PlacementPosAdvanced = new Vector3(149.5f, 166,0);
        PlacementPosBasic = new Vector3(-258, 193,0);
        PlacementWeaponRanged = new Vector3(783, 212,0);
        PlacementWeaponMelee = new Vector3(375, 209,0);
        PlacementItems = new Vector3(780,-202,0);
        SkillAdderButton.transform.localPosition = new Vector3(-16,164,0);
        //characterisitcs
        foreach (KeyValuePair<string, int> kvp in PlayerStats){
            if (TextEntries.ContainsKey(kvp.Key))
            {
                TextEntries[kvp.Key].UpdateValue(kvp.Value);   
            }
        }
        //skills
        foreach (KeyValuePair<string,int> kvp in PlayerSkills){
            Skill s = new Skill(SkillReference.GetSkill(kvp.Key), kvp.Value);
            if(s.visible)
            {
                if (s.basic)
                {
                    BasicSkills.Add(CreateSkill(s, PlacementPosBasic));
                    PlacementPosBasic -= SkillDisplacement;
                }
                else
                {
                    LastSkills.Push(CreateSkill(s, PlacementPosAdvanced));
                    PlacementPosAdvanced -= SkillDisplacement;
                    SkillAdderButton.transform.localPosition -= SkillDisplacement;
                }
            }
            else
            {
                GameObject hiddenSkill = CreateSkill(s,PlacementPosBasic);
                BasicSkills.Add(hiddenSkill);
                hiddenSkill.transform.SetParent(null);
            }
        }
        foreach(KeyValuePair<string, int> i in PlayerEquipment)
        {
            CreateItem(i.Key, i.Value);
        }
    }
    public bool IsInitalized(){
        return TextEntries != null; 
    }

    // input: a skill gameobject that was added by skilladderbutton
    // location: where the skill ought to be placed relative to the charactersheet
    // creates a skill input field at the designated location and increments the position for the next skill
    private GameObject CreateSkill(Skill input, Vector3 location)
    {
        GameObject newEntry = Instantiate(SkillInputButton) as GameObject;
        newEntry.transform.SetParent(gameObject.transform, false);
        newEntry.transform.localPosition = location;
        newEntry.GetComponent<SkillScript>().UpdateValue(input);
        return newEntry;
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

    // used to create advanced skills, should say buttonright not left
    public void CreateSkillButtonLeft()
    {
        if(LastSkills.Count < 24)
        {
            LastSkills.Push(CreateSkill(new Skill(SkillReference.GetSkill(SkillAdderName.captionText.text),1), PlacementPosAdvanced));
            PlacementPosAdvanced -= SkillDisplacement;
            SkillAdderButton.transform.localPosition -= SkillDisplacement;
        }
    }

    // uploads the names of all advanced skills to the dropdown, to be converted into skills later
    public void UpdateSkillDropdown()
    {
        SkillAdderName.ClearOptions();
        Dictionary<string, SkillTemplate> d = SkillReference.SkillsTemplates();
        List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
        foreach(string key in d.Keys)
        {
            if(!d[key].basic)
            {
                Dropdown.OptionData newData = new Dropdown.OptionData(); 
                newData.text = key;
                results.Add(newData);
            }
        }
        SkillAdderName.AddOptions(results);
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

    // Deletes the last skill in the queue and updates the position of the ui elements accordingly 
    public void DeleteSkill()
    {
        if(LastSkills.Count > 0)
        {
            Destroy(LastSkills.Pop());
            PlacementPosAdvanced += SkillDisplacement;
            SkillAdderButton.transform.localPosition += SkillDisplacement;
        }
    }

    // Master sends this to the client 
    [PunRPC]
    void RPC_SyncStats(string name, Dictionary<string,int> characteristics, Dictionary<string, int> skills, Dictionary<string, int> equipment)
    {
        PlayerDisplayName = name;
        PlayerStats = characteristics;
        PlayerSkills = skills;
        PlayerEquipment = equipment;
        UpdateStatsIn();
    }

    // Client sends this to the master
    [PunRPC]
    void RPC_SyncStatsOut(string newName,  Dictionary<string, int> newCharacteristics, Dictionary<string,int> newskills, Dictionary<string,int> newEquipment)
    {
        NameField.text = newName;
        PlayerStats = newCharacteristics;
        PlayerSkills = newskills;
        PlayerEquipment = newEquipment;
        UpdateStatsOut();
    }
}

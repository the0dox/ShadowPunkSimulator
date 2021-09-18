using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSheet : MonoBehaviour
{
    private Dictionary<string, InputFieldScript> TextEntries;
    
    private Dictionary<string, SkillScript> BasicSkillEntries;
    
    private Dictionary<int, WeaponInputScript> WeaponEntries;

    private static CharacterSaveData ActivePlayer; 
    private static PlayerStats ActivePlayerStats; 
    [SerializeField] private GameObject WeaponDisplay;
    [SerializeField] private InputField NameField;
    [SerializeField] private GameObject SkillInputButton;
    [SerializeField] private GameObject SkillAdderButton;
    private Stack<GameObject> LastSkills;
    private List<Item> Equipment;
    [SerializeField] private Dropdown SkillAdderName;
    [SerializeField] private GameObject ItemAdder;
    [SerializeField] private GameObject ItemDisplay;
    private Vector3 weaponDisplacementRight = new Vector3(0,98.5f,0);
    private Vector3 weaponDisplacementLeft = new Vector3(0,82.5f,0);
    private Vector3 SkillDisplacement = new Vector3(0,14.5f,0);
    private Vector3 startingPos = new Vector3(-227.6f,73.3f,0);
    private Vector3 ItemDisplacement = new Vector3(0,-16,0);
    Vector3 PlacementPosAdvanced;
    Vector3 PlacementPosBasic;
    Vector3 PlacementWeaponRanged;
    Vector3 PlacementWeaponMelee;
    Vector3 PlacementItems;
    private Dictionary<string, int> PlayerStats;
    private List<Skill> PlayerSkills;
    private List<Item> PlayerEquipment;
    private string PlayerDisplayName;
    public void Init(){
        CameraButtons.UIFreeze(true);
        transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform);
        transform.localPosition = startingPos;
        LastSkills = new Stack<GameObject>();
        Equipment = new List<Item>();
        ItemAdder.GetComponent<WeaponAdder>().Init();
        UpdateSkillDropdown();
        GameObject[] EntryList = GameObject.FindGameObjectsWithTag("Input");
        TextEntries = new Dictionary<string, InputFieldScript>();
        foreach (GameObject g in EntryList)
        {
            InputFieldScript t = g.GetComponent<InputFieldScript>();
            TextEntries.Add(t.GetStat(), t);
        }
        
        /*
            WeaponEntries = new Dictionary<int, WeaponInputScript>();
            WeaponEntries.Add(0, GameObject.FindGameObjectWithTag("WeaponInput").GetComponent<WeaponInputScript>());
        */
    
    }

    public void UpdateStatsOut()
    {
        CameraButtons.UIFreeze(false);
        if(ActivePlayerStats != null)
        {
            UpdateStatsOut(ActivePlayerStats);
        }
        else if(ActivePlayer != null)
        {
            UpdateStatsOut(ActivePlayer);
        }
    }

    //transfers sheet info to player
    public void UpdateStatsOut(PlayerStats output){
        output.playername = NameField.text;
        //characterisitcs
        foreach (KeyValuePair<string, InputFieldScript> kvp in TextEntries){
            int newValue = kvp.Value.GetValue();
            output.SetStat(kvp.Key, newValue);
        }
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
        List<Weapon> newEquipment = new List<Weapon>();
        //weapons
        foreach(Item i in Equipment)
        {
            PlayerEquipment.Add(i);
        }
        Destroy(gameObject);
    }

    //transfers sheet info to player
    public void UpdateStatsOut(CharacterSaveData output){
        output.playername = NameField.text;
        output.ClearSkills();
        output.ClearEquipment();
        //characterisitcs
        foreach (KeyValuePair<string, InputFieldScript> kvp in TextEntries){
            int newValue = kvp.Value.GetValue();
            ActivePlayer.SetStat(kvp.Key, newValue);
        }
        GameObject[] EntryListSkills = GameObject.FindGameObjectsWithTag("Skill"); 
        //skills
        foreach (GameObject g in EntryListSkills)
        {
            SkillScript input = g.GetComponent<SkillScript>();
            output.addSkill(input.GetSkill());
            Destroy(g);
        }
        List<Item> newEquipment = new List<Item>();
        //weapons
        foreach(Item i in Equipment)
        {
            newEquipment.Add(i);
        }
        ActivePlayer.AddEquipment(newEquipment);
        ActivePlayer = null;
        Destroy(gameObject);
    }

    public void UpdateStatsIn(PlayerStats input)
    {
        Init();
        ActivePlayerStats = input;
        PlayerStats = input.Stats;
        PlayerSkills = input.Skills;
        PlayerEquipment = input.equipment;
        PlayerDisplayName = input.playername;
        UpdateStatsIn();
    }
    public void UpdateStatsIn(CharacterSaveData input)
    {
        Init();
        ActivePlayer = input;
        PlayerStats = input.GetStats();
        PlayerSkills = input.GetSkills();
        PlayerEquipment = input.GetEquipment();
        PlayerDisplayName = input.playername;
        UpdateStatsIn();
    }
    public void UpdateStatsIn(){
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
        foreach (Skill s in PlayerSkills){
            if(s.visible)
            {
                if (s.basic)
                {
                    CreateSkill(s, PlacementPosBasic);
                    PlacementPosBasic -= SkillDisplacement;
                }
                else
                {
                    LastSkills.Push(CreateSkill(s, PlacementPosAdvanced));
                    PlacementPosAdvanced -= SkillDisplacement;
                    SkillAdderButton.transform.localPosition -= SkillDisplacement;
                }
            }
        }
        foreach(Item i in PlayerEquipment)
        {
            CreateItem(i);
        }
    }
    public bool IsInitalized(){
        return TextEntries != null; 
    }

    private GameObject CreateSkill(Skill input, Vector3 location)
    {
        GameObject newEntry = Instantiate(SkillInputButton) as GameObject;
        newEntry.transform.SetParent(gameObject.transform, false);
        newEntry.transform.localPosition = location;
        newEntry.GetComponent<SkillScript>().UpdateValue(input);
        return newEntry;
    }

    private GameObject CreateWeapon(Weapon w, Vector3 location)
    {
        GameObject newEntry = Instantiate(WeaponDisplay) as GameObject;
        newEntry.transform.SetParent(gameObject.transform, false);
        newEntry.transform.localPosition = location;
        newEntry.GetComponent<WeaponInputScript>().UpdateIn(w);
        return newEntry;
    }

    public void CreateSkillButtonLeft()
    {
        if(LastSkills.Count < 24)
        {
            LastSkills.Push(CreateSkill(new Skill(SkillReference.GetSkill(SkillAdderName.captionText.text),1), PlacementPosAdvanced));
            PlacementPosAdvanced -= SkillDisplacement;
            SkillAdderButton.transform.localPosition -= SkillDisplacement;
        }
    }

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
    public void CreateItem()
    {
        CreateItem(ItemAdder.GetComponent<WeaponAdder>().GetItem());
    }

    public void CreateItem(Item input)
    {
        bool stacked = false;
        if(input.Stackable())
        {
            Debug.Log(input.GetName() +"can be stacked");
            foreach(Item i in Equipment)
            {
                if(i.GetName().Equals(input.GetName()))
                {
                    i.AddStack();
                    Debug.Log("character now has " + i.GetStacks() + " " + i.GetName());
                    stacked = true;
                }
            }
        }
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
            newText.GetComponent<ItemInputField>().UpdateIn(input);
            newText.transform.SetParent(gameObject.transform);
            newText.transform.localPosition = PlacementItems;
            PlacementItems += ItemDisplacement;
        }
    }

    public void DeleteSkill()
    {
        if(LastSkills.Count > 0)
        {
            Destroy(LastSkills.Pop());
            PlacementPosAdvanced += SkillDisplacement;
            SkillAdderButton.transform.localPosition += SkillDisplacement;
        }
    }
}

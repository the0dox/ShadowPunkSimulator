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
    private Stack<GameObject> LastMeleeWeps;
    private Stack<GameObject> LastRangedWeps;
    [SerializeField] private Dropdown SkillAdderName;
    [SerializeField] private GameObject WeaponAdderMelee;
    [SerializeField] private GameObject WeaponAdderRanged;
    private Vector3 weaponDisplacementRight = new Vector3(0,98.5f,0);
    private Vector3 weaponDisplacementLeft = new Vector3(0,82.5f,0);
    private Vector3 SkillDisplacement = new Vector3(0,14.5f,0);
    private Vector3 startingPos = new Vector3(-227.6f,73.3f,0);
    Vector3 PlacementPosAdvanced;
    Vector3 PlacementPosBasic;
    Vector3 PlacementWeaponRanged;
    Vector3 PlacementWeaponMelee;
    private Dictionary<string, int> PlayerStats;
    private List<Skill> PlayerSkills;
    private List<Weapon> PlayerWeapons;
    private string PlayerDisplayName;
    public void Init(){
        CameraButtons.UIFreeze(true);
        transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform);
        transform.localPosition = startingPos;
        LastSkills = new Stack<GameObject>();
        LastRangedWeps = new Stack<GameObject>();
        LastMeleeWeps = new Stack<GameObject>();
        WeaponAdderMelee.GetComponent<WeaponAdder>().Init();
        WeaponAdderRanged.GetComponent<WeaponAdder>().Init();
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
        PlayerWeapons.Clear();
        List<Weapon> newEquipment = new List<Weapon>();
        //weapons
        foreach(GameObject g in LastMeleeWeps)
        {
            PlayerWeapons.Add(g.GetComponent<WeaponInputScript>().GetWeapon());
        }
        foreach(GameObject g in LastRangedWeps)
        {
            PlayerWeapons.Add(g.GetComponent<WeaponInputScript>().GetWeapon());
        }
        Destroy(gameObject);
    }

    //transfers sheet info to player
    public void UpdateStatsOut(CharacterSaveData output){
        output.playername = NameField.text;
        output.ClearSkills();
        output.ClearWeapons();
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
        List<Weapon> newEquipment = new List<Weapon>();
        //weapons
        foreach(GameObject g in LastMeleeWeps)
        {
            newEquipment.Add(g.GetComponent<WeaponInputScript>().GetWeapon());
        }
        foreach(GameObject g in LastRangedWeps)
        {
            newEquipment.Add(g.GetComponent<WeaponInputScript>().GetWeapon());
        }
        ActivePlayer.AddWeapons(newEquipment);
        ActivePlayer = null;
        Destroy(gameObject);
    }

    public void UpdateStatsIn(PlayerStats input)
    {
        Init();
        ActivePlayerStats = input;
        PlayerStats = input.Stats;
        PlayerSkills = input.Skills;
        PlayerWeapons = input.equipment;
        PlayerDisplayName = input.playername;
        UpdateStatsIn();
    }
    public void UpdateStatsIn(CharacterSaveData input)
    {
        Init();
        ActivePlayer = input;
        PlayerStats = input.GetStats();
        PlayerSkills = input.GetSkills();
        PlayerWeapons = input.GetWeapons();
        PlayerDisplayName = input.playername;
        UpdateStatsIn();
    }
    public void UpdateStatsIn(){
        NameField.text = PlayerDisplayName;
        PlacementPosAdvanced = new Vector3(149.5f, 166,0);
        PlacementPosBasic = new Vector3(-258, 193,0);
        PlacementWeaponRanged = new Vector3(783, 212,0);
        PlacementWeaponMelee = new Vector3(375, 209,0);
        SkillAdderButton.transform.localPosition = new Vector3(-16,164,0);
        WeaponAdderMelee.transform.localPosition = new Vector3(500, 200,0);
        WeaponAdderRanged.transform.localPosition = new Vector3(900, 200,0);
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
        foreach(Weapon w in PlayerWeapons)
        {
            if(w.IsWeaponClass("Melee"))
            {
                LastMeleeWeps.Push(CreateWeapon(w,PlacementWeaponMelee));
                PlacementWeaponMelee -= weaponDisplacementLeft;
                WeaponAdderMelee.transform.localPosition -= weaponDisplacementLeft;

            }
            else
            {
                LastRangedWeps.Push(CreateWeapon(w,PlacementWeaponRanged));
                PlacementWeaponRanged -= weaponDisplacementRight;
                WeaponAdderRanged.transform.localPosition -= weaponDisplacementRight;
            }
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

    public void CreateWeaponButtonLeft()
    {
        if(LastMeleeWeps.Count < 4)
        {
            LastMeleeWeps.Push(CreateWeapon(WeaponAdderMelee.GetComponent<WeaponAdder>().GetWeapon(), PlacementWeaponMelee));
            WeaponAdderMelee.transform.localPosition -= weaponDisplacementLeft;
            PlacementWeaponMelee -= weaponDisplacementLeft;
        }
    }
    public void CreateWeaponButtonRight()
    {
        if(LastRangedWeps.Count < 4)
        {
            LastRangedWeps.Push(CreateWeapon(WeaponAdderRanged.GetComponent<WeaponAdder>().GetWeapon(), PlacementWeaponRanged));
            WeaponAdderRanged.transform.localPosition -= weaponDisplacementRight;
            PlacementWeaponRanged -= weaponDisplacementRight;
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

    public void DeleteMelee()
    {
        if(LastMeleeWeps.Count > 0)
        {
            Destroy(LastMeleeWeps.Pop());
            WeaponAdderMelee.transform.localPosition += weaponDisplacementLeft;
            PlacementWeaponMelee += weaponDisplacementLeft;
        }
    }

    public void DeleteRanged()
    {
        if(LastRangedWeps.Count > 0)
        {
            Destroy(LastRangedWeps.Pop());
            WeaponAdderRanged.transform.localPosition += weaponDisplacementRight;
            PlacementWeaponRanged += weaponDisplacementRight;
        }
    }
}

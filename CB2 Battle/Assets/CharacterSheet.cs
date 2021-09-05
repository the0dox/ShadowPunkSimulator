using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSheet : MonoBehaviour
{
    public static bool active;
    private Dictionary<string, InputFieldScript> TextEntries;
    
    private Dictionary<string, SkillScript> BasicSkillEntries;
    
    private Dictionary<int, WeaponInputScript> WeaponEntries;

    private static PlayerStats ActivePlayer; 
    [SerializeField] private GameObject WeaponDisplay;
    [SerializeField] private InputField NameField;
    [SerializeField] private GameObject SkillInputButton;
    [SerializeField] private GameObject SkillAdderButton;
    private Stack<GameObject> LastSkills;
    private Stack<GameObject> LastMeleeWeps;
    private Stack<GameObject> LastRangedWeps;
    [SerializeField] private InputField SkillAdderName;
    [SerializeField] private InputField SkillAdderChar;
    [SerializeField] private GameObject WeaponAdderMelee;
    [SerializeField] private GameObject WeaponAdderRanged;
    private Vector3 weaponDisplacementRight = new Vector3(0,98.5f,0);
    private Vector3 weaponDisplacementLeft = new Vector3(0,82.5f,0);
    private Vector3 SkillDisplacement = new Vector3(0,14.5f,0);
    Vector3 PlacementPosAdvanced;
    Vector3 PlacementPosBasic;
    Vector3 PlacementWeaponRanged;
    Vector3 PlacementWeaponMelee;

    void Start()
    {
        active = false;
        gameObject.SetActive(false);
    }
    public void Init(){
        LastSkills = new Stack<GameObject>();
        LastRangedWeps = new Stack<GameObject>();
        LastMeleeWeps = new Stack<GameObject>();
        WeaponAdderMelee.GetComponent<WeaponAdder>().Init();
        WeaponAdderRanged.GetComponent<WeaponAdder>().Init();
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

    //transfers sheet info to player
    public void UpdateStatsOut(){
        active = false;
        ActivePlayer.SetName(NameField.text);
        foreach (KeyValuePair<string, InputFieldScript> kvp in TextEntries){
            int newValue = kvp.Value.GetValue();
            ActivePlayer.SetStat(kvp.Key, newValue);
        }
        GameObject[] EntryListSkills = GameObject.FindGameObjectsWithTag("Skill");    
        foreach (GameObject g in EntryListSkills)
        {
            SkillScript input = g.GetComponent<SkillScript>();
            ActivePlayer.SetSkill(input.GetSkill());
            Destroy(g);
        }
        gameObject.SetActive(false);
    }

    
    //transfers player info to sheet
    public void UpdateStatsIn(){
        LastSkills.Clear();
        LastRangedWeps.Clear();
        LastRangedWeps.Clear();
        active = true;
        gameObject.SetActive(true);
        NameField.text = ActivePlayer.GetName();
        PlacementPosAdvanced = new Vector3(149.5f, 166,0);
        PlacementPosBasic = new Vector3(-258, 193,0);
        PlacementWeaponRanged = new Vector3(783, 212,0);
        PlacementWeaponMelee = new Vector3(375, 209,0);
        SkillAdderButton.transform.localPosition = new Vector3(-16,164,0);
        WeaponAdderMelee.transform.localPosition = new Vector3(500, 200,0);
        WeaponAdderRanged.transform.localPosition = new Vector3(900, 200,0);
        Dictionary<string, int> d = ActivePlayer.Stats;
        //characterisitcs
        foreach (KeyValuePair<string, int> kvp in d){
            if (TextEntries.ContainsKey(kvp.Key))
            {
                TextEntries[kvp.Key].UpdateValue(kvp.Value);   
            }
        }
        //skills
        List<Skill> PlayerSkills = ActivePlayer.Skills;
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
            //method to place spawners at placementPos
        }
        //to implement, add more weapon entries
        foreach(Weapon w in ActivePlayer.equipment)
        {
            if(w.HasWeaponAttribute("Melee"))
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

    public void UpdatePlayer(PlayerStats input){
        ActivePlayer = input; 
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
            string skillname = SkillAdderName.text;
            string skillChar = SkillAdderChar.text;
            if(!ActivePlayer.Stats.ContainsKey(skillChar))
            {
                Debug.Log("Error: Characterisitc not found!");
            }
            else
            {
                LastSkills.Push(CreateSkill(new Skill(skillname,0,skillChar,false), PlacementPosAdvanced));
                PlacementPosAdvanced -= SkillDisplacement;
                SkillAdderButton.transform.localPosition -= SkillDisplacement;
            }
            SkillAdderName.text = null;
            SkillAdderChar.text = null;
        }
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

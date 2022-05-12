using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Used by charactersheet to add more skills to a player
public class SkillAdder : MonoBehaviour
{
    // Non static references to UI objects
    [SerializeField] private Transform DisplayRef;
    [SerializeField] private GameObject SkillinputRef;
    [SerializeField] private Transform ContentRef;
    [SerializeField] private GameObject ButtonRef;
    [SerializeField] private CharacterSheet mySheet;
    // Static references to objects
    // Display for skill adder
    private static Transform SContent;
    // Display for skills the player already has
    private static Transform SDisplay;
    // Object used to modify skill values
    private static GameObject SkillInput;
    // Object used to select which skills to add/remove
    private static GameObject SButton;
    // Reference to all skills the player currently has active
    private static List<SkillScript> Skills = new List<SkillScript>();
    // Reference to the player being edited
    private static CharacterSaveData owner;
    private static CharacterSheet SmySheet;
    // Saves the static references
    void Awake()
    {
        SDisplay = DisplayRef;
        SkillInput = SkillinputRef;
        SContent = ContentRef;
        SButton = ButtonRef;
        SmySheet = mySheet;
    }

    // Given newowner savedata, creates Skillinputs for each skill the player already knows
    public static void DownloadOwner(CharacterSaveData newonwer)
    { 
        owner = newonwer;

        foreach(SkillScript sc in Skills)
        {
            Destroy(sc);
        }
        Skills.Clear();
        Dictionary<string, SkillTemplate> allSkills = SkillReference.SkillsTemplates();
        foreach(string key in allSkills.Keys)  
        {
            AttributeKey currentSkillkey = allSkills[key].skillKey;
            if(newonwer.GetAttribute(currentSkillkey) > 0 && validSkill(allSkills[key]))
            {
                AddSkill(allSkills[key],false);
            }
        } 
        UpdateAdderContent();
    }
    
    // Adds a skill from skillreferences that matches a given name
    // if add, add the skill to the owner
    public static void AddSkill(SkillTemplate newSkill, bool add)
    {
        SkillScript indicator = Instantiate(SkillInput as GameObject).GetComponent<SkillScript>();
        indicator.DownloadCharacter(owner, newSkill, SmySheet);
        Skills.Add(indicator);
        indicator.transform.SetParent(SDisplay);
    }

    // Destroys the corresponding Skillinput and removes the skill from the player
    public static void RemoveSkill(SkillTemplate skill)
    {
        owner.SetAttribute(skill.skillKey, 0,false);
        owner.skillSpecialization.Remove(skill.name);
        SkillScript removedSkill = null;
        foreach(SkillScript sc in Skills)
        {
            if(sc.GetSkill().Equals(skill))
            {
                removedSkill = sc;
            }
        }
        Skills.Remove(removedSkill);
        Destroy(removedSkill.gameObject);
    }

    // If one of my buttons was clicked, check if I already own that skill
    // If I don't own it, create it
    // If I already own it, remove it
    public void OnClicked()
    {
        string name = EventSystem.current.currentSelectedGameObject.name;
        SkillTemplate newSkill = SkillReference.GetSkill(name);
        if(validSkill(newSkill))
        {
            AddSkill(newSkill,true);
        }
        else
        {
            RemoveSkill(newSkill);
        }
    }

    // Tells all skillinputs to update to reflect the new stats
    public static void UpdateSkillFields()
    {
        foreach(SkillScript sc in Skills)
        {
            sc.UpdateValue();
        }
    }

    // Given a skill name see if this player already owns it or not
    // returns true if I already own it
    private static bool validSkill(SkillTemplate newSkill)
    {
        foreach(SkillScript scripts in Skills)
        {
            if(scripts.GetSkill().Equals(newSkill))
            {
                return false;
            }
        }
        return true; 
    }

    // Adds button prefabs to SContent to represent all the skills the player can have 
    private static void UpdateAdderContent()
    {
        foreach(string skillname in SkillReference.SkillsTemplates().Keys)
        {
            GameObject newButton = Instantiate(SButton as GameObject);
            newButton.name = skillname; 
            newButton.GetComponentInChildren<Text>().text = skillname;
            newButton.transform.SetParent(SContent);
            newButton.transform.localScale = Vector3.one;
        }
    }
}

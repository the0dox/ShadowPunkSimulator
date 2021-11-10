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
    private static List<SkillScript> Skills;
    // Reference to the player being edited
    private static CharacterSaveData owner;
    
    // Saves the static references
    void Awake()
    {
        SDisplay = DisplayRef;
        SkillInput = SkillinputRef;
        SContent = ContentRef;
        SButton = ButtonRef;
    }

    // Given newowner savedata, creates Skillinputs for each skill the player already knows
    public static void DownloadOwner(CharacterSaveData newonwer)
    { 
        owner = newonwer;
        Skills = new List<SkillScript>();
        foreach(string s in owner.GetSkills().Keys)
        {
            AddSkill(s,false);
        } 
        UpdateAdderContent();
    }
    
    // Adds a skill from skillreferences that matches a given name
    // if add, add the skill to the owner
    public static void AddSkill(string skillName, bool add)
    {
        if(add)
        {
            owner.SetSkill(skillName,0);
        }
        SkillTemplate newSkill = SkillReference.GetSkill(skillName);
        SkillScript indicator = Instantiate(SkillInput as GameObject).GetComponent<SkillScript>();
        indicator.DownloadCharacter(owner, newSkill);
        Skills.Add(indicator);
        indicator.transform.SetParent(SDisplay);
    }

    // Destroys the corresponding Skillinput and removes the skill from the player
    public static void RemoveSkill(string skillName)
    {
        owner.skills.Remove(skillName);
        owner.skillSpecialization.Remove(skillName);
        SkillScript removedSkill = null;
        foreach(SkillScript sc in Skills)
        {
            if(sc.GetSkill().Equals(skillName))
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
        if(validSkill(name))
        {
            AddSkill(name,true);
        }
        else
        {
            RemoveSkill(name);
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
    private static bool validSkill(string newSkill)
    {
        foreach(string ownedSkill in owner.skills.Keys)
        {
            if(ownedSkill.Equals(newSkill))
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
        }
    }
}

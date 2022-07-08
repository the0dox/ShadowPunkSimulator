using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillScript : MonoBehaviour
{
    [SerializeField] public SkillTemplate mySkill;
    [SerializeField] private InputField IF;
    [SerializeField] private Text Level;
    [SerializeField] private Text FinalResult;
    [SerializeField] private Dropdown SpecializationField;
    [SerializeField] private CharacterSaveData myData;
    [SerializeField] private CharacterSheet mySheet;
    [SerializeField] private TooltipTrigger tooltipTrigger;
    public GameObject ButtonText; 

    public void Start()
    { 
        transform.localScale = Vector3.one;
    }

    public void DownloadCharacter(CharacterSaveData myData, SkillTemplate mySkill, CharacterSheet mySheet)
    {
        this.mySheet = mySheet; 
        this.myData = myData;
        this.mySkill = mySkill;
        SpecializationField.ClearOptions();
        List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
        Dropdown.OptionData baseResponse = new Dropdown.OptionData();
        baseResponse.text = "None";
        int specializationIndex = myData.GetSpecializationIndex(mySkill.name);
        results.Add(baseResponse);
        foreach(string Key in mySkill.Specializations)
        {
            Dropdown.OptionData NewData = new Dropdown.OptionData();
            NewData.text = Key;
            results.Add(NewData);
        }
        SpecializationField.AddOptions(results);
        SpecializationField.value = specializationIndex;
        UpdateValue();
    }

    public void UpdateValue()
    {
        int derivedValue = myData.GetAttribute(mySkill.derivedAttribute);
        int levels = myData.GetAttribute(mySkill.skillKey);
        int total = derivedValue + levels; 
        IF.text = "" + levels;
        if(ButtonText != null)
        {
            ButtonText.GetComponent<Text>().text = mySkill.name + " (" + mySkill.derivedAttribute + ")";
        }
        FinalResult.text = "[" + total +"]";

        string description = mySkill.displayText;
        description += "\n\n derived Attribute: " + mySkill.derivedAttribute;
        description += "\n Defaultable: " + mySkill.defaultable;
        if(levels < 1 && !mySkill.defaultable)
        {
            description += " \n\n cannot attempt with no training!";
        }
        else
        {
            description += " \n\n Die: " + total;
            description += " \n Base: " + levels + " from skill level";
            if(levels < 1 && mySkill.defaultable)
            {
                description += "\n -1 from defaulting";
            }
            description += " \n +" + derivedValue + " from " + mySkill.derivedAttribute + " attribute";  
            // space for modifiers
        }
        tooltipTrigger.header = mySkill.name;
        tooltipTrigger.content = description;
        SpecializationField.value = myData.GetSpecializationIndex(mySkill.name);
    }

    public void UpdateSpecalization()
    {
        mySheet.ChangeSpecialization(mySkill.name,SpecializationField.value);
    }

    public void OnValueChange()
    {
        int value = 0;
        if (!int.TryParse(Level.text, out value))
        {
            value = 0;
        }
        mySheet.UpdatedAttribute(mySkill.skillKey, value);
        UpdateValue();
    }

    public SkillTemplate GetSkill()
    {
        return mySkill;
    }

    public void SkillCheck()
    {
        myData.AbilityCheck(mySkill.skillKey);
    }
}

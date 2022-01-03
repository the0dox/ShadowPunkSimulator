using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillScript : MonoBehaviour
{
    [SerializeField] private SkillTemplate mySkill;
    [SerializeField] private InputField IF;
    [SerializeField] private Text Level;
    [SerializeField] private Text FinalResult;
    [SerializeField] private Dropdown SpecializationField;
    [SerializeField] private CharacterSaveData myData;
    [SerializeField] private TooltipTrigger tooltipTrigger;
    public GameObject ButtonText; 

    public void Start()
    {
        transform.localScale = Vector3.one;
    }

    public void DownloadCharacter(CharacterSaveData myData, SkillTemplate mySkill)
    {
        this.myData = myData;
        this.mySkill = mySkill;
        SpecializationField.ClearOptions();
        List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
        Dropdown.OptionData baseResponse = new Dropdown.OptionData();
        baseResponse.text = "None";
        int specializationIndex = myData.skillSpecialization[mySkill.name];
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
        int total = myData.GetSkill(mySkill.name,true);
        int levels = myData.skills[mySkill.name];
        IF.text = "" + levels;
        ButtonText.GetComponent<Text>().text = mySkill.name + " (" + mySkill.characterisitc + ")";
        FinalResult.text = "[" + total +"]";

        string description = mySkill.displayText;
        description += "\n\n Derrived Attribute: " + mySkill.characterisitc;
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
            description += " \n +" + myData.GetAttribute(mySkill.characterisitc) + " from " + mySkill.characterisitc + " attribute";  
            // space for modifiers
        }
        tooltipTrigger.header = mySkill.name;
        tooltipTrigger.content = description;
    }

    public void UpdateSpecalization()
    {
        myData.setSpecialization(mySkill.name,SpecializationField.value);
    }

    public void OnValueChange()
    {
        int value = 0;
        if (!int.TryParse(Level.text, out value))
        {
            value = 0;
        }
        myData.SetSkill(mySkill.name, value);
        UpdateValue();
    }

    public string GetSkill()
    {
        return mySkill.name;
    }

    public void SkillCheck()
    {
        myData.AbilityCheck(mySkill.name);
    }
}

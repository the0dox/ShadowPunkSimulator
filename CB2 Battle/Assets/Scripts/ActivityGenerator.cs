using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActivityGenerator : MonoBehaviour
{
    [SerializeField] private GameObject TypeField;
    [SerializeField] private GameObject InvestigateField;
    [SerializeField] private GameObject SkillField;
    [SerializeField] private GameObject P1Field;
    [SerializeField] private GameObject P2Field;
    [SerializeField] private GameObject P3Field;
    [SerializeField] private GameObject adderButton;
    [SerializeField] private GameObject ActivityObjectReference;
    [SerializeField] private GameObject ActivityTab;
    private PlayerStats[] PlayerSelection;
    private string SkillChoice;
    private string Difficultly;
    void OnEnable()
    {
        Difficultly = null;
        PlayerSelection = new PlayerStats[2];
        SkillChoice = null;
        TypeField.SetActive(true);
        ResetDD(TypeField);
        InvestigateField.SetActive(false);
        ResetDD(InvestigateField);
        SkillField.SetActive(false);
        ResetDD(SkillField);
        P1Field.SetActive(false);
        ResetDD(P1Field);
        P2Field.SetActive(false);
        ResetDD(P2Field);
        P3Field.SetActive(false);
        P2Field.transform.localPosition = new Vector3(31.7352f, -40, 0);
        P3Field.transform.localPosition = new Vector3(31.7352f, -80, 0);
        ResetDD(P3Field);
        adderButton.SetActive(false);
    }

    public void TypeSelection()
    {
        if(GetChoice(TypeField).Equals("Investigate"))
        {
            disableDD(TypeField);
            InvestigateField.SetActive(true);
        }
    }

    public void InvestigationSelection()
    {
        if(!GetChoice(InvestigateField).Equals("None"))
        {
            Difficultly = GetChoice(InvestigateField).Split()[0];
            disableDD(InvestigateField);
            P1Field.SetActive(true);
            GetRemainingPlayers(P1Field);
        }
    }

    public void PlayerSelectionOne()
    {
        if(!GetChoice(P1Field).Equals("None"))
        {
            PlayerSelection[0] = OverworldManager.Party[GetChoice(P1Field)];
            disableDD(P1Field);
        }
        if(GetChoice(TypeField).Equals("Investigate"))
        {
            SkillField.SetActive(true);
            SkillField.GetComponent<Dropdown>().ClearOptions();
            List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
            Dropdown.OptionData baseResponse = new Dropdown.OptionData();
            baseResponse.text = "None";
            results.Add(baseResponse);
            foreach(Skill s in PlayerSelection[0].Skills)
            {
                if(s.hasSkillDescriptor("Investigation"))
                {
                    Dropdown.OptionData newData = new Dropdown.OptionData(); 
                    newData.text = s.name;
                    results.Add(newData);
                }
            }
            SkillField.GetComponent<Dropdown>().AddOptions(results);
        }
        else
        {
            P2Field.SetActive(true);
            GetRemainingPlayers(P2Field);
            P2Field.transform.localPosition += new Vector3(0, 40, 0);
            P3Field.transform.localPosition += new Vector3(0, 40, 0);
            adderButton.SetActive(true);
        }
    }

    public void PlayerSelectionTwo()
    {
        if(!GetChoice(P2Field).Equals("None"))
        {
            PlayerSelection[1] = OverworldManager.Party[GetChoice(P2Field)];
            disableDD(P2Field);
            P3Field.SetActive(true);
            GetRemainingPlayers(P3Field);
        }
    }

    public void PlayerSelectionThree()
    {
        if(!GetChoice(P2Field).Equals("None"))
        {
            PlayerSelection[2] = OverworldManager.Party[GetChoice(P2Field)];
            disableDD(P2Field);
            P3Field.SetActive(true);
        }
    }

    public void SkillSelection()
    {
        if(!GetChoice(SkillField).Equals("None"))
        {
            SkillChoice = GetChoice(SkillField);
            disableDD(SkillField);
            P2Field.SetActive(true);
            GetRemainingPlayers(P2Field);
            adderButton.SetActive(true);
        } 
    }

    public void MakeActivity()
    {
        GameObject newActivity = Instantiate(ActivityObjectReference) as GameObject;
        newActivity.GetComponent<LeadScript>().UpdateLead(Difficultly,PlayerSelection,SkillChoice);
        ActionQueueDisplay.AddActivity(newActivity);
        gameObject.SetActive(false);
    }

    private string GetChoice(GameObject dropdown)
    {
        return dropdown.GetComponent<Dropdown>().captionText.text;
    }
    private void disableDD(GameObject dropdown)
    {
        dropdown.GetComponent<Dropdown>().interactable = false;
    }
    private void ResetDD(GameObject dropdown)
    {
        Dropdown myDD = dropdown.GetComponent<Dropdown>();
        myDD.interactable = true;
        myDD.value = 0;
    }

    private void GetRemainingPlayers(GameObject dropdown)
    {
        dropdown.GetComponent<Dropdown>().ClearOptions();
        List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
        Dropdown.OptionData baseResponse = new Dropdown.OptionData();
        baseResponse.text = "None";
        results.Add(baseResponse);
        foreach(string key in OverworldManager.Party.Keys)
        {
            PlayerStats current = OverworldManager.Party[key];
            bool Valid = true;
            for(int i = 0; i < PlayerSelection.Length;i++)
            {
                if((PlayerSelection[i] != null && PlayerSelection[i] == current) || current.IsOccupied())
                {
                    Valid = false;
                }
            }
            if(Valid)
            {
                Dropdown.OptionData newData = new Dropdown.OptionData(); 
                newData.text = key;
                results.Add(newData);
            }
        }
        dropdown.GetComponent<Dropdown>().AddOptions(results);
    }
}
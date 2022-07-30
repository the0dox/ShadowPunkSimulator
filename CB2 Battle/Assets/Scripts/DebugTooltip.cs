using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugTooltip : MonoBehaviour
{
    [SerializeField] private Text displayText;
    [SerializeField] private GameObject AdderPopup;
    [SerializeField] private GameObject SubtracterPopup;
    [SerializeField] private GameObject DieRollerPopup;
    [SerializeField] private Dropdown AdderConditionField;
    [SerializeField] private InputField RollerIF;
    [SerializeField] private InputField AdderIF;
    [SerializeField] private Dropdown SubtractConditionField;
    private PlayerStats myStats;

    public void UpdateStatIn(PlayerStats myStats)
    {
        this.myStats = myStats;
        displayText.text = "Debug for: " + myStats.GetName();
        CameraButtons.UIFreeze(true);
        ResetPopups();
    }

    public void AdderPopupButton()
    {
        ResetPopups();
        AdderPopup.SetActive(true);
        AdderConditionField.ClearOptions();
        List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
        Dictionary<Condition,ConditionTemplate> Conditions = ConditionsReference.ConditionTemplates();
        foreach(Condition Key in Conditions.Keys)
        {
            if(!myStats.hasCondition(Key))
            {
                Dropdown.OptionData NewData = new Dropdown.OptionData();
                NewData.text = Key.ToString();
                results.Add(NewData);
            }
        }
        AdderConditionField.GetComponent<Dropdown>().AddOptions(results);
        AdderConditionField.value = 0;
        AdderIF.text = "";
    }
    public void SubtracterPopupButton()
    {
        ResetPopups();
        SubtracterPopup.SetActive(true);
        SubtractConditionField.ClearOptions();
        List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
        Dictionary<Condition,ConditionTemplate> Conditions = ConditionsReference.ConditionTemplates();
        foreach(Condition Key in Conditions.Keys)
        {
            if(myStats.hasCondition(Key))
            {
                Dropdown.OptionData NewData = new Dropdown.OptionData();
                NewData.text = Key.ToString();
                results.Add(NewData);
            }
        }
        SubtractConditionField.GetComponent<Dropdown>().AddOptions(results);
        AdderConditionField.value = 0;
    }
    public void DieRollerButton()
    {
        ResetPopups();
        DieRollerPopup.SetActive(true);
        RollerIF.text = "";
    }

    public void DeleteButton()
    {
        GameObject.FindGameObjectWithTag("GameController").GetComponent<TurnManager>().RemovePlayer(myStats.gameObject);
        DestroyMe();
    }

    public void MovementResetButton()
    {    
        myStats.ResetMovement();
        DestroyMe();
    }

    public void StartTurnButton()
    {
        GameObject.FindGameObjectWithTag("GameController").GetComponent<TurnManager>().StartTurn(myStats,false);
        DestroyMe();
    }

    public void AddCondition()
    {
        Condition conditionSelection;
        Condition.TryParse<Condition>(AdderConditionField.captionText.text, true, out conditionSelection);
        int num = 0;
        int size = 0;
        int result = 0;
        string logOutput = "";
    
        string[] input = AdderIF.text.Split('d');

        if(input.Length == 1)
        {
            num = 0;
            size = int.Parse(input[0]);
        }
        else
        {
            num = int.Parse(input[0]);
            size = int.Parse(input[1]);
        }

        if(num == 0)
        {
            result = size;  
        }
        else
        {
            logOutput += "Rolling " + num + "d" + size +"\n";
            for(int i = 0; i < num; i++)
            {
                int roll = Random.Range(1,size + 1);
                logOutput += "[" + roll + "]";
                if(i < (num - 1))
                {
                    logOutput += " + ";
                }
                result += roll;
            }
            logOutput += " = " + result +"\n"; 
        }
        logOutput += myStats.GetName() + " gains the " + conditionSelection.ToString() + " condition for " + result + " rounds.";
        CombatLog.Log(logOutput);
        myStats.SetCondition(conditionSelection,result,true);
        DestroyMe();
    }

    public void RollDice()
    {
        int num = 0;
        int size = 0;
        int result = 0;
        string logOutput = "";

        string[] input = RollerIF.text.Split('d');

        if(input.Length == 1)
        {
            num = 0;
            size = int.Parse(input[0]);
        }
        else
        {
            num = int.Parse(input[0]);
            size = int.Parse(input[1]);
        }

        if(num == 0)
        {
            result = size;  
        }
        else
        {
            logOutput += "Rolling " + num + "d" + size +"\n";
            for(int i = 0; i < num; i++)
            {
                int roll = Random.Range(1,size + 1);
                logOutput += "[" + roll + "]";
                if(i < (num - 1))
                {
                    logOutput += " + ";
                }
                result += roll;
            }
            logOutput += " = " + result +"\n"; 
        }
        CombatLog.Log(logOutput);
        DestroyMe();
    }

    public void RemoveCondition()
    {
        Condition conditionSelection;
        Condition.TryParse<Condition>(AdderConditionField.captionText.text, true, out conditionSelection);
        CombatLog.Log(myStats.GetName() + " loses the " + conditionSelection.ToString() + " condition.");
        myStats.RemoveCondition(conditionSelection);
        DestroyMe();
    }

    public void DestroyMe()
    {
        CameraButtons.UIFreeze(false);
        Destroy(gameObject);
    }

    private void ResetPopups()
    {
        AdderPopup.SetActive(false);
        SubtracterPopup.SetActive(false);
        DieRollerPopup.SetActive(false);
    }

}

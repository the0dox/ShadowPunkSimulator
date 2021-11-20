using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class InputFieldScript : MonoBehaviour
{
    public string stat;
    public int value;
    public GameObject TextObject; 

    public GameObject Placeholder; 
    private CharacterSheet mySheet;

    public InputField IF;

    [SerializeField] private TooltipTrigger tooltipTrigger;
    
    [TextArea(5,5)]
    [SerializeField] private string baseDescription;

    public string GetStat()
    {
        return stat;
    }

    public int GetValue()
    {
        return value;
    }
    public void UpdateValue()
    {
        if (!int.TryParse(TextObject.GetComponent<Text>().text, out value))
        {
            value = 0;
        }
        mySheet.UpdatedAttribute(stat, value);
    }

    public void UpdateValue(int value, CharacterSheet mySheet)
    {
        this.mySheet = mySheet;
        this.value = value;
        IF.text = "" + value;
        TextObject.GetComponent<Text>().text = "" + value; 
        Placeholder.GetComponent<Text>().text = "" + value;  
        
        if(tooltipTrigger != null)
        {
            string description = baseDescription;    
            //modifiers
            description += " \n\n Die: " + value;
            description += " \n Base: " + value + " from characteristic";
            tooltipTrigger.content = description;
        }

    }

    public void ShowPlaceHolder()
    {
        Placeholder.GetComponent<Text>().text = "" + value; 
    }

    public void CharacteristicTest()
    {
        GameObject.FindGameObjectWithTag("GameController").GetComponent<TurnManager>().AbilityCheck(stat);
    }
}

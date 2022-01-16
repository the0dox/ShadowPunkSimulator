using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class InputFieldScript : MonoBehaviour
{
    public int value;
    public GameObject TextObject; 

    public GameObject Placeholder; 
    private CharacterSheet mySheet;

    public InputField IF;
    [SerializeField] private AttributeKey stat;

    [SerializeField] private TooltipTrigger tooltipTrigger;
    
    [TextArea(5,5)]
    [SerializeField] private string baseDescription;
    
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

    public void UpdateValue(CharacterSaveData myData,  CharacterSheet mySheet)
    {
        this.mySheet = mySheet;
        this.value = myData.GetAttribute(stat);
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
        //GameObject.FindGameObjectWithTag("GameController").GetComponent<TurnManager>().AbilityCheck(stat);
    }
}

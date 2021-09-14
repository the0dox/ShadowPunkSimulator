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

    public InputField IF;

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
    }

    public void UpdateValue(int value)
    {
        //Debug.Log("Successfuly called!");
        this.value = value;
        IF.text = "" + value;
        TextObject.GetComponent<Text>().text = "" + value; 
        Placeholder.GetComponent<Text>().text = "" + value;  
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

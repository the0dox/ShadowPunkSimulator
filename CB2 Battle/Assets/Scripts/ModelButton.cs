using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModelButton : MonoBehaviour
{
    [SerializeField] private Text display;
    private CharacterSaveData myData;
    private CharacterSelectorButton owner;

    public void SetData(string value, CharacterSaveData myData, CharacterSelectorButton owner)
    {
        display.text = value;
        this.myData = myData;
        this.owner = owner; 
    }

    public void OnButtonPressed()
    {
        myData.Model = display.text;
        Debug.Log("model is set to " + myData.Model);
        owner.ModelToggle();
    }
}

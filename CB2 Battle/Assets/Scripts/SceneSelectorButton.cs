using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Button containing a single scene, can be used to either edit the scene in level editor or play the scene on board
public class SceneSelectorButton : MonoBehaviour
{
    // Complied savedata that has not been transfered into a level yet
    private SceneSaveData myData;
    // Name displayed on the ui
    [SerializeField] private Text displayText;
    // Popup used to obscure options until name is pressed
    [SerializeField] private GameObject PopUp;

    // When created, this is called to store the scene data this button corresponds to
    public void SetData(SceneSaveData input)
    {
        myData = input;
        displayText.text = input.GetName();
        PopUp.SetActive(false);
    }

    // Called when the button is pressed. Normally opens up the popup menu for edit/play options
    // But as overworld scenes cannot be edited, the popup is skipped entirely and the scene is loaded
    public void OnButtonPressed()
    {
        if(myData.isOverworld())
        {
            GlobalManager.PlayLevel(myData,"Overworld");
        }
        else
        {
            PopUp.SetActive(!PopUp.activeInHierarchy);
        }
    }

    // Called by the play popup button, changes the scene to the battle board
    public void PlayScene()
    {
        Debug.Log(myData.GetName());
        GlobalManager.PlayLevel(myData, "Board");
    }

    // Called by the edit popup button, changes the scene to the level editor
    public void EditScene()
    {
        GlobalManager.PlayLevel(myData, "LevelEditor");
    }
}

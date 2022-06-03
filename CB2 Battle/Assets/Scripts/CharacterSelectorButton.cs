using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

// UI element that is used to edit/spawn charactersavedata
public class CharacterSelectorButton : MonoBehaviour
{
    // Each SelectorButton contains is own unique data
    private CharacterSaveData myData;
    // Name displayed on the ui
    [SerializeField] private Text displayText;
    // Seperate popup to reduce menu clutter
    [SerializeField] private GameObject PopUp;
    // Reference to the character sheet to spawn when edits need to be made
    [SerializeField] private GameObject CharacterSheet;
    // Window to display model options
    [SerializeField] private GameObject ModelDisplay;
    [SerializeField] private RectTransform ModelDisplayContent;
    // Button Reference to display model options
    [SerializeField] private GameObject ActionButton;
    // Index used for client side communication
    private int index;
    // client side buttons don't have the full functionality and are marked as dummy
    bool isDummy = false;

    // Spawning location off camera to give the player the oppertunity to place the character
    private Vector3 spawningPos = new Vector3(0,50f,0);

    // input: the save data this object will hold
    // called by DM menu when this object is created, saves its value to this object
    public void SetData(int index, CharacterSaveData input)
    {
        PopUp.SetActive(false);
        ModelDisplay.SetActive(false);
        myData = input;
        displayText.text = input.playername;
        Dictionary<string,Mesh> models = PlayerSpawner.GetPlayers();
        foreach(string s in models.Keys)
        {
            GameObject newButton = Instantiate(ActionButton) as GameObject;
            newButton.GetComponent<ModelButton>().SetData(s,myData,this);
            newButton.transform.SetParent(ModelDisplayContent);
        }
        isDummy = false;
    }

    public void SetDummyData(int index, string input)
    {
        PopUp.SetActive(false);
        ModelDisplay.SetActive(false);
        displayText.text = input;
        this.index = index;
        isDummy = true;
    }

    // creates a charactersheet from stored savedata for editing
    public void Edit()
    {
        DmMenu.DMDisplay(myData);
        DmMenu.Toggle();
    }

    // creates a map token and downloads savedata into that token
    public void Spawn()
    {
        PlayerSpawner.CreatePlayer(myData.playername,spawningPos,true);
        OnButtonPressed();
        DmMenu.Toggle();
    }
    // Opens popup menu
    public void OnButtonPressed()
    {
        if(isDummy)
        {
            DmMenu.AssignCharacter(index);
        }
        else
        {
            PopUp.SetActive(!PopUp.activeInHierarchy);
            ModelDisplay.SetActive(false);
        }
    }

    public void ModelToggle()
    {
        ModelDisplay.SetActive(!ModelDisplay.activeInHierarchy);
        PopUp.SetActive(false);
    }
}

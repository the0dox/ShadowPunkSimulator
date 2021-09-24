using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    // Reference to a basic player token gameobject
    [SerializeField] private GameObject PlayerReference;
    // Reference to a basic npc token gameobject
    [SerializeField] private GameObject NPCReference;
    // To implement: choose spawning location
    // Spawning location 
    private Vector3 spawningPos = new Vector3(-0.5f,0,0);

    // input: the save data this object will hold
    // called by DM menu when this object is created, saves its value to this object
    public void SetData(CharacterSaveData input)
    {
        PopUp.SetActive(false);
        myData = input;
        displayText.text = input.playername;
    }

    // creates a charactersheet from stored savedata for editing
    public void Edit()
    {
        GameObject newSheet = Instantiate(CharacterSheet) as GameObject;
        newSheet.GetComponent<CharacterSheet>().UpdateStatsIn(myData);
        OnButtonPressed();
    }

    // creates a map token and downloads savedata into that token
    public void Spawn()
    {
        //implement spawn specific locations;
        GameObject newPlayer;
        if(myData.team == 0)
        {
            newPlayer = Instantiate(PlayerReference) as GameObject;
        }
        else
        {
            newPlayer = Instantiate(NPCReference) as GameObject;
        }
        newPlayer.GetComponent<PlayerStats>().DownloadSaveData(myData);
        newPlayer.GetComponent<TacticsMovement>().Init();
        if(GameObject.FindGameObjectWithTag("GameController").TryGetComponent<TurnManager>(out TurnManager tm))
        {
            tm.AddPlayer(newPlayer);
        }
        else
        {
            OverworldManager.AddPlayer(newPlayer);
        }
        newPlayer.transform.position = spawningPos;
        OnButtonPressed();
        DmMenu.Toggle();
    }

    // Opens popup menu
    public void OnButtonPressed()
    {
        PopUp.SetActive(!PopUp.activeInHierarchy);
    }
}

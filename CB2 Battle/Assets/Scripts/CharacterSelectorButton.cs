using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectorButton : MonoBehaviour
{
    private CharacterSaveData myData;
    [SerializeField] private Text displayText;
    [SerializeField] private GameObject PopUp;
    [SerializeField] private GameObject CharacterSheet;
    [SerializeField] private GameObject PlayerReference;
    [SerializeField] private GameObject NPCReference;
    private Vector3 spawningPos = new Vector3(-0.5f,0,0);
    public void SetData(CharacterSaveData input)
    {
        PopUp.SetActive(false);
        myData = input;
        displayText.text = input.playername;
    }

    public void Edit()
    {
        GameObject newSheet = Instantiate(CharacterSheet) as GameObject;
        newSheet.GetComponent<CharacterSheet>().UpdateStatsIn(myData);
        OnButtonPressed();
    }

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

    public void OnButtonPressed()
    {
        PopUp.SetActive(!PopUp.activeInHierarchy);
    }
}

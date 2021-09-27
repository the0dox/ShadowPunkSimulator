using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SceneSelectorButton : MonoBehaviour
{
    private SceneSaveData myData;
    // Name displayed on the ui
    [SerializeField] private Text displayText;

    public void SetData(SceneSaveData input)
    {
        myData = input;
        displayText.text = input.GetName();
    }

    public void CreateScene()
    {
        GameObject[] prevTiles = GameObject.FindGameObjectsWithTag("Tile");
        GameObject[] prevPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach(GameObject g in prevTiles)
        {
            Destroy(g);
        }
        foreach(GameObject g in prevPlayers)
        {
            Destroy(g);
        }
        StartCoroutine(LoadingDelay());
    }

    IEnumerator LoadingDelay()
    {
        yield return new WaitForSeconds (1);
        Dictionary<Vector3,GameObject> entities = myData.GetTileLocations();
        Dictionary<Vector3,CharacterSaveData> playerEntities = myData.GetPlayerLocations();
        foreach(Vector3 pos in entities.Keys)
        {
            GameObject newEntity = Instantiate(entities[pos]) as GameObject;
            newEntity.transform.position = pos;
        }
        foreach(Vector3 pos in playerEntities.Keys)
        {
            PlayerSpawner.CreatePlayer(playerEntities[pos],pos, false);
        }
        GameObject GM = GameObject.FindGameObjectWithTag("GameController");
        if(GM.TryGetComponent<TurnManager>(out TurnManager tm))
        {
            tm.SortQueue();
        }
        else if (GM.TryGetComponent<LevelEditor>(out LevelEditor le))
        {
            le.LoadLevel();
        }
        DmMenu.Toggle();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalManager : MonoBehaviour 
{
    public static GlobalManager Instance;
    [SerializeField] private CriticalDamageReference cdr;
    [SerializeField] private SkillReference sr;
    [SerializeField] private ItemReference ir;
    [SerializeField] private TileReference tr;
    [SerializeField] private ConditionsReference cr;
    [SerializeField] private PlayerSpawner ps;
    public static SceneSaveData LoadedScene;

    void Start()   
       {
        if (Instance == null)
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
            cdr.Init();
            sr.Init();
            ir.Init();
            tr.Init();
            cr.Init();
            ps.Init();
        }
        else if (Instance != this)
        {
            Destroy (gameObject);
        }
        if (LoadedScene != null)
        {
            Instance.StartCoroutine(LoadOnStart());
        }
    }

    //Loads a level onto the board stage
    public static void PlayLevel(SceneSaveData myData, string type)
    {
        if(SceneManager.GetActiveScene().name == "Overworld")
        {
            OverworldManager.SaveOverworld();
        }
        LoadedScene = myData;
        SceneManager.LoadScene(type);
        
    }

    IEnumerator LoadOnStart()
    {
        yield return new WaitForSeconds(0.01f);
        CameraButtons.UIFreeze(false);
        Dictionary<Vector3,GameObject> entities = LoadedScene.GetTileLocations();
        Dictionary<Vector3,CharacterSaveData> playerEntities = LoadedScene.GetPlayerLocations();
        foreach(Vector3 pos in entities.Keys)
        {
            GameObject newEntity = Instantiate(entities[pos]) as GameObject;
            newEntity.transform.position = pos;
        }
        yield return new WaitForSeconds(0.01f);
        foreach(Vector3 pos in playerEntities.Keys)
        {
            CharacterSaveData csd = playerEntities[pos];
            PlayerSpawner.CreatePlayer(csd,pos, false);
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
        
        else if (GM.TryGetComponent<OverworldManager>(out OverworldManager om))
        {
            for(int i = 0; i < LoadedScene.LeadName.Length; i++)
            {
                if(LoadedScene.LeadName[i] != null)
                {
                    ActionQueueDisplay.LoadActivity(LoadedScene.LeadName[i],LoadedScene.LeadProgress[i],LoadedScene.LeadMax[i]);
                }
            }
        }

        LoadedScene = null;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class GlobalManager : MonoBehaviourPunCallbacks 
{
    public static GlobalManager Instance;
    [SerializeField] private CriticalDamageReference cdr;
    [SerializeField] private SkillReference sr;
    [SerializeField] private ItemReference ir;
    [SerializeField] private TileReference tr;
    [SerializeField] private ConditionsReference cr;
    [SerializeField] private PlayerSpawner ps;
    [SerializeField] private PhotonView pv;
    public static SceneSaveData LoadedScene;

    void Awake()   
    {
        DontDestroyOnLoad(gameObject);
        Instance = this;
        cdr.Init();
        sr.Init();
        ir.Init();
        tr.Init();
        cr.Init();
        ps.Init();  
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    [PunRPC]
    void RPC_Start()
    {
        Debug.Log("Built");
        
    }

    //Loads a level onto the board stage
    public static void PlayLevel(SceneSaveData myData, string type)
    {
        if(PhotonNetwork.IsMasterClient)
        {
            if(SceneManager.GetActiveScene().name == "Overworld")
            {
                OverworldManager.SaveOverworld();
            }
            LoadedScene = myData;
            PhotonNetwork.RemoveBufferedRPCs();
            PhotonNetwork.IsMessageQueueRunning = false;
            PhotonNetwork.LoadLevel(type);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(LoadedScene != null)
        {
            PhotonNetwork.IsMessageQueueRunning = true;
            Dictionary<string,string> tileLocs = LoadedScene.GetTileLocations();
            pv.RPC("RPC_LoadTile",RpcTarget.AllBuffered,tileLocs);
            Instance.StartCoroutine(LoadOnStart());
        }
    }

    IEnumerator LoadOnStart()
    {
        yield return new WaitForSeconds(0.05f);
        CameraButtons.UIFreeze(false);
        yield return new WaitForSeconds(0.05f);
        Dictionary<Vector3,string> playerEntities = LoadedScene.GetPlayerLocations();
        yield return new WaitForSeconds(0.01f);
        foreach(Vector3 pos in playerEntities.Keys)
        {
            PlayerSpawner.CreatePlayer(playerEntities[pos],pos, false);
        }
        GameObject GM = GameObject.FindGameObjectWithTag("GameController");
        if(GM.TryGetComponent<TurnManager>(out TurnManager tm))
        {
            tm.StartNewRound();
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
                    ActionQueueDisplay.AddActivity(LoadedScene.LeadName[i],LoadedScene.LeadProgress[i],LoadedScene.LeadMax[i]);
                }
            }
        }

        LoadedScene = null;
    }

    public static void RemoveTile(Tile destroyedTile)
    {
        Instance.pv.RPC("RPC_DestroyTile",RpcTarget.AllBuffered,destroyedTile.transform.position);
    }
    
    [PunRPC]
    void RPC_DestroyTile(Vector3 destroyKey)
    {
        BoardBehavior.RemoveTile(destroyKey);
    }

    [PunRPC]
    void RPC_LoadTile(Dictionary<string,string> TileLocations)
    {
        foreach(string posKey in TileLocations.Keys)
        {
            string[] posSplit = posKey.Split(',');
            Vector3 pos = new Vector3(float.Parse(posSplit[0]),float.Parse(posSplit[1]),float.Parse(posSplit[2]));
            GameObject Tile = TileReference.Tile(TileLocations[posKey]);
            GameObject newEntity = Instantiate(Tile) as GameObject;
            newEntity.transform.position = pos;
        }
        BoardBehavior.Init();
    }
}
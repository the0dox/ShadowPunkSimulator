using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// game master object that needs to be present in a scene to spawn characters, creates characters based of of charactersave data
public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject PlayerReference;
    [SerializeField] private Material PlayerMaterial;
    [SerializeField] private Material NPCMaterial;
    [SerializeField] private List<GameObject> Models;
    private static Material SPlayerMaterial;
    private static Material SNPCMaterial;

    private static GameObject SPlayerReference;
    private static GameObject SNPCREference;
    private static Dictionary<string, GameObject> SModels = new Dictionary<string, GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        SPlayerReference = PlayerReference;
        SPlayerMaterial = PlayerMaterial;
        SNPCMaterial = NPCMaterial;

        foreach(GameObject m in Models)
        {
            SModels.Add(m.name,m);
        }
    }

    public static void CreatePlayer(CharacterSaveData csd,Vector3 pos, bool controlable)
    {
        // Create Character with no model at position
        GameObject EmptyPlayer = Instantiate(SPlayerReference) as GameObject;
        EmptyPlayer.transform.position = pos;
        // Create visible mesh depending on the Model characteristics of csd
        GameObject PlayerModel = Instantiate(SModels[csd.Model]) as GameObject;
        PlayerModel.transform.SetParent(EmptyPlayer.transform);
        PlayerModel.transform.localPosition = new Vector3(0,-1,0);
        PlayerModel.name = PlayerModel.name.Split('(')[0];
        EmptyPlayer.GetComponent<PlayerStats>().model = PlayerModel;
        // Material is dependent on team
        if(csd.team == 0)
        {
            PlayerModel.GetComponentInChildren<MeshRenderer>().material = SPlayerMaterial;
        }
        else
        {
            PlayerModel.GetComponentInChildren<MeshRenderer>().material = SNPCMaterial;   
        }
        // Initalize the player 
        EmptyPlayer.GetComponent<PlayerStats>().DownloadSaveData(csd);
        EmptyPlayer.GetComponent<TacticsMovement>().Init();
        // insert the player in whatever scene its needed in
        GameObject GC = GameObject.FindGameObjectWithTag("GameController");
        if(GC.TryGetComponent<TurnManager>(out TurnManager tm))
        {
            tm.AddPlayer(EmptyPlayer);
        }
        else if(GC.TryGetComponent<OverworldManager>(out OverworldManager om))
        {
            OverworldManager.AddPlayer(EmptyPlayer);
        }
        else
        {
            if(controlable)
            {
                GC.GetComponent<LevelEditor>().AddPlayer(EmptyPlayer);
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// game master object that needs to be present in a scene to spawn characters, creates characters based of of charactersave data
public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    // Empty player object that is spawned
    [SerializeField] private GameObject PlayerReference;
    // Reference for the friendly color applied to new player objects 
    [SerializeField] private Material PlayerMaterial;
    // Reference for the enemy color applied to new player objects
    [SerializeField] private Material NPCMaterial;
    // List of all .obj files used for character models
    [SerializeField] private List<GameObject> Models;
    // Each new player is given a unique ID (different from their owner ID) which can be quickly sent through RPCs
    private static Dictionary<int, PlayerStats> IDs = new Dictionary<int, PlayerStats>();
    public static Material SPlayerMaterial;
    public static Material SNPCMaterial;

    private static GameObject SPlayerReference;
    private static Dictionary<string, Mesh> SModels = new Dictionary<string, Mesh>();
    private static PhotonView pv;

    // Start is called before the first frame update
    public void Init()
    {
        pv = GetComponent<PhotonView>();
        SPlayerReference = PlayerReference;
        SPlayerMaterial = PlayerMaterial;
        SNPCMaterial = NPCMaterial;

        foreach(GameObject g in Models)
        {
            SModels.Add(g.name,g.GetComponentInChildren<MeshFilter>().sharedMesh);
        }
    }

    // owner: player who deployed the drone
    // template: the Drone item that is being converted into a new player object
    // pos: the spawn location of the player
    public static GameObject CreatePlayer(PlayerStats owner, Drone template, Vector3 pos)
    {
        CharacterSaveData csd = new CharacterSaveData(owner, template);
        return CreatePlayer(csd, pos, false);
    }

    // csdname: name of a desired charactersavedata to be spawned
    // pos: the spawn location of the player
    // controlable: if set to true player is treated as a game token, else it cannot be interacted with
    public static GameObject CreatePlayer(string csdname, Vector3 pos, bool controlable)
    {
        CharacterSaveData csd = DmMenu.GetCSD(csdname);
        if(csd != null)
        {
            return CreatePlayer(csd, pos, controlable);
        }
        Debug.LogWarning("Error: charactersave invalid charactersavedata sent to player spawner, no player will be spawned");
        return null;
    }

    // csd: charactersave data to be downloaded into the new player
    // pos: the spawn location of the player
    // controlable: if set to true player is treated as a game token, else it cannot be interacted with
    public static GameObject CreatePlayer(CharacterSaveData csd,Vector3 pos, bool controlable)
    {
        // Create Character with no model at position
        GameObject EmptyPlayer = PhotonNetwork.Instantiate("Player", pos, Quaternion.identity);
        EmptyPlayer.transform.position = pos;
        // Create visible mesh depending on the Model characteristics of csd
        EmptyPlayer.GetComponentInChildren<MeshFilter>().mesh = SModels[csd.Model];
        // Material is dependent on team
        if(csd.team == 0)
        {
            EmptyPlayer.GetComponentInChildren<MeshRenderer>().material = SPlayerMaterial;
        }
        else
        {
            EmptyPlayer.GetComponentInChildren<MeshRenderer>().material = SNPCMaterial;   
        }
        // Initalize the player 
        int ID = IDs.Count;
        // Assign playerowner
        int ownerID = DmMenu.GetOwner(csd).ActorNumber;
        EmptyPlayer.GetComponent<PlayerStats>().DownloadSaveData(csd, ID, ownerID);
        EmptyPlayer.GetComponent<TacticsMovement>().Init();
        IDs.Add(ID,EmptyPlayer.GetComponent<PlayerStats>());

        // insert the player in whatever scene its needed in
        GameObject GC = GameObject.FindGameObjectWithTag("GameController");
        if(GC.TryGetComponent<TurnManager>(out TurnManager tm))
        {
            if(controlable)
            {
                tm.PlacePlayer(EmptyPlayer);
            }
            else
            {
                tm.AddPlayer(EmptyPlayer);
            }
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
        return EmptyPlayer;
    }

    public static PlayerStats IDtoPlayer(int id)
    {
        return IDs[id];
    }

    public static Dictionary<string,Mesh> GetPlayers()
    {
        return SModels;
    }

    public static void ClientUpdateIDs(PlayerStats newPlayer)
    {
        if(!pv.IsMine)
        {
            IDs.Add(newPlayer.GetID(), newPlayer);
        }
    }
}

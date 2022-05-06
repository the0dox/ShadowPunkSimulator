using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// game master object that needs to be present in a scene to spawn characters, creates characters based of of charactersave data
public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject PlayerReference;
    [SerializeField] private Material PlayerMaterial;
    [SerializeField] private Material NPCMaterial;
    [SerializeField] private List<GameObject> Models;
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

    public static GameObject CreatePlayer(PlayerStats owner, Drone template, Vector3 pos)
    {
        CharacterSaveData csd = new CharacterSaveData(owner, template);
        return CreatePlayer(csd, pos, false);
    }

    public static GameObject CreatePlayer(string csdname, Vector3 pos, bool controlable)
    {
        return CreatePlayer(DmMenu.GetCSD(csdname), pos, controlable);
    }

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

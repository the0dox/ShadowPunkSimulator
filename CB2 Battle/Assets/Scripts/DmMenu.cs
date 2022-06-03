using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class DmMenu : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject Display;
    static private Dictionary<int, CharacterSaveData> SavedCharacters;
    static private Dictionary<int, Photon.Realtime.Player> CharacterPermissions;
    static private Dictionary<int, CharacterSheet> ActiveCharacterSheets;
    [SerializeField] private GameObject PlayerScreen;
    [SerializeField] private GameObject SelectorButton;
    [SerializeField] private GameObject SceneButton;
    [SerializeField] private Text MDRtoggleStatus;
    [SerializeField] private GameObject ClientDisplay;
    [SerializeField] private PhotonView pv;
    [SerializeField] private RectTransform content;
    private static GameObject DisplayInstance;
    private static GameObject PlayerScreenInstance;
    private List<GameObject> PrevSelectorButtons = new List<GameObject>();
    private List<SceneSaveData> SavedScenes = new List<SceneSaveData>();
    private Vector3 CharacterSelectorPos;
    private static GameObject instance;
    private static PhotonView spv;
    private Dictionary<int, string> DummyCharacters = new Dictionary<int, string>();
    public static bool debugMode = false;
    private bool ready = false;

    public static CharacterSheet ActiveCharacterSheet;

    void Start()
    {
        spv = pv;
        if(pv.IsMine && SavedCharacters == null)
        {
            StartCoroutine(LoadDelay());
        }
        else
        {
            ready = true;
        }
        instance = gameObject;
        DisplayInstance = Display;
        PlayerScreenInstance = PlayerScreen;
        //AddMissingSkill("KnockDown",1);
        SkillPromptBehavior.ManualRolls = false;
        MDRToggle();
    }

    void Update()
    {
        // on pressing escape
        if(Input.GetKeyUp(KeyCode.Escape) && ready)
        {
            // delete sheets if active
            if(ActiveCharacterSheet != null)
            {
                ActiveCharacterSheet.OnExit();
                ActiveCharacterSheet = null;
                CameraButtons.UIFreeze(false);
            }
            // show dm menu to the server creator
            else if(pv.IsMine)
            {
                Toggle();
            }
            // if a single player
            else
            {
                // first time calling players need to open the character select screen
                SinglePlayerToggle();
            }
        }
    }

    IEnumerator LoadDelay()
    {
        yield return new WaitForSeconds(1f);
        SavedCharacters = new Dictionary<int, CharacterSaveData>();
        CharacterPermissions = new Dictionary<int, Photon.Realtime.Player>();
        ActiveCharacterSheets = new Dictionary<int, CharacterSheet>();
        int index = 0;
        foreach(CharacterSaveData csd in SaveSystem.LoadPlayer())
        {
            SavedCharacters.Add(index, csd);
            // add players 
            if(csd.team == 0)
            {
                CharacterPermissions.Add(index, null);
            }
            index++;
        }
        ready = true;
        SavedScenes = SaveSystem.LoadScenes();
    }

    /* Depreciated debugging tool
    private void AddMissingSkill(string newSkill,int level)
    {
        Skill missingSkill = new Skill(SkillReference.GetSkill(newSkill),level);
        foreach(CharacterSaveData csd in SavedCharacters)
        {
            csd.addSkill(missingSkill);
            Debug.Log(newSkill + " added to " + csd.playername);
        }
    }
    */

    public void MDRToggle()
    {
        SkillPromptBehavior.ManualRolls = !SkillPromptBehavior.ManualRolls;
        string addition = "";
        if(SkillPromptBehavior.ManualRolls)
        {
            addition = "Manual";
        }
        else
        {
            addition = "Automatic";
        }

        MDRtoggleStatus.text = "Die Entries: " + addition;
    }

    // A player client can either ask for available sheets if they don't have an avatar or call for their avatar's sheet
    public void SinglePlayerToggle()
    {   
        CameraButtons.UIFreeze(!ClientDisplay.activeInHierarchy);
        ClientDisplay.SetActive(!ClientDisplay.activeInHierarchy);
    }

    public void SinglePLayerCharacterSheet()
    {
        ClientDisplay.SetActive(false);
        pv.RPC("RPC_ClientDisplay", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    public static void Toggle()
    {
        CameraButtons.UIFreeze(!DisplayInstance.activeInHierarchy);
        DisplayInstance.SetActive(!DisplayInstance.activeInHierarchy);
        PlayerScreenInstance.SetActive(false);
        TurnManager.DebugTooltipToggle();
    }

    public void CreateCharacter()
    {
        CharacterSaveData newplayer = new CharacterSaveData(true);
        SavedCharacters.Add(SavedCharacters.Count, newplayer);
        CharacterPermissions.Add(SavedCharacters.Count, null);
        ViewCharacters();
    }
    public void CreateNPC()
    {
        CharacterSaveData newplayer = new CharacterSaveData(false);
        SavedCharacters.Add(SavedCharacters.Count, newplayer);
        ViewCharacters();
    }

    public void newScene()
    {
        CameraButtons.UIFreeze(false);
        PhotonNetwork.LoadLevel("LevelEditor");
    }

    public void DebugMode()
    {
        Debug.Log("WARNING: DEBUGMODE MUST BE ACTIVATED BEFORE LOADING A LEVEL");
        debugMode = true;
    }

    public void Quit()
    {
        if(pv.IsMine)
        {
            foreach(int index in SavedCharacters.Keys)
            {
                SavedCharacters[index].Quit();
            }
        }
        Application.Quit();
    }

    //smart view of all character sheets for DM only!
    public void ViewCharacters()
    {
        foreach(GameObject prevButton in PrevSelectorButtons)
        {
            Destroy(prevButton);
        }
        PlayerScreen.SetActive(true);
        foreach(int key in SavedCharacters.Keys)
        {
            GameObject newButton = Instantiate(SelectorButton) as GameObject;
            newButton.transform.SetParent(content);
            newButton.GetComponent<CharacterSelectorButton>().SetData(key, SavedCharacters[key]);
            PrevSelectorButtons.Add(newButton);
        }
        
    }

    public static void DMDisplay(CharacterSaveData csd)
    {
        Photon.Realtime.Player owner = GetOwner(csd);
        // if an entry hasn't yet been created for this player
        if(!ActiveCharacterSheets.ContainsKey(owner.ActorNumber))
        {
            ActiveCharacterSheets.Add(owner.ActorNumber, null);
        }
        // if sheet is currently active open it
        if(ActiveCharacterSheets[owner.ActorNumber] != null)
        {
            Debug.Log("tunring on sheet for master that already exisits");
            ActiveCharacterSheets[owner.ActorNumber].UpdateStatsIn(csd, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        // else create a new one
        else
        {   
            Debug.Log("creating new sheet for master");
            CharacterSheet newSheet = PhotonNetwork.Instantiate("CharacterSheet",new Vector3(), Quaternion.identity).GetComponent<CharacterSheet>();
            // create sheet of csd on masters screen
            newSheet.UpdateStatsIn(csd, PhotonNetwork.LocalPlayer.ActorNumber);
            ActiveCharacterSheets[owner.ActorNumber] = newSheet;
        }
    }

    // called on the master by a given client, sends back a charactersheet
    [PunRPC]
    void RPC_ClientDisplay(int callingPlayerID)
    {
        Photon.Realtime.Player CallingPlayer = PhotonNetwork.CurrentRoom.GetPlayer(callingPlayerID);
        // if I own a player, find it and send back his character sheet
        if(CharacterPermissions.ContainsValue(CallingPlayer))
        {
            foreach(KeyValuePair<int, Photon.Realtime.Player> kvp in CharacterPermissions)
            {
                if(kvp.Value != null && kvp.Value.Equals(CallingPlayer))
                {
                    CharacterSaveData csd = SavedCharacters[kvp.Key];
                    SendClientCharacterSheet(csd,callingPlayerID);
                }
            }
        }
        // if I don't own a player, display unowned players 
        else
        {
            Dictionary<int,string> dummyCopy = new Dictionary<int, string>();
            foreach(KeyValuePair<int,CharacterSaveData> kvp in SavedCharacters)
            {
                // if a player and not owned by anyone
                if(CharacterPermissions.ContainsKey(kvp.Key) && CharacterPermissions[kvp.Key] == null)
                {
                    dummyCopy.Add(kvp.Key,kvp.Value.playername);
                }
            }
            pv.RPC("RPC_Recieve_Players", CallingPlayer, dummyCopy);
        }
        
    }

    // master sends character data to calling player id
    private void SendClientCharacterSheet(CharacterSaveData csd, int callingPlayerID)
    {
        // if an entry hasn't yet been created for this player
        if(!ActiveCharacterSheets.ContainsKey(callingPlayerID))
        {
            ActiveCharacterSheets.Add(callingPlayerID, null);
        }
        // if this player's charactersheet is not active
        if(ActiveCharacterSheets[callingPlayerID] == null)
        {
            Debug.Log("creating new sheet for client, hidden on master");
            //flag indicating its safe to send charactersheet
            GameObject newSheet = PhotonNetwork.Instantiate("CharacterSheet", new Vector3(), Quaternion.identity);
            //create sheet of csd on calling players screen
            CharacterSheet characterSheet = newSheet.GetComponent<CharacterSheet>();
            ActiveCharacterSheets[callingPlayerID] = characterSheet;
            characterSheet.UpdateStatsIn(csd, callingPlayerID);
        }
        // if charactersheet is already active
        else
        {
            Debug.Log("activing sheet that already exisits on master for client");
            ActiveCharacterSheets[callingPlayerID].UpdateStatsIn(csd, callingPlayerID);
        }
    }

    // master knows that this player can ask for another charactersheet 
    public static void RemoveClientSheet(int callingPlayerID)
    {
        if(!ActiveCharacterSheets.ContainsKey(callingPlayerID))
        {
            ActiveCharacterSheets.Add(callingPlayerID, null);
        }
        ActiveCharacterSheets[callingPlayerID] = null;
    }

    [PunRPC]
    void RPC_Recieve_Players(Dictionary<int, string> newPlayers)
    {
        DummyCharacters = newPlayers;
        foreach(GameObject prevButton in PrevSelectorButtons)
        {
            Destroy(prevButton);
        }
        PlayerScreen.SetActive(true);
        foreach(int index in DummyCharacters.Keys)
        {
            GameObject newButton = Instantiate(SelectorButton) as GameObject;
            newButton.transform.SetParent(content);
            newButton.GetComponent<CharacterSelectorButton>().SetDummyData(index, DummyCharacters[index]);
            PrevSelectorButtons.Add(newButton);
        }
    }

    public void LoadScene()
    {
        foreach(GameObject prevButton in PrevSelectorButtons)
        {
            Destroy(prevButton);
        }
        PlayerScreen.SetActive(true);
        SavedScenes = SaveSystem.LoadScenes();
        foreach(SceneSaveData ssd in SavedScenes)
        {
            GameObject newButton = Instantiate(SceneButton) as GameObject;
            newButton.transform.SetParent(content);
            newButton.GetComponent<SceneSelectorButton>().SetData(ssd);
            PrevSelectorButtons.Add(newButton);
        }
    }

    public static CharacterSaveData GetCSD(string name)
    {
        foreach(int index in SavedCharacters.Keys)
        {
            CharacterSaveData csd = SavedCharacters[index];
            if(csd.playername == name)
            {
                return csd;
            }
        }
        return null;
    }

    // Player side reference to claim a character sends the corresponding RPC to master
    public static void AssignCharacter(int StatsID)
    {
        int myID = PhotonNetwork.LocalPlayer.ActorNumber;
        PlayerScreenInstance.SetActive(false);
        spv.RPC("RPC_AssignCharacter",RpcTarget.MasterClient,StatsID,myID);
    }

    public static Photon.Realtime.Player GetOwner(PlayerStats Player)
    {
        return GetOwner(Player.myData);
    }

    public static Photon.Realtime.Player GetOwner(CharacterSaveData csd)
    {
        if(csd.isMinion)
        {
            return GetOwner(csd.getOwner());
        }
        if(SavedCharacters.ContainsValue(csd))
        {
            foreach(KeyValuePair<int,CharacterSaveData> kvp in SavedCharacters)
            {
                if(kvp.Value == csd)
                {
                    int index = kvp.Key;
                    if(CharacterPermissions.ContainsKey(index) && CharacterPermissions[index] != null)
                    {
                        //Debug.Log("player " + player.GetName() + " is owned by player " + CharacterPermissions[index]);
                        return CharacterPermissions[index];
                    }
                }
            }
        }
        //Debug.Log("player " + player.GetName() + " is owned by the server");
        return PhotonNetwork.LocalPlayer;
    }

    // called from a player to the master, assigns selected character to player 
    [PunRPC]
    void RPC_AssignCharacter(int StatsID, int callingPlayerID)
    {
        Photon.Realtime.Player CallingPlayer = PhotonNetwork.CurrentRoom.GetPlayer(callingPlayerID);
        if(CharacterPermissions.ContainsKey(StatsID) && CharacterPermissions[StatsID] == null)
        {
            CharacterPermissions[StatsID] = CallingPlayer;
            CombatLog.Log("Character " + SavedCharacters[StatsID].playername + " is now assigned to Player " + callingPlayerID);
        }
        else
        {
            CombatLog.Log("Character " + SavedCharacters[StatsID].playername + " is already assigned!");
        }
    }
}

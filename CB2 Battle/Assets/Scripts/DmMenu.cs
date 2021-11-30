using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class DmMenu : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject Display;
    static private Dictionary<int, CharacterSaveData> SavedCharacters;
    
    [SerializeField] private GameObject PlayerScreen;
    [SerializeField] private GameObject SelectorButton;
    [SerializeField] private GameObject SceneButton;
    [SerializeField] private Text MDRtoggleStatus;
    [SerializeField] private PhotonView pv;
    private static GameObject DisplayInstance;
    private static GameObject PlayerScreenInstance;
    private List<GameObject> PrevSelectorButtons = new List<GameObject>();
    private List<SceneSaveData> SavedScenes = new List<SceneSaveData>();
    private Vector3 CharacterSelectorPos;
    private static GameObject instance;
    private static PhotonView spv;
    private Dictionary<int, string> DummyCharacters = new Dictionary<int, string>();

    void Start()
    {
        spv = pv;
        if(pv.IsMine && SavedCharacters == null)
        {
            StartCoroutine(LoadDelay());
        }
        instance = gameObject;
        DisplayInstance = Display;
        PlayerScreenInstance = PlayerScreen;
        //AddMissingSkill("KnockDown",1);
        SkillPromptBehavior.ManualRolls = false;
        MDRToggle();
    }

    IEnumerator LoadDelay()
    {
        yield return new WaitForSeconds(1f);
        SavedCharacters = new Dictionary<int, CharacterSaveData>();
        int index = 0;
        foreach(CharacterSaveData csd in SaveSystem.LoadPlayer())
        {
            SavedCharacters.Add(index, csd);
            index++;
        }
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

    public static void Toggle()
    {
        CameraButtons.UIFreeze(!DisplayInstance.activeInHierarchy);
        DisplayInstance.SetActive(!DisplayInstance.activeInHierarchy);
        PlayerScreenInstance.SetActive(false);
    }

    public void CreateCharacter()
    {
        CharacterSaveData newplayer = new CharacterSaveData(true);
        SavedCharacters.Add(SavedCharacters.Count, newplayer);
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

    public void Quit()
    {
        foreach(int index in SavedCharacters.Keys)
        {
            SavedCharacters[index].Quit();
        }
    }

    public void ViewCharacters()
    {
        pv.RPC("RPC_GetDummyPlayers", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    void RPC_GetDummyPlayers(int callingPlayerID)
    {
        Photon.Realtime.Player CallingPlayer = PhotonNetwork.CurrentRoom.GetPlayer(callingPlayerID);
        Dictionary<int,string> dummyCopy = new Dictionary<int, string>();
        foreach(KeyValuePair<int,CharacterSaveData> kvp in SavedCharacters)
        {
            dummyCopy.Add(kvp.Key,kvp.Value.playername);
        }
        pv.RPC("RPC_Recieve_Players", CallingPlayer, dummyCopy);
    }

    [PunRPC]
    void RPC_Recieve_Players(Dictionary<int, string> newPlayers)
    {
        DummyCharacters = newPlayers;
        bool amClient = pv.IsMine;
        foreach(GameObject prevButton in PrevSelectorButtons)
        {
            Destroy(prevButton);
        }
        CharacterSelectorPos = new Vector3(250,130,0);
        PlayerScreen.SetActive(true);
        foreach(int index in DummyCharacters.Keys)
        {
            GameObject newButton = Instantiate(SelectorButton) as GameObject;
            newButton.transform.SetParent(PlayerScreen.transform);
            newButton.transform.localPosition = CharacterSelectorPos;
            if(amClient)
            {
                newButton.GetComponent<CharacterSelectorButton>().SetData(index, SavedCharacters[index]);
            }
            else
            {  
                newButton.GetComponent<CharacterSelectorButton>().SetDummyData(index, DummyCharacters[index]);
            }
            newButton.transform.localScale = Vector3.one;
            PrevSelectorButtons.Add(newButton);
            CharacterSelectorPos -= new Vector3(125,0,0);
            if(CharacterSelectorPos.x < -250)
            {
                CharacterSelectorPos.x = 250;
                CharacterSelectorPos.y -= 75;
            }
        }
    }

    public void LoadScene()
    {
        foreach(GameObject prevButton in PrevSelectorButtons)
        {
            Destroy(prevButton);
        }
        CharacterSelectorPos = new Vector3(250,130,0);
        PlayerScreen.SetActive(true);
        SavedScenes = SaveSystem.LoadScenes();
        foreach(SceneSaveData ssd in SavedScenes)
        {
            GameObject newButton = Instantiate(SceneButton) as GameObject;
            newButton.transform.SetParent(PlayerScreen.transform);
            newButton.transform.localPosition = CharacterSelectorPos;
            newButton.GetComponent<SceneSelectorButton>().SetData(ssd);
            PrevSelectorButtons.Add(newButton);
            CharacterSelectorPos -= new Vector3(125,0,0);
            if(CharacterSelectorPos.x < -250)
            {
                CharacterSelectorPos.x = 250;
                CharacterSelectorPos.y -= 75;
            }
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

    public static void DisplayCharacterSheet(int StatsID)
    {
        int myID = PhotonNetwork.LocalPlayer.ActorNumber;   
        spv.RPC("RPC_CharacterSheet",RpcTarget.MasterClient,StatsID,myID);
    }

    [PunRPC]
    void RPC_CharacterSheet(int StatsID, int callingPlayerID)
    {
        CharacterSaveData csd = SavedCharacters[StatsID];
        GameObject newSheet = PhotonNetwork.Instantiate("CharacterSheet", new Vector3(), Quaternion.identity);
        newSheet.GetComponent<CharacterSheet>().UpdateStatsIn(csd, callingPlayerID);
    }
}

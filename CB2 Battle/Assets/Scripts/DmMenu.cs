using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DmMenu : MonoBehaviour
{
    [SerializeField] private GameObject Display;
    [SerializeField] private List<CharacterSaveData> SavedCharacters = new List<CharacterSaveData>();
    [SerializeField] private GameObject PlayerScreen;
    [SerializeField] private GameObject SelectorButton;
    [SerializeField] private GameObject SceneButton;
    [SerializeField] private Text MDRtoggleStatus;
    private static GameObject DisplayInstance;
    private static GameObject PlayerScreenInstance;
    private List<GameObject> PrevSelectorButtons = new List<GameObject>();
    private List<SceneSaveData> SavedScenes = new List<SceneSaveData>();
    private Vector3 CharacterSelectorPos;

    void Start()
    {
        DisplayInstance = Display;
        PlayerScreenInstance = PlayerScreen;
        SavedCharacters = SaveSystem.LoadPlayer();
        //AddMissingSkill("KnockDown",1);
        SavedScenes = SaveSystem.LoadScenes();
        SkillPromptBehavior.ManualRolls = false;
        MDRToggle();
    }

    private void AddMissingSkill(string newSkill,int level)
    {
        Skill missingSkill = new Skill(SkillReference.GetSkill(newSkill),level);
        foreach(CharacterSaveData csd in SavedCharacters)
        {
            csd.addSkill(missingSkill);
            Debug.Log(newSkill + " added to " + csd.playername);
        }
    }

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
        SavedCharacters.Add(newplayer);
        ViewCharacters();
    }
    public void CreateNPC()
    {
        CharacterSaveData newplayer = new CharacterSaveData(false);
        SavedCharacters.Add(newplayer);
        ViewCharacters();
    }

    public void Quit()
    {
        foreach(CharacterSaveData csd in SavedCharacters)
        {
            csd.Quit();
        }
    }

    public void ViewCharacters()
    {
        foreach(GameObject prevButton in PrevSelectorButtons)
        {
            Destroy(prevButton);
        }
        CharacterSelectorPos = new Vector3(250,130,0);
        PlayerScreen.SetActive(true);
        foreach(CharacterSaveData csd in SavedCharacters)
        {
            GameObject newButton = Instantiate(SelectorButton) as GameObject;
            newButton.transform.SetParent(PlayerScreen.transform);
            newButton.transform.localPosition = CharacterSelectorPos;
            newButton.GetComponent<CharacterSelectorButton>().SetData(csd);
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
}

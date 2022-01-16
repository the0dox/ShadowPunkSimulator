using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// UI manager to create new overworld activities
public class ActivityGenerator : MonoBehaviour
{
    // Selects type of activity
    [SerializeField] private GameObject TypeField;
    // Selects difficulty of investigation
    [SerializeField] private GameObject InvestigateField;
    // Selects duration of a clock
    [SerializeField] private GameObject DurationField;
    // Selects item quality for investigation
    [SerializeField] private GameObject ItemField;
    // Selects skill used to investigate
    [SerializeField] private GameObject SkillField;
    // Selects player in charge of the investigation
    [SerializeField] private GameObject P1Field;
    // Selects player assisting in the investigation
    [SerializeField] private GameObject P2Field;
    // Selects player assisting in the investigation
    [SerializeField] private GameObject P3Field;
    // Creates the activity
    [SerializeField] private GameObject adderButton;
    // Reference to create more activity objects
    [SerializeField] private GameObject ActivityObjectReference;
    // Reference to ActionQueueDisplay to store newly created investigations
    [SerializeField] private GameObject ActivityTab;
    // All players participating in the investigation
    private PlayerStats[] PlayerSelection;
    // Skill selected by SkillField, can help advance the investigation if passed
    private string SkillChoice;
    // Item selected by ItemField
    private string ItemChoice;
    // Conditional modifiers on the Skillchoice depending on type of investigation
    private int Modifier;
    // Amount of Time required to complete Acitivity
    private int Time;
    // Keys correspond to item availability, values correspond to difficulty of finding item.
    private Dictionary<string,int> AvailablityToDifficulty = new Dictionary<string, int>{
        {"Abundant",30},
        {"Plentiful",20},
        {"Common",10},
        {"Average",0},
        {"Scarce",-10},
        {"Rare",-20},
        {"Very Rare", -30}
    };
    // Keys correspond to item availability, values correspond to time to find item.
    private Dictionary<string,int[]> AvailablityToTime = new Dictionary<string, int[]>{
        {"Abundant", new int[2]{1,1}},
        {"Plentiful",new int[2]{1,1}},
        {"Common",new int[2]{1,1}},
        {"Average",new int[2]{10,1}},
        {"Scarce", new int[2]{10,24}},
        {"Rare", new int[2]{10,168}},
        {"Very Rare", new int[2]{5,672}}
    };
    // Keys correspond to lead complexity, values correspond to difficulty of completing lead.
    private Dictionary<string, int> ComplexityToDifficulty = new Dictionary<string, int>
    {
        {"Simple",30},
        {"Basic",20},
        {"Drudging",10},
        {"Taxing",0},
        {"Arduous",-10},
        {"Involved",20},
        {"Labyrinthine",30}
    };
    // Keys correspond to lead complexity, values correspond to time to complete lead.
    private Dictionary<string, int> ComplexitytoTime = new Dictionary<string, int>
    {
        {"Simple",1},
        {"Basic",6},
        {"Drudging",24},
        {"Taxing",72},
        {"Arduous",264},
        {"Involved",744},
        {"Labyrinthine",8640}
    };
    // When enabled, this object resets all of its fields 
    void OnEnable()
    {
        Modifier = 0;
        Time = 0;
        PlayerSelection = new PlayerStats[2];
        SkillChoice = null;
        ItemChoice = null;
        TypeField.SetActive(true);
        ResetDD(TypeField);
        InvestigateField.SetActive(false);
        ResetDD(InvestigateField);
        DurationField.SetActive(false);
        ResetDD(DurationField);
        ItemField.SetActive(false);
        ResetDD(ItemField);
        SkillField.SetActive(false);
        ResetDD(SkillField);
        P1Field.SetActive(false);
        ResetDD(P1Field);
        P2Field.SetActive(false);
        ResetDD(P2Field);
        P3Field.SetActive(false);
        P2Field.transform.localPosition = new Vector3(31.7352f, -40, 0);
        P3Field.transform.localPosition = new Vector3(31.7352f, -80, 0);
        ResetDD(P3Field);
        adderButton.SetActive(false);
    }
    // Selects investigation type, depending on selection, enables branching paths
    public void TypeSelection()
    {
        if(GetChoice(TypeField).Equals("Investigate"))
        {
            disableDD(TypeField);
            InvestigateField.SetActive(true);
        }
        if(GetChoice(TypeField).Equals("Find Equipment"))
        {
            disableDD(TypeField);
            ItemField.SetActive(true);
            ItemField.GetComponent<Dropdown>().ClearOptions();
            List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
            Dropdown.OptionData baseResponse = new Dropdown.OptionData();
            baseResponse.text = "None";
            results.Add(baseResponse);
            Dictionary<string,ItemTemplate> Items = ItemReference.ItemTemplates();
            foreach(string Key in Items.Keys)
            {
                if(!Key.Equals("Unarmed"))
                {
                    Dropdown.OptionData NewData = new Dropdown.OptionData();
                    NewData.text = Key + " (" + Items[Key].availablity + ")";
                    results.Add(NewData);
                }
            }
            ItemField.GetComponent<Dropdown>().AddOptions(results);
        }
        if(GetChoice(TypeField).Equals("Clock"))
        {
            disableDD(TypeField);
            DurationField.SetActive(true);
        }
    }
    // On selecting investigation, players then select the complexity of the investigation
    public void InvestigationSelection()
    {
        if(!GetChoice(InvestigateField).Equals("None"))
        {
            string Complexity = GetChoice(InvestigateField).Split()[0];
            Modifier = ComplexityToDifficulty[Complexity];
            Time = ComplexitytoTime[Complexity];
            disableDD(InvestigateField);
            P1Field.SetActive(true);
            GetRemainingPlayers(P1Field);
        }
    }

    // On selecting Duration, player is prompted to create clock
    public void DurationSelection()
    {
        if(!GetChoice(DurationField).Equals("None"))
        {
            string Complexity = GetChoice(DurationField);
            Time = int.Parse(Complexity);
            disableDD(DurationField);
            adderButton.SetActive(true);
        }
    }

    // On selecting item, players can select an item and applies modifiers depending on item availability 
    public void ItemSelection()
    {
        if(!GetChoice(ItemField).Equals("None"))
        {
            ItemChoice = GetChoice(ItemField);
            ItemChoice = ItemChoice.Split(new string[]{" ("}, System.StringSplitOptions.None)[0];
            Debug.Log(ItemChoice);
            string availablity = ItemReference.ItemTemplates()[ItemChoice].availablity;
            Modifier = AvailablityToDifficulty[availablity];
            Debug.Log(Modifier);
            Time = Random.Range(1,AvailablityToTime[availablity][0]) * AvailablityToTime[availablity][1];
            disableDD(InvestigateField);
            P1Field.SetActive(true);
            GetRemainingPlayers(P1Field);
        }
    }
    // On selecting player, allows player to select the appropriate skill they have for the investigation
    public void PlayerSelectionOne()
    {
        if(!GetChoice(P1Field).Equals("None"))
        {
            PlayerSelection[0] = OverworldManager.Party[GetChoice(P1Field)];
            disableDD(P1Field);
        }
        if(GetChoice(TypeField).Equals("Investigate"))
        {
            SkillField.SetActive(true);
            SkillField.GetComponent<Dropdown>().ClearOptions();
            List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
            Dropdown.OptionData baseResponse = new Dropdown.OptionData();
            baseResponse.text = "None";
            results.Add(baseResponse);
            /*
            foreach(string s in PlayerSelection[0].myData.attribues)
            {
                SkillTemplate currentSkill = SkillReference.GetSkill(s);
                if(currentSkill.Descriptor.Equals("Investigation"))
                {
                    Dropdown.OptionData newData = new Dropdown.OptionData(); 
                    newData.text = s;
                    results.Add(newData);
                }
            }
            */
            SkillField.GetComponent<Dropdown>().AddOptions(results);
        }
        else
        {
            P2Field.SetActive(true);
            GetRemainingPlayers(P2Field);
            P2Field.transform.localPosition += new Vector3(0, 40, 0);
            P3Field.transform.localPosition += new Vector3(0, 40, 0);
            adderButton.SetActive(true);
        }
    }

    // On selecting skills, allows player to add one or two others to aid, or just attempt by themselves
    public void SkillSelection()
    {
        if(!GetChoice(SkillField).Equals("None"))
        {
            SkillChoice = GetChoice(SkillField);
            disableDD(SkillField);
            P2Field.SetActive(true);
            GetRemainingPlayers(P2Field);
            adderButton.SetActive(true);
        } 
    }

    // Each player adds a +10 modifier
    public void PlayerSelectionTwo()
    {
        if(!GetChoice(P2Field).Equals("None"))
        {
            PlayerSelection[1] = OverworldManager.Party[GetChoice(P2Field)];
            disableDD(P2Field);
            P3Field.SetActive(true);
            GetRemainingPlayers(P3Field);
        }
    }
    // Each player adds a +10 modifier
    public void PlayerSelectionThree()
    {
        if(!GetChoice(P2Field).Equals("None"))
        {
            PlayerSelection[2] = OverworldManager.Party[GetChoice(P2Field)];
            disableDD(P2Field);
            P3Field.SetActive(true);
        }
    }

    // Creates a new activity and sends it to the queue to be displayed
    public void MakeActivity()
    {
        string name = "New Lead";
        if(GetChoice(TypeField) == "Clock")
        {
            ActionQueueDisplay.AddActivity("Clock",0,Time);
        }
        else
        {
            if(ItemChoice != null)
            {
                name = ItemChoice;
                SkillChoice = "Inquiry";
            }
            else
            {
                name = PlayerSelection[0].GetName() +"'s lead"; 
            }
            int modifier = 0;
            for(int i = 1; i < PlayerSelection.Length; i++)
            {
                if (PlayerSelection[i] != null)
                {
                    CombatLog.Log(PlayerSelection[i].GetName() + " assists and gives a + 10 Bonus!");
                    modifier += 10;
                }
            }   
            RollResult LeadResult = PlayerSelection[0].AbilityCheck(SkillChoice,modifier,"Lead");
            StartCoroutine(waitForResult(LeadResult, name));
        }
    }

    IEnumerator waitForResult(RollResult input, string name)
    {
        while(!input.Completed())
        {
            yield return new WaitForSeconds(0.5f);
        }
        int extrahours = 0;
        if(input.Passed())
        {
            int dieroll = Random.Range(1,11);
            string usedStat = "";//SkillReference.GetSkill(input.GetSkillType()).derrivedAttribute;
            int statScoreBonus = 0; // input.getOwner().GetAttribute(usedStat);
            extrahours = dieroll + statScoreBonus;
            CombatLog.Log(input.getOwner().playername + " succedes on their check and makes 1d10 + " + usedStat + " score = ( <" + dieroll + "> + " + statScoreBonus + " = " + extrahours + ") hours of progress!");
            extrahours += input.GetDOF();
            CombatLog.Log("Each degree of success (" + input.GetDOF() + ") counts for one more hour of progress!");
        }
        ActionQueueDisplay.AddActivity(name, extrahours,Time);
        gameObject.SetActive(false);
    }

    // dropdown: A gameobject containing a dropwdown component
    // given dropdown returns a string of whatever the dropdown had currently selected
    private string GetChoice(GameObject dropdown)
    {
        return dropdown.GetComponent<Dropdown>().captionText.text;
    }
    // dropdown: A gameobject containing a dropdown component
    // given dropdown, prevents players from changing options
    private void disableDD(GameObject dropdown)
    {
        dropdown.GetComponent<Dropdown>().interactable = false;
    }
    
    // dropdown: A gameobject containing a dropdown component
    // given dropdown, clears the values within dropdown and allows it be used again
    private void ResetDD(GameObject dropdown)
    {
        Dropdown myDD = dropdown.GetComponent<Dropdown>();
        myDD.interactable = true;
        myDD.value = 0;
    }

    // dropdown: A gameobject containing a dropdown component
    // given dropdown, adds players to options that aren't already busy with an activity
    private void GetRemainingPlayers(GameObject dropdown)
    {
        dropdown.GetComponent<Dropdown>().ClearOptions();
        List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
        Dropdown.OptionData baseResponse = new Dropdown.OptionData();
        baseResponse.text = "None";
        results.Add(baseResponse);
        foreach(string key in OverworldManager.Party.Keys)
        {
            PlayerStats current = OverworldManager.Party[key];
            bool Valid = true;
            for(int i = 0; i < PlayerSelection.Length;i++)
            {
                if((PlayerSelection[i] != null && PlayerSelection[i] == current) || current.IsOccupied())
                {
                    Valid = false;
                }
            }
            if(Valid)
            {
                Dropdown.OptionData newData = new Dropdown.OptionData(); 
                newData.text = key;
                results.Add(newData);
            }
        }
        dropdown.GetComponent<Dropdown>().AddOptions(results);
    }
}
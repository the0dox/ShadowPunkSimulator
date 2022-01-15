using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// handles manual die entry. In single player versions of the game, players may want to roll phyiscal die and input the result
// This object is widely unused when the static bool TurnActions.ManualRolls is disabled
public class SkillPromptBehavior : MonoBehaviour
{

    // Instead of handling each die roll at the same time, each dice roll is entered into this queue and resolved sequentially     
    private static Queue<RollResult> RollQueue = new Queue<RollResult>();
    // used to determine if rolls are manual or automatic
    public static bool ManualRolls = true;

    // Reference to the earilest entry in the RollQueue
    private RollResult currentRoll;

    // Reference to the visual popup, set inactive when there are no queued rolls
    [SerializeField] private GameObject display; 
    // Skill used in the current roll
    [SerializeField] private Dropdown SkillDD;
    // Attribute used in the current roll
    [SerializeField] private Dropdown AttributeDD;
    // Max number of successes possible
    [SerializeField] private Dropdown LimitDD;

    // Reference to the input field the player uses to determine the modifier to the pool
    [SerializeField] private InputField ModifierIF;
    // Reference to the input field the player uses to determine the hits required for a successes
    [SerializeField] private InputField ThresholdIF;
    // References to the text explaining what the roll is for
    [SerializeField] private Text displayText;
    [SerializeField] private Text CalculationText;

    private List<string> Limits = new List<string>{"PHY", "SOC", "MEN"};

    // Called on the first frame
    void Start()
    {
        StartCoroutine(QueueCheck());
        ManualRolls = true;
        display.SetActive(false);
    }

    // Delayed check to see if currentRoll is empty, replaces currentRoll with the earilest entry in rollQueue and activate the display
    IEnumerator QueueCheck()
    {
        while(true)
        {
            yield return new WaitForSeconds(0.5f);
            if(RollQueue.Count > 0 && currentRoll == null)
            {
                currentRoll = RollQueue.Dequeue();
                UpdatePrompt();
            }
        }
    }

    // Called whenever a new rollresult is made, adds the roll to the end of the RollQueue
    public static void NewRoll(RollResult input)
    {
        RollQueue.Enqueue(input);
    }

    // Activates and updates the display to prompt the user for a manual die entry
    public void UpdatePrompt()
    {
        if(currentRoll != null)
        {
            UpdateSkillDropDown();
            UpdateAttributeDropDown();
            UpdateLimitDropDown();
            ModifierIF.text = "" + currentRoll.modifiers;
            ThresholdIF.text = "" + currentRoll.threshold;
            CameraButtons.UIFreeze(true);
            display.SetActive(true); 
            string displayName = "";
            if(!string.IsNullOrEmpty(currentRoll.customName))
            {
                displayName = currentRoll.customName;
            }
            else
            {
                displayName = currentRoll.GetSkillType();
            }
            displayText.text = currentRoll.getOwner().playername + " is attempting a " + displayName + " check";
            OnValueChange();
        }
    }

    private void UpdateSkillDropDown()
    {
        SkillDD.ClearOptions();
        List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
        Dropdown.OptionData baseResponse = new Dropdown.OptionData();
        baseResponse.text = currentRoll.skillKey;
        results.Add(baseResponse);
        foreach(string Key in currentRoll.getOwner().skills.Keys)
        {
            if(!Key.Equals(currentRoll.skillKey))
            {
                Dropdown.OptionData NewData = new Dropdown.OptionData();
                NewData.text = Key;
                results.Add(NewData);
            }
        }
        SkillDD.AddOptions(results);
    }

    private void UpdateAttributeDropDown()
    {
        AttributeDD.ClearOptions();
        List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
        Dropdown.OptionData baseResponse = new Dropdown.OptionData();
        baseResponse.text = currentRoll.attributeKey;
        results.Add(baseResponse);
        foreach(string Key in currentRoll.getOwner().attribues.Keys)
        {
            if(!Key.Equals(currentRoll.attributeKey))
            {
                Dropdown.OptionData NewData = new Dropdown.OptionData();
                NewData.text = Key;
                results.Add(NewData);
            }
        }
        AttributeDD.AddOptions(results);
    }

    private void UpdateLimitDropDown()
    {
        LimitDD.ClearOptions();
        List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
        Dropdown.OptionData baseResponse = new Dropdown.OptionData();
        if(currentRoll.useWeapon)
        {
            baseResponse.text = "Weapon Accuracy";
            LimitDD.interactable = false;
        }
        else
        {
            baseResponse.text = currentRoll.LimitKey;
            results.Add(baseResponse);
            foreach(string Key in Limits)
            {
                if(!Key.Equals(currentRoll.LimitKey))
                {
                    Dropdown.OptionData NewData = new Dropdown.OptionData();
                    NewData.text = Key;
                    results.Add(NewData);
                }
            }
        }
        LimitDD.AddOptions(results);
    }

    public void OnSkillChanged()
    {
        currentRoll.skillKey = SkillDD.captionText.text;
        OnValueChange();
    }

    public void OnAttributeChanged()
    {
        currentRoll.attributeKey = AttributeDD.captionText.text;
        OnValueChange();
    }
    public void OnLimitChanged()
    {
        currentRoll.LimitKey = LimitDD.captionText.text;
        OnValueChange();
    }

    public void OnModifierChanged()
    {
        int value;
        if (int.TryParse(ModifierIF.text, out value))
        {
            currentRoll.modifiers = value;
        }
        else
        {
            currentRoll.modifiers = 0;
        }
        OnValueChange();
    }  

    public void OnThresholdChanged()
    {
        int value;
        if (int.TryParse(ThresholdIF.text, out value))
        {
            currentRoll.threshold = value;
        }
        else
        {
            currentRoll.threshold = 0;
        }
        OnValueChange();
    }  

    public void OnValueChange()
    {
        int pool = currentRoll.GetPool();
        
        CalculationText.text = "";
        if(!string.IsNullOrEmpty(currentRoll.skillKey))
        {
            CalculationText.text += currentRoll.skillKey + " + ";
        }
        if(!string.IsNullOrEmpty(currentRoll.attributeKey))
        {
            CalculationText.text += currentRoll.attributeKey + " ";
        }
        if(currentRoll.useWeapon)
        {
            int limitVal = currentRoll.WeaponAccuracy;
            CalculationText.text += "[" + limitVal + "] ";
        }
        else if(!string.IsNullOrEmpty(currentRoll.LimitKey))
        {
            int limitVal = currentRoll.getOwner().GetAttribute(currentRoll.LimitKey);
            CalculationText.text += "[" + limitVal + "] ";
        }
        if(currentRoll.threshold > 0)
        {
            CalculationText.text += "(" + currentRoll.threshold + ") ";
        }
        CalculationText.text += "TEST";
        CalculationText.text += "\n Dice Pool: " + pool;
    } 
    
    // Called when the display button is pressed, passes the entry in inputText back to CurrentRoll and marks it as complete 
    public void OnButtonPressed()
    {
        currentRoll.Roll();
        currentRoll = null;
        CameraButtons.UIFreeze(false);
        display.SetActive(false);
    }
}

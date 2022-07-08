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
            displayText.text = currentRoll.getOwner().playername + " is attempting a " + currentRoll.displayName() + " check";
            OnValueChange();
        }
    }

    private void UpdateSkillDropDown()
    {
        SkillDD.ClearOptions();
        List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
        foreach(AttributeKey Key in AttribueReference.keys)
        {
            Dropdown.OptionData NewData = new Dropdown.OptionData();
            NewData.text = Key.ToString();
            results.Add(NewData);
        }
        SkillDD.AddOptions(results);
        SkillDD.value = (int)currentRoll.firstField;
    }

    private void UpdateAttributeDropDown()
    {
        AttributeDD.ClearOptions();
        List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
        foreach(AttributeKey Key in AttribueReference.keys)
        {
            Dropdown.OptionData NewData = new Dropdown.OptionData();
            NewData.text = Key.ToString();
            results.Add(NewData);
        }
        AttributeDD.AddOptions(results);
        AttributeDD.value = (int)currentRoll.secondField;
    }

    private void UpdateLimitDropDown()
    {
        
        LimitDD.ClearOptions();
        List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
        if(currentRoll.useWeapon)
        {
            Dropdown.OptionData baseResponse = new Dropdown.OptionData();
            baseResponse.text = "Weapon Accuracy";
            results.Add(baseResponse);
            LimitDD.interactable = false;
        }
        else
        {
            foreach(AttributeKey Key in AttribueReference.keys)
            {
                Dropdown.OptionData NewData = new Dropdown.OptionData();
                NewData.text = Key.ToString();
                results.Add(NewData);
            }
            LimitDD.AddOptions(results);
            LimitDD.value = (int)currentRoll.LimitKey;
            LimitDD.interactable = true;
        }
        LimitDD.AddOptions(results);
    }

    public void OnSkillChanged()
    {
        currentRoll.firstField = (AttributeKey)SkillDD.value;
        OnValueChange();
    }

    public void OnAttributeChanged()
    {
        currentRoll.secondField = (AttributeKey)AttributeDD.value;
        OnValueChange();
    }
    public void OnLimitChanged()
    {
        currentRoll.LimitKey = (AttributeKey)LimitDD.value;
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
        if(currentRoll.firstField != AttributeKey.Empty)
        {
            CalculationText.text += currentRoll.firstField.ToString() + " + ";
        }
        if(currentRoll.secondField != AttributeKey.Empty)
        {
            if(currentRoll.getOwner().isMinion)
            {
                CalculationText.text += "owner's " + currentRoll.secondField.ToString() + " ";
            }
            else 
            {
                CalculationText.text += currentRoll.secondField.ToString() + " ";
            }
        }
        string endText = "no limit";
        if(currentRoll.useWeapon)
        {
            CalculationText.text += "[Accuracy] ";
            endText = "limit: " + currentRoll.WeaponAccuracy;
        }
        else if(currentRoll.LimitKey != AttributeKey.Empty)
        {
            string limitText = currentRoll.LimitKey.ToString(); 
            CalculationText.text += "[" + limitText + "] ";
            endText = "limit: " + currentRoll.getOwner().GetAttribute(currentRoll.LimitKey);
        }
        if(currentRoll.threshold > 0)
        {
            CalculationText.text += "(" + currentRoll.threshold + ") ";
        }
        CalculationText.text += "TEST";
        CalculationText.text += "\n Dice Pool: " + pool;
        CalculationText.text += "\n " + endText;
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

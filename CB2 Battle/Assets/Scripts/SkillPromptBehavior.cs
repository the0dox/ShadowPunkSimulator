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
    // Reference to the input field the player uses to enter the roll
    [SerializeField] private InputField inputText;
    // References to the text explaining what the roll is for
    [SerializeField] private Text displayText;

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
            CameraButtons.UIFreeze(true);
            display.SetActive(true); 
            displayText.text = currentRoll.getOwner().GetName() + " is attempting a " + currentRoll.GetSkillType() + " check\n Target: " + currentRoll.GetTarget() + "\n Input Result:";
            inputText.text = "";
        }
       
    }

    // Called when the display button is pressed, passes the entry in inputText back to CurrentRoll and marks it as complete 
    public void OnButtonPressed()
    {
        int value;
        //if a numerical value cannot be found, substitute a random roll
        if (!int.TryParse(inputText.text, out value))
        {
            value = Random.Range(1,101);
        }
        currentRoll.SetRoll(value);
        currentRoll = null;
        CameraButtons.UIFreeze(false);
        display.SetActive(false);
    }
}

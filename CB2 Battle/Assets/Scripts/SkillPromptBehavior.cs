using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillPromptBehavior : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Text inputText;
    [SerializeField] private Text displayText;
    [SerializeField] private string skill;

    public void SetValue(string skill)
    {
        this.skill = skill;
        displayText.text = "Attempting a " + skill + " check\nAdd Modifier?";
    } 
    public void OnButtonPressed()
    {
        int value;
        if (!int.TryParse(inputText.text, out value))
        {
            value = 0;
        }
        GameObject.FindGameObjectWithTag("GameController").GetComponent<TurnManager>().AbilityCheck(skill,value);
        Destroy(gameObject);
    }
}

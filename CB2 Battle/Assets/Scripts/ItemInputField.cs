using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemInputField : MonoBehaviour
{
    private CharacterSheet mySheet;
    [SerializeField] private Text displayText;
    private string myItem;
    private int myStacks;

    public void UpdateStacks(int stacks)
    {
        myStacks = stacks;
        displayText.text = myItem + " x " + myStacks;
    }

    public void UpdateIn(string input, int stacks, CharacterSheet mySheet)
    {
        this.mySheet = mySheet;
        myItem = input;
        UpdateStacks(stacks);
    }

    public void ReduceItem()
    {
        myStacks--;
        UpdateStacks(myStacks);
        if(myStacks < 1)
        {
            mySheet.Remove(myItem);
            Destroy(gameObject);
        }
    }
}

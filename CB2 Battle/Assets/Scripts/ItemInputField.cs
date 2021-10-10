using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemInputField : MonoBehaviour
{
    private CharacterSheet mySheet;
    [SerializeField] private Text displayText;
    private Item myItem;

    void Update()
    {
        if(myItem != null)
        {
            displayText.text = myItem.GetStacks() + "x " + myItem.GetName();
        }
    }
    public void UpdateIn(Item input, CharacterSheet mySheet)
    {
        this.mySheet = mySheet;
        myItem = input;
    }

    public void ReduceItem()
    {
        myItem.SubtractStack();
        if(myItem.IsConsumed() || !myItem.Stackable())
        {
            mySheet.Remove(myItem);
            Destroy(gameObject);
        }
    }

    public Item UpdateOut()
    {
        return myItem;
    }
}

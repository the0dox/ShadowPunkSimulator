using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemInputField : MonoBehaviour
{
    [SerializeField] private Text displayText;
    private Item myItem;

    void Update()
    {
        if(myItem != null)
        {
            displayText.text = myItem.GetStacks() + "x " + myItem.GetName();
        }
    }
    public void UpdateIn(Item input)
    {
        myItem = input;
    }

    public Item UpdateOut()
    {
        return myItem;
    }
}

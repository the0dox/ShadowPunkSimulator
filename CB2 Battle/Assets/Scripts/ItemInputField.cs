using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemInputField : MonoBehaviour
{
    [SerializeField] private Text displayText;
    [SerializeField] private TooltipTrigger tooltipContent;
    [SerializeField] private GameObject popUpMenu;
    [SerializeField] private Image displayImage;
    [SerializeField] private Sprite EmptyImage;
    [SerializeField] private ItemAdder ItemAdder;
    private Item myData;

    public void SetItem(Item myData)
    {
        if(myData != null)
        {
            this.myData = myData;
            displayImage.sprite = myData.GetSprite();
            tooltipContent.enabled = true;
            if(!myData.Stackable())
            {
                displayText.text = "";
            }
        }
        else
        {
            Clear();
        }
    }

    void Update()
    {
        if(myData != null)
        {
            displayText.text = "x" + myData.GetStacks();
            tooltipContent.content = myData.getTooltip();
            tooltipContent.header = myData.GetName();
        }
    }

    public Item GetItem()
    {
        return myData;
    }

    public void OnButtonPressed()
    {
        ItemAdder.OnItemClicked(this);
    }

    public void Clear()
    {
        myData = null;
        displayText.text = "";
        displayImage.sprite = EmptyImage;  
        tooltipContent.enabled = false;
    }
}

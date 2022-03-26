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
    private CharacterSaveData owner;
    private Item myData;

    public void DownloadCharacter(CharacterSaveData owner, Item myData)
    {
        this.owner = owner;
        if(myData != null)
        {
            this.myData = myData;
            displayImage.sprite = myData.GetSprite();
            if(!myData.Stackable())
            {
                displayText.text = "";
            }
        }
        else
        {
            this.myData = null;
            displayText.text = "";
            displayImage.sprite = EmptyImage;  
            TooltipSystem.hide();
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

    public void Reduce()
    {
        owner.ReduceItemInventory(myData);
    }

    public Item GetItem()
    {
        return myData;
    }

    public void OnButtonPressed()
    {
        ItemAdder.OnItemClicked(this);
    }
}

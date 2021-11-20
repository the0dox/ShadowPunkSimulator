using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemInputField : MonoBehaviour
{
    [SerializeField] private Text displayText;
    [SerializeField] private TooltipTrigger tooltipContent;
    private CharacterSaveData owner;
    private Item myData;
    
    public void DownloadCharacter(CharacterSaveData owner, Item myData)
    {
        this.owner = owner;
        this.myData = myData;
    }

    void Update()
    {
        displayText.text = myData.GetName() + " x " + myData.GetStacks();
        tooltipContent.content = myData.getTooltip();
        tooltipContent.header = myData.GetName();
    }

    public void Reduce()
    {
        owner.RemoveItem(myData);
        if(!owner.equipmentObjects.Contains(myData))
        {
            Destroy(gameObject);
        }
    }

    public void displayTooltip()
    {
        Debug.Log(myData.getTooltip());
    }
}

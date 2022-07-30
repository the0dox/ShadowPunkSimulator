using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TalentInputField : MonoBehaviour
{
    [SerializeField] private TooltipTrigger tooltipContent;
    [SerializeField] private GameObject popUpMenu;
    [SerializeField] private Image displayImage;
    [SerializeField] private Sprite EmptyImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject CrossOut;
    [SerializeField] private CharacterSheet Mysheet;
    private CharacterSaveData owner;
    private Talent myData;

    public void DownloadCharacter(CharacterSaveData owner, TalentKey key)
    {
        this.owner = owner;
        this.myData = TalentReference.GetTalent(key);
        displayImage.sprite = myData.Icon;
        tooltipContent.header = myData.name;
        tooltipContent.content = myData.getDescription(owner);
    }

    public void OnButtonPressed()
    {
        Mysheet.UpdateTalent(myData.key);
    }

    public void UpdateDisplay()
    {
        if(owner != null)
        {    
            tooltipContent.content = myData.getDescription(owner);
            if(owner.hasTalent(myData.key))
            {
                backgroundImage.color = Color.red;
            }
            else
            {
                backgroundImage.color = Color.white;
            }
            if(CrossOut != null)
            {
                if(!myData.CanSelect(owner))
                {
                    CrossOut.SetActive(true);
                }
                else
                {
                    CrossOut.SetActive(false);
                }
            }
            
        }
    }
}

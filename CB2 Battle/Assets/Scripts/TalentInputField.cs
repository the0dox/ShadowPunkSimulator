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
    private CharacterSaveData owner;
    private Talent myData;

    public void DownloadCharacter(CharacterSaveData owner, TalentKey key)
    {
        this.owner = owner;
        this.myData = TalentReference.GetTalent(key);
        displayImage.sprite = myData.Icon;
        tooltipContent.header = myData.name;
        tooltipContent.content = myData.getDescription(owner);
        ToggleSelected(owner.hasTalent(key));
    }

    public void OnButtonPressed()
    {
        Debug.Log("pressed");
        // button pressed when owner already has the talent, then remove it
        if(owner.hasTalent(myData.key))
        {
            Debug.Log("adding " + name + "talent");
            owner.SetTalent(myData.key,false);
            ToggleSelected(false);
        }
        else if(myData.CanSelect(owner))
        {
            Debug.Log("removing " + name + "talent");
            owner.SetTalent(myData.key,true);
            ToggleSelected(true);
        }
        TalentAdder.OnValueChanged();
    }

    public void ToggleSelected(bool active)
    {
        if(active)
        {
            backgroundImage.color = Color.red;
        }
        else
        {
            backgroundImage.color = Color.white;
        }
    }

    public void UpdateDisplay()
    {
        if(owner != null)
        {
            tooltipContent.content = myData.getDescription(owner);
            if(!myData.CanSelect(owner))
            {
                CrossOut.SetActive(true);
                ToggleSelected(false);
                owner.SetTalent(myData.key, false);
            }
            else
            {
                CrossOut.SetActive(false);
            }
        }
    }
}

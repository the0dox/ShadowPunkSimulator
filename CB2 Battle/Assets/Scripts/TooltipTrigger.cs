using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// A component that can be added to any object that you want to have a tooltip associated with
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // content that is sent to tooltip system
    public string content;
    // header that is sent to tooltip system
    public string header;
    // The time (in seconds) it takes to popup
    float popupDelay = 0.75f;
    // used to cancel the popup if the user scrolls away
    bool selected = false;
    // When hovering over the gameobject with this component, starts a delayed popup 
    public void OnPointerEnter(PointerEventData eventData)
    {
        selected = true;
        StartCoroutine(ClickDelay());
    }
    // When hovering away from the gameobject with this component, hides the popup/stops the delay
    public void OnPointerExit(PointerEventData eventData)
    {
        selected = false;
        StopCoroutine(ClickDelay());
        HideTooltip();
    }
    public void ShowTooltip()
    {
        TooltipSystem.show(content,header);
    }
    public void HideTooltip()
    {
        TooltipSystem.hide();
    }

    // Sends the message to tooltip system with header content after popupDelay seconds
    IEnumerator ClickDelay()
    {
        yield return new WaitForSeconds(popupDelay);
        if(selected)
        {
            ShowTooltip();
        }
    }
}

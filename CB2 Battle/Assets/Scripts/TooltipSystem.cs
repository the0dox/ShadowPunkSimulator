using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Static reference for that all scripts to access the tooltip object 
public class TooltipSystem : MonoBehaviour
{
    // Static reference to monobehavior
    private static TooltipSystem current;
    // monobehavior reference to the singular tooltip object
    [SerializeField] private UITooltipBehavior tooltip;
    // set up static reference
    void Start()
    {  
        current = this;
        hide();
    }
    // Given content and header, sets enables the tooltip object after a short delay
    public static void show(string content, string header = "")
    {
        current.tooltip.SetText(content,header);
        current.tooltip.gameObject.SetActive(true);
    }
    // Turns off the tooltip
    public static void hide()
    {
        current.tooltip.gameObject.SetActive(false);
    }

    public static bool active()
    {
        return current.tooltip.gameObject.activeInHierarchy;
    }
}

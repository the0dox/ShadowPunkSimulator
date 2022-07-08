using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TooltipComponent : MonoBehaviour
{
    private string myTooltip;

    public void SetTooltip(string tip)
    {
        myTooltip = tip;
    }

    public string getTooltip()
    {
        return myTooltip;
    }
}

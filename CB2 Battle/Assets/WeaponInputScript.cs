using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponInputScript : MonoBehaviour
{
    public Text Name;
    public Text Class;
    public Text Range;
    public Text Damage;
    public Text ROF;
    public Text Pen;
    public Text Clip;
    public Text Reload;
    public Text Type;
    public Text Rules;
    

    // Update Values from players weapon
    public void UpdateIn(Weapon input)
    {
        Name.text = input.GetName();
        Class.text = "Basic";
        if(input.HasWeaponAttribute("Thrown"))
        {
            Range.text = "SB x 3";
        }
        else if (input.HasWeaponAttribute("Melee"))
        {
            Range.text = "";
            ROF.text = "";
            Clip.text = "";
            Reload.text = "";
            Rules.gameObject.transform.localPosition += new Vector3(0, 17.5f, 0);
        }
        else
        {
            Range.text = "" + input.getRange();
            ROF.text = input.ROFtoString();
            Pen.text = "" + input.GetAP();
            Clip.text = "" + input.getClip();
            Reload.text = "" + input.reloadToString();
        }
        Damage.text = input.DisplayDamageRange();

        Type.text = input.GetDamageType();
        Rules.text = input.AttributesToString();
    }


}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponInputScript : MonoBehaviour
{
    private Weapon myWeapon;
    [SerializeField]private Text Name;
    [SerializeField]private Text Class;
    [SerializeField]private Text Range;
    [SerializeField]private Text Damage;
    [SerializeField]private Text ROF;
    [SerializeField]private Text Pen;
    [SerializeField]private Text Clip;
    [SerializeField]private Text Reload;
    [SerializeField]private Text Type;
    [SerializeField]private Text Rules;
    

    // Update Values from players weapon
    public void UpdateIn(Weapon input)
    {
        myWeapon = input;
        Name.text = input.GetName();
        Class.text = input.GetClass();
        if(input.IsWeaponClass("Thrown"))
        {
            Range.text = "SB x 3";
        }
        else if (input.IsWeaponClass("Melee"))
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
            Reload.text = "" + input.ReloadString();
        }
        Damage.text = input.DisplayDamageRange();

        Type.text = input.GetDamageType();
        Rules.text = input.AttributesToString();
    }

    public Weapon GetItem()
    {
        return myWeapon;
    }

}

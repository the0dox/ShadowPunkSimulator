using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Armor : Item
{
    private Dictionary<string, int> ArmorValues = new Dictionary<string, int>();
    private List<string> Attribues;

    public Armor(ArmorTemplate template) 
    {
        this.name = template.name;
        this.weight = template.weight;
        this.cost = template.cost;
        this.unique = template.unique;
        this.stacks = 1;
        this.availablity = template.availablity;
        this.description = template.description;
        this.Attribues = template.Attributes; 
        for(int i = 0; i < template.Parts.Length; i++)
        {
            ArmorValues.Add(template.Parts[i], template.AP);
        }
    }
    //when the damage source is known modify AP value;
    public int GetAP(string HitLocation, Weapon w)
    {
        if(!ArmorValues.ContainsKey(HitLocation))
        {
            Debug.Log(GetName() + " doesn't cover " + HitLocation);
            return 0;
        }
        int output = ArmorValues[HitLocation];
        //non primitive damage source against primitive armor
        if(w != null && !w.HasWeaponAttribute("Primitive") && Attribues.Contains("Primitive"))
        {
            output /= 2;
        }
        //primitve damage source against non primitive armor
        else if (w != null && w.HasWeaponAttribute("Primitive") && !Attribues.Contains("Primitive"))
        {
            output *= 2;
        }
        return output;
    }
}

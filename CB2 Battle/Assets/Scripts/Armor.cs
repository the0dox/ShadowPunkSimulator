using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Armor is a subtype of Item that reduces incoming damage on wearers
public class Armor : Item
{
    // Keys correspond to hit locations like head, legs etc. 
    private List<string> ArmorLocation;
    // Each location has the same protective value
    private int AP;
    // Attributes are any special rules armor can have
    private List<string> Attribues;
    // Template is a scriptable object
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
        this.ArmorLocation = new List<string>();
        for(int i = 0; i < template.Parts.Length; i++)
        {
            ArmorLocation.Add(template.Parts[i]);
        }
        this.AP = template.AP;
    }
    // HitLocation: key that corresponds to body part hit
    // w: Weapon that dealt the damage 
    // returns the damage reduction from that hitlocation
    public int GetAP(string HitLocation, Weapon w)
    {
        // If the armor doesn't cover this area, no damage is reduced
        if(!ArmorLocation.Contains(HitLocation))
        {
            return 0;
        }
        int output = AP;
        // Flak armor is more effective against explosions
        if(w != null && w.HasWeaponAttribute("Blast") && Attribues.Contains("Flak"))
        {
            AP = 5;
        }
        // advanced armor is twice as effective against primitive damage sources
        if(w != null && !w.HasWeaponAttribute("Primitive") && Attribues.Contains("Primitive"))
        {
            output /= 2;
        }
        // primitive armor is half as effective against advanced damage sources
        else if (w != null && w.HasWeaponAttribute("Primitive") && !Attribues.Contains("Primitive"))
        {
            output *= 2;
        }
        return output;
    }
}

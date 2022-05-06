using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Armor is a subtype of Item that reduces incoming damage on wearers
public class Armor : Item
{
    // protective Armor value
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
        this.AP = template.AP;
        this.Template = template;
        
        upgrades = new Dictionary<ItemTemplate, bool>();
        foreach(ItemTemplate ug in template.upgrades)
        {
            this.upgrades.Add(ug, false);
        }
        UpdateTooltip();
    }

    public int GetAP()
    {
        return AP;
    }

    public override Sprite GetSprite()
    {
        if(Template.icon != null)
        {
            return Template.icon;
        }
        return Resources.Load<Sprite>("Assets/Resources/Materials/Icons/Items/Vest.png");
    
    }

    public override void UpdateTooltip()
    {
        tooltip = "Rating " + rating + " Armor ";
        tooltip += "\nArmor Points: " + AP;
        tooltip += "\nupgrades:";
        string upgradedesc = " ";
        foreach(ItemTemplate ug in upgrades.Keys)
        {
            if(upgrades[ug])
            {
                upgradedesc += ug.name + ",";
            } 
            upgradedesc = upgradedesc.TrimEnd(upgradedesc[upgradedesc.Length - 1]);
        }
        tooltip += upgradedesc;
        tooltip += "\nweight: " + weight;
        tooltip += "\nvalue: " + cost;
        tooltip += "\n\n\"" + description + "\"";
    }
}

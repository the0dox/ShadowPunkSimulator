using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item
{
    public ItemTemplate Template;
    protected private string name;
    protected private float weight;
    protected private float cost;
    protected private string availablity;
    protected private string description;
    protected private int stacks;
    protected private bool unique;
    protected private string tooltip;
    protected private int rating;
    protected private Dictionary<ItemTemplate, bool> upgrades; 
    public Item(ItemTemplate template)
    {
        Template = template;
        this.name = template.name;
        this.weight = template.weight;
        this.cost = template.cost;
        this.availablity = template.availablity;
        this.description = template.description;
        this.rating = template.rating;
        this.stacks = 1;
        this.unique = template.unique;
        upgrades = new Dictionary<ItemTemplate, bool>();
        foreach(ItemTemplate ug in template.upgrades)
        {
            this.upgrades.Add(ug, false);
        }
        UpdateTooltip();
    }

    public void AddStack()
    {
        stacks++;
        UpdateTooltip();
    }
    public void SubtractStack()
    {
        stacks--;
        UpdateTooltip();
    }
    public void SubtractStack(int value)
    {
        stacks -= value;
        UpdateTooltip();
    }
    public void AddStack(int value)
    {
        stacks += value;
        UpdateTooltip();
    }
    public void SetStack(int amount)
    {
        stacks = amount;
        UpdateTooltip();
    }
    public int GetStacks()
    {
        if(unique)
        {
            return 1;
        }
        else
        {
            return stacks;
        }
    }
    public bool IsConsumed()
    {
        if(stacks < 1)
        {
            Debug.Log("im fully consumed");
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool Stackable()
    {
        return !unique;
    }
    public Item()
    {
    }
    public string GetName()
    {
        return name;
    }

    public float getWeight()
    {
        return weight;
    }

    public string CompileUpgrades()
    {
        if(upgrades.Count == 0)
        {
            return "";
        }
        string output = "";
        foreach(ItemTemplate ug in upgrades.Keys)
        {
            if(upgrades[ug])
            {
                output += "1,";
            }
            else
            {
                output += "0,";
            }
        }
        output = output.TrimEnd(output[output.Length - 1]);
        Debug.Log(name + " saving upgrade keys " + output);
        return output;
    }

    public void SetUpgrade(int position, bool value)
    {
        int index = 0;
        foreach(ItemTemplate ug in upgrades.Keys)
        {
            if(index == position)
            {
                upgrades[ug] = value;
                //Debug.Log(name + " setting upgrade: " + ug.name + " = " + value);
                break;
            }
            index++;
        }
        UpdateTooltip();
    }

    public void SetUpgrade(string key, bool value)
    {
        foreach(ItemTemplate ug in upgrades.Keys)
        {
            if(ug.name.Equals(key))
            {
                upgrades[ug] = value;
                break;
            }
        }
        UpdateTooltip();
    }

    public bool hasUpgrade(string key)
    {
        ItemTemplate template = ItemReference.GetTemplate(key);
        if(!upgrades.ContainsKey(template))
        {
            return false;
        }
        return upgrades[template];
    }

    public virtual void UpdateTooltip()
    {
        tooltip = "Rating " + rating + " item";
        tooltip += "\n\n \"" + description + "\"";
        tooltip += "\n weight: " + weight;
        tooltip += "\n value: " + cost;
        tooltip += "\n rating: " + rating;
        tooltip += "\n upgrades:";
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
    }

    public string getTooltip()
    {
        return tooltip;
    }

    public bool SameTemplate(Item other)
    {
        return(this.Template == other.Template);
    }
}


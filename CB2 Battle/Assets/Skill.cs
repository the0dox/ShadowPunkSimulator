using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skill
{
    //indicates where skill should be displayed/affects other properties
    public bool basic;
    //indicates what kind of skill this is, a player cannot have more than one of any skill
    public string name;
    //level of training a player has in this skill
    public int levels;
    //indicates which characterisitc is referenced when this skill is attempted
    public string characterisitc;
    public bool visible;
    private List<string> Descriptor;
    private string displayText;

    public Skill(string name, int levels, string characterisitc, bool basic)
    {
        this.name = name;
        this.levels = levels;
        this.characterisitc = characterisitc;
        this.basic = basic;
        if(name.Equals("Parry"))
        {
            visible = false;
        }
        else
        {
            visible = true;
        }
    }

    public Skill(SkillTemplate template, int levels)
    {
        this.name = template.name;
        this.levels = levels;
        this.characterisitc = template.characterisitc;
        this.basic = template.basic;
        this.visible = template.visible;
        this.Descriptor = template.Descriptor;
        this.displayText = template.displayText;
    }

    //returns true and updates value with other skill if they are the same, else returns false;
    public bool compareSkill(Skill other)
    {
        if(this.name.Equals(other.name))
        {
            this.levels = other.levels;
            return true;
        }
        return false;
    }
}

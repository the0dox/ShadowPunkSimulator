using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSaveData : ScriptableObject
{
    public Dictionary<string, int> Stats = new Dictionary<string, int>
    {
        { "BS", 30 },
        { "WS", 30 },
        { "S", 30 },
        { "T", 30 },
        { "A", 30 },
        { "INT", 30 },
        { "PER", 30 },
        { "WP", 30 },
        { "FEL", 30 },
        { "Wounds", 10},
        { "MaxWounds", 10},
        { "MoveHalf", 0 },
        { "MoveFull", 0}, 
        { "MoveCharge", 0},
        { "MoveRun", 0},
        { "Head",0},
        { "Right Arm", 0},
        { "Left Arm", 0},
        { "Body", 0},
        { "Right Leg", 0},
        { "Left Leg", 0},
        { "Fatigue", 0},
        { "Critical", 0},
        { "Fate", 0},
        { "FateMax", 0}
    };
    private List<Skill> Skills;
    public List<WeaponTemplate> WeaponInitalizer = new List<WeaponTemplate>();
    Dictionary<int, string> HitLocations;
    public void Init(GameObject sheet)
    {
        BasicSkills();
        StandardHitLocations();
    }
    
    private void BasicSkills()
    {
        Skills.Add(new Skill("Parry",0,"WS",true));
        Skills.Add(new Skill("Awareness",0,"PER",true));
        Skills.Add(new Skill("Barter",0,"FEL",true));
        Skills.Add(new Skill("Carouse",0,"T",true));
        Skills.Add(new Skill("Charm",0,"FEL",true));
        Skills.Add(new Skill("Concealment",0,"A",true));
        Skills.Add(new Skill("Contortionist",0,"A",true));
        Skills.Add(new Skill("Deceive",0,"FEL",true));
        Skills.Add(new Skill("Disguise",0,"FEL",true));
        Skills.Add(new Skill("Dodge",0,"A",true));
        Skills.Add(new Skill("Evaluate",0,"INT",true));
        Skills.Add(new Skill("Gamble",0,"INT",true));
        Skills.Add(new Skill("Inquiry",0,"FEL",true));
        Skills.Add(new Skill("Intimidate",0,"S",true));
        Skills.Add(new Skill("Logic",0,"INT",true));
        Skills.Add(new Skill("Climb",0,"S",true));
        Skills.Add(new Skill("Scrutiny",0,"PER",true));
        Skills.Add(new Skill("Search",0,"PER",true));
        Skills.Add(new Skill("SilentMove",0,"A",true));
        Skills.Add(new Skill("Swim",0,"S",true));
    }
    private void StandardHitLocations()
    {
        HitLocations = new Dictionary<int, string>();
        HitLocations.Add(10, "Head");
        HitLocations.Add(20, "Right Arm");
        HitLocations.Add(30, "Left Arm");
        HitLocations.Add(70, "Body");
        HitLocations.Add(85, "Right Leg");
        HitLocations.Add(100, "Left Leg");
    }
}

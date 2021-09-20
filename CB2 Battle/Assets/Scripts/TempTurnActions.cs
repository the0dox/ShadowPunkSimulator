/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    //for determine who can or cannot be targeted in combat
    public int team; 
    
    //display name
    public string playername; 
    private string repeatingAction;
    private List<string> CompletedActions = new List<string>(); 
    public PlayerStats grappler;
    public PlayerStats grappleTarget;
    public GameObject model;
    public Material SelectedColor;
    private int AdvanceBonus;
    //Reference for all player stats
    public Dictionary<string, int> Stats = new Dictionary<string, int>
    {
        { "BS", 50 },
        { "WS", 50 },
        { "S", 40 },
        { "T", 30 },
        { "A", 30 },
        { "INT", 20 },
        { "PER", 50 },
        { "WP", 50 },
        { "FEL", 30 },
        { "Wounds", 10},
        { "MaxWounds", 10},
        { "MoveHalf", 0 },
        { "MoveFull", 0}, 
        { "MoveCharge", 0},
        { "MoveRun", 0},
        { "Head",0},
        { "Right Arm", 2},
        { "Left Arm", 2},
        { "Body", 3},
        { "Right Leg", 2},
        { "Left Leg", 2},
        { "Fatigue", 0}
    };
    public Dictionary<ConditionTemplate, int> Conditions = new Dictionary<ConditionTemplate, int>();


    //level of skill training a character has, is called first
    public Dictionary<string, int> Skills = new Dictionary<string, int>{
        {"Parry", 1},
        {"Awareness", 1},
        {"Barter", 1},
        {"Carouse", 1},
        {"Charm", 1},
        {"Concealment", 1},
        {"Contortionist", 1},
        {"Deceive", 1},
        {"Disguise", 1},
        {"Dodge", 1},
        {"Evaluate", 1},
        {"Gamble", 1},
        {"Inquiry", 3},
        {"Intimidate", 1},
        {"Logic", 1},
        {"Climb", 1},
        {"Scrutiny", 1},
        {"Search", 1},
        {"SilentMove", 1},
        {"Swim", 1},
    };

    //ability score each skill is assigned to, called second
    public Dictionary<string, string> SkillsToStats = new Dictionary<string, string>{
        {"Parry", "WS"},
        {"Awareness", "PER"},
        {"Barter", "FEL"},
        {"Carouse", "T"},
        {"Charm", "FEL"},
        {"Concealment", "A"},
        {"Contortionist", "A"},
        {"Deceive", "FEL"},
        {"Disguise", "FEL"},
        {"Dodge", "A"},
        {"Evaluate", "INT"},
        {"Gamble", "INT"},
        {"Inquiry", "FEL"},
        {"Intimidate", "S"},
        {"Logic", "INT"},
        {"Climb", "S"},
        {"Scrutiny", "PER"},
        {"Search", "PER"},
        {"SilentMove", "A"},
        {"Swim", "S"},
    };

    public List<Weapon> equipment = new List<Weapon>();

    public List<WeaponTemplate> WeaponInitalizer = new List<WeaponTemplate>();
    public Weapon SecondaryWeapon;
    public Weapon PrimaryWeapon; 

    //quick reference for what die rolls correspond to a hit location
    private Dictionary<int, string> HitLocations;

    // Start is called before the first frame update
    void Start()
    {
        StandardHitLocations();
        UpdateMovement(); 
        foreach(WeaponTemplate wt in WeaponInitalizer)
        {
            Weapon w = new Weapon(wt);
            equipment.Add(w);
            
            if(SecondaryWeapon == null || PrimaryWeapon == null)
            {
                Equip(w);
            }
        }
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

    public int getWounds()
    {
        return Stats["Wounds"];
    }

    public int GetTeam()
    {
        return team; 
    }

    public string GetName()
    {
        return playername;
    }

    /*  damage: how much incoming damage already altered 
        location: reveresed die roll for hit location
        takes unaltered damage and applies any damage reduction and subtracts from health
    public void takDamage(int damage, string location)
    {
        //Debug.Log(playername + "'s " + location + " armor reduces the incoming damage to " + damage);
        if (damage > 0) {
            Stats["Wounds"] -= damage;
        }
    }

    public void takeFatigue(int levels)
    {
        if (levels > 0)
        {
            CombatLog.Log(name + " takes " + levels + " levels of fatigue");
            Stats["Fatigue"] += levels;
        }
        if(Stats["Fatigue"] > GetStatScore("T"))
        {
            CombatLog.Log(name + " takes more fatigue than their Toughness bonus and is knocked out!");
            SetCondition("Unconscious",0,true);
        }
    }

    public Dictionary<int, string> GetHitLocations()
    {
        return HitLocations; 
    } 

    public int GetStat(string key)
    {
        if(!Stats.ContainsKey(key))
        {
            Debug.Log("Error: " + key + " does not exist!");
            return 0; 
        }
        return Stats[key]; 
    }
    public int GetStatScore(string key)
    {
        if(!Stats.ContainsKey(key))
        {
            Debug.Log("Error: " + key + " does not exist!");
            return 0; 
        }
        return Stats[key]/10; 
    }

    public void SetStat(string key, int value)
    {
        Stats[key] = value; 
        UpdateMovement();
    }

    public void SetSkill(string key, int value, string characteristic)
    {
        if(!Skills.ContainsKey(key))
        {   
            Debug.Log("adding" + key + "to skills, value" + value);
            Skills.Add(key,value);
            SkillsToStats.Add(key, characteristic);
        }
        else
        {
            Skills[key] = value; 
        }
    }

    //simple equip reference, returns true if possible
    public bool Equip(Weapon w)
    {
        if(SecondaryWeapon != null && SecondaryWeapon.IsWeaponClass("Basic"))
        {
            SecondaryWeapon = null;
            PrimaryWeapon = null; 
        } 
        //replacement required for a two handed weapon
        if (w.IsWeaponClass("Basic"))
        {
            SecondaryWeapon = w;
            PrimaryWeapon = w; 
            //Debug.Log("Equiped " + w.GetName() + "!");
            return true;
        }
        else if ( PrimaryWeapon == null)
        {
            PrimaryWeapon = w;
            //Debug.Log("Equiped " + w.GetName() + " in the right hand!");
            return true;
        }
        else if ( SecondaryWeapon == null)
        {
            SecondaryWeapon = w;
            //Debug.Log("Equiped " + w.GetName() + " in the left hand!");
            return true;
        }
        else
        {
            //Debug.Log("Can't equip a weapon! needs to free hands!");
            return false;
        }
    }

    public void Equip(Weapon w, string location)
    {
        // double replacement required for a two handed weapon
        if (w.IsWeaponClass("Basic"))
        {
            CombatLog.Log("put away all weapons and equiped " + w);
            SecondaryWeapon = null;
            PrimaryWeapon = null;

            Equip(w);
        } 
        // if holding a two handed weapon, it must be put away with both hands, regardless of the number required for new weapon
        else if (!isDualWielding())
        {
            CombatLog.Log("put away " + SecondaryWeapon.GetName());
            SecondaryWeapon = null;
            PrimaryWeapon = null;
        }
        if(location.Equals("Left"))
        {
            SecondaryWeapon = w;
        }
        else if(location.Equals("Right"))
        {
            PrimaryWeapon = w;
        }
        else
        {
            CombatLog.Log("Invalid location entered!");
        }
    }

    public bool isDualWielding()
    {
        return ( SecondaryWeapon != null && PrimaryWeapon != null &&  SecondaryWeapon != PrimaryWeapon);
    }
    public bool isHoldingDualWeapon()
    {
        return ( SecondaryWeapon != null && PrimaryWeapon == SecondaryWeapon);
    }

    //attempts a skill OR Characteristic! from the skill dictionary, applying any modifiers if necessary, returns degrees of successes, not true/false
    public RollResult AbilityCheck(string input, int modifiers)
    {
        int SkillTarget;
        string type;
        int ConditionalModifiers = 0;
        Debug.Log("attempting " + input);
        //for skills not characteristics
        if (SkillsToStats.ContainsKey(input))
        {
            //modifier for skills
            ConditionalModifiers += CalculateStatModifiers(input);
            type = SkillsToStats[input];
            int LevelsTrained = Skills[input];
            SkillTarget = GetStat(type);
            if (LevelsTrained < 1)
                {
                    SkillTarget = SkillTarget/2;
                }
                else
                {
                    SkillTarget += 10 * (LevelsTrained-1);
                } 
        }
        //for characteristic tests only
        else
        {
            type = input; 
            SkillTarget = GetStat(type);
        }
        //modifier that always applies 
        ConditionalModifiers += CalculateStatModifiers(type);
        int Roll = Random.Range(0,100);
        //untrained makes the skill half as likely to work

        SkillTarget += modifiers += ConditionalModifiers;   
        int DOF = (SkillTarget - Roll)/10;
        CombatLog.Log(input + ": rolled a " + Roll + " against a " + SkillTarget + " for " + DOF + "Degrees of success");
        RollResult output = new RollResult(DOF, Roll, SkillTarget, type);
        PopUpText.CreateText(output.Print(),output.GetColor(),gameObject);
        return output;
    }

    //returns DOF, positive if I win, negative if I lose, 0 if stalemate
    public int OpposedAbilityCheck(string input, PlayerStats target, int myModifier, int targetModifier)
    {
        RollResult myResult = AbilityCheck(input,myModifier);
        RollResult targetResult = target.AbilityCheck(input,targetModifier);
        if(!myResult.Passed() && !targetResult.Passed())
        {
            Debug.Log("No one wins");
            return 0;
        }
        if(myResult.Passed() && !targetResult.Passed())
        {
            Debug.Log("I win");
            return 1;
        }
        else if(!myResult.Passed() && targetResult.Passed())
        {
            Debug.Log("They win");
            return -targetResult.GetDOF();
        }
        else{
            int OpposedDOF = myResult.GetDOF() - targetResult.GetDOF();
            if(OpposedDOF != 0)
            {
                Debug.Log("ODOF Result:" + OpposedDOF);
                return OpposedDOF;
            }
            else{
                if(myResult.GetRoll() < targetResult.GetRoll())
                {
                    Debug.Log("Tie breaker goes to me");
                    return 1;
                }
                else if(myResult.GetRoll() > targetResult.GetRoll())
                {
                    Debug.Log("Tie breaker goes to target");
                    return -1;
                }
                else
                {
                    Debug.Log("No one wins");
                    return 0;
                }
            }
        }
    }
    
    public float RollInitaitve()
    {
        return GetStat("A")/10f + Random.Range(1,10);
    }

    public string HealthToString()
    {
        return GetStat("Wounds") + "/" + GetStat("MaxWounds");
    }
    public bool ValidAction(string actionName)
    {
      return (!CompletedActions.Contains(actionName));
    }
    public void SpendAction(string actionName)
    {
      CompletedActions.Add(actionName);
    }

    public void ResetActions()
    {
        CompletedActions.Clear();
    }

    public bool CanParry()
    {
        return (SecondaryWeapon != null && SecondaryWeapon.IsWeaponClass("Melee")) || (PrimaryWeapon != null && PrimaryWeapon.IsWeaponClass("Melee")); 
    }

    //enforces rules on movement, must be called whenever A is changed
    public void UpdateMovement()
    {
        int agilityScore = GetStatScore("A");
        Stats["MoveHalf"] = agilityScore;
        Stats["MoveFull"] = agilityScore * 2;
        Stats["MoveCharge"] = agilityScore * 3;
        Stats["MoveRun"] = agilityScore * 6;
    }
    //sets string as a reference for tactics action to repeat untill ClearRepeatingAction() is called
    public void SetRepeatingAction(string input)
    {
        repeatingAction = input;
    }

    public void CompleteRepeatingAction()
    {
        repeatingAction = null;
    }

    public string GetRepeatingAction()
    {
        return repeatingAction;
    }

    public void SetCondition(string name, int duration, bool visible)
    {
        ConditionTemplate c = ConditionsReference.Condition(name);
        if (visible)
        {
            PopUpText.CreateText(c.name + "!", Color.yellow, gameObject);
        }
        //if the condition already exists, set its duration to the new duration
        if(hasCondition(c.name))
        {
            Conditions[c] = duration;
        }
        //else add it like normal
        else
        {
            Debug.Log("Added" + c.name + "Condition!");
            Conditions.Add(c,duration);
        }
    }
    public bool hasCondition(string Name)
    {
        ConditionTemplate c = ConditionsReference.Condition(Name);
        return Conditions.ContainsKey(c);
    }
    public void RemoveCondition(string Name)
    {
        ConditionTemplate c = ConditionsReference.Condition(Name);
        Conditions.Remove(c);
    }

    public int CalculateStatModifiers(string ability)
    {
        int total = 0;
        foreach(ConditionTemplate key in Conditions.Keys)
        {
            total += key.GetModifier(ability);
        }
        return total;
    }

    public Stack<string> DisplayStatModifiers(string ability)
    {
        Stack<string> outputStack = new Stack<string>();
        foreach(ConditionTemplate key in Conditions.Keys)
        {
            int value = key.GetModifier(ability);
            if(value != 0)
            {
                string header = " ";
                if(value > 0)
                {
                    header = " +";
                }
                outputStack.Push(header + value + "%: " + key.name);
            }
        }
        return outputStack;
    }

    //calls at the beginning And end of turn each interger value refers to beginning and ending of turns 
    public void UpdateConditions(bool startTurn)
    {
        List<ConditionTemplate> removedKeys = new List<ConditionTemplate>();
        List<ConditionTemplate> IncrementKeys = new List<ConditionTemplate>();
        foreach (ConditionTemplate Key in Conditions.Keys)
        {
            if(startTurn && Key.clearOnStart)
            {
                removedKeys.Add(Key);
            }
            //Conditional update on turn starts here
            else if(Key.isCondition("Pinned") && !startTurn)
            {
                int modifiers = 0;
                if(!hasCondition("Under Fire"))
                {  
                    modifiers += 30;
                }
                else
                {
                    removedKeys.Add(ConditionsReference.Condition("Under Fire"));
                }
                if(AbilityCheck("WP",modifiers).Passed())
                {
                    PopUpText.CreateText("Unpinned!",Color.green, gameObject);
                    //removes at the end
                    removedKeys.Add(Key);
                }
            }
            //decay conditions at the end of turns, conditions with 0 as their length do not decay
            if(Conditions[Key] > 1 && !startTurn)
            {
                IncrementKeys.Add(Key); 
            }
            if(Conditions[Key] == 1 && !startTurn)
            {
                removedKeys.Add(Key);
            }
        }
        foreach(ConditionTemplate Key in IncrementKeys)
        {
            Conditions[Key]--;
        }
        foreach(ConditionTemplate Key in removedKeys)
        {
            Debug.Log(Key.name + " condition removed");
            Conditions.Remove(Key);
        }
    }

    public void PaintTarget()
    {
        StartCoroutine(PaintCoroutine());    
    }

    public void SetGrappler(PlayerStats attacker)
    {
        grappleTarget = null;
        grappler = attacker;
        attacker.grappleTarget = this;
        SetCondition("Grappled",0,true);
        CombatLog.Log(GetName() + ": is grappled by" + grappler.GetName());
        SpendAction("reaction");
        attacker.SpendAction("reaction");
    }

    public void ControlGrapple()
    {
        RemoveCondition("Grappled");
        grappler.SetGrappler(this);
        grappler = null;
    }

    public void ReleaseGrapple()
    {
        CombatLog.Log(GetName() + ": is no longer grappling" + grappleTarget.GetName());
        PopUpText.CreateText("Released!", Color.yellow, grappleTarget.gameObject);
        grappleTarget.grappler = null;
        grappleTarget.RemoveCondition("Grappled");
        grappleTarget = null;
    }

    public bool grappling()
    {
        return (grappleTarget != null || grappler != null);
    }

    IEnumerator PaintCoroutine()
    {
        Material original = model.GetComponent<MeshRenderer>().material;
        model.GetComponent<MeshRenderer>().material = SelectedColor;;
        yield return new WaitForSeconds (0.2f);
        model.GetComponent<MeshRenderer>().material = original;
    }

    public void ApplyAdvanceBonus(int bonus)
    {
        AdvanceBonus = bonus;
    }

    public int GetAdvanceBonus(string location)
    {
        if (location != "Body" && location != "Left Leg" && location != "Right Leg")
        {
            return 0;
        }
        return AdvanceBonus;
    }

    public string GetAverageSoak()
    {
        int highestAP = 0;
        int lowestAP = 100;
        foreach(KeyValuePair<int,string> kvp in HitLocations)
        {
            int AP = GetStat(kvp.Value);
            if ( AP > highestAP)
            {
                highestAP = AP;
            }
            if ( AP < lowestAP)
            {
                lowestAP = AP;
            }
        }
        return "Soak: " + (lowestAP + GetStatScore("T")) + " - " + (highestAP + GetStatScore("T"));
    }

    public override string ToString()
    {
        string output = GetName();
        return output;
    }
}

*/

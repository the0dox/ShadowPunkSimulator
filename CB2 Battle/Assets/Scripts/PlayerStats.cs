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
    private Material DefaultColor;
    private bool painted;
    public CharacterSaveData myData;
    private int AdvanceBonus;
    public GameObject MyCharacterSheet;
    //Reference for all player stats
    public Dictionary<string, int> Stats = new Dictionary<string, int>();
    public Dictionary<ConditionTemplate, int> Conditions = new Dictionary<ConditionTemplate, int>();
    private bool Occupied = false;


    //level of skill training a character has, is called first
    public List<Skill> Skills = new List<Skill>();
    public List<Item> equipment = new List<Item>();
    public Weapon SecondaryWeapon;
    public Weapon PrimaryWeapon; 

    //quick reference for what die rolls correspond to a hit location
    private Dictionary<int, string> HitLocations;

    public void DownloadSaveData(CharacterSaveData myData)
    {
        this.myData = myData;
        this.playername = myData.playername;
        this.team = myData.team;
        this.Stats = myData.GetStats();
        this.Skills = myData.GetSkills();
        this.HitLocations = myData.StandardHitLocations();
        this.equipment = myData.GetEquipment();
        Init();

    }
    // Start is called before the first frame update
    public void Init()
    {
        UpdateMovement();
        UpdateAP();
        StartCoroutine(modelDisplayDelay());
        DefaultColor = model.GetComponentInChildren<MeshRenderer>().material;
        Weapon[] startingequipment = GetWeaponsForEquipment().ToArray();
        if (startingequipment.Length > 0)
        {
            Weapon firstWep = startingequipment[0];
            if(firstWep != null)
            {
                EquipPrimary(firstWep);
            }
        }
    }

    IEnumerator modelDisplayDelay()
    {
        while(true)
        {
            yield return new WaitForSeconds (0.02f);
            if(painted)
            {
                painted = false;
                model.GetComponentInChildren<MeshRenderer>().material = SelectedColor;
            }
            else
            {    
                model.GetComponentInChildren<MeshRenderer>().material = DefaultColor;
            }
        }
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
    
    public void SetName(string input)
    {
        playername = input;
    }

    /*  damage: how much incoming damage already altered 
        location: reveresed die roll for hit location
        takes unaltered damage and applies any damage reduction and subtracts from health
    */
    public void takeDamage(int damage, string location)
    {
        takeDamage(damage,location, "I");
    }

    public void takeDamage(int damage, string location,string damageType)
    {
        //Debug.Log(playername + "'s " + location + " armor reduces the incoming damage to " + damage);
        if (damage > 0) {
            Stats["Wounds"] -= damage;
        }
        if(Stats["Wounds"] < 0)
        {    
            Stats["Critical"] -= Stats["Wounds"];
            Stats["Wounds"] = 0;
            CriticalDamageReference.DealCritical(this, damageType, Stats["Critical"],location);
        }
    }

    public void takeFatigue(int levels)
    {
        if (levels > 0)
        {
            if(!hasCondition("Fatigued"))
            {
                SetCondition("Fatigued",0,true);
            }
            CombatLog.Log(GetName() + " takes " + levels + " levels of fatigue");
            Stats["Fatigue"] += levels;
        }
        if(Stats["Fatigue"] > GetStatScore("T"))
        {
            CombatLog.Log(GetName() + " takes more fatigue than their Toughness bonus and is knocked out!");
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
    }

    public void CompleteDownload()
    {
        UpdateMovement();
        UpdateAP();
    }

    public void SetSkill(Skill input)
    {
        bool contains = false;
        foreach(Skill s in Skills)
        {
            if(s.compareSkill(input))
            {
                contains = true;
            }
        }
        if(!contains)
        {   
            Debug.Log("adding" + input.name + "to skills, value" + input.levels);
            Skills.Add(input);
        }
    }

    public void Unequip(Weapon w)
    {
        if(SecondaryWeapon == w)
        {
            SecondaryWeapon = null;
        }
        if(PrimaryWeapon == w)
        {
            PrimaryWeapon = null;
        }
    }

    public void EquipPrimary(Weapon w)
    {
        PrimaryWeapon = w;
    }
    public void EquipSecondary(Weapon w)
    {
        SecondaryWeapon = w;
    }

    public bool IsDualWielding()
    {
        return(PrimaryWeapon != null && SecondaryWeapon != null);
    }

    public bool OffHandPenalty(Weapon w)
    {
        return (w == SecondaryWeapon);
    }

    //attempts a skill OR Characteristic! from the skill dictionary, applying any modifiers if necessary, returns degrees of successes, not true/false
    public RollResult AbilityCheck(string input, int modifiers)
    {
        int SkillTarget;
        string type;
        int ConditionalModifiers = 0;
        //Debug.Log("attempting " + input);
        Skill convertedSkill = GetSkillReference(input);
        //for skills not characteristics
        if (convertedSkill != null)
        {
            //modifier for skills
            ConditionalModifiers += CalculateStatModifiers(input);
            type = convertedSkill.characterisitc;
            int LevelsTrained = convertedSkill.levels;
            SkillTarget = GetStat(type);
            if (LevelsTrained < 1)
                {
                    //to implement, custom basic skills    
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
        
        string clstring = GetName() + ": "  + input + " check \n    Target:" + SkillTarget +"\n     Roll: " + Roll +"\n     Result: ";
        
        if(Roll <= SkillTarget)
        {   
            clstring += "passed by " + DOF + " degree(s)";
        }
        else
        {
            clstring += "failed by " + DOF + " degree(s)";
        }
        CombatLog.Log(clstring);
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

    public void SpendFate()
    {
        if(Stats["Fate"] > 0)
        {
            Stats["Fate"]--;
        }
    }

    public void BurnFate()
    {
        if(Stats["FateMax"] > 0)
        {
            Stats["FateMax"]--;
        }
        if(Stats["Fate"] > Stats["FateMax"])
        {
            Stats["Fate"] = Stats["FateMax"];
        }
    }

    public void ResetActions()
    {
        CompletedActions.Clear();
    }

    public bool CanParry()
    {
        return (SecondaryWeapon != null && SecondaryWeapon.CanParry() || (PrimaryWeapon != null && PrimaryWeapon.CanParry())); 
    }

    public int ParryBonus()
    {
        int primaryBonus = -99;
        if(PrimaryWeapon != null && PrimaryWeapon.CanParry())
        {
            primaryBonus = 0;
            if (PrimaryWeapon.HasWeaponAttribute("Defensive"))
            {
                primaryBonus += 15;
            }
            if(PrimaryWeapon.HasWeaponAttribute("Balanced"))
            {
                primaryBonus += 10;
            }
            if(PrimaryWeapon.HasWeaponAttribute("Unbalanced"))
            {
                primaryBonus -= 10;
            }
        }
        
        int secondaryBonus = -99;
        if(SecondaryWeapon != null && SecondaryWeapon.CanParry())
        {
            secondaryBonus = 0;
            if (SecondaryWeapon != null && SecondaryWeapon.HasWeaponAttribute("Defensive"))
            {
                secondaryBonus += 15;
            }
            if(SecondaryWeapon != null && SecondaryWeapon.HasWeaponAttribute("Balanced"))
            {
                secondaryBonus += 10;
            }
            if(SecondaryWeapon.HasWeaponAttribute("Unbalanced"))
            {
                secondaryBonus -= 10;
            }
        }
        if((primaryBonus > secondaryBonus))
        {
            return primaryBonus;
        }
        else
        {
            return secondaryBonus;
        }
    }

    public bool PowerFieldAbility()
    {
        if((PrimaryWeapon == null || !PrimaryWeapon.HasWeaponAttribute("Power Field")) && (SecondaryWeapon == null || !SecondaryWeapon.HasWeaponAttribute("Power Field")))
        {
            return false;
        }
        else
        {
            if (Random.Range(1,100) >= 25)
            {
                CombatLog.Log(GetName() + " 's power field shatters the attackers weapon!");
                return true;
            }
            return false;  
        }
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

    //given a hitlocation and if the damage source is primitive, returns if the player has any armor covering that point 
    public int GetAP(string HitLocation, Weapon w)
    {
        int bestAP = 0;
        foreach(Item i in equipment)
        {
            //only check armor
            if(i.GetType() == typeof(Armor))
            {
                Armor a = (Armor)i;
                int currentAP = a.GetAP(HitLocation, w);
                if(currentAP > bestAP)
                {
                    bestAP = currentAP;
                }
            }
        }
        return bestAP;
    }

    //like updatemovement, players can't actually input their AP instead the display is modified to show their best AP from their equiped armor
    public void UpdateAP()
    {
        foreach(KeyValuePair<int,string> kvp in HitLocations)
        {
            Stats[kvp.Value] = GetAP(kvp.Value, null);
        }
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
        painted = true;
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

    private Skill GetSkillReference(string input)
    {
        foreach(Skill s in Skills)
        {
            if(s.name.Equals(input))
            {
                return s;
            }
        }
        return null;
    }

    public List<Weapon> GetWeaponsForEquipment()
    {
        List<Weapon> output = new List<Weapon>();
        foreach(Item i in equipment)
        {
            if(i.GetType() == typeof(Weapon))
            {
                output.Add((Weapon)i);
            }
        }
        return output;
    }

    public bool IsHelpless()
    {
        if(hasCondition("Immobilised") || hasCondition("Unconscious"))
        {
            CombatLog.Log(GetName() + " is helpless! attacks automatically succeded and take the highest of two results for damage!");
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool IsOccupied()
    {
        return Occupied;
    }
    public void StartJob()
    {
        Occupied = true;
    }
    public void EndJob()
    {
        Occupied = false;
    }
    public override string ToString()
    {
        string output = GetName();
        return output;
    }
}

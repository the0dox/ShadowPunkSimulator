using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class representing a basic die roll which can be stored and accessed later
public class RollResult
{
    // reference to the owner of the roll
    private CharacterSaveData owner;
    // used to determine how succesful the roll is 
    private int DegreeSuccess;
    // the actual unmodified roll
    private int DieRoll;
    // the modified target the roll needs to pass to be successful
    private int Target; 
    // the type of roll
    private string type;
    // if enabled, signals to game controller that the roll has been completed by player input
    private bool completed = false;
    // when an advanced roll is complete, command string is invoked for an attack sequence for example
    private string command;
    private RollResult opposingRoll;

    // SR5 rules
    int pool;
    public int threshold;
    private int[] dice; 
    public AttributeKey firstField;
    public AttributeKey secondField; 
    public AttributeKey LimitKey;
    private int failures;
    private int successes;
    public int modifiers;
    public string customName;
    public int WeaponAccuracy;
    public bool useWeapon = false;
    
    /* Depreciated dH system
    public RollResult(CharacterSaveData owner, int Target, string type, string command)
    {
        this.owner = owner;
        this.command = command;
        this.Target = Target;
        this.type = type;
        if(SkillPromptBehavior.ManualRolls)
        {
            this.DegreeSuccess = -1;
            this.DieRoll = -1;
            completed = false;
            SkillPromptBehavior.NewRoll(this);
        }
        else
        {
            SetRoll(Random.Range(1,101));
        }
    }
    */

    public RollResult(CharacterSaveData owner, Weapon weapon, int modifiers)
    {
        this.useWeapon = true;
        this.WeaponAccuracy = weapon.Template.accuracy;
        this.owner = owner; 
        this.firstField = weapon.Template.WeaponSkill.skillKey;
        this.secondField = weapon.Template.WeaponSkill.derrivedAttribute;
        this.threshold = 0;
        this.modifiers = modifiers; 

        if(SkillPromptBehavior.ManualRolls)
        {
            completed = false;
            SkillPromptBehavior.NewRoll(this);
        }
        else
        {
            Roll();
        }
    }

    public RollResult(CharacterSaveData owner, AttributeKey skillKey, int threshold = 0, int modifiers = 0)
    {
        this.owner = owner;
        this.modifiers = modifiers;
        this.threshold = threshold;
        SkillTemplate testedSkill = SkillReference.GetSkill(skillKey);
        firstField = skillKey;
        secondField = testedSkill.derrivedAttribute;
        LimitKey = testedSkill.limit;
        if(SkillPromptBehavior.ManualRolls)
        {
            completed = false;
            SkillPromptBehavior.NewRoll(this);
        }
        else
        {
            Roll();
        }
    }

    public RollResult(CharacterSaveData owner, AttributeKey firstField = AttributeKey.Empty, AttributeKey secondField = AttributeKey.Empty, AttributeKey LimitKey = AttributeKey.Empty, int threshold = 0, int modifiers = 0)//,  PlayerStats other = null)
    {
        this.owner = owner;
        this.firstField = firstField;
        this.secondField = secondField;
        this.LimitKey = LimitKey;
        this.modifiers = modifiers;
        this.threshold = threshold;
        if(SkillPromptBehavior.ManualRolls)
        {
            completed = false;
            SkillPromptBehavior.NewRoll(this);
        }
        else
        {
            Roll();
        }
    }

    // advanced roll that requires manual entry before its data can be accessed
    public void Roll()
    {
        dice = new int[GetPool()];
        
        failures = 0;
        successes = 0;
        
        for(int i = 0; i < dice.Length; i++)
        {
            int result = Random.Range(1,7);
            dice[i] = result;
            if(result > 4)
            {
                successes++;
            }
            else if(result == 1)
            {
                failures++;
            }
        }

        LimitSuccesses();

        completed = true;

        PrintResult();

        //Debug.Log(owner.playername + " rolls and gets " + GetHits() + " successes out of " + pool + " rolls!");
    }
    
    // Given skills and attributes, creates the dice pool to be rolled
    public int GetPool()
    {
        pool = 0;
        if(firstField != AttributeKey.Empty)
        {
            pool += owner.GetAttribute(firstField);
        }
        if(secondField != AttributeKey.Empty)
        {
            pool += owner.GetAttribute(secondField);
        }
        pool += modifiers;
        //Debug.Log("Die Pool: " + pool);
        if(pool < 0)
        {
            pool = 0;
        }
        dice = new int[pool];
        return pool;
    }

    // Total successes cannot exceede limit, do not execute if Limit does not Exist 
    private void LimitSuccesses()
    {
        if(useWeapon)
        {
            int Dicelimit = WeaponAccuracy;
            if(successes > Dicelimit)
            {
                successes = Dicelimit;
            }
        }
        else if(LimitKey != AttributeKey.Empty)
        {
            int Dicelimit = owner.GetAttribute(LimitKey);
            if(successes > Dicelimit)
            {
                successes = Dicelimit;
            }
        }
    }

    public bool Passed()
    {
        return successes >= threshold;
    }

    // returns a int value if glitched, 0 for no glitch, 1 for standard, 2 for critical
    public int Glitched()
    {
        int value = 0;
        float percentageOnes = (float)failures/(float)dice.Length;
        bool halfOnes = percentageOnes >= 0.5f;
        value += halfOnes ? 1 : 0;
        value += halfOnes && successes == 0 ? 1 : 0;
        //Debug.Log("GV:" + value);
        return value;
    }

    public int GetHits()
    {
        if(opposingRoll != null)
        {
            return successes - threshold - opposingRoll.GetHits();
        }
        return successes - threshold;
    }

    public void OpposedRoll(RollResult other)
    {
        opposingRoll = other;
    }

    // passes a dummy roll
    public RollResult()
    {
        DieRoll = -1;
        completed = true;
    }

    // used to inform other scripts if this object has a DieRoll
    public bool Completed()
    {
        if(opposingRoll != null)
        {
            return opposingRoll.Completed() && completed;
        }
        return completed;
    }


    // Completes a die roll 
    public void SetRoll(int roll)
    {
        this.DieRoll = roll;
        completed = true;
        DegreeSuccess = (Passed() ? 1 : -1) + (Target - DieRoll)/10;
        //owner.RollComplete(this);
        PrintResult();
    }

    private void PrintResult()
    {
        string clstring = owner.playername + ": "  + displayName() + " check: \n ";
        
        int printableSuccesses = successes;
        int printableFailures = failures;
        //Debug.Log(printableSuccesses + "succs" + printableFailures + "fails");
        for(int i = 0; i < dice.Length; i++)
        {
            clstring += "| ";
            if(printableSuccesses > 0)
            {
                printableSuccesses--;
                clstring += " H ";
            }
            else if (printableFailures > 0)
            {
                printableFailures--;
                clstring += " F ";
            }
            else
            {
                clstring += " - ";
            }
        }
        clstring += " |\n ";
        
        int netHits = GetHits();    
        if(netHits > 0)
        {
            clstring += "passed with " + netHits + " hits!";
        }
        else
        {
            clstring += "failed by " + (-netHits) + " hits!";
        }

        int faliureValue = Glitched();
         
        string additionalText = "";
        if(faliureValue > 1)
        {
            additionalText = "Critical Glitch!";
        }
        else if (faliureValue > 0)
        {
            additionalText = "Glitch!";
        }
        if(!string.IsNullOrEmpty(additionalText))
        {
            clstring += "\n " + additionalText;
        }

        CombatLog.Log(clstring);
        //PopUpText.CreateText(Print(), GetColor(), owner.gameObject);
    }

    public int GetDOF()
    {
        if(opposingRoll != null)
        {
            if(Passed())
            {
                return DegreeSuccess - opposingRoll.GetDOF();
            }
            else if(opposingRoll.Passed())
            {
                 return -opposingRoll.GetDOF();
            }
            else
            {
                return 0;
            }
        }
        return DegreeSuccess;
    }

    public int GetRoll()
    {
        return DieRoll;
    }

    public CharacterSaveData getOwner()
    {
        return owner;
    }

    public int GetTarget()
    {
        return Target;
    }

    public string displayName()
    {
        if(!string.IsNullOrEmpty(customName))
        {
            return customName;
        }
        if(firstField != AttributeKey.Empty)
        {
            return firstField.ToString();
        }
        if(secondField != AttributeKey.Empty)
        {
            return secondField.ToString();
        } 
        return "skill";
    }

    public string GetCommand()
    {
        return command;
    }
    public string Print()
    {
        return type + ": " + DieRoll + " ("  + DegreeSuccess + ")";
    }

    public Color GetColor()
    {
        if (Passed())
        {
            return Color.green;
        }
        return Color.red;
    }

}

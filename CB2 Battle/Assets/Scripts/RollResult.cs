using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Class representing a basic die roll which can be stored and accessed later
public class RollResult
{
    // reference to the owner of the roll
    private PlayerStats owner;
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
    private int[] dice; 
    private string skillKey;
    private string attributeKey; 
    private string LimitKey;
    private int failures;
    private int successes;
    
    // advanced roll that requires manual entry before its data can be accessed
    public RollResult(PlayerStats owner, int Target, string type, string command)
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

    
    public RollResult(PlayerStats owner, string skillKey, string attributeKey = "", string LimitKey = "")//,  PlayerStats other = null)
    {
        this.owner = owner;
        this.skillKey = skillKey;
        this.attributeKey = attributeKey;
        this.LimitKey = LimitKey;
        if(!SkillPromptBehavior.ManualRolls)
        {
            Roll();
        }
    }

    // advanced roll that requires manual entry before its data can be accessed
    public void Roll()
    {
        int Dicelimit = 99;
        if(string.IsNullOrEmpty(attributeKey))
        {
            attributeKey = SkillReference.GetDerrivedAttribute(skillKey);
        }
        if(!string.IsNullOrEmpty(LimitKey))
        {
            Dicelimit = owner.myData.GetAttribute(LimitKey);
        }

        int pool = owner.myData.GetSkill(skillKey,false) + owner.myData.GetAttribute(attributeKey);

        dice = new int[pool];
        
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
    }

    public bool Glitched()
    {
        float percentageOnes = (float)failures/(float)dice.Length;
        return percentageOnes >= 0.5f;
    }

    public int GetHits()
    {
        return successes;
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
        owner.RollComplete(this);
        PrintResult();
    }

    private void PrintResult()
    {

        string clstring = owner.GetName() + ": "  + type + " check \n    Target:" + Target +"\n     Roll: " + DieRoll +"\n     Result: ";
        
        if(DieRoll <= Target)
        {   
            clstring += "passed by " + DegreeSuccess + " degree(s)";
        }
        else
        {
            clstring += "failed by " + DegreeSuccess + " degree(s)";
        }
        CombatLog.Log(clstring);
        PopUpText.CreateText(Print(), GetColor(), owner.gameObject);
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

    public PlayerStats getOwner()
    {
        return owner;
    }

    public int GetTarget()
    {
        return Target;
    }

    public string GetSkillType()
    {
        return type;
    }

    public string GetCommand()
    {
        return command;
    }

    public bool Passed()
    {
        return DieRoll <= Target;
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

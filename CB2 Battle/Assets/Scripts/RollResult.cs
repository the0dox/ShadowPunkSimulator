using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RollResult
{
    private int DegreeSuccess;
    private int DieRoll;
    private int Target; 
    private string type;
    
    public RollResult(int DegreeSuccess, int DieRoll, int Target, string type)
    {
        this.DegreeSuccess = DegreeSuccess;
        this.DieRoll = DieRoll;
        this.Target = Target;
        this.type = type;
    }

    public int GetDOF()
    {
        return DegreeSuccess;
    }

    public int GetRoll()
    {
        return DieRoll;
    }

    public int GetTarget()
    {
        return Target;
    }

    public string GetMyType()
    {
        return type;
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Range Class", menuName = "ScriptableObjects/Ranges")]
public class RangeWeaponClass : ScriptableObject
{
    [SerializeField] private int shortRange;
    [SerializeField] private int mediumRange;
    [SerializeField] private int longRange;
    [SerializeField] private int extremeRange;

    public int GetRangePenalty(int distance)
    {
        if(distance <= shortRange)
        {
            return 0;
        }
        if(distance <= mediumRange)
        {
            return -1;
        }
        if(distance <= longRange)
        {
            return -3;
        }
        if(distance <= extremeRange)
        {
            return -6;
        }
        Debug.Log("outside range");
        return 0;
    }

    public bool withinRange(int distance)
    {
        return distance <= extremeRange;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Conditions", menuName = "ScriptableObjects/Conditions")]
public class ConditionTemplate : ScriptableObject
{  
    [SerializeField] public string[] Stats;
    [SerializeField] public int[] Modifiers;
    [SerializeField] public int duration;
    [SerializeField] public bool clearOnStart;  
    
    [SerializeField] public Dictionary<string, int> StatReference = new Dictionary<string, int>();

    public int GetModifier(string skill)
    {
        CreateReference();
        if(StatReference.ContainsKey(skill))
        {
            //Debug.Log(name + " modifies " + skill + " by " + StatReference[skill]);
            return StatReference[skill];
        }
        return 0;
    }

    public void CreateReference()
    {
        if(StatReference.Count == 0)
        {
            for (int i = 0; i < Stats.Length; i++)
            {
                StatReference.Add(Stats[i], Modifiers[i]);
            }
        }
    }

    public bool isCondition(string type)
    {
        return type.Equals(name);
    }
}

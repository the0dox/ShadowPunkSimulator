using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A basic template for all conditions in the game, because conditions can be modified the scriptable object can't be assigned to multiple players. 
// So instead a seperate Condition script is created based on the stats of this template and assigned
[CreateAssetMenu(fileName = "Conditions", menuName = "ScriptableObjects/Conditions")]
public class ConditionTemplate : ScriptableObject
{  
    // characteristics of the condition object
    [SerializeField] public string IgnoreKey;
    [SerializeField] public int Modifiers;
    [SerializeField] public int duration;
    [SerializeField] public bool clearOnTurnEnd;  
    [SerializeField] public bool clearOnMove;
    [SerializeField] public bool clearOnAttack;
    [SerializeField] public Condition conditionKey;
    [SerializeField] public EffectKey effectKey;
    public int GetModifier(string key)
    {
        if(!string.IsNullOrEmpty(key) && !key.Equals(IgnoreKey))
        {
            return Modifiers;
        }
        return 0;
    }

}

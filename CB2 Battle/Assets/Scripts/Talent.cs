using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Talent", menuName = "ScriptableObjects/Talents")]
public class Talent : ScriptableObject
{
    [SerializeField] public TalentKey key;
    [TextArea(5,10)]
    [SerializeField] private string description;
    [SerializeField] private List<TalentKey> prerequisites;
    [SerializeField] private List<TalentKey> conflictsWith;
    [SerializeField] public Sprite Icon;

    public bool CanSelect(CharacterSaveData csd)
    {
        // must have each prerequisite
        foreach(TalentKey key in prerequisites)
        {
            if(!csd.hasTalent(key))
            {
                return false;
            }
        }

        // must not have any talents that conflict with this one
        foreach(TalentKey key in conflictsWith)
        {
            if(csd.hasTalent(key))
            {
                return false;
            }
        }
        return true;
    }

    public string getDescription(CharacterSaveData csd)
    {
        string output = "";
        if(!CanSelect(csd))
        {
            output += "Cannot Select:";   
        }
        // must have each prerequisite
        foreach(TalentKey key in prerequisites)
        {
            if(!csd.hasTalent(key))
            {
                output += "\n X: has " + key.ToString() + " talent"; 
            }
            else
            {
                output += "\n O: has " + key.ToString() + " talent"; 
            }
        }

            // must not have any talents that conflict with this one
            foreach(TalentKey key in conflictsWith)
            {
                if(csd.hasTalent(key))
                {
                    output += "\n X: does not have " + key.ToString() + " talent";
                }
                else
                {
                    output += "\n O: does not have " + key.ToString() + " talent";
                }
            }
        output += "\n\n" + description;
        return output;
    }
}

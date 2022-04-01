using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalentReference : MonoBehaviour
{
    [SerializeField] private List<Talent> TalentInitalizer = new List<Talent>();
    private static Dictionary<TalentKey, Talent> Library = new Dictionary<TalentKey, Talent>();
    public void Init()
    {
        foreach(Talent t in TalentInitalizer)
        {
            Library.Add(t.key, t);
        }
    }
    
    public static Talent GetTalent(TalentKey key)
    {
        if(!Library.ContainsKey(key))
        {
            Debug.Log("Error " + key.ToString() + " not found!");
        }
        return Library[key];
    }

    public static Dictionary<TalentKey, Talent> getLibraries()
    {
        return Library;
    }
}

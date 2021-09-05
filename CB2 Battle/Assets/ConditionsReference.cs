using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionsReference : MonoBehaviour
{
    [SerializeField] public List<ConditionTemplate> ConditionsInitializer = new List<ConditionTemplate>();
    private static Dictionary<string, ConditionTemplate> Library = new Dictionary<string, ConditionTemplate>();

    void Start()
    {
        foreach(ConditionTemplate c in ConditionsInitializer)
        {
            Library.Add(c.name, c);
        }
    }

    public static ConditionTemplate Condition(string name)
    {
        if(!Library.ContainsKey(name))
        {
            Debug.Log("Error " + name + " not found!");
        }
        return Library[name];
    }
}

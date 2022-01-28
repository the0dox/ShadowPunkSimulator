using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A static library that contains all conditions TEMPLATES so that scripts can easily create add/edit conditions
public class ConditionsReference : MonoBehaviour
{
    // IMPORTANT: you must add all scriptable objects to this list in order for them to be intialized in the game scene
    [SerializeField] public List<ConditionTemplate> ConditionsInitializer = new List<ConditionTemplate>();
    // static reference of conditionsinitializer for other scripts
    private static Dictionary<Condition, ConditionTemplate> Library = new Dictionary<Condition, ConditionTemplate>();

    // creates Library so that it can be referenced statically
    public void Init()
    {
        foreach(ConditionTemplate c in ConditionsInitializer)
        {
            Library.Add(c.conditionKey, c);
        }
    }

    // name: the name of the scriptable object that needs to be copied
    // creates and returns a regular condition object out of the template that shares a name with input
    public static ConditionTemplate GetTemplate(Condition name)
    {
        if(!Library.ContainsKey(name))
        {
            Debug.Log("Error " + name + " not found!");
        }
        return Library[name];
    }

    // returns the library
    public static Dictionary<Condition, ConditionTemplate> ConditionTemplates()
    {
        return Library;
    }
}

public enum Condition
{
    Aiming,
    Prone,
    Grappled,
    Grappler,
    CalledShot,
    Covered,
    Running,
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionReference : MonoBehaviour
{
    [SerializeField] private List<ActionTemplate> ActionInitalizer = new List<ActionTemplate>();
    private static Dictionary<string, ActionTemplate> Library = new Dictionary<string, ActionTemplate>();
    public void Init()
    {
        foreach(ActionTemplate a in ActionInitalizer)
        {
            Library.Add(a.name, a);
        }
    }
    
    public static ActionTemplate GetActionTemplate(string key)
    {
        if(!Library.ContainsKey(key))
        {
            return null;
        }
        return Library[key];
    }
}

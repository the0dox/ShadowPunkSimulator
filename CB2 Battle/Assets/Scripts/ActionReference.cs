using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionReference : MonoBehaviour
{
    [SerializeField] private List<ActionTemplate> ActionInitalizer = new List<ActionTemplate>();
    private static List<ActionTemplate> sActionReference;
    public void Init()
    {
        sActionReference = ActionInitalizer;
    }
    
    public static ActionTemplate GetActionTemplate(string key)
    {
        foreach(ActionTemplate a in sActionReference)
        {
            if(a.code.Equals(key))
            {
                return a;
            }
        }
        return null;
    }
}

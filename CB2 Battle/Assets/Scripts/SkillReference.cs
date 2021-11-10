using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillReference : MonoBehaviour
{
    [SerializeField] private List<SkillTemplate> SkillTemplateInitializer = new List<SkillTemplate>();
    private static Dictionary<string, SkillTemplate> Library = new Dictionary<string, SkillTemplate>();

    public void Init()
    {
        foreach(SkillTemplate c in SkillTemplateInitializer)
        {
            Library.Add(c.name, c);
        }
    }

    public static SkillTemplate GetSkill(string name)
    {
        if(!Library.ContainsKey(name))
        {
            Debug.Log("Error " + name + " not found!");
        }
        return Library[name];
    }

    public static bool Defaultable(string name)
    {
        return Library[name].defaultable;
    }

    public static string GetDerrivedAttribute(string name)
    {
        return Library[name].characterisitc;
    }

    public static Dictionary<string, SkillTemplate> SkillsTemplates()
    {
        return Library;
    }
}

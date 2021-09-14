using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillReference : MonoBehaviour
{
    [SerializeField] private List<SkillTemplate> SkillTemplateInitializer = new List<SkillTemplate>();
    private static Dictionary<string, SkillTemplate> Library = new Dictionary<string, SkillTemplate>();

    void Start()
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

    public static Dictionary<string, SkillTemplate> SkillsTemplates()
    {
        return Library;
    }
}

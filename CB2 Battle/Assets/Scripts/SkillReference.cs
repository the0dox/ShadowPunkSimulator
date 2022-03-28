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

    public static SkillTemplate GetSkill(AttributeKey skillKey)
    {
        foreach(string key in Library.Keys)
        {
            SkillTemplate template = Library[key];
            if(template.skillKey.Equals(skillKey))
            {
                return template;
            }
        }
        throw new System.NullReferenceException();
    }

    public static Dictionary<string, SkillTemplate> SkillsTemplates()
    {
        return Library;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillReference : MonoBehaviour
{
    [SerializeField] private List<SkillReference> WeaponTemplateInitializer = new List<SkillReference>();
    private static Dictionary<string, SkillReference> Library = new Dictionary<string, SkillReference>();

    void Start()
    {
        foreach(SkillReference c in WeaponTemplateInitializer)
        {
            Library.Add(c.name, c);
        }
    }

    public static SkillReference GetSkill(string name)
    {
        if(!Library.ContainsKey(name))
        {
            Debug.Log("Error " + name + " not found!");
        }
        return Library[name];
    }

    public static Dictionary<string, SkillReference> SkillsTemplates()
    {
        return Library;
    }
}

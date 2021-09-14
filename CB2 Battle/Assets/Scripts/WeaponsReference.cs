using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsReference : MonoBehaviour
{
    [SerializeField] private List<WeaponTemplate> WeaponTemplateInitializer = new List<WeaponTemplate>();
    private static Dictionary<string, WeaponTemplate> Library = new Dictionary<string, WeaponTemplate>();

    void Start()
    {
        foreach(WeaponTemplate c in WeaponTemplateInitializer)
        {
            Library.Add(c.name, c);
        }
    }

    public static WeaponTemplate GetWeapon(string name)
    {
        if(!Library.ContainsKey(name))
        {
            Debug.Log("Error " + name + " not found!");
        }
        return Library[name];
    }

    public static Dictionary<string, WeaponTemplate> WeaponTemplates()
    {
        return Library;
    }
}

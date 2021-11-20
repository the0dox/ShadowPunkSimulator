using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemReference : MonoBehaviour
{
    [SerializeField] private List<ItemTemplate> ItemTemplateInitializer = new List<ItemTemplate>();
    private static Dictionary<string, ItemTemplate> Library = new Dictionary<string, ItemTemplate>();
    public void Init()
    {
        foreach(ItemTemplate c in ItemTemplateInitializer)
        {
            Library.Add(c.name, c);
        }
    }

    public static Item GetItem(string name)
    {
        if(!Library.ContainsKey(name))
        {
            Debug.Log("Error " + name + " not found!");
        }
        ItemTemplate template = GetTemplate(name);
        if(template.GetType().Equals(typeof(WeaponTemplate)))
        {
            return new Weapon((WeaponTemplate)template);
        }
        return new Item(GetTemplate(name));
    }
    
    public static Item GetItem(string name, int stacks, string[] upgrades)
    {
        Item output = GetItem(name);
        output.SetStack(stacks);
        for(int i = 0; i < upgrades.Length; i++)
        {
            output.SetUpgrade(i, upgrades[i].Equals("1"));
        }
        return output;
    }

    public static ItemTemplate GetTemplate(string name)
    {
        return Library[name];
    }

    public static Dictionary<string, ItemTemplate> ItemTemplates()
    {
        return Library;
    }
}

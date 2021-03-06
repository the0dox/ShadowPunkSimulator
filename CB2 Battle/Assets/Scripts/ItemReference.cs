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
            return null;
        }
        else{
            ItemTemplate template = GetTemplate(name);
            if(template.GetType().Equals(typeof(WeaponTemplate)))
            {
                return new Weapon((WeaponTemplate)template);
            }
            else if(template.GetType().Equals(typeof(DroneTemplate)))
            {
                return new Drone((DroneTemplate)template);
            }
            else if (template.GetType().Equals(typeof(ArmorTemplate)))
            {
                return new Armor((ArmorTemplate)template);
            }
            return new Item(GetTemplate(name));
        }
    }
    
    public static Item GetItem(string name, int stacks, string[] upgrades)
    {
        Item output = GetItem(name);
        if(output != null)
        {
            output.SetStack(stacks);
            for(int i = 0; i < upgrades.Length; i++)
            {
                output.SetUpgrade(i, upgrades[i].Equals("1"));
            }
        }
        return output;
    }

    public static ItemTemplate GetTemplate(string name)
    {
        if(!Library.ContainsKey(name))
        {
            Debug.Log("item " + name + " doesn't exisit");
            return null;
        }
        return Library[name];
    }

    public static Dictionary<string, ItemTemplate> ItemTemplates()
    {
        return Library;
    }
}

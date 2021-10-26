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

    public static ItemTemplate GetItem(string name)
    {
        if(!Library.ContainsKey(name))
        {
            Debug.Log("Error " + name + " not found!");
        }
        return Library[name];
    }

    public static Dictionary<string, ItemTemplate> ItemTemplates()
    {
        return Library;
    }
    
    public static List<Item> DownloadEquipment(Dictionary<string,int> PhotonEquipment)
    {
        List<Item> output = new List<Item>();
        foreach(KeyValuePair<string,int> kvp in PhotonEquipment)
        {
            // specific types of items have to be created seperately
            Item current = null;
            if(ItemReference.ItemTemplates()[kvp.Key].GetType() == typeof(WeaponTemplate))
            {
                current = new Weapon((WeaponTemplate)ItemReference.ItemTemplates()[kvp.Key]);
            }
            else if(ItemReference.ItemTemplates()[kvp.Key].GetType() == typeof(ArmorTemplate))
            {
                current = new Armor((ArmorTemplate)ItemReference.ItemTemplates()[kvp.Key]);
            }
            else
            {
                current = new Item(ItemReference.GetItem(kvp.Key));
            }
            current.SetStack(kvp.Value);
            output.Add(current);
        }
        return output;
    }
}

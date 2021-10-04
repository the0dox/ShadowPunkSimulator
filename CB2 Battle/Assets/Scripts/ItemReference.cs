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
}

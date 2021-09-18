using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponAdder : MonoBehaviour
{
    //drop down menu for weapon selection
    [SerializeField] private Dropdown Selector;
    private Dictionary<string,ItemTemplate> weaponLibrary;
    //indicates which kind of weapon should be shown in the selector
    
    public void Init()
    {
        Selector.ClearOptions();
        weaponLibrary = ItemReference.ItemTemplates();
        List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
        foreach(string key in weaponLibrary.Keys)
        {
            if(!key.Equals("Unarmed"))
            {
                Dropdown.OptionData newData = new Dropdown.OptionData(); 
                newData.text = key;
                results.Add(newData);
            }
        }
        Selector.AddOptions(results);
    }

    public bool isWeapon()
    {
        ItemTemplate selection = weaponLibrary[Selector.captionText.text];
        if(selection.GetType() == typeof(ItemTemplate))
        {
            Debug.Log("selection is an item");
            return false;
        }
        else
        {
            Debug.Log("selection is a weapon");
            return true;
        }
    }
    public Item GetItem()
    {
        if(isWeapon())
        {
            return new Weapon((WeaponTemplate) weaponLibrary[Selector.captionText.text]);
        }
        return new Item(weaponLibrary[Selector.captionText.text]);
    }
}

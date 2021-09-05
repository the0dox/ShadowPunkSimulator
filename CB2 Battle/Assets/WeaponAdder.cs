using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponAdder : MonoBehaviour
{
    //drop down menu for weapon selection
    [SerializeField] private Dropdown Selector;
    private Dictionary<string,WeaponTemplate> weaponLibrary;
    [SerializeField] private bool Melee;
    //indicates which kind of weapon should be shown in the selector
    
    public void Init()
    {
        Selector.ClearOptions();
        weaponLibrary = WeaponsReference.WeaponTemplates();
        Debug.Log(weaponLibrary.Count);
        List<Dropdown.OptionData> results = new List<Dropdown.OptionData>();
        foreach(string key in weaponLibrary.Keys)
        {
            if(Melee && weaponLibrary[key].Attributes.Contains("Melee") || !Melee && !weaponLibrary[key].Attributes.Contains("Melee"))
            {
                Dropdown.OptionData newData = new Dropdown.OptionData(); 
                newData.text = key;
                results.Add(newData);
            }
        }
        Selector.AddOptions(results);
    }

    public Weapon GetWeapon()
    {
        return new Weapon(weaponLibrary[Selector.captionText.text]);
    }
}

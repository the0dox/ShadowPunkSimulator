using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Scriptable object that serves as the template for a specific piece of armor
[CreateAssetMenu(fileName = "Armor", menuName = "ScriptableObjects/Armor")]
public class ArmorTemplate : ItemTemplate
{
    // locations covered by armor
    [SerializeField] public string[] Parts;
    // damage reduction provided by armor
    [SerializeField] public int AP;
    // special rules for armor
    [SerializeField] public List<string> Attributes;
}

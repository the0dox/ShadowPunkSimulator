using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Armor", menuName = "ScriptableObjects/Armor")]
public class ArmorTemplate : ItemTemplate
{
    [SerializeField] public string[] Parts;
    [SerializeField] public int AP;
    [SerializeField] public List<string> Attributes;
}

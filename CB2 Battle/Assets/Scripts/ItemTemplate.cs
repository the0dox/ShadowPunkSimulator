using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Item", menuName = "ScriptableObjects/Items")]
public class ItemTemplate : ScriptableObject
{
    [SerializeField] public float weight;
    [SerializeField] public float cost;
    [SerializeField] public string availablity;
    [TextArea(5,10)]
    [SerializeField] public string description;
    [SerializeField] public bool unique;
    [SerializeField] public int rating;
    [SerializeField] public Sprite icon;
    [SerializeField] public List<ItemTemplate> upgrades;
}

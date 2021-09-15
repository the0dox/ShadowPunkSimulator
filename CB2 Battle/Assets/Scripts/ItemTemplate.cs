using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "ScriptableObjects/Items")]
public class ItemTemplate : ScriptableObject
{
    [SerializeField] public float weight;
    [SerializeField] public int cost;
    [SerializeField] public string availablity;
    [SerializeField] public string description;
}

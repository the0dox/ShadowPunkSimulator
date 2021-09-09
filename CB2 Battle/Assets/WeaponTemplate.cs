using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Weapons", menuName = "ScriptableObjects/Weapons")]
public class WeaponTemplate : ScriptableObject
{
    [SerializeField] public List<string> Attributes = new List<string>();
    [SerializeField] public string Class;
    //number of dice rolled to determine damage Xd10 
    [SerializeField] public int numDice; 
    //random range of damage per die roll 1dX
    [SerializeField] public int sizeDice;
    //Flat damage bonus for ranged weapons, multiplier for SB for melee weapons
    [SerializeField] public int damageBonus;
    //effective range for weapon
    [SerializeField] public int range;
    //Negates AP of target
    [SerializeField] public int pen;
    
    //number of half actions must exceed this value to reload 
    [SerializeField] public int reloadMax;  
    //number of shots fired in single firemode
    [SerializeField] public int single;
    //number of shots fired in semi auto mode, 
    [SerializeField] public int semi;
    //number of shots fired in full auto
    [SerializeField] public int auto;
    //clip capacity
    [SerializeField] public int clipMax;
    //damage type for determining critical table
    [SerializeField] public string damageType;
    [SerializeField] public int blast;
}

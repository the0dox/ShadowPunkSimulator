using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A single attack sequence that is processed one at a time by the game so that there isn't visual clutter
public class AttackSequence
{
    // Weapon used in the attack
    public Weapon ActiveWeapon;
    // The player attacking with the weapon
    public PlayerStats attacker;
    // The player on the recieving end of the attack
    public PlayerStats target;
    // The fire rate used by the weapon if ranged
    public string FireRate;
    // number of additional attacks caused by weapon. different than mulitple attack sequences because attacks 
    // can be dodged more than once, while attacksequences can only be avoided once per turn 
    public int attacks;
    // The body location where the attack will land, 
    public string HitLocation;

    // creates an attack sequence without specifying a hitlocation
    public AttackSequence (PlayerStats target, PlayerStats attacker, Weapon ActiveWeapon, string ROF,int attacks)
    {
        this.ActiveWeapon = ActiveWeapon;
        this.attacker = attacker;
        this.target = target;
        this.FireRate = ROF;
        this.attacks =attacks;
    }
    // creates an attack sequence while specifying a hitlocation
    public AttackSequence (PlayerStats target, PlayerStats attacker, Weapon ActiveWeapon, string ROF,int attacks,string HitLocation)
    {
        this.ActiveWeapon = ActiveWeapon;
        this.attacker = attacker;
        this.target = target;
        this.FireRate = ROF;
        this.attacks =attacks;
        this.HitLocation = HitLocation;
    }
}

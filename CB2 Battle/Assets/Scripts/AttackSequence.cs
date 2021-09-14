using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackSequence
{
    public Weapon ActiveWeapon;
    public PlayerStats attacker;
    public PlayerStats target;
    public string FireRate;
    public int attacks;
    public string HitLocation;

    public AttackSequence (PlayerStats target, PlayerStats attacker, Weapon ActiveWeapon, string ROF,int attacks)
    {
        this.ActiveWeapon = ActiveWeapon;
        this.attacker = attacker;
        this.target = target;
        this.FireRate = ROF;
        this.attacks =attacks;
    }
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

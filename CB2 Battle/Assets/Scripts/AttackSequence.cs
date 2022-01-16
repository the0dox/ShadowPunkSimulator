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
    public RollResult attackRoll;
    public bool attackRolled = false;
    public RollResult reactionRoll;
    public bool reactionRolled = false;
    public RollResult soakRoll;
    public bool soakRolled = false;
    public bool AttackMissed = false;

    // creates an attack sequence without specifying a hitlocation
    public AttackSequence (PlayerStats target, PlayerStats attacker, Weapon ActiveWeapon, string ROF,int attacks, bool skipAttack)
    {
        this.ActiveWeapon = ActiveWeapon;
        this.attacker = attacker;
        this.target = target;
        this.FireRate = ROF;
        this.attacks =attacks;
        if(skipAttack)
        {
            attackRoll = new RollResult();
        }
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
    
    public void AttackRollComplete()
    {
        attackRolled = true;
        if(attackRoll.GetHits() < 1)
        {
            CombatLog.Log(attacker.GetName() + " misses!");
            PopUpText.CreateText("Missed", Color.yellow, target.gameObject);
            AttackMissed = true;
        }
    }
    public void ReactionRollComplete()
    {
        reactionRolled = true;
        // See compare hits between attack and defense roll
        int netHits = attackRoll.GetHits();
        if (reactionRoll != null)
        {
            netHits -= reactionRoll.GetHits();
        } 
        // Attack misses if net hits is 0 or less TO IMPLEMENT: Scrape
        if(netHits < 1)
        {
            CombatLog.Log(target.GetName() + " avoids the attack!");
            PopUpText.CreateText("Missed", Color.yellow, target.gameObject);
            AttackMissed = true;
        }
        // If attack hits, start a soak roll
        else
        {
            int armorMod = target.GetAP();
            int armorPen = ActiveWeapon.GetAP();
            if(armorMod > 0)
            {
                armorMod -= armorPen;
            }
            if(armorMod < 0)
            {
                armorMod = 0;
            }
            soakRoll = target.AbilityCheck(AttributeKey.Body, AttributeKey.Empty, AttributeKey.Empty, "Armor", 0, armorMod);
        }
    }
    
    public void SoakRollComplete()
    {
        soakRolled = true;
    }
}

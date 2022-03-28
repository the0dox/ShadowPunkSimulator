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
    public Tile CoverTile;
    public int coverRange = 0;
    private bool skipAttack = false;
    int flatDamage = 0;
    int flatAP = 0;

    // creates an attack sequence of damage being dealt directly to player with no attacker
    public AttackSequence(PlayerStats target, int damage, int AP)
    {
        this.target = target;   
        attackRolled = true;
        reactionRolled = true;
        skipAttack = true;
        flatDamage = damage;
        flatAP = AP;
        SoakRoll();
    }

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
            attackRolled = true;
            reactionRolled = true;
            this.skipAttack = true;
            SoakRoll();
        }
    }
    
    public void AttackRollComplete()
    {
        attackRolled = true;
    }
    public void ReactionRollComplete()
    {
        UIPlayerInfo.ShowAllInfo(attacker);
        reactionRolled = true;
        // See compare hits between attack and defense roll
        int netHits = attackRoll.GetHits();
        if (reactionRoll != null)
        {
            netHits -= reactionRoll.GetHits();
        } 
        // Attack misses if net hits is 0
        if(netHits < 0)
        {
            CombatLog.Log(target.GetName() + " avoids the attack!");
            PopUpText.CreateText("Missed", Color.yellow, target.gameObject);
            AttackMissed = true;
        }
        else if(netHits <= coverRange)
        {
            HitCover();
            AttackMissed = true;
        }
        // If attack hits, start a soak roll
        else
        {
            SoakRoll();    
        }
    }

    public void SoakRoll()
    {
        int armorPen = flatAP;
        if(ActiveWeapon != null)
        {
            armorPen = ActiveWeapon.GetAP();
        }
        int armorMod = target.GetAP();
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

    private void HitCover()
    {
        if(CoverTile != null && target.hasCondition(Condition.Covered))
        {
            CoverTile.HitCover(this);
        }
        else
        {
            CombatLog.Log("by getting 0 hits, " + target.GetName() + " suffers a grazing hit!");
            PopUpText.CreateText("Grazed!", Color.yellow, target.gameObject);
        }
    }
    
    public int GetNetHits()
    {
        if(skipAttack)
        {
            return 0;
        }
        return attackRoll.GetHits() - reactionRoll.GetHits();
    }

    public int GetWeaponDamage()
    {
        if(ActiveWeapon != null)
        {
            return ActiveWeapon.Template.damageBonus;
        }
        return flatDamage;
    }

    public void SoakRollComplete()
    {
        soakRolled = true;
    }
}

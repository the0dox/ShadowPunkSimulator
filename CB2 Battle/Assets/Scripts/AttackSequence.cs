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
        int shots = ActiveWeapon.ExpendAmmo(FireRate);
        if(attacks == 0)
        {
            if(TacticsAttack.Jammed(attackRoll.GetRoll(), ActiveWeapon, FireRate, attacker))
            {
                attacks = 0;
            }
            else if (attackRoll.Passed() || target.IsHelpless())
            {
                if(ActiveWeapon.IsWeaponClass("Melee"))
                {
                    attacks = 1;
                }
                else
                {
                    bool scatterDistance = Vector3.Distance(attacker.gameObject.transform.position, target.gameObject.transform.position) <= 3;
                    attacks = ActiveWeapon.GetAdditionalHits(FireRate, attackRoll.GetDOF(), shots, scatterDistance);
                }
            }
            else if(TacticsAttack.adjacencyBonus(target, attacker,ActiveWeapon) == -20)
            {
                int friendlyFireTarget = attackRoll.GetTarget() + 20; 
                Debug.Log("Could friendlyfire! under a " + friendlyFireTarget);
                if(attackRoll.GetRoll() <= friendlyFireTarget)
                {
                    PlayerStats[] adjacentTargets = target.GetComponent<TacticsMovement>().AdjacentPlayers().ToArray();
                    target.GetComponent<TacticsMovement>().PaintCurrentTile(false);
                    target = adjacentTargets[Random.Range(0,adjacentTargets.Length - 1)];
                    CombatLog.Log("By missing their target by 20, " + attacker.GetName() + " the shot hits " + target.GetName() + " instead!");
                    attacks = 1;
                } 
            }
            else
            {
                attacks = 0;
            }
        }
    }
    public void ReactionRollComplete()
    {
        reactionRolled = true;

        if (reactionRoll.Passed())
        {
            if(reactionRoll.GetSkillType().Equals("Dodge"))
            {
                int dodgedAttacks = reactionRoll.GetDOF() + 1;
                attacks -= dodgedAttacks;
                CombatLog.Log(target.GetName() + " dodges " + dodgedAttacks + " attack(s)");
            }
            else
            {
                attacks--;
                CombatLog.Log(target.GetName() + " parries the incoming attack!");
                if(target.PowerFieldAbility())
                {
                    PopUpText.CreateText(ActiveWeapon.GetName() + " Shattered!", Color.red, attacker.gameObject);
                    CombatLog.Log(target.GetName() + "'s power field shatters the attackers weapon!");
                    attacker.Unequip(ActiveWeapon);
                    attacker.equipment.Remove(ActiveWeapon);
                }
            }
        }
        else
        {
            CombatLog.Log(target.GetName() + " fails to avoid the incoming attack!");
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using UnityEngine.UI;

// static class used to calculate attack rolls
public static class TacticsAttack
{
    public static Dictionary<string, int> AmmoExpenditure = new Dictionary<string, int>(){
        {"SS",1},{"SA",1},{"SAB", 3},{"BF",3},{"LB",6},{"FA",6},{"FAB",10}
    };
    // target: stats of the defender
    // mystats: stats of the attacker
    // w: weapon used in the attack
    // type: the fire rate of w
    // creates an attacksequence objects that contains a single attack sequence between an attacker and a target
    public static AttackSequence Attack(PlayerStats target, PlayerStats myStats, Weapon w, string type)
    {
        AttackSequence output = new AttackSequence(target,myStats,w,type,0,false);

        int totalModifiers = 0;
        Dictionary<string,int> modifiers = ApplyModifiers(output,true);
        
        foreach(string key in modifiers.Keys)
        {
            totalModifiers += modifiers[key];
        }

        // expend ammo & increase recoil
        if(w.IsWeaponClass(WeaponClass.ranged))
        {
            w.ExpendAmmo(AmmoExpenditure[type]);
            myStats.IncreaseRecoilPenalty(AmmoExpenditure[type]);
        }

        // Save cover 
        Tile coverTile = GetCoverTile(myStats.gameObject, target.gameObject);
        if(coverTile != null)
        {
            output.CoverTile = coverTile;
        }

        RollResult attackRoll = new RollResult(myStats.myData, w, totalModifiers);
        output.attackRoll = attackRoll;
        return output;
    }

    public static void Defend(AttackSequence CurrentAttack, int modifiers = 0)
    {
        int totalModifiers = modifiers;
        Dictionary<string, int> conditionalModifiers = ApplyModifiers(CurrentAttack,false);
        foreach(string key in conditionalModifiers.Keys)
        {
            totalModifiers += conditionalModifiers[key];
        }
        //apply extra defense penalties to target
        CurrentAttack.target.IncreaseDefensePenality();
        CurrentAttack.reactionRoll = CurrentAttack.target.AbilityCheck(AttributeKey.Reaction, AttributeKey.Intuition, AttributeKey.PhysicalLimit,"defense",0, totalModifiers);
    }

    public static void DealDamage(AttackSequence currentAttack)
    {
        int damageBonus = currentAttack.attackRoll.GetHits() - currentAttack.reactionRoll.GetHits() - currentAttack.soakRoll.GetHits();
        int weaponDamage = currentAttack.ActiveWeapon.GetDamage();
        if(currentAttack.ActiveWeapon.IsWeaponClass(WeaponClass.melee))
        {
            weaponDamage += currentAttack.attacker.myData.GetAttribute(AttributeKey.Strength);
        }
        int totalDamage = damageBonus + weaponDamage;
        if(totalDamage < 0)
        {
            totalDamage = 0;
        }
        if(currentAttack.ActiveWeapon.Template.Lethal)
        {
            currentAttack.target.takeDamage(totalDamage);
            PopUpText.CreateText("-" + (damageBonus + weaponDamage), Color.red, currentAttack.target.gameObject); 
        }
        else
        {
            currentAttack.target.takeStun(totalDamage);
            PopUpText.CreateText("-" + (damageBonus + weaponDamage), Color.cyan, currentAttack.target.gameObject); 
        }
        CombatLog.Log(currentAttack.ActiveWeapon.GetName() + " damage roll:\n "+ weaponDamage + " from base weapon damage\n +" + (currentAttack.attackRoll.GetHits() - currentAttack.reactionRoll.GetHits()) + " from net attack hits\n -" + currentAttack.soakRoll.GetHits() + " from resist hits\n " + currentAttack.target.GetName() + " suffers " + totalDamage + " damage");
        
    }

    public static void DealDamage(PlayerStats target, PlayerStats myStats, string hitBodyPart, Weapon w)
    {
        throw new System.NotImplementedException();
    }

    public static bool HasValidTarget(PlayerStats target, PlayerStats myStats, Weapon w)
    {
        //if they are on the same team
        if (target.GetTeam() == myStats.GetTeam()){
            //CombatLog.Log(myStats.GetName() + " (team " + myStats.GetTeam() + ": But thats my friend!"); 
            return false;
        }
        int distance = Mathf.CeilToInt(Vector3.Distance(myStats.transform.position, target.transform.position));

        if(!w.Template.rangeClass.withinRange(distance))
        {
            return false;
        }
        
        return HasLOS(target.gameObject, myStats.gameObject);
    }

    public static bool HasLOS(GameObject target, GameObject attacker)
    {
            Vector3 myPOV = attacker.transform.position + new Vector3(0, attacker.GetComponent<Collider>().bounds.extents.y/2,0);
            Vector3 TargetPOV = target.transform.position + new Vector3(0, target.GetComponent<Collider>().bounds.extents.y/2,0);
            RaycastHit interceptPoint;
            // check to see if any tile are between target and defender
            if(Physics.Linecast(myPOV, TargetPOV, out interceptPoint, LayerMask.GetMask("Obstacle")))
            {
                Tile BlockingTile = interceptPoint.collider.GetComponent<Tile>();
                // If blocking tile can be ignored by attacker, ignore it and see if the rest of LOS is obscured
                if(IgnoreTile(myPOV, BlockingTile))
                {
                    // temporaryly disable collision with "safe" tile to look through it
                    BlockingTile.GetComponent<Collider>().enabled = false;
                    if(Physics.Linecast(BlockingTile.transform.position, TargetPOV, out interceptPoint, LayerMask.GetMask("Obstacle")))
                    {
                        BlockingTile.GetComponent<Collider>().enabled = true;
                        // if the second line hits, it can only be ignored if adjacacnet to the target itself
                        if(IgnoreTile(TargetPOV, interceptPoint.collider.GetComponent<Tile>()))
                        {
                            return true;
                        }
                        return false;
                    }
                    // if the second line doesn't hit, then its a valid shot
                    BlockingTile.GetComponent<Collider>().enabled = true;
                    return true;
                }
                // If the only blocking tile can be ignored by the defender than we can target them
                else if(IgnoreTile(TargetPOV, BlockingTile))
                {
                    return true;
                }
                // If blocks cannot be ignored attack is invalid
                return false;
            }
            // if no tiles are present the attack is valid
            return true;
    }

    // Tiles can be ignored if they are directly adjacent to origin. But ONLY if they are not adjacent to two other blocks on each side
    private static bool IgnoreTile(Vector3 origin, Tile BlockingTile)
    {
        //Debug.Log("evaluating tile at " + BlockingTile.transform.position + " to be ignored");
        // if the blocking tile is too far to block 
        if(Vector3.Distance(origin, BlockingTile.transform.position) > 1.5f)
        {
            //Debug.Log("tile is too far to be cover");
            return false;
        }
        Vector3 relative = BlockingTile.transform.InverseTransformPoint(origin); 
        Quaternion rightRotation = Quaternion.Euler(0,90,0);
        Quaternion leftRotation = Quaternion.Euler(0,-90,0);
        Vector3 startingDirection = Vector3.forward;
        Vector3 OriginDir = relative.normalized;
        Vector3 LeftDir = leftRotation * OriginDir;
        Vector3 RightDir = rightRotation * OriginDir;
        Debug.DrawRay(BlockingTile.transform.position,relative,Color.blue, 3);
        Debug.DrawRay(BlockingTile.transform.position,LeftDir,Color.green, 3);
        Debug.DrawRay(BlockingTile.transform.position, RightDir, Color.red, 3);
        RaycastHit debugHit;
        if(Physics.Raycast(BlockingTile.transform.position, LeftDir, out debugHit, 1, LayerMask.GetMask("Obstacle")))
        {

            //Debug.Log("cover is blocked to its left " + debugHit.collider.transform.position);
            if(Physics.Raycast(BlockingTile.transform.position, RightDir, out debugHit, 1,LayerMask.GetMask("Obstacle")))
            {
                //Debug.Log("cover is also blocked from its right, cannot be ignored " + debugHit.collider.transform.position);
                {
                    return false;
                }
            }
        }
        return true;
    }


    public static Tile GetCoverTile(GameObject attacker,  GameObject target)
    {

        RaycastHit hit;
        Tile tile;

        if (Physics.Raycast(target.transform.position, Vector3.left, out hit, 1))
        {
            tile = hit.collider.GetComponent<Tile>(); 
            if (tile != null)
            {
                Vector3 relative = tile.transform.InverseTransformPoint(attacker.transform.position);
                float angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
                int roundedAngle = Mathf.RoundToInt(angle);

                if (roundedAngle < 0)
                {                
                    //CombatLog.Log("Covered from the left");
                    return tile;
                }
            }
        }
        if (Physics.Raycast(target.transform.position, Vector3.right, out hit, 1))
        {
            tile = hit.collider.GetComponent<Tile>(); 
            if (tile != null)
            {
                Vector3 relative = tile.transform.InverseTransformPoint(attacker.transform.position);
                float angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
                int roundedAngle = Mathf.RoundToInt(angle);

                if (roundedAngle > 0 && roundedAngle < 180)
                {                
                    //CombatLog.Log("Covered from the right");
                    return tile;
                }
            }
        }
        if (Physics.Raycast(target.transform.position, Vector3.forward, out hit, 1))
        {
            tile = hit.collider.GetComponent<Tile>(); 
            if (tile != null)
            {
                Vector3 relative = tile.transform.InverseTransformPoint(attacker.transform.position);
                float angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
                int roundedAngle = Mathf.RoundToInt(angle);

                if (roundedAngle > -90 && roundedAngle < 90 )
                {                
                    //CombatLog.Log("Covered from the front");
                    return tile;
                }
            }
        }
        if (Physics.Raycast(target.transform.position, Vector3.back, out hit, 1))
        {
            tile = hit.collider.GetComponent<Tile>(); 
            if (tile != null)
            {
                Vector3 relative = tile.transform.InverseTransformPoint(attacker.transform.position);
                float angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
                int roundedAngle = Mathf.RoundToInt(angle);

                if (roundedAngle < -90 || roundedAngle > 90)
                {                
                    //CombatLog.Log("Covered from the back");
                    return tile;
                }
            }
        }
        return null;
    }

    public static int CalculateHeightAdvantage(PlayerStats target, PlayerStats attacker)
   {
        Vector3 difference = attacker.transform.position - target.transform.position;
        float HeightDifference = difference.y;
        if (HeightDifference > 2.0f)
        {
            return 10;
        }
        else
        {
            return 0;
        }
   }

    public static List<string> GenerateTooltip(PlayerStats target, PlayerStats myStats, Weapon w, string type)
    {
        AttackSequence tempAttackSequence = new AttackSequence(target,myStats, w, type, 0, false);
        List<string> output = new List<string>();
        if(target.GetTeam() == myStats.GetTeam())
        {
            output.Add("Invalid Target: Ally");
        }
        else
        {
            Stack<string> outputStack = new Stack<string>();
            int attackDice = 0;
            int defenseDice = 0;
            
            Dictionary<string, int> modifiersDefensive = ApplyModifiers(tempAttackSequence,false);
            foreach(string key in modifiersDefensive.Keys)
            {
                int modifier = modifiersDefensive[key];
                string addition = "";
                if(modifier > 0)
                {
                    addition = "+";
                }
                if(modifier != 0)
                {
                    outputStack.Push(addition + modifier + " from " + key);
                    defenseDice += modifier;
                }
            }
            // bonus from attribute
            defenseDice += target.myData.GetAttribute(AttributeKey.Reaction) + target.myData.GetAttribute(AttributeKey.Intuition);
            outputStack.Push("Total Defense Dice: " + defenseDice);
            outputStack.Push("------------");

            Dictionary<string, int> modifiers = ApplyModifiers(tempAttackSequence,true);
            foreach(string key in modifiers.Keys)
            {
                int modifier = modifiers[key];
                string addition = "";
                if(modifier > 0)
                {
                    addition = "+";
                }
                if(modifier != 0)
                {
                    outputStack.Push(addition + modifier + " from " + key);
                    attackDice += modifier;
                }
            }
            // bonus from attribute
            int attributeDice = myStats.myData.GetAttribute(w.Template.WeaponSkill.derrivedAttribute);
            outputStack.Push("+" + attributeDice + " from " + w.Template.WeaponSkill.derrivedAttribute + " attribute");
            attackDice += attributeDice;
            // bonus from skill
            int skillDice = myStats.myData.GetAttribute(w.Template.WeaponSkill.skillKey);
            outputStack.Push("+" + skillDice + " from " + w.Template.WeaponSkill.name + " skill");
            attackDice += skillDice;

            

            outputStack.Push("Accuracy Limit: " + w.Template.accuracy);

            outputStack.Push("Total Attack Dice: " + attackDice);

            while(outputStack.Count > 0)
            {
                output.Add(outputStack.Pop());
            }    
        }
        return output;
    }

    public static int adjacencyBonus(PlayerStats target, PlayerStats attacker, Weapon w)
    {
        List<PlayerStats> adjacentPlayers = target.GetComponent<TacticsMovement>().AdjacentPlayers();
        int adjacentEnemies = 0;
        foreach(PlayerStats ps in adjacentPlayers)
        {
            if (ps.GetTeam() == attacker.GetTeam())
            {
                adjacentEnemies++;
            }
        }
        if(w.IsWeaponClass(WeaponClass.melee))
        {
            if(adjacentEnemies > 2)
            {
                return 20;
            }
            else if(adjacentEnemies > 1)
            {
                return 10;
            }
        }   
        else if(adjacentEnemies > 0)
        {
            if(adjacentPlayers.Contains(attacker))
            {
                return 0;
            }
            else
            {
                return -20;
            }
        }
        return 0;
    }

    private static Dictionary<string,int> ApplyModifiers(AttackSequence thisAttack, bool type)
    {
        Dictionary<string,int> modifiers = new Dictionary<string, int>();
        modifiers.Add(" from enemy fire rate", ROFDefensePenalty(thisAttack,type));
        modifiers.Add(" from recoil", recoilPenalty(thisAttack,type));
        modifiers.Add(" from specialization", fromSpecialization(thisAttack,type));
        modifiers.Add(" from defense penalty", GetDefensePenality(thisAttack,type));
        modifiers.Add(" from range", rangePenalty(thisAttack,type));
        modifiers.Add(" from aiming", fromAiming(thisAttack,type));
        modifiers.Add(" from low ammo", InsufficientBulletPenalty(thisAttack,type));
        modifiers.Add(" from reach", GetReachBonus(thisAttack,type));
        modifiers.Add(" from running", GetChargingBonus(thisAttack,type));
        modifiers.Add(" from prone condition", GetPronePenalty(thisAttack,type));
        modifiers.Add(" from prone target", GetProneTargetPenalty(thisAttack,type));
        modifiers.Add(" from cover", GetCoverBonus(thisAttack,type));
        return modifiers;
    }

    private static int rangePenalty(AttackSequence thisAttack, bool attack)
    {
        if(!attack || thisAttack.ActiveWeapon.IsWeaponClass(WeaponClass.melee))
        {
            return 0;
        }
        int distance = Mathf.RoundToInt(Vector3.Distance(thisAttack.attacker.transform.position, thisAttack.target.transform.position));
        Debug.Log("distance " + distance);
        return thisAttack.ActiveWeapon.Template.rangeClass.GetRangePenalty(distance);
    }

    private static int fromSpecialization(AttackSequence thisAttack, bool attack)
    {
        if(attack && thisAttack.attacker.myData.hasSpecialization(thisAttack.ActiveWeapon.Template.WeaponSkill.name,thisAttack.ActiveWeapon.Template.WeaponSpecialization))
        {
            return 2;
        }
        return 0;
    }

    private static int fromAiming(AttackSequence thisAttack, bool attack)
    {
        if(attack && thisAttack.attacker.hasCondition(Condition.Aiming))
        {
            return 1;
        }
        return 0;
    }

    private static int recoilPenalty(AttackSequence thisAttack, bool attack)
    {
        if(!attack || thisAttack.ActiveWeapon.IsWeaponClass(WeaponClass.melee))
        {
            return 0;
        }
        return thisAttack.attacker.CalculateRecoilPenalty(AmmoExpenditure[thisAttack.FireRate]);
    }

    public static int ROFDefensePenalty(AttackSequence thisAttack, bool attack)
    {
        if(attack || thisAttack.ActiveWeapon.IsWeaponClass(WeaponClass.melee))
        {
            return 0;
        }
        return 1-AmmoExpenditure[thisAttack.FireRate];
    } 

    private static int InsufficientBulletPenalty(AttackSequence thisAttack, bool attack)
    {
        if(!attack || thisAttack.ActiveWeapon.IsWeaponClass(WeaponClass.melee))
        {
            return 0;
        }
        int requiredBullets = AmmoExpenditure[thisAttack.FireRate];
        int availableBullets = thisAttack.ActiveWeapon.getClip();
        if(requiredBullets <= availableBullets)
        {
            return 0;
        }
        else
        {
            return availableBullets - requiredBullets;
        }
    }

    private static int GetDefensePenality(AttackSequence thisAttack, bool attack)
    {
        if(attack)
        {
            return 0;
        }
        return thisAttack.target.GetDefensePenality();
    }

    private static int GetReachBonus(AttackSequence thisAttack, bool attack)
    {
        if(attack || !thisAttack.ActiveWeapon.IsWeaponClass(WeaponClass.melee))
        {
            return 0;
        }
        int attackerReach = thisAttack.ActiveWeapon.Template.rangeClass.getReach();
        int defenderReach = 0;
        int primaryReach = 0;
        int secondaryReach = 0;
        if(thisAttack.target.PrimaryWeapon != null && thisAttack.target.PrimaryWeapon.IsWeaponClass(WeaponClass.melee))
        {
            primaryReach = thisAttack.target.PrimaryWeapon.Template.rangeClass.getReach();
        }
        if(thisAttack.target.SecondaryWeapon != null && thisAttack.target.SecondaryWeapon.IsWeaponClass(WeaponClass.melee))
        {
            secondaryReach = thisAttack.target.PrimaryWeapon.Template.rangeClass.getReach();
        }
        if(primaryReach > secondaryReach)
        {
            defenderReach = primaryReach;
        }
        else
        {
            defenderReach = secondaryReach;
        }
        int reachDifference = defenderReach - attackerReach;
        if(reachDifference > 0)
        {
            return reachDifference;
        }
        return 0;
    }

    private static int GetChargingBonus(AttackSequence thisAttack, bool attack)
    {
        if(attack)
        {
            if(thisAttack.attacker.hasCondition(Condition.Running))
            {
                if(thisAttack.ActiveWeapon.IsWeaponClass(WeaponClass.melee))
                {
                    return 2;
                }
                else
                {
                    return -2;
                }
            }
        }
        else if(thisAttack.target.hasCondition(Condition.Running))
        {
            return -2;
        }
        return 0;
    }

    private static int GetPronePenalty(AttackSequence thisAttack, bool attack)
    {
        if(attack && thisAttack.attacker.hasCondition(Condition.Prone))
        {
            return -1;
        }
        return 0;
    }
    
    private static int GetProneTargetPenalty(AttackSequence thisAttack, bool attack)
    {
        if(thisAttack.target.hasCondition(Condition.Prone) && thisAttack.ActiveWeapon.IsWeaponClass(WeaponClass.melee))
        {
            if(attack)
            {
                return 1;
            }
            else
            {
                return -2; 
            }
        }
        return 0;
    }

    private static int GetCoverBonus(AttackSequence thisAttack, bool attack)
    {
        Tile cover = GetCoverTile(thisAttack.attacker.gameObject, thisAttack.target.gameObject);
        if(!attack)
        {
            if(thisAttack.target.hasCondition(Condition.Covered) && cover != null)
            {
                if(BoardBehavior.isAdvancedCover(cover))
                {
                    return 4;
                }
                return 2;
            }
        }
        return 0;
    }
}

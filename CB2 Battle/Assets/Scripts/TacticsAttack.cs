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
        if(!DmMenu.debugMode)
        { 
            if(w.IsWeaponClass(WeaponClass.ranged))
            {
                w.ExpendAmmo(AmmoExpenditure[type]);
                if(!w.HasWeaponAttribute("recoilless") && !type.Equals("SS"))
                {
                    myStats.IncreaseRecoilPenalty(AmmoExpenditure[type]);
                }
            }
        }

        // Save cover 
        Tile interceptingTile = GetCoverTile(myStats.transform.position, target.transform.position, true);
        PlayerStats interceptingPlayer = IsDefendedByRook(myStats, target);
        if(interceptingTile != null)
        {
            if(interceptingTile.IsStackedTile())
            {
                output.coverRange = 4;
            }
            else
            {
                output.coverRange = 2;
            }
            output.interceptingGameobject = interceptingTile.gameObject;
        }
        else if(interceptingPlayer != null)
        {
            output.coverRange = 4;
            output.interceptingGameobject = interceptingPlayer.gameObject;
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
        // no modifiers in debug mode
        if(!DmMenu.debugMode)
        {
            CurrentAttack.target.IncreaseDefensePenality();
        }
        CurrentAttack.reactionRoll = CurrentAttack.target.AbilityCheck(AttributeKey.Reaction, AttributeKey.Intuition, AttributeKey.PhysicalLimit,"defense",0, totalModifiers);
    }


    public static void DealDamage(AttackSequence currentAttack)
    {
        int damage = 0;
        int ap = 0;
        string damageString = "";
        if(currentAttack.ActiveWeapon == null)
        {
            damage = currentAttack.flatDamage;
            ap = currentAttack.flatAP;
            damageString = " damage: " + damage;
        }
        else
        {
            int roll = currentAttack.ActiveWeapon.RollDamage();
            damage += roll;
            string rollString = "<" + roll + "> + ";
            ap = currentAttack.ActiveWeapon.GetAP();
            if(currentAttack.ActiveWeapon.IsWeaponClass(WeaponClass.melee))
            {
                int strengthBonus = currentAttack.attacker.myData.GetAttribute(AttributeKey.Strength);
                damage += strengthBonus;
                damageString = " damage roll:\n " + currentAttack.ActiveWeapon.DisplayDamageRange() + " = " + rollString + strengthBonus + " = " + damage;
            }
            else
            {
                damage += currentAttack.ActiveWeapon.GetDamageBonus();
                damageString = " damage roll:\n " + currentAttack.ActiveWeapon.DisplayDamageRange() + " = " + rollString + currentAttack.ActiveWeapon.GetDamageBonus() + " = " + damage;
        
            }
        }
        // calculate armor
        int damageSoak = CalculateSoak(currentAttack);
        // subtract total damage by armor
        damage -= damageSoak;
        // damage can never be reduced to 0
        if(damage  < 1)
        {
            damage = 1;
        }
        // apply damage
        if(currentAttack.ActiveWeapon != null && !currentAttack.ActiveWeapon.Template.Lethal)
        {
            currentAttack.target.takeStun(damage);
            PopUpText.CreateText("-" + (damage), Color.cyan, currentAttack.target.gameObject); 
        }
        else
        {
            currentAttack.target.takeDamage(damage);
            PopUpText.CreateText("-" + (damage), Color.red, currentAttack.target.gameObject);
        }
        CombatLog.Log(damageString + "\n -" + damageSoak + " from armor\n " + currentAttack.target.GetName() + " suffers " + damage + " damage");
        ApplyCalledShots(currentAttack);
    }

    public static int CalculateSoak(AttackSequence currentAttack)
    {
        int armor = currentAttack.target.GetAP();
        if(currentAttack.attacker != null && TargetIsBlocking(currentAttack.attacker,currentAttack.target))
        {
            armor += 3;
        }
        // calculate armor pen last
        if(currentAttack.ActiveWeapon != null)
        {
            int pen = currentAttack.ActiveWeapon.GetAP();
            // if positive pen
            if(pen > 0)
            {
                // if pen would make armor negative, set armor to 0
                if(pen > armor)
                {
                    armor = 0;
                }
                // otherwise just subtrack
                else
                {
                    armor -= pen;
                }
            }
            // if negative pen and target has armor, add armor
            else if (armor > 0)
            {
                armor -= pen;
            }
        }
        return armor;
    }

    public static void ApplyCalledShots(AttackSequence currentAttack)
    {
        bool apply = false;
        if(currentAttack.ActiveWeapon != null)
        {
            if(currentAttack.attacker.hasCondition(Condition.Disarm))
            {
                string popuptext = " has no weapon to disarm";
                if(currentAttack.target.PrimaryWeapon != null)
                {
                    popuptext = "'s " + currentAttack.target.PrimaryWeapon.GetName() + " is dropped!";
                    currentAttack.target.Unequip(currentAttack.target.PrimaryWeapon);
                }
                else if(currentAttack.attacker.SecondaryWeapon != null)
                {
                    popuptext = "'s " + currentAttack.target.SecondaryWeapon.GetName() + " is dropped!";
                    currentAttack.target.Unequip(currentAttack.target.SecondaryWeapon);
                }
                CombatLog.Log(currentAttack.target.GetName() + popuptext); 
                apply = true;
            }
            else if(currentAttack.attacker.hasCondition(Condition.ShakeUp))
            {
                TurnManager.instance.SubtractIniative(currentAttack.target, 5);
                PopUpText.CreateText("Shaken!", Color.yellow, currentAttack.target.gameObject);
                CombatLog.Log(currentAttack.target.GetName() + " loses 5 initative");
                apply = true;
            }
            else if(currentAttack.attacker.hasCondition(Condition.KnockDown) && currentAttack.ActiveWeapon.IsWeaponClass(WeaponClass.melee))
            {
                int attackerKnockHits = currentAttack.attackRoll.GetDOF() + currentAttack.attacker.myData.GetAttribute(AttributeKey.Strength);
                int defenderKnockLimit = currentAttack.target.myData.GetAttribute(AttributeKey.PhysicalLimit);
                CombatLog.Log("(Strength + hits) = " + attackerKnockHits + "(physical limit) = " + defenderKnockLimit);
                if(attackerKnockHits >= defenderKnockLimit)
                {
                    currentAttack.target.SetCondition(Condition.Prone, 0, true);
                    CombatLog.Log("prone attack exceeds physical limit and knocks " + currentAttack.target.GetName() + " prone");
                }
                PopUpText.CreateText("Resisted", Color.yellow, currentAttack.target.gameObject);    
                CombatLog.Log("prone attack fails to reach physical limit");
                apply = true;
            }
        }
        // if they sucessfully land a calledshot
        if(apply)
        {
            if(currentAttack.attacker.myData.hasTalent(TalentKey.Desperado))
            {
                CombatLog.Log("By landing a called shot " + currentAttack.attacker.GetName() + " enters momentum!");
                currentAttack.attacker.SetCondition(Condition.Momentum, -1, true);
            }
        }
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

    public static bool TargetIsBlocking(PlayerStats attacker, PlayerStats target)
    {
        // inveresetransform point is inconsistent if the rotation is off, 
        int roundedAngle = GetRelativeAngle(attacker.transform.position, target.transform);
        if (target.hasCondition(Condition.RookLeft))
        {
            if (roundedAngle < 0)
            {                
                return true;
            }
        }
        if (target.hasCondition(Condition.RookRight))
        {
            if (roundedAngle > 0 && roundedAngle < 180)
            {                
                return true;
            }
        }
        if (target.hasCondition(Condition.RookUp))
        {
            if (roundedAngle > -90 && roundedAngle < 90 )
            {                
                return true;
            }
        }
        if (target.hasCondition(Condition.RookDown))
        {
            if (roundedAngle < -90 || roundedAngle > 90)
            {                
                return true;
            }
        }
        return false;
    }


    public static Tile GetCoverTile(Vector3 attacker,  Vector3 target, bool repeat)
    {
        RaycastHit hit;
        Tile tile;
        Vector3 targetnormpos = new Vector3(target.x, Mathf.FloorToInt(target.y) + 1, target.z);
        if (Physics.Raycast(targetnormpos, Vector3.left, out hit, 1))
        {
            tile = hit.collider.GetComponent<Tile>(); 
            if (tile != null)
            {
                int roundedAngle = GetRelativeAngle(attacker, tile.transform);;
                Debug.Log(roundedAngle);
                if (roundedAngle < 0)
                {                
                    return tile;
                }
            }
        }
        if (Physics.Raycast(targetnormpos, Vector3.right, out hit, 1))
        {
            tile = hit.collider.GetComponent<Tile>(); 
            if (tile != null)
            {   
                int roundedAngle = GetRelativeAngle(attacker, tile.transform);;
                if (roundedAngle > 0 && roundedAngle < 180)
                {                
                    return tile;
                }
            }
        }
        if (Physics.Raycast(targetnormpos, Vector3.forward, out hit, 1))
        {
            tile = hit.collider.GetComponent<Tile>(); 
            if (tile != null)
            {   
                int roundedAngle = GetRelativeAngle(attacker, tile.transform);;
                if (roundedAngle > -90 && roundedAngle < 90 )
                {                
                    return tile;
                }
            }
        }
        if (Physics.Raycast(targetnormpos, Vector3.back, out hit, 1))
        {
            tile = hit.collider.GetComponent<Tile>(); 
            if (tile != null)
            {    
                int roundedAngle = GetRelativeAngle(attacker, tile.transform);;
                if (roundedAngle < -90 || roundedAngle > 90)
                {                
                    return tile;
                }
            }
        }
        if(repeat)
        {
            return GetCoverTile(attacker, target + new Vector3(0,-1,0), false);
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

            if(TargetIsBlocking(myStats,target))
            {
                outputStack.Push("TargetBlocking");
            }
            if(IsDefendedByRook(myStats,target))
            {
                outputStack.Push("Defended by Rook");
            }

            int attackDice = 0;
            int defenseDice = 0;
            // bonus from attribute
            
            Dictionary<string, int> defenseModifiers = ApplyModifiers(tempAttackSequence,false);
            if(!target.myData.isMinion)
            {
                defenseDice += target.myData.GetAttribute(AttributeKey.Reaction) + target.myData.GetAttribute(AttributeKey.Intuition);
            }
            else
            {
                defenseDice += target.myData.GetAttribute(AttributeKey.DroneHandling) + target.myData.getOwner().myData.GetAttribute(AttributeKey.Pilot);
            }
            foreach(string key in defenseModifiers.Keys)
            {
                int modifier = defenseModifiers[key];
                defenseDice += modifier;
            }
            outputStack.Push(GetCoverTooltip(tempAttackSequence));
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
            int attributeDice = 0;
            int skillDice = 0;
            //drones calcualte attacks differently:
            if(!myStats.myData.isMinion)
            {
                // bonus from attribute
                attributeDice = myStats.myData.GetAttribute(w.Template.WeaponSkill.derrivedAttribute);
                // bonus from skill
                skillDice = myStats.myData.GetAttribute(w.Template.WeaponSkill.skillKey);
            }
            else
            {
                 // bonus from attribute
                attributeDice = myStats.myData.GetAttribute(AttributeKey.DronePiloting);
                // bonus from skill
                skillDice = myStats.myData.getOwner().myData.GetAttribute(AttributeKey.Gunnery);
            }
            
            outputStack.Push("Base Attack Pool: " + (attributeDice + skillDice));
            attackDice += (attributeDice + skillDice);
            outputStack.Push("Accuracy Limit: " + w.Template.accuracy);
            outputStack.Push("Opposed by Defense: " + defenseDice);
            outputStack.Push("Total Attack Dice: " + attackDice);

            while(outputStack.Count > 0)
            {
                output.Add(outputStack.Pop());
            }    
        }
        return output;
    }

    public static PlayerStats IsDefendedByRook(PlayerStats attacker, PlayerStats target)
    {
        RaycastHit hit;
        PlayerStats interceptingPlayer;
        Vector3 targetnormpos = new Vector3(target.transform.position.x, Mathf.FloorToInt(target.transform.position.y) + 1, target.transform.position.z);
        if (Physics.Raycast(targetnormpos, Vector3.left, out hit, 1))
        {
            //Debug.Log("hit something to my left");
            interceptingPlayer = hit.collider.GetComponent<PlayerStats>(); 
            if (interceptingPlayer != null && interceptingPlayer.GetTeam() == target.GetTeam() && interceptingPlayer.hasCondition(Condition.RookLeft))
            {
                int roundedAngle = GetRelativeAngle(attacker.transform.position, interceptingPlayer.transform);
                if (roundedAngle < 0)
                {                
                    return interceptingPlayer;
                }
            }
        }
        if (Physics.Raycast(targetnormpos, Vector3.right, out hit, 1))
        {
            //Debug.Log("hit something to my right");
            interceptingPlayer = hit.collider.GetComponent<PlayerStats>(); 
            if (interceptingPlayer != null && interceptingPlayer.GetTeam() == target.GetTeam() && interceptingPlayer.hasCondition(Condition.RookRight))
            {
                int roundedAngle = GetRelativeAngle(attacker.transform.position, interceptingPlayer.transform);
                if (roundedAngle > 0 && roundedAngle < 180)
                {                
                    return interceptingPlayer;
                }
            }
        }
        if (Physics.Raycast(targetnormpos, Vector3.forward, out hit, 1))
        {
            //Debug.Log("hit something to my up");
            interceptingPlayer = hit.collider.GetComponent<PlayerStats>(); 
            if (interceptingPlayer != null && interceptingPlayer.GetTeam() == target.GetTeam() && interceptingPlayer.hasCondition(Condition.RookUp))
            {
                int roundedAngle = GetRelativeAngle(attacker.transform.position, interceptingPlayer.transform);
                //Debug.Log("incoming angle: " + roundedAngle);
                if (roundedAngle > -90 && roundedAngle < 90 )
                {                
                    return interceptingPlayer;
                }
            }
        }
        if (Physics.Raycast(targetnormpos, Vector3.back, out hit, 1))
        {
            //Debug.Log("hit something to my down");
            interceptingPlayer = hit.collider.GetComponent<PlayerStats>(); 
            if (interceptingPlayer != null && interceptingPlayer.GetTeam() == target.GetTeam() && interceptingPlayer.hasCondition(Condition.RookDown))
            {
                int roundedAngle = GetRelativeAngle(attacker.transform.position, interceptingPlayer.transform);
                if (roundedAngle < -90 || roundedAngle > 90)
                {                
                    return interceptingPlayer;
                }
            }
        }
        return null;
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

    private static int GetRelativeAngle(Vector3 incomingposition, Transform defendingtransform)
    {
        Vector3 prevRotation = defendingtransform.transform.eulerAngles;
        defendingtransform.transform.eulerAngles = Vector3.zero;
        Vector3 relative = defendingtransform.transform.InverseTransformPoint(incomingposition);
        defendingtransform.transform.eulerAngles = prevRotation;
        float angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
        return Mathf.RoundToInt(angle);
    }

    public static Dictionary<string,int> ApplyModifiers(AttackSequence thisAttack, bool type)
    {
        Dictionary<string,int> modifiers = new Dictionary<string, int>();
        modifiers.Add("enemy fire rate", ROFDefensePenalty(thisAttack,type));
        modifiers.Add("recoil", recoilPenalty(thisAttack,type));
        modifiers.Add("specialization", fromSpecialization(thisAttack,type));
        modifiers.Add("defense penalty", GetDefensePenality(thisAttack,type));
        modifiers.Add("range", rangePenalty(thisAttack,type));
        modifiers.Add("aiming", fromAiming(thisAttack,type));
        modifiers.Add("low ammo", InsufficientBulletPenalty(thisAttack,type));
        modifiers.Add("reach", GetReachBonus(thisAttack,type));
        modifiers.Add("running", GetChargingBonus(thisAttack,type));
        modifiers.Add("prone", GetPronePenalty(thisAttack,type));
        modifiers.Add("prone target", GetProneTargetPenalty(thisAttack,type));
        modifiers.Add("full defense", GetFulldefenseBonus(thisAttack, type));
        modifiers.Add("called shot",GetCalledShotPenalty(thisAttack,type));
        modifiers.Add("presence", GetIntimidationPenalty(thisAttack, type));
        modifiers.Add("direct", GetDirectBonus(thisAttack,type));
        modifiers.Add("flanking",GetFlankingPenalty(thisAttack,type));
        modifiers.Add("Momentum", GetMomentumPenalty(thisAttack, type));
        return modifiers;
    }

    private static int rangePenalty(AttackSequence thisAttack, bool attack)
    {
        if(!attack || thisAttack.ActiveWeapon.IsWeaponClass(WeaponClass.melee))
        {
            return 0;
        }
        int distance = Mathf.RoundToInt(Vector3.Distance(thisAttack.attacker.transform.position, thisAttack.target.transform.position));
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
            return 2;
        }
        return 0;
    }

    private static int GetPronePenalty(AttackSequence thisAttack, bool attack)
    {
        if(attack && thisAttack.attacker.hasCondition(Condition.Prone))
        {
            return -2;
        }
        return 0;
    }
    
    private static int GetProneTargetPenalty(AttackSequence thisAttack, bool attack)
    {
        if(thisAttack.target.hasCondition(Condition.Prone) && thisAttack.ActiveWeapon.IsWeaponClass(WeaponClass.melee))
        {
            if(attack)
            {
                return 2;
            }
            else
            {
                return -4; 
            }
        }
        return 0;
    }

    private static int GetFulldefenseBonus(AttackSequence thisAttack, bool attack)
    {
        if(!attack && thisAttack.target.hasCondition(Condition.FullDefense))
        {
            return thisAttack.target.myData.GetAttribute(AttributeKey.Willpower);
        }
        return 0;
    }

    private static string GetCoverTooltip(AttackSequence thisAttack)
    {
        Tile cover = GetCoverTile(thisAttack.attacker.transform.position, thisAttack.target.transform.position, true);
        if(cover != null)
        {
            // stacked cover provides more of a bonus
            if(cover.IsStackedTile())
            {
                return "target threshold: >4";
            }
            return "target threshold: >2";
        }
        if(IsDefendedByRook(thisAttack.attacker, thisAttack.target))
        {
            return "target threshold: >4";
        }
        return "target threshold: >0";
    }

    private static int GetCalledShotPenalty(AttackSequence thisAttack, bool attack)
    {
        if(attack && (thisAttack.attacker.hasCondition(Condition.Disarm) || thisAttack.attacker.hasCondition(Condition.ShakeUp) || thisAttack.attacker.hasCondition(Condition.KnockDown)))
        {
            return -4;
        }
        return 0;
    }

    private static int GetIntimidationPenalty(AttackSequence thisAttack, bool attack)
    {
        if(attack && thisAttack.attacker.hasCondition(Condition.Intimidated) && !thisAttack.target.hasCondition(Condition.Presence))
        {
            return -3;
        }
        return 0;
    }

    private static int GetDirectBonus(AttackSequence thisAttack, bool attack)
    {
        if(attack && thisAttack.attacker.hasCondition(Condition.Direction))
        {
            ConditionTemplate directionTemplate = ConditionsReference.GetTemplate(Condition.Direction);
            return thisAttack.attacker.Conditions[directionTemplate];
        }
        return 0;
    }

    private static int GetFlankingPenalty(AttackSequence thisAttack, bool attack)
    {
        if(!attack && thisAttack.ActiveWeapon.IsWeaponClass(WeaponClass.ranged) && thisAttack.attacker.hasCondition(Condition.FocusFlank))
        {
            // if the target is up against cover AND is not covered from the incomming attack
            if(BoardBehavior.InCover(thisAttack.target.gameObject) && thisAttack.coverRange == 0)
            {
                return -2;
            }
        }
        return 0;
    }

    private static int GetMomentumPenalty(AttackSequence thisAttack, bool attack)
    {
        if(!attack && thisAttack.target.hasCondition(Condition.Momentum))
        {
            return 4;
        }
        return 0;
    }
}

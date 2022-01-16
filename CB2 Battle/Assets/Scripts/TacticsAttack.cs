using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using UnityEngine.UI;

// static class used to calculate attack rolls
public static class TacticsAttack
{
    private static Dictionary<string, int> AmmoExpenditure = new Dictionary<string, int>(){
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
        //add modiifers;
        //modifiers += adjacencyBonus(target,myStats, w);
        //modifiers += TacticsAttack.CalculateHeightAdvantage(target,myStats);
        // ranged weps get a bonus depending on distance to target
        //int attackModifiers = w.RangeBonus(target.transform, myStats) + modifiers;
        int modifiers = 0;
        if(!type.Equals("SS"))
        {
            modifiers += myStats.CalculateRecoilPenalty(AmmoExpenditure[type], true);
        }
        int distance = Mathf.RoundToInt(Vector3.Distance(myStats.transform.position, target.transform.position));
        modifiers += w.Template.rangeClass.GetRangePenalty(distance);

        w.ExpendAmmo(AmmoExpenditure[type]);
        RollResult attackRoll = new RollResult(myStats.myData, w, modifiers);
        output.attackRoll = attackRoll;
        return output;
    }

    public static void DealDamage(AttackSequence currentAttack)
    {
        int damageBonus = currentAttack.attackRoll.GetHits() - currentAttack.reactionRoll.GetHits() - currentAttack.soakRoll.GetHits();
        int weaponDamage = currentAttack.ActiveWeapon.GetDamage();
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

    public static void DealDamage(PlayerStats target, PlayerStats myStats, int netHits, Weapon w)
    {
        int totalDamage = w.Template.damageBonus + netHits;
        //int soakModifier = target.GetArmor() - w.GetAP();

        string hitBodyPart = "";
        DealDamage(target, myStats, hitBodyPart, w);
    }

    public static int ROFDefensePenalty(AttackSequence thisAttack)
    {
        return 1-AmmoExpenditure[thisAttack.FireRate];
    } 

    public static void DealDamage(PlayerStats target, PlayerStats myStats, string hitBodyPart, Weapon w)
    {
        Tile cover = CalculateCover(myStats.gameObject, target.gameObject, hitBodyPart);
        int damageRoll = w.GetDamage();
        int AP = target.GetAP();
        if(w.HasWeaponAttribute("Scatter"))
        {
            CombatLog.Log(w.GetName() + "'s scatter attribute makes armor twice as effective at long range!");
            AP *= 2;
        } 
        AP += myStats.GetAdvanceBonus(hitBodyPart);
        int soak = target.GetStat(AttributeKey.Body);
        if(cover != null && !w.HasWeaponAttribute("Flame") && !w.HasWeaponAttribute("Blast"))
        {
            AP += cover.CoverReduction(damageRoll, w.GetAP());
        } 
        AP -= w.GetAP();
        if(AP < 0)
        {
            AP = 0;
        }
        int result = damageRoll - AP - soak;
        if (result < 0)
        {
            result = 0;
        }
        if(w.HasWeaponAttribute("Unarmed"))
        {
            target.takeFatigue(1);
        }
        CombatLog.Log("Incoming damage is reduced by " + AP + " from armor/cover and " + soak + " Toughness for a total of " + result);
        PopUpText.CreateText("Hit " + hitBodyPart + "!: (-" + result + ")", Color.red, target.gameObject); 
        target.takeDamage(result, hitBodyPart,w.GetDamageType());
        
        target.ApplySpecialEffects(hitBodyPart,w,result);
    }

    public static bool HasValidTarget(PlayerStats target, PlayerStats myStats, Weapon w)
    {
        //if they are on the same team
        if (target.GetTeam() == myStats.GetTeam()){
            //CombatLog.Log(myStats.GetName() + " (team " + myStats.GetTeam() + ": But thats my friend!"); 
            return false;
        }
        float distance = Vector3.Distance(myStats.transform.position, target.transform.position);

        List<PlayerStats> meleeCombatants = myStats.gameObject.GetComponent<TacticsMovement>().AdjacentPlayers();
        bool inCombat = (myStats.gameObject.GetComponent<TacticsMovement>().GetAdjacentEnemies(myStats.GetTeam()) > 0);

        if(w.IsWeaponClass("Melee") || (inCombat && w.IsWeaponClass("Pistol")))
        {    
            return meleeCombatants.Contains(target);
        }
        else
        {
            //ranged weapons other than pistols can't be fired into melee 
            if (inCombat)
            {
                //CombatLog.Log(target.GetName() + " is too close to shoot!");
                return false;
            }

            //ranged weapons require line of sight 
            return HasLOS(target.gameObject, myStats.gameObject);
        }
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


    public static Tile CalculateCover(GameObject attacker,  GameObject target, string HitLocation)
    {
        //cover never protects arms and head
        if (HitLocation != "Body" && HitLocation != "LeftLeg" && HitLocation != "RightLeg")
        {
            return null;
        }


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

    public static int SaveCoverBonus(PlayerStats target)
    {


        RaycastHit hit;
        Tile tile;

        if (Physics.Raycast(target.transform.position, Vector3.left, out hit, 1))
        {
            tile = hit.collider.GetComponent<Tile>(); 
            if (tile != null)
            {
                return tile.CoverReduction(0,0);
            }
        }
        if (Physics.Raycast(target.transform.position, Vector3.right, out hit, 1))
        {
            tile = hit.collider.GetComponent<Tile>(); 
            if (tile != null)
            {
                return tile.CoverReduction(0,0);
            }
        }
        if (Physics.Raycast(target.transform.position, Vector3.forward, out hit, 1))
        {
            tile = hit.collider.GetComponent<Tile>(); 
            if (tile != null)
            {
                return tile.CoverReduction(0,0);
            }
        }
        if (Physics.Raycast(target.transform.position, Vector3.back, out hit, 1))
        {
            tile = hit.collider.GetComponent<Tile>(); 
            if (tile != null)
            {
                return tile.CoverReduction(0,0);
            }
        }
        return 0;
    }

    public static int ROFBonus(Weapon w, string type)
    {
        if(w.IsWeaponClass("Thrown") || w.IsWeaponClass("Melee"))
        {
            return 0;
        }
        switch(type)
            {
                case "S":
                return 10;
                case "Auto":
                return -10;
            }
        return 0;
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

    //true stops attack sequence, false continues it
   public static bool Jammed(int rollResult, Weapon w, string type, PlayerStats owner)
   {
       if(w.IsWeaponClass("Melee"))
       {
           return false;
       } 
       if(w.HasWeaponAttribute("Flame"))
       {
           if(rollResult == 9)
           {
            CombatLog.Log(w.GetName() + " Jams by getting a 9 on the damage roll!");
            PopUpText.CreateText("Jammed!",Color.red,owner.gameObject);
            w.SetJamStatus(true);
            return true;
           }
           else
           {
               return false;
           }
       }
       int JamTarget = 96;
       if(type.Equals("Auto") || type.Equals("Semi"))
       {
           JamTarget = 94;
       }
       if(w.HasWeaponAttribute("Unreliable") || w.HasWeaponAttribute("Overheats"))
       {
           JamTarget = 91; 
       }
       //on jam
       if(rollResult >= JamTarget)
       {
           CombatLog.Log(w.GetName() + " Jams by exceeding a " + JamTarget + " on the hit roll!");
           //9 in 10 chance to not jam
           if(w.HasWeaponAttribute("Reliable"))
           {

               int reliableRoll = Random.Range(1,11);
               if(reliableRoll == 10)
               {
                    PopUpText.CreateText("Jammed!",Color.red,owner.gameObject);
                    w.SetJamStatus(true);
                    return true;
               }
               //still a miss but no ammo is lost
               else
               {
                   CombatLog.Log(w.GetName() + "'s reliable quality prevented it from being jammed!");
                   PopUpText.CreateText("Reliable!",Color.green, owner.gameObject);
                   return true;
               }
           }
           else
           {
               if(w.HasWeaponAttribute("Overheats"))
               {
                    int overheatResult = Random.Range(1,11);
                    if(overheatResult > 8)
                    {
                        foreach(PlayerStats p in owner.GetComponent<TacticsMovement>().AdjacentPlayers())
                        {
                            DealDamage(p,owner,"Body",w);
                        }
                        DealDamage(owner,owner,"Body",w);
                        owner.Unequip(w);
                        owner.myData.RemoveItem(w);
                        CombatLog.Log("The " + w.GetName() +" explodes! hiting everyone within a meter of " + owner.GetName());
                    }
                    else if(overheatResult > 5)
                    {
                        int energyDamage = Random.Range(1,11) + 2;
                        int rounds = Random.Range(1,11);
                        owner.Unequip(w);
                        //owner.takeDamage(energyDamage - owner.GetStat("RightArm") - owner.GetStat("T"), "RightArm");
                        CombatLog.Log("The " + w.GetName() + " burns the hands of " + owner.GetName() + " dealing 1d10 + 2 = " + energyDamage + "Energy Damage" + "and cannot be used for " + rounds + " rounds");
                    }
                    else
                    {
                        int rounds = Random.Range(1,11);
                        owner.Unequip(w);
                        CombatLog.Log("The " + w.GetName() + " overheats and cannot be used for " + rounds + " rounds");
                    }
                   PopUpText.CreateText("Overheats!",Color.red,owner.gameObject);
               }
               else
               {
                    PopUpText.CreateText("Jammed!",Color.red,owner.gameObject);
               }
               w.SetJamStatus(true);
               return true;
           }
       }
       return false;
   }   
    public static List<string> GenerateTooltip(PlayerStats target, PlayerStats myStats, Weapon w, string type)
    {
        List<string> output = new List<string>();
        if(target.GetTeam() == myStats.GetTeam())
        {
            output.Add("Invalid Target: Ally");
        }
        else
        {
            Stack<string> outputStack = new Stack<string>();
            int attackDice = 0;
            // penalty from range 
            int distance = Mathf.RoundToInt(Vector3.Distance(myStats.transform.position, target.transform.position));
            int rangePenalty = w.Template.rangeClass.GetRangePenalty(distance);
            if(rangePenalty < 0)
            {
                attackDice += rangePenalty;
                outputStack.Push(rangePenalty + " from range");
            }
            // penalty from recoil 
            int recoilPenalty = myStats.CalculateRecoilPenalty(AmmoExpenditure[type],false);
            if(recoilPenalty < 0)
            {
                attackDice += recoilPenalty;
                outputStack.Push(recoilPenalty + " from recoil");
            }
            // bonus from specialization
            if(myStats.myData.hasSpecialization(w.Template.WeaponSkill.name,w.Template.WeaponSpecialization))
            {
                outputStack.Push("+2 from skill specialization");
                attackDice += 2;
            }
            // bonus from attribute
            int attributeDice = myStats.myData.GetAttribute(w.Template.WeaponSkill.derrivedAttribute);
            outputStack.Push("+" + attributeDice + " from " + w.Template.WeaponSkill.derrivedAttribute + " skill");
            attackDice += attributeDice;
            // bonus from skill
            int skillDice = myStats.myData.GetAttribute(w.Template.WeaponSkill);
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
        if(w.IsWeaponClass("Melee"))
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
            if(adjacentPlayers.Contains(attacker) && w.IsWeaponClass("Pistol"))
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

    /* Depreciated 
    public static int Critical(PlayerStats attacker, Weapon w, bool confirmed)
    {
        //only players can get fury
        if(attacker.GetTeam() != 0)
        {
            return 0;
        }
        string attackSkill;
        if(w.IsWeaponClass("Melee"))
        {
            attackSkill = "WS";
        }
        else
        {
            attackSkill = "BS";
        }
        RollResult critConfirm = attacker.AbilityCheck(attackSkill, 0);
        if(critConfirm.Passed() || confirmed)
        {
            PopUpText.CreateText("Righteous Fury!", Color.green, attacker.gameObject);
            int damageRoll = Random.Range(1,10);
            CombatLog.Log(attacker.GetName() + " confirms righteous fury and deals an additional " + damageRoll + " damage!");
            if(damageRoll == 10)
            {
                damageRoll += Critical(attacker, w, true);
            }
            return damageRoll;
        }
        else
        {
            return 0;
        }
    }
    */
}

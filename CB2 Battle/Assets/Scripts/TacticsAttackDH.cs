using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using UnityEngine.UI;

// static class used to calculate attack rolls
public static class TacticsAttackDH
{
    // target: stats of the defender
    // mystats: stats of the attacker
    // w: weapon used in the attack
    // type: the fire rate of w
    // creates an attacksequence objects that contains a single attack sequence between an attacker and a target
    public static AttackSequence Attack(PlayerStats target, PlayerStats myStats, Weapon w, string type)
    {
        int modifiers = 0;
        AttackSequence output = new AttackSequence(target,myStats,w,type,0,false);
        // Melee modifiers
        if (w.IsWeaponClass("Melee"))
        {
            // Only melee weps can have the two handed attribute as there is only one class for melee weps unlike ranged weps
            if(w.HasWeaponAttribute("Two Handed") && myStats.IsDualWielding())
            {
                modifiers -= 20;
            }
            if(w.HasWeaponAttribute("Defensive"))
            {
                modifiers -= 10;
            }
            if(target.hasCondition("Defensive Stance"))
            {
                modifiers -= 20;
            }
            if(target.hasCondition("Grappled"))
            {
                modifiers += 20;
            }
            if(target.hasCondition("Running"))
            {
                modifiers += 20;
            }
            if(w.GetName().Equals("Unarmed") && ((target.SecondaryWeapon != null && target.SecondaryWeapon.IsWeaponClass("Melee")) || (target.PrimaryWeapon != null && target.PrimaryWeapon.IsWeaponClass("Melee"))))
            {
                modifiers -= 20;
            }
            if(target.hasCondition("Prone"))
            {
                modifiers += 10;
            }
            // unless stated otherwise, melee doesn't use type, and always makes one attack
        }
        // Ranged Modifiers
        else
        {
            if(w.IsWeaponClass("Basic") && myStats.IsDualWielding())
            {
                modifiers -= 20;
            }
            if(w.IsWeaponClass("Heavy") && !myStats.hasCondition("Braced"))
            {
                modifiers -= 30;
            }
            if(target.hasCondition("Running"))
            {
                modifiers -= 20;
            }
            if(target.hasCondition("Prone"))
            {
                modifiers -= 10;
            }
        }
        // universal modifiers that melee and ranged weapons can use
        if(w.HasWeaponAttribute("Accurate") && (myStats.hasCondition("Half Aiming") || myStats.hasCondition("Full Aiming")))
        {
            Debug.Log("Accurate");
            modifiers += 10;  
        }
        if(w.HasWeaponAttribute("Inaccurate") && (myStats.hasCondition("Half Aiming")))
        {
            modifiers -= 10;  
        }
        if(w.HasWeaponAttribute("Inaccurate") && (myStats.hasCondition("Full Aiming")))
        {
            modifiers -= 20;  
        }
        if(myStats.OffHandPenalty(w))
        {
            modifiers -= 20;
        }
        if(target.hasCondition("Stunned"))
        {
            modifiers += 20;
        }
        if(target.hasCondition("Obscured"))
        {
            modifiers -= 20;
        }
        modifiers += adjacencyBonus(target,myStats, w);
        modifiers += TacticsAttack.CalculateHeightAdvantage(target,myStats);
        // ranged weps get a bonus depending on distance to target
        int attackModifiers = w.RangeBonus(target.transform, myStats) + modifiers;
        if(!myStats.hasCondition("Called"))
        {
            attackModifiers += ROFBonus(w, type);
        }
        RollResult attackRoll = myStats.AbilityCheck(w.Template.WeaponSkill, "", "", attackModifiers);
        //attackRoll.OpposedRoll();
        output.attackRoll = attackRoll;
        /*
        if(TacticsAttack.Jammed(AttackResult.GetRoll(), w, type, myStats))
        {
            return output;
        }
        if (AttackResult.Passed() || target.IsHelpless())
        {
            bool scatterDistance = Vector3.Distance(myStats.gameObject.transform.position, target.gameObject.transform.position) <= 3;
            output.attacks = w.GetAdditionalHits(type, AttackResult.GetDOF(),shotsFired, scatterDistance);
            return output;
        }
        else
        {
            return output;
        }
        */
        return output;
    }

    public static void DealDamage(PlayerStats target, PlayerStats myStats, int attackRoll, Weapon w)
    {
        string hitBodyPart = "";
        DealDamage(target, myStats, hitBodyPart, w);
    }

    public static void DealDamage(PlayerStats target, PlayerStats myStats, string hitBodyPart, Weapon w)
    {
        Tile cover = CalculateCover(myStats.gameObject, target.gameObject, hitBodyPart);
        int damageRoll = w.rollDamage(myStats,target, hitBodyPart);
        int AP = target.GetAP(hitBodyPart, w);
        if(w.HasWeaponAttribute("Scatter") && w.RangeBonus(target.transform, myStats) < 0)
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
                        owner.takeDamage(energyDamage - owner.GetStat("RightArm") - owner.GetStat("T"), "RightArm");
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
                Stack<string> conditionStack;
                int chanceToHit = 0;
                if (w.IsWeaponClass("Melee"))
                {
                    chanceToHit = myStats.GetStat("WS") + myStats.CalculateStatModifiers("WS");// + myStats.CalculateStatModifiers("WS");
                    conditionStack = myStats.DisplayStatModifiers("WS");
                    if(w.HasWeaponAttribute("Two Handed") && myStats.IsDualWielding())
                    {
                        outputStack.Push(" -20%: One Handing");
                        chanceToHit -= 20;
                    }        
                    if(w.HasWeaponAttribute("Defensive"))
                    {
                        outputStack.Push("- -10%: Shield Bashing");
                        chanceToHit -= 10;
                    }
                    if(target.hasCondition("Defensive Stance"))
                    {
                        outputStack.Push(" -20%: Target Defending");
                        chanceToHit -= 20;
                    }
                    if(target.hasCondition("Grappled"))
                    {
                        outputStack.Push(" +20%: Target Grappled");
                        chanceToHit += 20;
                    }
                    if(target.hasCondition("Running"))
                    {
                        outputStack.Push(" +20%: Target Running");
                        chanceToHit += 20;
                    }
                    if(target.hasCondition("Prone"))
                    {
                        outputStack.Push(" +10%: Target Prone");
                        chanceToHit += 10;
                    }
                    if(w.GetName().Equals("Unarmed") && ((target.SecondaryWeapon != null && target.SecondaryWeapon.IsWeaponClass("Melee")) || (target.PrimaryWeapon != null && target.PrimaryWeapon.IsWeaponClass("Melee"))))
                    {
                        outputStack.Push(" -20%: Unarmed");
                        chanceToHit -= 20;
                    }
                    int GangupBonus = TacticsAttack.adjacencyBonus(target,myStats, w);
                    if(GangupBonus != 0)
                    {
                        chanceToHit += GangupBonus;
                        outputStack.Push(" +" + GangupBonus +"%: Outnumbering");
                    }
                }
                else
                {
                    chanceToHit = myStats.GetStat("BS") + myStats.CalculateStatModifiers("BS");
                    conditionStack = myStats.DisplayStatModifiers("BS");
                    if(w.IsWeaponClass("Basic") && myStats.IsDualWielding())
                    {
                        outputStack.Push(" -20%: One Handing");
                        chanceToHit -= 20;
                    }
                    if(w.IsWeaponClass("Heavy") && !myStats.hasCondition("Braced"))
                    {
                        outputStack.Push(" -30%: Not Bracing");
                        chanceToHit -= 30;
                    }
                    if(target.hasCondition("Running"))
                    {
                        outputStack.Push(" -20%: Target Running");
                        chanceToHit -= 20;
                    }
                    if(target.hasCondition("Prone"))
                    {
                        outputStack.Push(" -10%: Target Prone");
                        chanceToHit -= 10;
                    }
                    int rangemodifier = w.RangeBonus(target.transform, myStats);
                    chanceToHit += rangemodifier;
                    if(rangemodifier != 0)
                    {
                        outputStack.Push(" +" + rangemodifier +"%: range");
                    }
                    else
                    {
                        outputStack.Push(" " + rangemodifier +"%: range");
                    }
                    int firebonus = ROFBonus(w, type);
                    if(firebonus != 0 && !myStats.hasCondition("Called"))
                    {
                        chanceToHit += firebonus;
                        string addition = " ";
                        if(firebonus > 0)
                        {
                            addition = " +";
                        }
                        outputStack.Push(addition + firebonus +": " + type);
                    }
                    int GangupBonus = TacticsAttack.adjacencyBonus(target, myStats,w);
                    if(GangupBonus != 0)
                    {
                        chanceToHit += GangupBonus;
                        outputStack.Push(" " + GangupBonus +"%: Allies in Melee");
                    }
                    int heightbonus = TacticsAttack.CalculateHeightAdvantage(target,myStats);
                    if(heightbonus > 0)
                    {
                        chanceToHit += heightbonus;
                        outputStack.Push(" +" + heightbonus +"%: Height");    
                    }
                }
                if(w.HasWeaponAttribute("Accurate") && (myStats.hasCondition("Half Aiming") || myStats.hasCondition("Full Aiming")))
                {
                    outputStack.Push(" +10%: Accurate");
                    chanceToHit += 10;    
                }
                if(w.HasWeaponAttribute("Inaccurate") && (myStats.hasCondition("Half Aiming")))
                {
                    outputStack.Push(" -10%: Inaccurate");
                    chanceToHit -= 10;  
                }
                if(w.HasWeaponAttribute("Inaccurate") && (myStats.hasCondition("Full Aiming")))
                {
                    outputStack.Push(" -20%: Inaccurate");
                    chanceToHit -= 20;  
                }
                if(myStats.OffHandPenalty(w))
                { 
                    outputStack.Push(" -20%: Using Off Hand");
                    chanceToHit -= 20;
                }
                if(target.hasCondition("Stunned"))
                {
                    outputStack.Push(" +20%: Target Stunned");
                    chanceToHit += 20;
                }
                if(target.hasCondition("Obscured"))
                {
                    outputStack.Push(" -20%: Target Obscured");
                    chanceToHit -= 20;
                }

                output.Add("Attacking: " + target.GetName());
                output.Add("To Hit: " + chanceToHit + "%");
                while (conditionStack.Count > 0)
                {
                    output.Add(conditionStack.Pop());
                }
                while (outputStack.Count > 0)
                {
                    output.Add(outputStack.Pop());
                }
                output.Add(w.DisplayDamageRange());
                //output.Add(target.GetAverageSoak());
                if(!w.IsWeaponClass("Melee") && !myStats.hasCondition("Called"))
                {
                    Tile cover = CalculateCover(myStats.gameObject,target.gameObject,"Left Leg");
                    if(cover != null)
                    {    
                        output.Add("70% chance to hit cover");
                    }
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

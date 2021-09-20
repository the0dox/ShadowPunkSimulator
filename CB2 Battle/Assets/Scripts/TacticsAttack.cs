using System.Collections;
using System.Collections.Generic;
using UnityEngine; 
using UnityEngine.UI;

public class TacticsAttack : MonoBehaviour
{
    //public GameObject displayText;

    // Returns the ammount of times hit
    public static AttackSequence Attack(PlayerStats target, PlayerStats myStats, Weapon w, string type)
    {
        int modifiers = 0;
        AttackSequence output = new AttackSequence(target,myStats,w,type,0);
        string attackSkill;
        int shotsFired;
        if (w.IsWeaponClass("Melee"))
        {
            if(w.HasWeaponAttribute("Two Handed") && myStats.IsDualWielding())
            {
                Debug.Log("dualwielding");
                modifiers -= 20;
            }
            if(w.HasWeaponAttribute("Defensive"))
            {
                Debug.Log("defensive");
                modifiers -= 10;
            }
            if(target.hasCondition("Defensive Stance"))
            {
                Debug.Log("defensive stance");
                modifiers -= 20;
            }
            if(target.hasCondition("Grappled"))
            {
                Debug.Log("Grappled");
                modifiers += 20;
            }
            if(target.hasCondition("Running"))
            {
                Debug.Log("Running");
                modifiers += 20;
            }
            if(w.HasWeaponAttribute("Unarmed") && ((target.SecondaryWeapon != null && target.SecondaryWeapon.IsWeaponClass("Melee")) || (target.SecondaryWeapon != null && target.PrimaryWeapon.IsWeaponClass("Melee")))) 
            {
                Debug.Log(w.GetName());
                modifiers -= 20;
            }
            attackSkill = "WS";
            shotsFired = 1;
        }
        else
        {
            if(w.IsWeaponClass("Basic") && myStats.IsDualWielding())
            {
                Debug.Log("dualwielding");
                modifiers -= 20;
            }
            if(target.hasCondition("Running"))
            {
                Debug.Log("Running");
                modifiers -= 20;
            }
            attackSkill = "BS";
            shotsFired = w.ExpendAmmo(type);
        }
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
            Debug.Log("offhand");
            modifiers -= 20;
        }
        if(target.hasCondition("Stunned"))
        {
            Debug.Log("Stunned");
            modifiers += 20;
        }
        if(target.hasCondition("Obscured"))
        {
            modifiers -= 20;
        }
        if(target.hasCondition("Prone"))
        {
            
            Debug.Log("Prone");
            modifiers += 10;
        }
        modifiers += adjacencyBonus(target,w);
        modifiers += TacticsAttack.CalculateHeightAdvantage(target,myStats);
        int attackModifiers = w.RangeBonus(target.transform, myStats) + ROFBonus(type) + modifiers;
        RollResult AttackResult = myStats.AbilityCheck(attackSkill, attackModifiers);
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
    }
    public static string HitLocation(int attackRoll, PlayerStats target)
    {
        int attackOnes = attackRoll % 10;
        int attackTens = attackRoll / 10;
        int location = attackTens + (attackOnes * 10); 
        foreach (KeyValuePair<int, string> kvp in target.GetHitLocations()){
            if (location <= kvp.Key)
            {
                CombatLog.Log("Reversed hit roll of " + location + " hits the target's " + kvp.Value);
                return kvp.Value;  
            }
        }
        return "Left Leg";
    }

    public static void DealDamage(PlayerStats target, PlayerStats myStats, int attackRoll, Weapon w)
    {
            string hitBodyPart = HitLocation(attackRoll, target);
            DealDamage(target, myStats, hitBodyPart, w);
    }

    public static void DealDamage(PlayerStats target, PlayerStats myStats, string hitBodyPart, Weapon w)
    {
            Tile cover = CalculateCover(myStats.gameObject, target.gameObject, hitBodyPart);
            int damageRoll = w.rollDamage(myStats,target);
            int AP = target.GetAP(hitBodyPart, w) + myStats.GetAdvanceBonus(hitBodyPart);
            int soak = target.GetStatScore("T");
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
            
            TacticsAttack.ApplySpecialEffects(target,myStats,hitBodyPart,w,result);
            
    }

    public static void ApplySpecialEffects(PlayerStats target, PlayerStats myStats, string hitBodyPart, Weapon w, int result)
    {
        if(result > 0 && w.HasWeaponAttribute("Shocking"))
        {
            int shockModifier = target.GetStat(hitBodyPart) * 10;
            if(!target.AbilityCheck("T",shockModifier).Passed())
            {
                int rounds = result;
                if(rounds / 2 > 0)
                {
                    rounds = rounds/2;
                }
                CombatLog.Log("The shocking attribute of " +myStats.GetName() +"'s " + w.GetName() + " stuns " +target.GetName() + " for " + rounds + " rounds");
                target.SetCondition("Stunned",rounds,true);
            }
            else
            {
                CombatLog.Log(target.GetName() + " resists the shocking attribute of " +myStats.GetName() +"'s " + w.GetName());
                PopUpText.CreateText("Resisted!", Color.yellow, target.gameObject);
            }
        }
        if(result > 0 && w.HasWeaponAttribute("Toxic"))
        {
            int toxicModifier = result * -5;
            if(!target.AbilityCheck("T",toxicModifier).Passed())
            {
                int damage = Random.Range(1,10);
                target.takeDamage(damage,"Body");
                CombatLog.Log("The toxic attribute of " +myStats.GetName() +"'s " + w.GetName() + " deals an additional 1d10 = " + damage + " ignoring soak");
                PopUpText.CreateText("Posioned!", Color.red, target.gameObject);
            }
            else
            {
                CombatLog.Log(target.GetName() + " resists the toxic attribute of " +myStats.GetName() +"'s " + w.GetName());
                PopUpText.CreateText("Resisted!", Color.yellow, target.gameObject);
            }
        }
        if(w.HasWeaponAttribute("Snare"))
        {
            if(!target.AbilityCheck("A",0).Passed())
            {
                CombatLog.Log("The snare attribute of " +myStats.GetName() +"'s " + w.GetName() + " immobilises " + target.GetName());
                target.SetCondition("Immobilised",0,true);
                target.SetCondition("Prone",0,true);
                target.SpendAction("Reaction");
            }
            else
            {
                CombatLog.Log(target.GetName() + " avoids the snare attribute of " +myStats.GetName() +"'s " + w.GetName());
            }
        }
        if(w.HasWeaponAttribute("Smoke"))
        {
            CombatLog.Log("Target is covered by smoke!");
            target.SetCondition("Obscured",3,true);
        }
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

        if(w.IsWeaponClass("Melee"))
        {    
            return meleeCombatants.Contains(target);
        }
        else
        {
            //ranged weapons other than pistols can't be fired into melee 
            if (inCombat && !w.IsWeaponClass("Pistol"))
            {
                //CombatLog.Log(target.GetName() + " is too close to shoot!");
                return false;
            }

            //ranged weapons require line of sight 
            Vector3 myPOV = myStats.gameObject.transform.position + new Vector3(0, myStats.gameObject.GetComponent<Collider>().bounds.extents.y,0);
            Vector3 TargetPOV = target.gameObject.transform.position + new Vector3(0, target.gameObject.GetComponent<Collider>().bounds.extents.y,0);
            if(Physics.Linecast(myPOV, TargetPOV, LayerMask.GetMask("Obstacle")))
            {
                return false;
            }
            return true;
        }
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

    public static int ROFBonus(string type)
    {
        switch(type)
            {
                case "Semi":
                return 10;
                case "Auto":
                return 20;
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

               int reliableRoll = Random.Range(1,10);
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
                    int overheatResult = Random.Range(1,10);
                    if(overheatResult > 8)
                    {
                        foreach(PlayerStats p in owner.GetComponent<TacticsMovement>().AdjacentPlayers())
                        {
                            DealDamage(p,owner,"Body",w);
                        }
                        DealDamage(owner,owner,"Body",w);
                        owner.Unequip(w);
                        owner.equipment.Remove(w);
                        CombatLog.Log("The " + w.GetName() +" explodes! hiting everyone within a meter of " + owner.GetName());
                    }
                    else if(overheatResult > 5)
                    {
                        int energyDamage = Random.Range(1,10) + 2;
                        int rounds = Random.Range(1,10);
                        owner.Unequip(w);
                        owner.takeDamage(energyDamage - owner.GetStat("RightArm") - owner.GetStat("T"), "RightArm");
                        CombatLog.Log("The " + w.GetName() + " burns the hands of " + owner.GetName() + " dealing 1d10 + 2 = " + energyDamage + "Energy Damage" + "and cannot be used for " + rounds + " rounds");
                    }
                    else
                    {
                        int rounds = Random.Range(1,10);
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
                    if(w.HasWeaponAttribute("Unarmed") && ((target.SecondaryWeapon != null && target.SecondaryWeapon.IsWeaponClass("Melee")) || (target.PrimaryWeapon != null && target.PrimaryWeapon.IsWeaponClass("Melee"))))
                    {
                        outputStack.Push(" -20%: Unarmed");
                        chanceToHit -= 20;
                    }
                    int GangupBonus = TacticsAttack.adjacencyBonus(target,w);
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
                    if(target.hasCondition("Running"))
                    {
                        outputStack.Push(" -20%: Target Running");
                        chanceToHit -= 20;
                    }
                    int rangemodifier = w.RangeBonus(target.transform, myStats);
                    chanceToHit += rangemodifier;
                    if(rangemodifier > 0)
                    {
                        outputStack.Push(" +" + rangemodifier +"%: range");
                    }
                    else
                    {
                        outputStack.Push(" " + rangemodifier +"%: range");
                    }
                    chanceToHit += ROFBonus(type);
                    if(ROFBonus(type) > 0)
                    {
                        outputStack.Push(" +" + ROFBonus(type) +": " + type);
                    }
                    int GangupBonus = TacticsAttack.adjacencyBonus(target,w);
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
                if(target.hasCondition("Prone"))
                {
                    outputStack.Push(" +10%: Target Prone");
                    chanceToHit += 10;
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
                output.Add(target.GetAverageSoak());
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

    public static int adjacencyBonus( PlayerStats target, Weapon w)
    {
        int adjacentEnemies = target.GetComponent<TacticsMovement>().GetAdjacentEnemies(target.GetTeam());
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
        else if(adjacentEnemies > 0 && !w.IsWeaponClass("Pistol"))
        {
            return -20;
        }
        return 0;
    }

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
}

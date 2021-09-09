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
            if(w.HasWeaponAttribute("Unarmed") && (target.LeftHand != null && target.LeftHand.IsWeaponClass("Melee")) || (target.LeftHand != null && target.RightHand.IsWeaponClass("Melee")))
            {
                modifiers -= 20;
            }
            attackSkill = "WS";
            shotsFired = 1;
        }
        else
        {
            if(target.hasCondition("Running"))
            {
                modifiers -= 20;
            }
            attackSkill = "BS";
            shotsFired = w.ExpendAmmo(type);
        }
        if(target.hasCondition("Stunned"))
        {
            modifiers += 20;
        }
        if(target.hasCondition("Prone"))
        {
            modifiers += 10;
        }
        modifiers += adjacencyBonus(target,w);
        modifiers += TacticsAttack.CalculateHeightAdvantage(target,myStats);
        int attackModifiers = w.RangeBonus(target.transform, myStats) + ROFBonus(type) + modifiers;
        RollResult AttackResult = myStats.AbilityCheck(attackSkill, attackModifiers);
        if(TacticsAttack.Jammed(AttackResult.GetRoll(), w, type, myStats))
        {
            w.SetJamStatus(true);
            return output;
        }
        if (AttackResult.Passed())
        {    
            output.attacks = GetAdditionalHits(type, AttackResult.GetDOF(),shotsFired);
            return output;
        }
        else
        {
            return output;
        }
    }
    //depreciated
    public static int Attack(PlayerStats target, PlayerStats myStats, Weapon w, string type, int modifiers)
    {
        string attackSkill;
        int shotsFired;
        if (w.IsWeaponClass("Melee"))
        {
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
            if(w.HasWeaponAttribute("Unarmed") && (target.LeftHand.IsWeaponClass("Melee") || target.RightHand.IsWeaponClass("Melee")))
            {
                modifiers -= 20;
            }
            attackSkill = "WS";
            shotsFired = 1;
        }
        else
        {
            if(target.hasCondition("Running"))
            {
                modifiers -= 20;
            }
            attackSkill = "BS";
            shotsFired = w.ExpendAmmo(type);
        }
        if(target.hasCondition("Stunned"))
        {
            modifiers += 20;
        }
        if(target.hasCondition("Prone"))
        {
            modifiers += 10;
        }
        modifiers += adjacencyBonus(target,w);
        modifiers += TacticsAttack.CalculateHeightAdvantage(target,myStats);
        int attackModifiers = w.RangeBonus(target.transform, myStats) + ROFBonus(type) + modifiers;
        RollResult AttackResult = myStats.AbilityCheck(attackSkill, attackModifiers);
        if(TacticsAttack.Jammed(AttackResult.GetRoll(), w, type, myStats))
        {
            w.SetJamStatus(true);
            return 0;
        }
        if (AttackResult.Passed())
        {    
            return GetAdditionalHits(type, AttackResult.GetDOF(),shotsFired);
        }
        else
        {
            return 0;
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
            int damageRoll = w.rollDamage(myStats);
            int AP = target.Stats[hitBodyPart] + myStats.GetAdvanceBonus(hitBodyPart);
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
            int CriticalBleed = target.takeDamage(result, hitBodyPart);
            Debug.Log(CriticalBleed + "");
            if(CriticalBleed > 0)
            {
                CriticalDamageReference.DealCritical(target,w,CriticalBleed,hitBodyPart);
            }
    }

    public static bool HasValidTarget(PlayerStats target, PlayerStats myStats, Weapon w)
    {
        //if they are on the same team
        if (target.GetTeam() == myStats.GetTeam()){
            CombatLog.Log(myStats.GetName() + " (team " + myStats.GetTeam() + ": But thats my friend!"); 
                    
            return false;
        }
        float distance = Vector3.Distance(myStats.transform.position, target.transform.position);

        List<PlayerStats> meleeCombatants = myStats.gameObject.GetComponent<TacticsMovement>().AdjacentPlayers();
        bool inCombat = (myStats.gameObject.GetComponent<TacticsMovement>().GetAdjacentEnemies(myStats.GetTeam()) > 0);

        //melee weapons can't be used unless adjacent
        if (!meleeCombatants.Contains(target) && w.IsWeaponClass("Melee"))
        {
            Debug.Log(target.GetName() + " is out of melee range");
            return false;
            
        }
        //ranged weapons other than pistols can't be fired into melee 
        else if (inCombat && !w.IsWeaponClass("Melee") && !w.HasWeaponAttribute("Pistol"))
        {
            CombatLog.Log(target.GetName() + " is too close to shoot!");
            return false;
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
    
    
    public static int GetAdditionalHits(string type, int DOF, int ROF)
    {
        if(type.Equals("S"))
        {
            return 1;
        }
        int extraAttacks = DOF;
        if(type.Equals("Semi"))
        {
            extraAttacks /= 2;
        }
        int numAttacks = 1 + extraAttacks;
        //cannot get a number of extra attacks equal to 
        if(numAttacks > ROF)
        {
            numAttacks = ROF;
        }
        return numAttacks;
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
       if(w.HasWeaponAttribute("Unreliable"))
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
               PopUpText.CreateText("Jammed!",Color.red,owner.gameObject);
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
                    if(w.HasWeaponAttribute("Unarmed") && ( (target.LeftHand != null && target.LeftHand.IsWeaponClass("Melee")) || (target.RightHand != null && target.RightHand.IsWeaponClass("Melee"))))
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
                    if(target.hasCondition("Running"))
                    {
                        outputStack.Push(" -20: Target Running");
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
                if(target.hasCondition("Stunned"))
                {
                    outputStack.Push(" +20%: Target Stunned");
                    chanceToHit += 20;
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
                output.Add(w.DisplayDamageRange(myStats));
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
        else if(adjacentEnemies > 0 && !w.HasWeaponAttribute("Pistol"))
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




/* Instantiate(displayText, transform.position + new Vector3(0, 1.75f, 0), Quaternion.identity).GetComponent<DisplayTextScript>().setText(AttackResult.Print(), Color.green);
            int numAttacks = GetAdditionalHits(type, AttackResult.GetDOF(),shotsFired);

            int dodgeModifiers = 0; 
            
            if (HasReaction(target.gameObject))
            {
                //to implement: choice to dodge
            }
            
            RollResult dodgeResult = target.AbilityCheck("Dodge", dodgeModifiers);

            if (dodgeResult.Passed())
            {
                Instantiate(displayText, target.gameObject.transform.position + new Vector3(0, 1.75f, 0), Quaternion.identity).GetComponent<DisplayTextScript>().setText(dodgeResult.Print(), Color.yellow);
            }
            else
            {
                //Instantiate(displayText, target.gameObject.transform.position + new Vector3(0, 1.75f, 0), Quaternion.identity).GetComponent<DisplayTextScript>().setText("Failed Dodge!: (" + dodgeRoll + " < " + (target.GetStat("A")/2) + ")", Color.red);
                
                DealDamage(target, myStats, AttackResult.GetRoll(), w);          
            }
        }
        else
        {
            Instantiate(displayText, transform.position + new Vector3(0, 1.75f, 0), Quaternion.identity).GetComponent<DisplayTextScript>().setText(AttackResult.Print(), Color.red);
        }
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnActions : MonoBehaviour
{
    public TacticsMovement ActivePlayer; 
    public List<string> DefaultActions;
    public PlayerStats ActivePlayerStats;
    protected string currentAction;
    protected Weapon ActiveWeapon;
    public GameObject Canvas;
    public GameObject ActionUIButton;
    protected int halfActions;
    protected string FireRate;
    public GameObject PlayerUIDisplay;
    protected PlayerStats target;
    protected int attacks;
    protected string HitLocation; 
    public GameObject ThreatRangePrefab;
    public WeaponTemplate unarmed;
    int RepeatedAttacks;
    protected string InteruptedAction;
    public List<PlayerStats> ValidTargets;
    protected Queue<AttackSequence> AttackQueue = new Queue<AttackSequence>();
    protected AttackSequence CurrentAttack;

    public void Combat()
    {
        currentAction = null;
        Dictionary<string,string> d = new Dictionary<string, string>();
        
        if(ActivePlayerStats.RightHand != null) 
        {
            //if charging only melee weapons are usable
            if(ActivePlayerStats.ValidAction("Charge") || !ActivePlayerStats.ValidAction("Charge") && ActivePlayerStats.RightHand.HasWeaponAttribute("Melee"))
            {    
                d.Add(ActivePlayerStats.RightHand.GetName(), "RightHandWeapon");
            }
        }
        if (ActivePlayerStats.LeftHand != null && ActivePlayerStats.LeftHand != ActivePlayerStats.RightHand)
        {
            //if charging only melee weapons are usable
            if(ActivePlayerStats.ValidAction("Charge") || !ActivePlayerStats.ValidAction("Charge") && ActivePlayerStats.LeftHand.HasWeaponAttribute("Melee"))
            { 
            d.Add(ActivePlayerStats.LeftHand.GetName(),"LeftHandWeapon");
            }
        }
        d.Add("Unarmed","Unarmed");
        //can't cancel if they charged!
        if(ActivePlayerStats.ValidAction("Charge"))
        {
            d.Add("Cancel","Cancel");
        }
        ConstructActions(d);
    }

    public void Movement()
    {
        List<string> l = new List<string>();
        
        if(ActivePlayerStats.hasCondition("Prone"))
        {
            l.Add("Stand");
        }
        else
        {
            if(halfActions > 1)
            {
                l.Add("Charge");
                l.Add("Run");
                if(TacticsAttack.SaveCoverBonus(ActivePlayerStats) > 0)
                {
                    l.Add("Advance");
                }
            }
            if(ActivePlayer.GetAdjacentEnemies(ActivePlayerStats.GetTeam()) > 0)
            {
                l.Add("Disengage");
            }
        }
        l.Add("Cancel");
        ConstructActions(l);
    }

    public void RightHandWeapon()
    {
        ActiveWeapon = ActivePlayerStats.RightHand;
        GetWeaponActions();
    }

    public void LeftHandWeapon()
    {
        ActiveWeapon = ActivePlayerStats.LeftHand;
        GetWeaponActions();
    }

    public void Unarmed()
    {
        ActiveWeapon = new Weapon(unarmed);
        GetWeaponActions();
    }

    public void GetWeaponActions()
    {
        Dictionary<string,string> d = new Dictionary<string, string>();
        //only melee weapons can be used on charge 
        if(!ActivePlayerStats.ValidAction("Charge"))
        {
            d.Add("Standard","ChargeAttack");
            if(ActiveWeapon.HasWeaponAttribute("Unarmed"))
            {
                d.Add("Grapple","GrappleAttack");    
            }
        }
        else if(ActivePlayerStats.ValidAction("Attack"))
        {
            if(ActiveWeapon.HasWeaponAttribute("Unarmed"))
            {
                d.Add("Standard","StandardAttack");
                d.Add("Knock-Down","KnockDown");
                if(halfActions > 1)
                {
                    d.Add("Stun","Stun");
                    d.Add("Grapple","GrappleAttack");
                }
            }
            else if(ActiveWeapon.HasWeaponAttribute("Melee"))
            {
                d.Add("Standard","StandardAttack");
                d.Add("Called","CalledShot");
                d.Add("Feint","Feint");
                if(halfActions > 1)
                {
                    d.Add("Guarded","GuardedAttack");
                    d.Add("All Out","AllOut");
                }
                
            }
            else if(ActivePlayerStats.ValidAction("Attack"))
            {
                if (ActiveWeapon.CanFire("S"))
                {
                    if(ActiveWeapon.HasWeaponAttribute("Flame"))
                    {
                        d.Add("Standard","FlameAttack");
                    }
                    else if(ActiveWeapon.HasWeaponAttribute("Blast"))
                    {
                        d.Add("Standard","BlastAttack");
                    }
                    else{
                    d.Add("Standard","StandardAttack");
                    d.Add("Called","CalledShot");
                    }
                }
                if (ActiveWeapon.CanFire("Semi") && halfActions > 1)
                {
                    d.Add("Semi Auto","SemiAuto");
                }
                if (ActiveWeapon.CanFire("Auto") && halfActions > 1)
                {
                    d.Add("Full Auto","FullAuto");
                    d.Add("Overwatch","Overwatch");
                    d.Add("Supression","SupressingFire");
                }
                if(ActiveWeapon.isJammed() && halfActions > 1)
                {
                    d.Add("Unjam","Unjam");
                }
            }
        }
        d.Add("Cancel","Combat");
        ConstructActions(d);
    }

    public void GuardedAttack()
    { 
        currentAction = "Attack";
        FireRate = "Full";
        ActivePlayerStats.SetCondition("GuardedAttack", 0, false);
        Dictionary<string,string> d = new Dictionary<string,string>();
        d.Add("Cancel","GuardedCancel");
        ConstructActions(d);
    }

    public void AllOut()
    {
        currentAction = "Attack";
        FireRate = "Full";
        ActivePlayerStats.SetCondition("AllOut", 0, false);
        Dictionary<string,string> d = new Dictionary<string,string>();
        d.Add("Cancel","AllOutCancel");
        ConstructActions(d);
    }

    public void DefensiveStance()
    {
        ActivePlayerStats.SetCondition("Defensive Stance", 0, true);
        halfActions -= 2;
    }

    public void Feint()
    {
        currentAction = "CM";
        FireRate = "Feint";
        ConstructActions(new List<string>{"Cancel"});
    }

    public void Stun()
    {
        currentAction = "CM";
        FireRate = "Stun";
        ConstructActions(new List<string>{"Cancel"});
    }
    
    public void KnockDown()
    {
        currentAction = "CM";
        FireRate = "Knock";
        ConstructActions(new List<string>{"Cancel"});
    }

    public void Charge()
    {
        FireRate = "Full";
        currentAction = "Charge";
        ConstructActions(new List<string>{"Cancel"});
    } 
    public void ChargeGrapple()
    {
        FireRate = "Grapple";
        currentAction = "Charge";
        ConstructActions(new List<string>{"Cancel"});
    } 

    public void GrappleAttack()
    {
        currentAction = "CM";
        FireRate = "Grapple";
        ConstructActions(new List<string>{"Cancel"});
    } 

    public void Unjam()
    {
        RollResult unjamResult = ActivePlayerStats.AbilityCheck("BS",0);
        if(unjamResult.Passed())
        {
            CombatLog.Log(ActivePlayerStats.GetName() + " successfully unjams their weapon!");
            PopUpText.CreateText("Unjammed!",Color.green,ActivePlayerStats.gameObject);
            ActiveWeapon.ExpendAmmo("Jam");
            ActiveWeapon.SetJamStatus(false);
        }
        else
        {
            CombatLog.Log(ActivePlayerStats.GetName() + " fails to unjams their weapon!");
        }
        halfActions-= 2;
        Cancel();
    }

    public void GrappleControl()
    {
        int result = ActivePlayerStats.OpposedAbilityCheck("S",ActivePlayerStats.grappler,0,0);
        if(result > 0)
        {
            ActivePlayerStats.ControlGrapple();
        }
        halfActions -= 2;
        Cancel();
    }

    public void GrappleScuffle()
    {
        int result = ActivePlayerStats.OpposedAbilityCheck("S",ActivePlayerStats.grappleTarget,0,0);
        if(result > 0)
        {
            TacticsAttack.DealDamage(ActivePlayerStats.grappleTarget,ActivePlayerStats,"Body",new Weapon(unarmed));
        }
        halfActions -= 2;
        Cancel();
    }

    public void GrappleRelease()
    {
        ActivePlayerStats.ReleaseGrapple();
        Cancel();
    }

    public void Run()
    {
        currentAction = "Run";
        ConstructActions(new List<string>{"Cancel"});
    }

    public void Advance()
    {
        currentAction = "Advance";
        ConstructActions(new List<string>{"Cancel"});
    }

    public void Move()
    {
        currentAction = "Move";
    }

    public void Disengage()
    {
        currentAction = "Disengage";
        ConstructActions(new List<string>{"Cancel"});
    }

    public void Stand()
    {
        currentAction = "Stand";
    }

    public void Misc()
    {
        Dictionary<string,string> d = new Dictionary<string, string>();
        if(ActivePlayerStats.ValidAction("Aim"))
        {
            d.Add("Aim (Half)","Aim1");
            d.Add("Reload","Reload");
            if(halfActions > 1)
            {
                d.Add("Aim (Full)","Aim2");
                d.Add("Defensive","DefensiveStance");
            }
        } 
        d.Add("Cancel","Cancel");
        ConstructActions(d);
    }

    public void Reload()
    {
        Dictionary<string,string> d = new Dictionary<string, string>();
        Weapon rw = ActivePlayerStats.RightHand;
        Weapon lw = ActivePlayerStats.LeftHand;
        if(rw != null && rw.CanReload())
        {
            d.Add(rw.GetName() + " (" + rw.GetReloads() + " half actions)", "ReloadRightHandWeapon");
        }
        if(lw != null && lw != rw && lw.CanReload())
        {
           d.Add(lw.GetName()  + " (" + lw.GetReloads() + " half actions)","ReloadLeftHandWeapon");
        }
        d.Add("Cancel","Cancel");
        ConstructActions(d);
    }

    public void ReloadRightHandWeapon()
    {
        ActivePlayerStats.SetRepeatingAction("ReloadRightHandWeapon");
        Cancel();
    }
    public void ReloadLeftHandWeapon()
    {
        ActivePlayerStats.SetRepeatingAction("ReloadLeftHandWeapon");
        Cancel();
    }

    //contructs actions where display name and function called are the same
    public void ConstructActions(List<string> l)
    {
        Dictionary<string,string> d = new Dictionary<string, string>();
        foreach (string s in l)
        {
            d.Add(s,s);
        }
        ConstructActions(d);
    }
    //constructs default actions 
    public void ConstructActions()
    {   
        Dictionary<string,string> d = new Dictionary<string, string>();
        if(ActivePlayerStats.grappling())
        {
            if(halfActions > 1)
            {
                if(ActivePlayerStats.hasCondition("Grappled"))
                {
                    d.Add("Control Grapple","GrappleControl");
                }
                else
                {
                    d.Add("Scuffle", "GrappleScuffle");
                    d.Add("Release Grappe","GrappleRelease");
                }
            }
        }
        else{
            foreach(string s in DefaultActions)
            {
                if(s.Equals("Attack") || ActivePlayerStats.ValidAction(s))
                {
                    d.Add(s,s);
                }
            }
        }
        d.Add("End Turn","EndTurn");
        ConstructActions(d);
    }
    //creates a set of interactable buttons can key = text value = method called
    public void ConstructActions(Dictionary<string, string> d)
    {
        GameObject[] oldButtons = GameObject.FindGameObjectsWithTag("ActionInput");
        foreach(GameObject g in oldButtons)
        {
            g.GetComponent<ActionButtonScript>().DestroyMe();
        }
        if(d != null)
        {
            int displacement = 0;
            foreach (KeyValuePair<string, string> kvp in d)
            {
                GameObject newButton = Instantiate(ActionUIButton) as GameObject;
                newButton.transform.SetParent(Canvas.transform, false);
                newButton.transform.position += new Vector3(displacement,0,0);
                newButton.GetComponent<ActionButtonScript>().SetAction(kvp.Value);
                newButton.GetComponent<ActionButtonScript>().SetText(kvp.Key);
                displacement += 150;
            }
        }
    }

    //is passed the action value of a button as an input
    public void OnButtonPressed(string input)
    {
        //if the input is the name of an equiped weapon
        if(isWeaponInput(input))
        {
            ActiveWeapon = StringToWeapon(input);
            if (currentAction.Equals("Ready"))
            {
                if(ActivePlayerStats.Equip(ActiveWeapon))
                {   
                    halfActions--;
                    ActivePlayerStats.SpendAction("Ready");
                    Cancel();
                }
                else
                {
                    Invoke("Ready2",0);
                }
            }
        }
        //if not then just invoke the corresponding function
        else if(!ActivePlayer.moving || currentAction != "Move")
        {    
            Invoke(input,0);
        }
    }
    
    public bool inMelee()
    {
        foreach ( PlayerStats p in ActivePlayer.AdjacentPlayers())
        {
            if (p != null && p.GetTeam() != ActivePlayerStats.GetTeam())
            {
                return true;
            }
        }
        return false;
    }
    
    public void RightHandUnequip()
    {
        ActivePlayerStats.Equip(ActiveWeapon, "Right");
        ActivePlayerStats.SpendAction("Ready");
        halfActions--;
        Cancel();
    }

    public void LeftHandUnequip()
    {
        ActivePlayerStats.Equip(ActiveWeapon, "Left");
        ActivePlayerStats.SpendAction("Ready");
        halfActions--;
        Cancel();
    }

    public void StandardAttack()
    {
        currentAction = "Attack";
        FireRate = "S";
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    }
    public void ChargeAttack()
    {
        currentAction ="Attack";
        FireRate = "Full";
        Dictionary<string,string> d = new Dictionary<string, string>();
        d.Add("Cancel","Combat");
        ConstructActions(d);
    }
    public void CalledShot()
    {
        Dictionary<string, string> d = new Dictionary<string, string>();
        d.Add("Head", "Head");
        d.Add("Right Arm","RightArm");
        d.Add("Left Arm","LeftArm");
        d.Add("Body","Body");
        d.Add("Right Leg","RightLeg");
        d.Add("Left Leg","LeftLeg");
        d.Add("Cancel","Cancel");
        currentAction = null;
        ConstructActions(d);
    }

    public void Head()
    {
        currentAction = "Attack";
        FireRate = "S";
        HitLocation = "Head";
        ActivePlayerStats.SetCondition("Called",1,false);

        Dictionary<string, string> d = new Dictionary<string,string>();
        d.Add("Cancel", "CalledCancel");
        ConstructActions(d);
    }
    public void RightArm()
    {
        currentAction = "Attack";
        FireRate = "S";
        HitLocation = "RightArm";
        ActivePlayerStats.SetCondition("Called",1,false);
        
        Dictionary<string, string> d = new Dictionary<string,string>();
        d.Add("Cancel", "CalledCancel");
        ConstructActions(d);
    }
    public void LeftArm()
    {
        currentAction = "Attack";
        FireRate = "S";
        HitLocation = "LeftArm";
        ActivePlayerStats.SetCondition("Called",1,false);
        
        Dictionary<string, string> d = new Dictionary<string,string>();
        d.Add("Cancel", "CalledCancel");
        ConstructActions(d);
    }
    public void Body()
    {
        currentAction = "Attack";
        FireRate = "S";
        HitLocation = "Body";
        ActivePlayerStats.SetCondition("Called",1,false);
        
        Dictionary<string, string> d = new Dictionary<string,string>();
        d.Add("Cancel", "CalledCancel");
        ConstructActions(d);
    }
    public void RightLeg()
    {
        currentAction = "Attack";
        FireRate = "S";
        HitLocation = "RightLeg";
        ActivePlayerStats.SetCondition("Called",1,false);
        
        Dictionary<string, string> d = new Dictionary<string,string>();
        d.Add("Cancel", "CalledCancel");
        ConstructActions(d);
    }
    public void LeftLeg()
    {
        currentAction = "Attack";
        FireRate = "S";
        HitLocation = "LeftLeg";
        ActivePlayerStats.SetCondition("Called",1,false);
        
        Dictionary<string, string> d = new Dictionary<string,string>();
        d.Add("Cancel", "CalledCancel");
        ConstructActions(d);
    }

    public void CalledCancel()
    {
        //to implement trait that avoids this penalty
        ActivePlayerStats.RemoveCondition("Called");
        Cancel();
    }
    
    public void SemiAuto()
    {
        currentAction = "Attack";
        FireRate = "Semi";
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    }
    public void FullAuto()
    {
        currentAction = "Attack";
        FireRate = "Auto";
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    }
    public void BlastAttack()
    {
        currentAction = "ThreatRange";
        CreateThreatRange("Blast");
        Dictionary<string, string> d = new Dictionary<string, string>();
        d.Add("Cancel", "RemoveRange");
        ConstructActions(d);
    }

    public void BlastAttack(List<Transform> targets)
    {
        FireRate = "S";
        ActiveWeapon.ExpendAmmo(FireRate);
        foreach(Transform t in targets)
        {
            AttackQueue.Enqueue(new AttackSequence(t.GetComponent<PlayerStats>(),ActivePlayerStats,ActiveWeapon,FireRate,1));
        }
        RemoveRange(ActivePlayerStats);
        halfActions--;
        Cancel();
    }

    public void FlameAttack()
    {
        currentAction = "ThreatRange";
        CreateThreatRange("Flame");
        Dictionary<string, string> d = new Dictionary<string, string>();
        d.Add("Cancel", "RemoveRange");
        ConstructActions(d);
    }
    public void FlameAttack(List<Transform> targets)
    {
        FireRate = "S";
        ActiveWeapon.ExpendAmmo(FireRate);
        int roll = Random.Range(1,10);
        if(!TacticsAttack.Jammed(roll, ActiveWeapon,FireRate,ActivePlayerStats))
        {
            foreach(Transform t in targets)
            {
                PlayerStats CurrentStats = t.GetComponent<PlayerStats>();
                //only attacker is out of range of the spray. ALLIES CAN GET HIT TOO
                if(CurrentStats != ActivePlayerStats)
                {
                    CurrentStats.SetCondition("Under Fire", 1,false);
                    RollResult AvoidResult = CurrentStats.AbilityCheck("A",0);
                    if(!AvoidResult.Passed())
                    {
                        CombatLog.Log(CurrentStats.GetName() + " is caught in the fire!");
                        RollResult FireResult = CurrentStats.AbilityCheck("A",0);
                        if(!FireResult.Passed())
                        {
                            CombatLog.Log(CurrentStats.GetName() + " is set ablaze!");
                            CurrentStats.SetCondition("On Fire", 0, true);
                        }
                        TacticsAttack.DealDamage(CurrentStats, ActivePlayerStats, "Body", ActiveWeapon);
                    }
                    else
                    {
                        CombatLog.Log(CurrentStats.GetName() + " avoids the fire!");
                    }
                }
            }
        }
        halfActions--;
        Cancel();
    }



    public void Ready()
    {
        List<string> l = new List<string>();
        foreach(Weapon w in ActivePlayerStats.equipment)
        {
            if (w != ActivePlayerStats.LeftHand && w != ActivePlayerStats.RightHand)
            {
                l.Add(w.GetName());
            }
        }
        //if no equipment exists!
        if(l.Count == 0)
        {
            Debug.Log("No equipment to display!");
        }
        else
        {   
            l.Add("Cancel");
            currentAction = "Ready";
            ConstructActions(l);
        }
    }

    public void Ready2()
    {
        Dictionary<string,string> d = new Dictionary<string, string>();
        d.Add(ActivePlayerStats.RightHand.GetName(), "RightHandUnequip");
        d.Add(ActivePlayerStats.LeftHand.GetName(), "LeftHandUnequip");
        ConstructActions(d);
    }

    public void Aim1()
    {
        ActivePlayerStats.SpendAction("Aim");
        int modifier = 0;
        //so if this is the last action made this turn the aim will presist to the next turn
        if(halfActions == 1)
        {
            modifier++;
        }
        ActivePlayerStats.SetCondition("Half Aiming", 1 + modifier, true);
        halfActions--;
        Cancel();
    }

    public void Aim2()
    {
        ActivePlayerStats.SpendAction("Aim");
        ActivePlayerStats.SetCondition("Full Aiming", 2, true);
        halfActions-= 2;
        Cancel();
    }

    public bool isWeaponInput(string input)
    {
        foreach(Weapon w in ActivePlayerStats.equipment)
        {
            if(w.GetName().Equals(input))
            {
                return true;
            }
        }
        return false;
    }

    public Weapon StringToWeapon(string input)
    {
        foreach(Weapon w in ActivePlayerStats.equipment)
        {
            if(w.GetName().Equals(input))
            {
                return w;
            }
        }
        return null;
    }
    
    public void SupressingFire()
    {
        currentAction = "ThreatRange";
        CreateThreatRange("Supress");
        Dictionary<string, string> d = new Dictionary<string, string>();
        d.Add("Cancel", "RemoveRange");
        ConstructActions(d);
    }

    public void Overwatch()
    {
        currentAction = "ThreatRange";
        CreateThreatRange("Overwatch");
        Dictionary<string, string> d = new Dictionary<string, string>();
        d.Add("Cancel", "RemoveRange");
        ConstructActions(d);
    }

    public void OverwatchFinished()
    {
        halfActions -= 2;
        Cancel();
    }
    public void RemoveRange()
    {
        RemoveRange(ActivePlayerStats);
        Cancel();
    }

    public void RemoveRange(PlayerStats owner)
    {
        GameObject[] ActiveRanges = GameObject.FindGameObjectsWithTag("Range");
        foreach(GameObject g in ActiveRanges)
        {
            //only remove ranges that are tied to me
            g.GetComponent<ThreatRangeBehavior>().RemoveRange(owner);
            //if its my turn then cancel
        }
    }

    public void TryReaction()
    {
        currentAction = "Reacting";
        CurrentAttack.target.GetComponent<TacticsMovement>().FindSelectableTiles(0,0,ActivePlayerStats.GetTeam());
        if(CurrentAttack.attacks > 0 && CurrentAttack.target.ValidAction("reaction") && !CurrentAttack.target.hasCondition("AllOut") && !CurrentAttack.attacker.hasCondition("Feinted"))
        {
            List<string> l = new List<string>();
            l.Add("Dodge");
            if(CurrentAttack.ActiveWeapon.HasWeaponAttribute("Melee") && CurrentAttack.target.CanParry() && !CurrentAttack.ActiveWeapon.HasWeaponAttribute("Flexible"))
            {
                l.Add("Parry");
            }
            l.Add("NoReaction");
            ConstructActions(l);
        }
        else
        {
            ResolveHit();
        }
    }

    public void Dodge()
    {
        CurrentAttack.target.SpendAction("reaction");
        RollResult DodgeResult = CurrentAttack.target.AbilityCheck("Dodge",0);
        if(DodgeResult.Passed())
        {
            CurrentAttack.attacks -= (DodgeResult.GetDOF() + 1); 
        }
        RemoveRange(target);
        ClearActions();
        ResolveHit();
    }

    public void Parry()
    { 
        CurrentAttack.target.SpendAction("reaction");
        int modifier = 0;
        RollResult ParryResult = CurrentAttack.target.AbilityCheck("Parry",modifier);
        if(ParryResult.Passed())
        {
            CurrentAttack.attacks--; 
        }
        RemoveRange(target);
        ClearActions();
        ResolveHit();
    }
    public void NoReaction()
    {
        ClearActions();
        ResolveHit();
    }

    public void ResolveHit()
    {
        if(CurrentAttack.FireRate.Equals("Stun"))
        {
            if(CurrentAttack.attacks > 0)
            {
                int StunDamage = Random.Range(1,10) + CurrentAttack.attacker.GetStatScore("S");
                int StunResist = Random.Range(1,10) + CurrentAttack.target.GetStatScore("T") + CurrentAttack.target.GetStat("Head");
                CombatLog.Log(CurrentAttack.attacker.GetName() + " Rolls 1d10 + SB = " + StunDamage);
                CombatLog.Log(CurrentAttack.target.name + " Rolls 1d10 + T + HeadAP = " + StunResist);
                int diff = StunDamage - StunResist;
                if (diff == 0)
                {
                    diff = 1;
                }
                if (diff > 0) 
                {
                    CurrentAttack.target.takeFatigue(1);
                    CombatLog.Log(CurrentAttack.target.GetName() + " is stunned for " + diff + " rounds");
                    target.SetCondition("Stunned", (diff), true);
                }
                else
                {
                    PopUpText.CreateText("Resisted!", Color.yellow,CurrentAttack.target.gameObject);
                }
            }
            
            CurrentAttack = null;
            Cancel();
        }
        else if(CurrentAttack.FireRate.Equals("Grapple"))
        {
            if(CurrentAttack.attacks > 0)
            {
                CurrentAttack.target.SetGrappler(CurrentAttack.attacker);
            }
            
            CurrentAttack = null;
            Cancel();
        }
        else
        {    
            /*if(AttackQueue.Peek().FireRate != "Free" && target != ActivePlayerStats)
            {
            ActivePlayerStats.SpendAction("Attack");  
            }
            */
            //Debug.Log("Hits " + attacks);
            if(CurrentAttack.HitLocation != null && CurrentAttack.attacks > 0)
            {
                TacticsAttack.DealDamage(CurrentAttack.target, CurrentAttack.attacker, CurrentAttack.HitLocation, CurrentAttack.ActiveWeapon);
                CurrentAttack = null;
                //halfActions--;
                Cancel();
            }
            else
            {
                StartCoroutine(AttackCoroutine());
            }
        }

    }

    public void SupressingFire(List<Transform> targets)
    {
        FireRate = "Auto";
        ValidTargets = new List<PlayerStats>();
        foreach(Transform t in targets)
        {
            PlayerStats CurrentStats = t.GetComponent<PlayerStats>();
            //only attacker is out of range of the spray. ALLIES CAN GET HIT TOO
            if(CurrentStats != ActivePlayerStats)
            {
                ValidTargets.Add(CurrentStats);
            }
            if(CurrentStats.GetTeam() != ActivePlayerStats.GetTeam())
            {
                if(!CurrentStats.AbilityCheck("WP",-20).Passed())
                {
                    CurrentStats.SetCondition("Pinned",0,true);
                }
                CurrentStats.SetCondition("Under Fire", 1,false);
            }
        } 
        RollResult SprayResult = ActivePlayerStats.AbilityCheck("BS",-20);
        int maxShots = ActiveWeapon.ExpendAmmo("Auto");
        //allocate random attacks
        if(SprayResult.Passed() && targets.Count > 0)
        {
            RepeatedAttacks = 1 + (SprayResult.GetDOF()/2);
            if(RepeatedAttacks > maxShots)
            {
                RepeatedAttacks = maxShots;
            }
            for(int i = 0; i < RepeatedAttacks; i++)
            {
                int randomIndex = Random.Range(1,targets.Count);
                PlayerStats currentStats = targets[randomIndex].GetComponent<PlayerStats>();
                //to prevent the shots from hitting the user
                AttackQueue.Enqueue(new AttackSequence (currentStats, ActivePlayerStats, ActiveWeapon,FireRate,1));
            }
        }
        halfActions -= 2;
    }

    public void CreateThreatRange(string type)
    {
        GameObject newThreatRange = Instantiate(ThreatRangePrefab, ActivePlayer.transform.position - new Vector3(0,ActivePlayer.transform.position.y - 0.05f,0), Quaternion.identity);
        newThreatRange.GetComponent<ThreatRangeBehavior>().SetParameters(type, ActiveWeapon, ActivePlayerStats);
    }
    IEnumerator AttackCoroutine()
    {
        
        for (int i = 0 ; i < CurrentAttack.attacks; i++)
        {
            yield return new WaitForSeconds(2);
            TacticsAttack.DealDamage(CurrentAttack.target, CurrentAttack.attacker, Random.Range(1,100), CurrentAttack.ActiveWeapon);
        }
        /*so that overwatch doesn't eat up your actions
        if(ActivePlayerStats != target)
        {
            if(FireRate != "Free")
            {
                ActivePlayerStats.SpendAction("Attack");
                halfActions--;
                if(FireRate != "S")
                {
                    halfActions--;
                }
            }
        }
        */
        
        while (!PopUpText.FinishedPrinting())
        {
            yield return new WaitForSeconds(0.2f);
        }
        
        CurrentAttack = null;
        Cancel();
    }

    public void ClearActions()
    {
        GameObject[] oldButtons = GameObject.FindGameObjectsWithTag("ActionInput");
        foreach(GameObject g in oldButtons)
        {
            g.GetComponent<ActionButtonScript>().DestroyMe();
        }
    }

    public void AddAction()
    {
        halfActions++;
        Cancel();
    }

    public void SubtractAction()
    {
        halfActions--;
        Cancel();
    }

    public void GuardedCancel()
    {
        ActivePlayerStats.RemoveCondition("GuardedAttack");
        Cancel();
    }

    public void AllOutCancel()
    {
        ActivePlayerStats.RemoveCondition("AllOut");
        Cancel();
    }

    public void Cancel()
    {
        ActiveWeapon = null;
        ConstructActions();
        if(ActivePlayerStats.GetRepeatingAction() != null)
        {
            currentAction = ActivePlayerStats.GetRepeatingAction();
        }
        else
        {
            currentAction = "Move";
        }
        FireRate = null;
        target = null;
        HitLocation = null;
        ValidTargets = null;
        RepeatedAttacks = 0;
        attacks = 0;
        PlayerUIDisplay.GetComponent<UIPlayerInfo>().UpdateDisplay(ActivePlayerStats, halfActions);
    }
}

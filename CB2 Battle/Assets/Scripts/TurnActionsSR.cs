using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TurnActionsSR : UIButtonManager
{
    public TacticsMovement ActivePlayer; 
    public List<string> DefaultActions;
    public PlayerStats ActivePlayerStats;
    protected string currentAction;
    protected Weapon ActiveWeapon;
    public GameObject Canvas;
    public GameObject ActionUIButton;
    protected int halfActions;
    protected int freeActions;
    protected string FireRate;
    protected PlayerStats target;
    protected int attacks;
    public GameObject ThreatRangePrefab;
    public WeaponTemplate unarmed;
    int RepeatedAttacks;
    protected string InteruptedAction;
    public List<PlayerStats> ValidTargets;
    protected Queue<AttackSequence> AttackQueue = new Queue<AttackSequence>();
    protected AttackSequence CurrentAttack;

    public void Combat()
    {
        TokenDragBehavior.ToggleMovement(false);
        Dictionary<string,string> d = new Dictionary<string, string>();
        
        if(ActivePlayerStats.PrimaryWeapon != null) 
        {
            d.Add(ActivePlayerStats.PrimaryWeapon.GetName() + " (Primary)", "PrimaryWeapon");
        }
        if (ActivePlayerStats.SecondaryWeapon != null)
        {
            d.Add(ActivePlayerStats.SecondaryWeapon.GetName() + " (Off Handed)","SecondaryWeapon");
        }
        d.Add("Cancel","Cancel");
        ConstructActions(d);
    }

    public void Movement()
    {
        currentAction = null;
        List<string> l = new List<string>();
        
        if(ActivePlayerStats.hasCondition(Condition.Prone))
        {
            l.Add("Stand");
        }
        else
        {
            l.Add("Prone");
            l.Add("Run");
        }
        l.Add("Cancel");
        ConstructActions(l);
    }

    public void Run()
    {
        ActivePlayerStats.Run();
        SpendFreeAction();
        Cancel();
    }

    
    public void Prone()
    {
        ActivePlayerStats.Prone();
        SpendFreeAction();
        Cancel();
    }


    public void PrimaryWeapon()
    {
        ActiveWeapon = ActivePlayerStats.PrimaryWeapon;
        GetWeaponActions();
    }

    public void SecondaryWeapon()
    {
        ActiveWeapon = ActivePlayerStats.SecondaryWeapon;
        GetWeaponActions();
    }

    public void Unarmed()
    {
        ActiveWeapon = new Weapon(unarmed);
        GetWeaponActions();
    }

    public void GetWeaponActions()
    {
        Dictionary<string,string> d = ActiveWeapon.GetWeaponActions(halfActions > 1);
        d.Add("Cancel","Combat");
        ConstructActions(d);
        /*
        //only melee weapons can be used on charge 
        if(!ActivePlayerStats.ValidAction("Charge"))
        {
            d.Add("Standard","ChargeAttack");
            if(ActiveWeapon.HasWeaponAttribute("Unarmed"))
            {
                d.Add("Grapple","GrappleAttack");    
            }
        }
        else if(ActivePlayerStats.ValidAction("Attack") && halfActions > 0)
        {
            if(ActiveWeapon.HasWeaponAttribute("Unarmed"))
            {
                ActivePlayer.RemoveSelectableTiles();
                ActivePlayer.GetValidAttackTargets(ActiveWeapon);
                d.Add("Standard","StandardAttack");
                d.Add("Knock-Down","KnockDown");
                if(halfActions > 1)
                {
                    d.Add("Stun","Stun");
                    d.Add("Grapple","GrappleAttack");
                }
            }
            else if(ActiveWeapon.IsWeaponClass("Melee"))
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
                if(!ActiveWeapon.HasWeaponAttribute("Heavy") || (ActiveWeapon.HasWeaponAttribute("Heavy") && !ActivePlayerStats.IsDualWielding()))
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
                    // Heavy weapons cannot be fired in semi or full automatic unless a character braces
                    if(ActiveWeapon.IsWeaponClass("Heavy") && !ActivePlayerStats.hasCondition("Braced"))
                    {
                        d.Add("Brace (cover)","Brace");
                        d.Add("Brace (prone)","BraceProne");
                    }
                    else
                    {
                        if (ActiveWeapon.CanFire("Semi"))
                        {
                            d.Add("Semi Auto","SemiAuto");
                        }
                        if(ActiveWeapon.CanFire("Auto"))
                        {
                            d.Add("Full Auto","FullAuto");
                        }
                        if (( ActiveWeapon.CanFire("Semi") || ActiveWeapon.CanFire("Auto")) && halfActions > 1 )
                        {
                            d.Add("Overwatch","Overwatch");
                            d.Add("Supression","SupressingFire");
                        }
                    }
                }
                if(ActiveWeapon.isJammed() && halfActions > 1)
                {
                    d.Add("Unjam","Unjam");
                }
            }
        }
        */
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
        //ActivePlayer.FindChargableTiles(ActivePlayerStats.GetStat("MoveCharge"),ActivePlayerStats.GetTeam());     
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
        StartCoroutine(UnjamDelay());
    }

    IEnumerator UnjamDelay()
    {
        RollResult unjamResult = ActivePlayerStats.AbilityCheck("BS",0);
        while(!unjamResult.Completed())
        {
            yield return new WaitForSeconds(0.5f);
        }
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

    //restricted actions: actions that are only available when the player is restricted somehow
    public void GrappleControl()
    {
        StartCoroutine(GrappleControlDelay());
    }

    IEnumerator GrappleControlDelay()
    {
        RollResult opposedStrengthcheck = ActivePlayerStats.AbilityCheck("S",0,null,ActivePlayerStats.grappler);
        while(!opposedStrengthcheck.Completed())
        {
            yield return new WaitForSeconds(0.5f);
        }
        int result = opposedStrengthcheck.GetDOF();
        if(result > 0)
        {
            CombatLog.Log(ActivePlayerStats.GetName() + " gains control of the grapple!");
            ActivePlayerStats.ControlGrapple();
        }
        else
        {
            CombatLog.Log(ActivePlayerStats.GetName() + " fails to control the grapple!");
        }
        halfActions -= 2;
        Cancel();
    }

    public void GrappleContortionist()
    {
        ActivePlayerStats.AbilityCheck("Contortionist",0,"EscapeBonds");
        halfActions -=2;
        Cancel();
    }

    public void GrappleScuffle()
    {
        StartCoroutine(GrappleScuffleDelay());
    }
    IEnumerator GrappleScuffleDelay()
    {
        RollResult opposedStrengthcheck = ActivePlayerStats.AbilityCheck("S",0,null,ActivePlayerStats.grappleTarget);
        while(!opposedStrengthcheck.Completed())
        {
            yield return new WaitForSeconds(0.5f);
        }
        int result = opposedStrengthcheck.GetDOF();
        if(result > 0)
        {
            CombatLog.Log(ActivePlayerStats.GetName() + " strikes their grapple target!");
            // if the grapple target has an unarmed weapon, attack with that weapon instead
            Weapon unarmedWeapon;
            if(ActivePlayerStats.PrimaryWeapon.HasWeaponAttribute("Unarmed"))
            {
                unarmedWeapon = ActivePlayerStats.PrimaryWeapon;
            }
            else if(ActivePlayerStats.SecondaryWeapon.HasWeaponAttribute("Unarmed"))
            {
                unarmedWeapon = ActivePlayerStats.SecondaryWeapon;
            }
            else
            {
                unarmedWeapon = new Weapon(unarmed);
            }
            TacticsAttack.DealDamage(ActivePlayerStats.grappleTarget,ActivePlayerStats,"Body",unarmedWeapon);
        }
        CombatLog.Log(ActivePlayerStats.GetName() + " is unable to strike their grapple target.");
        halfActions -= 2;
        Cancel();
    }

    public void GrappleRelease()
    {
        CombatLog.Log(ActivePlayerStats.GetName() + " chooses to end the grapple!");
        ActivePlayerStats.ReleaseGrapple();
        Cancel();
    }

    public void Move()
    {
        currentAction = "Move"; 
        ActivePlayer.GetCurrentTile();
    }

    public void Stand()
    {
       ActivePlayerStats.Stand();
       halfActions--;
       Cancel();
    }

    public void Misc()
    {
        currentAction = null;
        Dictionary<string,string> d = new Dictionary<string, string>();
        if(halfActions > 0)
        {
            d.Add("Aim","Aim");
            d.Add("Reload","Reload");
            if(BoardBehavior.InCover(ActivePlayerStats.gameObject))
            {
                d.Add("Take Cover","takecover");
            }
        }
        d.Add("Cancel","Cancel");
        ConstructActions(d);
    }

    public void Reload()
    {
        Dictionary<string,string> d = new Dictionary<string, string>();
        Weapon rw = ActivePlayerStats.PrimaryWeapon;
        Weapon lw = ActivePlayerStats.SecondaryWeapon;
        if(rw != null && rw.CanReload(ActivePlayerStats))
        {
            d.Add(rw.GetName() + " " +  rw.ReloadString(), "ReloadPrimaryWeapon");
        }
        if(lw != null && lw != rw && lw.CanReload(ActivePlayerStats))
        {
            string addition = "";
            if(rw.GetName() == lw.GetName())
            {
                addition = " (off hand)";
            }
           d.Add(lw.GetName() + addition + " " + lw.ReloadString(),"ReloadSecondaryWeapon");
        }
        d.Add("Cancel","Cancel");
        ConstructActions(d);
    }

    public void ReloadPrimaryWeapon()
    {
        halfActions -= ActivePlayerStats.PrimaryWeapon.ReloadWeapon(ActivePlayerStats, true);
        Cancel();
    }
    public void ReloadSecondaryWeapon()
    {
        halfActions -= ActivePlayerStats.SecondaryWeapon.ReloadWeapon(ActivePlayerStats, true);
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
        if(ActivePlayerStats.Grappling())
        {
            if(halfActions > 1)
            {
                if(ActivePlayerStats.hasCondition(Condition.Grappled))
                {
                    d.Add("Control Grapple","GrappleControl");
                    d.Add("Escape Grapple","GrappleContortionist");
                }
                else
                {
                    d.Add("Scuffle", "GrappleScuffle");
                    d.Add("Release Grapple","GrappleRelease");
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

    //is passed the action value of a button as an input
    override public void OnButtonPressed(string input)
    {
        int index;
        if(int.TryParse(input,out index))
        {
            ActiveWeapon = ActivePlayerStats.GetWeaponsForEquipment().ToArray()[index];
            Ready2();
        }
        else if(!ActivePlayer.moving || currentAction != "Move")
        {    
            Invoke(input,0);
            //pv.RPC("RPC_OnButtonPressed",RpcTarget.MasterClient,input);
        }
    }
    
    public void PrimaryWeaponUnequip()
    {
        ActivePlayerStats.EquipPrimary(ActiveWeapon);
        ActivePlayerStats.SpendAction("Ready");
        halfActions--;
        Cancel();
    }

    public void SecondaryWeaponUnequip()
    {
        ActivePlayerStats.EquipSecondary(ActiveWeapon);
        ActivePlayerStats.SpendAction("Ready");
        halfActions--;
        Cancel();
    }

    public void takecover()
    {
        ActivePlayerStats.SetCondition(Condition.Covered, -1, true);
        halfActions--;
        Cancel();
    }

    public void ChargeAttack()
    {
        currentAction ="Attack";
        FireRate = "Full";
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
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

    public void CalledCancel()
    {
        //to implement trait that avoids this penalty
        ActivePlayerStats.RemoveCondition(Condition.CalledShot);
        Cancel();
    }
    
    public void BlastAttack()
    {
        currentAction = "ThreatRange";
        CreateThreatRange("Blast");
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        Dictionary<string, string> d = new Dictionary<string, string>();
        d.Add("Cancel", "RemoveRange");
        ConstructActions(d);
    }

    public void ChangeFireRate()
    {
        Dictionary<string,string> d = ActiveWeapon.GetSelectableFireRates();
        d.Add("Cancel","Cancel");
        ConstructActions(d);
    }

    public void Melee()
    {
        currentAction = "Attack";
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    }

    public void fireSS()
    {
        currentAction = "Attack";
        FireRate = "SS";
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    }

    public void fireSA()
    {
        currentAction = "Attack";
        FireRate = "SA";
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    }
    public void fireSAB()
    {
        currentAction = "Attack";
        FireRate = "SAB";
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    }

    public void fireBF()
    {
        currentAction = "Attack";
        FireRate = "BF";
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    }
    public void fireLB()
    {
        currentAction = "Attack";
        FireRate = "LB";
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    } 
    public void fireFA()
    {
        currentAction = "Attack";
        FireRate = "FA";
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    }

    public void fireFAB()
    {
        currentAction = "Attack";
        FireRate = "FAB";
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    } 

    public void setSS()
    {
        CombatLog.Log(ActivePlayerStats.GetName() + " sets their " + ActiveWeapon.GetName() + " to single shot mode!");
        ActiveWeapon.setFireRate("SS");
        halfActions--;
        Cancel();
    }

    public void setSA()
    {
        CombatLog.Log(ActivePlayerStats.GetName() + " sets their " + ActiveWeapon.GetName() + " to semi-auto mode!");
        ActiveWeapon.setFireRate("SA");
        halfActions--;
        Cancel();
    }

    public void setBF()
    {
        CombatLog.Log(ActivePlayerStats.GetName() + " sets their " + ActiveWeapon.GetName() + " to burst-fire mode!");
        ActiveWeapon.setFireRate("BF");
        halfActions--;
        Cancel();
    }
    public void setFA()
    {
        CombatLog.Log(ActivePlayerStats.GetName() + " sets their " + ActiveWeapon.GetName() + " to full-auto mode!");
        ActiveWeapon.setFireRate("FA");
        halfActions--;
        Cancel();
    }

    public void BlastAttack(List<Transform> targets)
    {
        FireRate = "S";
        ActiveWeapon.ExpendAmmo(FireRate);
        foreach(Transform t in targets)
        {
            AttackQueue.Enqueue(new AttackSequence(t.GetComponent<PlayerStats>(), ActivePlayerStats, ActiveWeapon,FireRate,1,true));
        }
        halfActions--;
        RemoveRange(ActivePlayerStats);
        Cancel();
    }

    public void FlameAttack()
    {
        currentAction = "ThreatRange";
        CreateThreatRange("Flame");
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        Dictionary<string, string> d = new Dictionary<string, string>();
        d.Add("Cancel", "RemoveRange");
        ConstructActions(d);
    }
    public void FlameAttack(List<Transform> targets)
    {
        FireRate = "S";
        if(targets.Count > 0)
        {
            ActiveWeapon.ExpendAmmo(FireRate);
            int roll = Random.Range(1,11);
            /*
            if(!TacticsAttack.Jammed(roll, ActiveWeapon,FireRate,ActivePlayerStats))
            {
                foreach(Transform t in targets)
                {
                    PlayerStats CurrentStats = t.GetComponent<PlayerStats>();
                    //only attacker is out of range of the spray. ALLIES CAN GET HIT TOO
                    StartCoroutine(WaitForFireResult(CurrentStats, ActiveWeapon));
                }
            }
            */
        halfActions--;
        }
        Cancel();
    }

    IEnumerator WaitForFireResult(PlayerStats target, Weapon w)
    {
        RollResult AvoidResult = target.AbilityCheck("A",0);
        while(!AvoidResult.Completed())
        {
            yield return new WaitForSeconds(0.5f);
        }
        if(!AvoidResult.Passed())
        {
            CombatLog.Log(target.GetName() + " is hit by the " + w.GetName() + "'s spray!");
            /*if(!target.hasCondition("On Fire"))
            {
                target.AbilityCheck("A",0,"Fire");
            }
            */
            TacticsAttack.DealDamage(target, ActivePlayerStats, "Body", w);
        }
        else
        {
            CombatLog.Log(target.GetName() + " avoids the " + w.GetName() + "'s spray!");
        }
    }



    public void Ready()
    {
        currentAction = null;
        Dictionary<string,string> d = new Dictionary<string, string>();
        
        if(halfActions > 0)
        {
            Weapon[] equipment = ActivePlayerStats.GetWeaponsForEquipment().ToArray();
            for(int i = 0; i < equipment.Length; i++)
            {
                Weapon w = equipment[i];
                if (w != ActivePlayerStats.SecondaryWeapon && w != ActivePlayerStats.PrimaryWeapon && !d.ContainsKey(w.GetName()))
                {
                    d.Add(w.GetName(),"" + i);
                }
            }
            if(ActivePlayerStats.PrimaryWeapon != null)
            {
                d.Add("Unequip Primary (" + ActivePlayerStats.PrimaryWeapon.GetName() +")", "PrimaryWeaponUnequip");
            }
            if(ActivePlayerStats.SecondaryWeapon != null)
            {
                d.Add("Unequip Secondary (" + ActivePlayerStats.SecondaryWeapon.GetName() +")", "SecondaryWeaponUnequip");
            }
            currentAction = "Ready";
        }
        d.Add("Cancel","Cancel");
        ConstructActions(d);
    }



    public void Ready2()
    {
        Dictionary<string,string> d = new Dictionary<string,string>();
        string firstButtonName = "Equip Primary";
        string SecondButtonName = null;
        if(ActivePlayerStats.PrimaryWeapon != null)
        {
            firstButtonName = "Swap Primary (" + ActivePlayerStats.PrimaryWeapon.GetName() +")";
            SecondButtonName = "Equip Secondary";
            if(ActivePlayerStats.SecondaryWeapon != null)
            {
                SecondButtonName = "Swap Secondary (" + ActivePlayerStats.SecondaryWeapon.GetName() +")";
            }
        }
        d.Add(firstButtonName,"PrimaryWeaponUnequip");
        if(SecondButtonName != null)
        {
            d.Add(SecondButtonName,"SecondaryWeaponUnequip");
        }
        d.Add("Cancel","Cancel");
        ConstructActions(d);
    }

    public void Aim()
    {
        ActivePlayerStats.SetCondition(Condition.Aiming, 0, true);
        ActivePlayerStats.ResetRecoilPenalty();
        CombatLog.Log(ActivePlayerStats.GetName() + " steadies their aim, reseting their recoil penalty!");
        halfActions--;
        Cancel();
    }

    public void Extinguish()
    {
        ActivePlayerStats.AbilityCheck("A",0,"Fire");
        halfActions -= 2;
        Cancel();
    }

    public bool isWeaponInput(string input)
    {
        foreach(Weapon w in ActivePlayerStats.GetWeaponsForEquipment())
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
        foreach(Weapon w in ActivePlayerStats.GetWeaponsForEquipment())
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
        //CurrentAttack.attacker.GetComponent<TacticsMovement>().RemoveSelectableTiles();
        CurrentAttack.target.GetComponent<TacticsMovement>().PaintCurrentTile("current");
        if(!CurrentAttack.AttackMissed)
        {
            CombatLog.Log(CurrentAttack.target.GetName() + " has to react against against an incoming attack!");
            List<string> l = new List<string>();
            l.Add("TotalDefense");
            if(CurrentAttack.ActiveWeapon.IsWeaponClass(WeaponClass.melee))
            {
                if(CurrentAttack.target.CanParry())
                {
                    l.Add("Parry");
                }
                l.Add("Block");
                l.Add("Dodge");
            }
            l.Add("NoReaction");
            ConstructActions(l);
        }
    }

    public void ResolveHit()
    {
        TacticsAttack.DealDamage(CurrentAttack);
        CurrentAttack.attacker.RemoveCondition(Condition.Aiming);
        CurrentAttack = null;
        Cancel();
    }

    public void SupressingFire(List<Transform> targets)
    {
        FireRate = "Auto";
        foreach(Transform t in targets)
        {
            PlayerStats CurrentStats = t.GetComponent<PlayerStats>();
            //only attacker is out of range of the spray. ALLIES CAN GET HIT TOO
            if(CurrentStats.GetTeam() != ActivePlayerStats.GetTeam())
            {
                //CurrentStats.AbilityCheck("WP",-20,"Suppression");
            }
            //CurrentStats.SetCondition("Under Fire", 1,false);
        } 
        StartCoroutine(SuppressionDelay(targets));
    }

    IEnumerator SuppressionDelay(List<Transform> targets)
    {
        RollResult SprayResult = ActivePlayerStats.AbilityCheck("BS",-20);
        while(!SprayResult.Completed())
        {
            yield return new WaitForSeconds(0.5f);
        }
        int maxShots;
        if(ActiveWeapon.CanFire("Auto"))
        {
            maxShots = ActiveWeapon.ExpendAmmo("Auto");
        }
        else
        {
            maxShots = ActiveWeapon.ExpendAmmo("Semi");
        }
        if(SprayResult.Passed() && targets.Count > 0)
        {
            RepeatedAttacks = 1 + (SprayResult.GetDOF()/2);
            Debug.Log("attacks " + RepeatedAttacks);
            if(RepeatedAttacks > maxShots)
            {
                RepeatedAttacks = maxShots;
            }
            for(int i = 0; i < RepeatedAttacks; i++)
            {
                int randomIndex = Random.Range(0,targets.Count - 1);
                PlayerStats currentStats = targets[randomIndex].GetComponent<PlayerStats>();
                AttackQueue.Enqueue(new AttackSequence (currentStats, ActivePlayerStats, ActiveWeapon,FireRate,1,true));
            }
        }
        halfActions -= 2;
        Cancel();
    }

    public void CreateThreatRange(string type)
    {
        GameObject newThreatRange = PhotonNetwork.Instantiate("ThreatRange", ActivePlayer.transform.position + new Vector3(0,0.05f,0), Quaternion.identity);
        newThreatRange.GetComponent<ThreatRangeBehavior>().SetParameters(type, ActiveWeapon, ActivePlayerStats);
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

    public void PushToolTips()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Dictionary<Vector3, List<string>> output = new Dictionary<Vector3, List<string>>();
        foreach(GameObject g in players)
        {
            PlayerStats selectedPlayer = g.GetComponent<PlayerStats>();
            if(currentAction.Equals("Attack"))
            {
                output.Add(g.transform.position, TacticsAttack.GenerateTooltip(selectedPlayer,ActivePlayerStats,ActiveWeapon,FireRate));
            }
            else
            {
                List<string> currentTooltip = new List<string>();
                currentTooltip.Add(selectedPlayer.ToString());
                output.Add(g.transform.position, currentTooltip);   
            }
        }
        TooltipBehavior.UpdateToolTips(output);
    }

    private void SpendFreeAction()
    {
        if(freeActions > 0)
        {
            freeActions--;
        }
        else
        {
            halfActions--;
        }
    }

    public void Cancel()
    {
        if(halfActions < 0)
        {
            halfActions = 0;
        }
        ActiveWeapon = null;
        ConstructActions();
        Move();
        FireRate = null;
        target = null;
        ValidTargets = null;
        RepeatedAttacks = 0;
        attacks = 0;
        UIPlayerInfo.UpdateDisplay(ActivePlayerStats, halfActions, freeActions);
        PushToolTips();
        
        TokenDragBehavior.ToggleMovement(true);
    }
}

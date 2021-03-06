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
    protected Drone ActiveDrone;
    public GameObject Canvas;
    public GameObject ActionUIButton;
    protected int halfActions;
    protected int freeActions;
    protected string FireRate;
    protected PlayerStats target;
    protected int multipleTargetsLimit;
    protected List<GameObject> multipleTargets = new List<GameObject>();
    public WeaponTemplate unarmed;
    int RepeatedAttacks;
    public List<PlayerStats> ValidTargets;
    protected Queue<AttackSequence> AttackQueue = new Queue<AttackSequence>();
    protected AttackSequence CurrentAttack;
    protected int MaxSelectionRange;
    public Vector3 DirectionPointerInfo;

    public void Combat()
    {
        TokenDragBehavior.ToggleMovement(false);
        UIPlayerInfo.UpdateCustomCommand("Select Equipped Weapon");
        Dictionary<string,string> d = new Dictionary<string, string>();
        
        if(ActivePlayerStats.PrimaryWeapon != null) 
        {
            d.Add(ActivePlayerStats.PrimaryWeapon.GetName() + " (Primary)", "PrimaryWeapon");
        }
        if (ActivePlayerStats.SecondaryWeapon != null)
        {
            d.Add(ActivePlayerStats.SecondaryWeapon.GetName() + " (Off Handed)","SecondaryWeapon");
        }
        d.Add("Called Shot", "CalledShot");
        d.Add("Cancel","Cancel");
        ConstructActions(d);
    }

    public void Movement()
    {
        currentAction = null;
        List<string> l = new List<string>();
        TokenDragBehavior.ToggleMovement(false);
        if(ActivePlayerStats.hasCondition(Condition.Prone))
        {
            l.Add("Stand");
        }
        else
        {
            l.Add("Prone");
            l.Add("Run");
            //l.Add("Jump");
        }
        l.Add("Cancel");
        ConstructActions(l);
    }

    public void Run()
    {
        ActivePlayerStats.Run();
        if(ActivePlayerStats.myData.hasTalent(TalentKey.Momentum))
        {
            CombatLog.Log(ActivePlayerStats.GetName() + " enters a state of momentum!");
            ActivePlayerStats.SetCondition(Condition.Momentum, -1, true);
        }
        halfActions -= 2;
        Cancel();
    }

    
    public void Prone()
    {
        ActivePlayerStats.Prone();
        SpendFreeAction();
        Cancel();
    }

    public void Jump()
    {
        StartCoroutine(JumpDelay());

    }

    IEnumerator JumpDelay()
    {
        ClearActions();
        
        GameObject dragToken = PhotonNetwork.Instantiate("LineDragToken", ActivePlayerStats.transform.position, Quaternion.identity);
        LineDragBehavior line = dragToken.GetComponent<LineDragBehavior>();
        line.SetParameters(DmMenu.GetOwner(ActivePlayerStats), 1, ActivePlayerStats.transform.position, true, 5);
        while(!line.finished)
        {
            yield return new WaitForSeconds(0.2f);
        }
        Vector3[] path = line.GetPath().ToArray();
        Debug.Log("length" + path.Length);
        if(path.Length == 2)
        {
            Vector3 distance = path[1] - path[0];
            float distanceY = distance.y;
            bool jumpingDown = distanceY > 0;
            Debug.Log("Player is trying to jump y tiles");
            int thresholdY = Mathf.CeilToInt(Mathf.Abs(distanceY));
            int jumpModifier = 0;
            Debug.Log("jumping down: " + jumpingDown);
            /*
            if(ActivePlayerStats.myData.hasTalent(TalentKey.Courier))
            {
                jumpModifier += 5;
            }
            */
            RollResult jumpRoll = new RollResult(ActivePlayerStats.myData, AttributeKey.Gymnastics, thresholdY, jumpModifier);
            while(!jumpRoll.Completed())
            {
                yield return new WaitForSeconds(0.2f);
            }
            if(jumpRoll.Passed())
            {
                CombatLog.Log(ActivePlayerStats.GetName() + "gets at least [" + thresholdY + "] hits and successfully jumps up!");
                ActivePlayer.moveToTile(path);

            }
            else
            {
                CombatLog.Log(ActivePlayerStats.GetName() + "fails to gets at least [" + thresholdY + "] hits and cannot make the jump!");
                if(jumpingDown)
                {
                    Vector3 fallVector = new Vector3(path[0].x, ActivePlayer.transform.position.y, path[0].z);
                    Debug.Log(ActivePlayer.transform.position.ToString() + " ->" + fallVector);
                    ActivePlayer.SetPosition(fallVector);
                    ActivePlayer.FallingCheck();
                }
            }
            line.RemoveLine();
            SpendFreeAction();
        }
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
        UIPlayerInfo.UpdateCustomCommand("Select Fire Rate");
        Dictionary<string,string> d = ActiveWeapon.GetWeaponActions(halfActions > 1);
        d.Add("Cancel","Cancel");
        ConstructActions(d);
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

    public void CalledShot()
    {
        UIPlayerInfo.UpdateCustomCommand("Select Called Shot Type");
        Dictionary<string,string> d = new Dictionary<string, string>();
        if(!ActivePlayerStats.hasCondition(Condition.Disarm) && !ActivePlayerStats.hasCondition(Condition.KnockDown) && !ActivePlayerStats.hasCondition(Condition.ShakeUp))
        {
            d.Add("Disarm","Disarm");
            d.Add("Shake Up","ShakeUp");
            if(ActivePlayerStats.ThreateningMelee())
            {
                d.Add("Knock Down","KnockDown");
            }
        }
        d.Add("Cancel","Cancel");
        ConstructActions(d);
    }

    public void Disarm()
    {
        ActivePlayerStats.SetCondition(Condition.Disarm,1,true);
        SpendFreeAction();
        Cancel();
    }

    public void ShakeUp()
    {
        ActivePlayerStats.SetCondition(Condition.ShakeUp,1,true);
        SpendFreeAction();
        Cancel();
    }
    public void KnockDown()
    {
        ActivePlayerStats.SetCondition(Condition.KnockDown,1,true);
        SpendFreeAction();
        Cancel();
    }

    //restricted actions: actions that are only available when the player is restricted somehow
    public void GrappleControl()
    {
        StartCoroutine(GrappleControlDelay());
    }

    IEnumerator GrappleControlDelay()
    {
        RollResult opposedStrengthcheck = ActivePlayerStats.AbilityCheck(AttributeKey.UnarmedCombat);
        opposedStrengthcheck.OpposedRoll(target.AbilityCheck(AttributeKey.UnarmedCombat));
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

    public void GrappleScuffle()
    {
        StartCoroutine(GrappleScuffleDelay());
    }
    IEnumerator GrappleScuffleDelay()
    {
        RollResult opposedStrengthcheck = ActivePlayerStats.AbilityCheck(AttributeKey.UnarmedCombat);
        opposedStrengthcheck.OpposedRoll(target.AbilityCheck(AttributeKey.UnarmedCombat));
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
        TokenDragBehavior.ToggleMovement(false);
        currentAction = null;
        Dictionary<string,string> d = new Dictionary<string, string>();
        if(halfActions > 0)
        {
            d.Add("Aim","Aim");
            d.Add("Toggle AR","AR");
            d.Add("Reload","Reload");
        }
        if(ActivePlayerStats.myData.hasTalent(TalentKey.Adrenaline) && !ActivePlayerStats.hasCondition(Condition.Winded))
        {
            d.Add("Adrenaline","Adrenaline");
        }
        if(ActivePlayerStats.myData.hasTalent(TalentKey.Presence))
        {
            d.Add("Presence","Presence");
        }
        if(ActivePlayerStats.myData.hasTalent(TalentKey.Rook) && ActivePlayerStats.HoldingWeaponClass(WeaponClass.shield))
        {
            d.Add("Rook","Rook");
        }
        if(ActivePlayerStats.myData.hasTalent(TalentKey.Direct))
        {
            d.Add("Direct","Direct");
        }
        if(ActivePlayerStats.myData.hasTalent(TalentKey.Inspire))
        {
            d.Add("Inspire","Inspire");
        }
        if(ActivePlayerStats.myData.hasTalent(TalentKey.FocusFlank))
        {
            d.Add("Tactics Focus: Flanking","FlankingStyle");
        }
        if(ActivePlayerStats.myData.hasTalent(TalentKey.SpellTerrainProjectile))
        {
            d.Add("Lob Terrain","SpellLob");
        }
        if(ActivePlayerStats.myData.hasTalent(TalentKey.SpellWall))
        {
            d.Add("Create Barrier", "SpellWall");
        }
        if(ActivePlayerStats.myData.hasTalent(TalentKey.SpellTerrainArmor))
        {
            d.Add("Industrial Armor","SpellArmor");
        }
        if(ActivePlayerStats.myData.hasTalent(TalentKey.Desperado) && ActivePlayerStats.hasCondition(Condition.Momentum))
        {
            d.Add("Reposition","Reposition");
        }
        if(ActivePlayerStats.myData.hasTalent(TalentKey.SimTechProficiency) && ActivePlayerStats.hasCondition(Condition.AR))
        {
            d.Add("Drone Menu", "Drone");
        }
        /*
        if(ActivePlayerStats.myData.hasTalent(TalentKey.TwoWeaponFighting))
        {
            d.Add("TwoWeaponFighting","TwoWeaponFighting");   
        }
        if(ActivePlayerStats.myData.hasTalent(TalentKey.Dropkick) && ActivePlayerStats.hasCondition(Condition.Momentum))
        {
            d.Add("Dropkick","Dropkick");
        }
        */
        d.Add("Cancel","Cancel");
        
        ConstructActions(d);
    }

    public void AR()
    {
        if(ActivePlayerStats.hasCondition(Condition.AR))
        {
            ActivePlayerStats.RemoveCondition(Condition.AR);
            CombatLog.Log(ActivePlayerStats.GetName() + " exits AR!");
        }
        else
        {
            ActivePlayerStats.SetCondition(Condition.AR, -1, true);
            CombatLog.Log(ActivePlayerStats.GetName() + " enters AR!");
        }  
        SpendFreeAction();
        Cancel();
    }

    // SimTech Skills
    public void Drone()
    {
        Dictionary<string, string> d = new Dictionary<string, string>();
        int addition = 0;
        foreach(Item i in ActivePlayerStats.myData.equipmentObjects)
        {
            if(i.GetType() == typeof(Drone))
            {
                Drone DroneCast = (Drone)i;
                if(DroneCast.deployed)
                {
                    d.Add(DroneCast.GetName() + "\n(control)", "Drone" + addition);
                }
                else{
                    d.Add(DroneCast.GetName() + "\n(deploy)", "Drone" + addition);
                }
                addition++;
            }
        }
        d.Add("Cancel","Cancel");
        ConstructActions(d);
    }


    public void CreateMinion()
    {
        StartCoroutine(MinionDelay());
    }

    IEnumerator MinionDelay()
    {
        UIPlayerInfo.UpdateCustomCommand("Place Drone [max range 5]");
        MaxSelectionRange = 5;
        currentAction = "SelectLocation";
        ConstructActions(new List<string>{"Cancel"});
        while(multipleTargets.Count == 0)
        {
            yield return new WaitForSeconds(0.2f);
        }
        Vector3 spawnPosition = multipleTargets[0].transform.position + new Vector3(0, 1.5f, 0);
        ActiveDrone.DeployDrone(ActivePlayerStats, spawnPosition);
    }

    // Sam skills
    public void Rook()
    {
        GameObject indicator = PhotonNetwork.Instantiate("DirectionPointer", ActivePlayerStats.transform.position, Quaternion.identity);
        indicator.GetComponent<DirectionSelectorBehavior>().SetLocation(ActivePlayerStats.transform.position, DmMenu.GetOwner(ActivePlayerStats));
        StartCoroutine(RookDelay());
        Dictionary<string,string> d = new Dictionary<string, string>();
        d.Add("Cancel","CancelRook");
        ConstructActions(d);
    }

    public void TwoWeaponFighting()
    {
        Debug.LogWarning("TWF talent is untested");
        ActivePlayerStats.SetCondition(Condition.Flurry,0,true);
        ActiveWeapon = ActivePlayerStats.PrimaryWeapon;
        UIPlayerInfo.UpdateCustomCommand("Select Attack Target");
        currentAction = "Attack";
        FireRate = ActiveWeapon.getFirerateKey(false);
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    }

    IEnumerator RookDelay()
    {
        UIPlayerInfo.UpdateCustomCommand("Select a direction to block");
        while(DirectionPointerInfo == Vector3.one)
        {
            yield return new WaitForSeconds(0.2f);
        }
        ActivePlayerStats.RemoveCondition(Condition.RookUp);
        ActivePlayerStats.RemoveCondition(Condition.RookRight);
        ActivePlayerStats.RemoveCondition(Condition.RookDown);
        ActivePlayerStats.RemoveCondition(Condition.RookLeft);
        float y = DirectionPointerInfo.y;
        if(y >= 315 || y < 45)
        {
            ActivePlayerStats.SetCondition(Condition.RookUp, 0, false);
        }
        else if(y > 45 && y < 135)
        {
            ActivePlayerStats.SetCondition(Condition.RookRight, 0, false);
        }
        else if(y > 135 && y < 225)
        {
            ActivePlayerStats.SetCondition(Condition.RookDown, 0, false);
        }
        else
        {
            ActivePlayerStats.SetCondition(Condition.RookLeft, 0, false);
        }
        PopUpText.CreateText("Blocking!", Color.green, ActivePlayerStats.gameObject);
        CombatLog.Log(ActivePlayerStats.GetName() + " readies their shield in a specific direction!");
        halfActions -= 2;
        Cancel();
    }

    public void CancelRook()
    {
        DirectionSelectorBehavior.RemovePointer();
        Cancel();
    }

    public void Adrenaline()
    {
        SpendFreeAction();
        halfActions = 2;
        CombatLog.Log(ActivePlayerStats.GetName() + " uses Adrenaline to make an additional attack this turn!");
        PopUpText.CreateText("Adrenaline!", Color.red, ActivePlayerStats.gameObject);
        TacticsAttack.DealStress(ActivePlayerStats,3);
        ActivePlayerStats.SetCondition(Condition.Winded, 0, false);
        Cancel();
    }

    public void Presence()
    {
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        StartCoroutine(PresenceDelay());
        ConstructActions(new List<string>{"Cancel"});
    }

    IEnumerator PresenceDelay()
    {
        UIPlayerInfo.UpdateCustomCommand("Select an enemy to intimidate");
        currentAction = "SelectSingleEnemy";
        while(target == null)
        {
            yield return new WaitForSeconds(0.2f);
        }
        ClearActions();
        RollResult IntimidationRoll = new RollResult(ActivePlayerStats.myData, AttributeKey.Intimidation, 0, 0);
        IntimidationRoll.OpposedRoll( new RollResult(target.myData, AttributeKey.Willpower, AttributeKey.Charisma, AttributeKey.MentalLimit, 0,0));
        while(!IntimidationRoll.Completed())
        {
            yield return new WaitForSeconds(0.2f);
        }
        int Hits = IntimidationRoll.GetHits();
        if(Hits > 0)
        {
            CombatLog.Log("By exceeding enemy hits, " + ActivePlayerStats.GetName() + " asserts their Presence!");
            PopUpText.CreateText("Presence!", Color.green, ActivePlayerStats.gameObject);
            ActivePlayerStats.SetCondition(Condition.Presence, Hits, false);
            PopUpText.CreateText("Intimidated!", Color.red, target.gameObject);
            target.SetCondition(Condition.Intimidated, Hits, false);
        }
        else
        {
            CombatLog.Log("By failing to exceed enemy hits, " + ActivePlayerStats.GetName() + " is unable to assert their Presence!");
            PopUpText.CreateText("Resisted!", Color.yellow, target.gameObject);
        }
        halfActions-= 2;
        TacticsAttack.DealStress(ActivePlayerStats,1);
        Cancel();
    }

    // Face Skills
    public void Direct()
    {
        MaxSelectionRange = 10;
        ActivePlayer.GetValidAllys(MaxSelectionRange);
        StartCoroutine(DirectDelay());
        ConstructActions(new List<string>{"Cancel"});
    }

    IEnumerator DirectDelay()
    {
        UIPlayerInfo.UpdateCustomCommand("Select an ally to buff");
        currentAction = "SelectSingleAlly";
        while(target == null)
        {
            yield return new WaitForSeconds(0.2f);
        }
        ClearActions();
        RollResult DirectRoll = new RollResult(ActivePlayerStats.myData, AttributeKey.Leadership, 0, 0);
        while(!DirectRoll.Completed())
        {
            yield return new WaitForSeconds(0.2f);
        }
        if(DirectRoll.Passed())
        {
            int hits = DirectRoll.GetHits();
            PopUpText.CreateText("Assisted!", Color.green, target.gameObject);
            target.SetCondition(Condition.Direction,hits,false);
            CombatLog.Log(ActivePlayerStats.GetName() + " assists their ally!");
        }
        else
        {
            PopUpText.CreateText("Assist Failed!", Color.red, target.gameObject);
            CombatLog.Log(ActivePlayerStats.GetName() + " fails to assist their ally!");
        }
        halfActions--;
        TacticsAttack.DealStress(ActivePlayerStats,1);
        Cancel();
    }

    public void Inspire()
    {
        StartCoroutine(InspireDelay());
    }

    IEnumerator InspireDelay()
    {
        RollResult InspireRoll = new RollResult(ActivePlayerStats.myData, AttributeKey.Leadership, 0, 0);
        while(!InspireRoll.Completed())
        {
            yield return new WaitForSeconds(0.2f);
        }
        if(InspireRoll.Passed())
        {
            int hits = Mathf.CeilToInt((float)InspireRoll.GetHits()/2f);
            int myTeam = ActivePlayerStats.GetTeam();
            GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
            for(int i = 0; i < allPlayers.Length; i++)
            {
                PlayerStats currentPlayer = allPlayers[i].GetComponent<PlayerStats>();
                if(currentPlayer.GetTeam() == myTeam && currentPlayer != ActivePlayerStats)
                {
                    TurnManager.instance.IncreaseInitiative(currentPlayer,hits);
                    PopUpText.CreateText("Inspired!", Color.green, currentPlayer.gameObject);
                }
            }
            CombatLog.Log(ActivePlayerStats.GetName() + " adds half their hits rounding up (" + hits + ") to their team's Initiative!");
        }
        else
        {
            PopUpText.CreateText("Inspire Failed!", Color.red, ActivePlayerStats.gameObject);
            CombatLog.Log(ActivePlayerStats.GetName() + " fails to inspire their team!");
        }
        halfActions-= 2;
        TacticsAttack.DealStress(ActivePlayerStats,1);
        Cancel();
    }

    public void FlankingStyle()
    {
        StartCoroutine(FlankingStyleDelay());
    }

    IEnumerator FlankingStyleDelay()
    {
    RollResult InspireRoll = new RollResult(ActivePlayerStats.myData, AttributeKey.Leadership, 0, 0);
    while(!InspireRoll.Completed())
    {
        yield return new WaitForSeconds(0.2f);
    }
    if(InspireRoll.Passed())
    {
        int hits = InspireRoll.GetHits();
        int myTeam = ActivePlayerStats.GetTeam();
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        for(int i = 0; i < allPlayers.Length; i++)
        {
            PlayerStats currentPlayer = allPlayers[i].GetComponent<PlayerStats>();
            if(currentPlayer.GetTeam() == myTeam)
            {
                currentPlayer.SetCondition(Condition.FocusFlank, hits, false);
                PopUpText.CreateText("Focus: Flanking!", Color.green, currentPlayer.gameObject);
            }
        }
        CombatLog.Log(ActivePlayerStats.GetName() + "'s team gains a bonus to flanking for their hits rounding up (" + hits + ") turns!");
    }
    else
    {
        PopUpText.CreateText("Style Failed!", Color.red, ActivePlayerStats.gameObject);
        CombatLog.Log(ActivePlayerStats.GetName() + " fails to lead their team!");
    }
    halfActions-= 2;
        TacticsAttack.DealStress(ActivePlayerStats,2);
    Cancel();
    }

    // Industrialist Mage
    public void SpellLob()
    {
        multipleTargetsLimit = 3;
        MaxSelectionRange = 5;
        UIPlayerInfo.UpdateCustomCommand("Select up to 3 tiles [max range 5]");
        currentAction = "SelectTargetsTiles";
        Dictionary<string,string> d = new Dictionary<string, string>();
        d.Add("Attack", "SpellLobFinished");
        d.Add("Cancel","Cancel");
        ConstructActions(d);
    }

    public void SpellLobFinished()
    {
        MaxSelectionRange = -1;
        int numTiles = 0;
        foreach(GameObject g in multipleTargets)
        {
            numTiles++;
        }
        if(numTiles > 0)
        {
            UIPlayerInfo.UpdateCustomCommand("Select attack target");
            Weapon SpellAttack = (Weapon)ItemReference.GetItem("TerrainLob");
            SpellAttack.SetAP(numTiles);
            SpellAttack.SetDamage(numTiles);
            ActiveWeapon = SpellAttack;
            ClearTiles();
            fireSS();
            TacticsAttack.DealStress(ActivePlayerStats, 1);
        }
        else
        {
            Cancel();
        }
    }

    public void SpellWall()
    {
        StartCoroutine(SpellWallDelay());
    }

    IEnumerator SpellWallDelay()
    {
        RollResult SpellRoll = new RollResult(ActivePlayerStats.myData, AttributeKey.Artisan, AttributeKey.Magic, 0, 0);
        while(!SpellRoll.Completed())
        {
            yield return new WaitForSeconds(0.2f);
        }
        if(SpellRoll.Passed())
        {
            ClearActions();
            UIPlayerInfo.UpdateCustomCommand("Draw Barrier Within Range");
            GameObject dragToken = PhotonNetwork.Instantiate("LineDragToken", ActivePlayerStats.transform.position, Quaternion.identity);
            LineDragBehavior line = dragToken.GetComponent<LineDragBehavior>();
            line.SetParameters(DmMenu.GetOwner(ActivePlayerStats), SpellRoll.GetHits(), ActivePlayerStats.transform.position, true, 10, 1);
            while(!line.finished)
            {
                yield return new WaitForSeconds(0.2f);
            }
            List<Vector3> path = line.GetPath();
            foreach(Vector3 location in path)
            {
                GlobalManager.CreateTile(location + Vector3.up,"TileIndustrail01");
                GlobalManager.CreateTile(location + new Vector3(0,2,0),"TileIndustrail01");
            }
            line.RemoveLine();
        }
        else
        {
            CombatLog.Log(ActivePlayerStats.GetName() + " fails to manifest their spell!");
        }
        TacticsAttack.DealStress(ActivePlayerStats, 2);
        halfActions -= 2;
        Cancel();
    }

    public void SpellArmor()
    {
        multipleTargetsLimit = 6;
        MaxSelectionRange = 5;
        UIPlayerInfo.UpdateCustomCommand("Select up to 6 Tiles [max range 5]");
        currentAction = "SelectTargetsTiles";
        Dictionary<string,string> d = new Dictionary<string, string>();
        d.Add("Finished", "SpellArmorFinished");
        d.Add("Cancel","Cancel");
        ConstructActions(d);
        ActivePlayer.GetValidAllys(MaxSelectionRange);
    }

    public void SpellArmorFinished()
    {
        if(multipleTargets.Count > 0)
        {
            ClearActions();
            StartCoroutine(SpellArmorDelay());
        }
        else
        {
            Cancel();
        }
    }

    IEnumerator SpellArmorDelay()
    {
        int armorBonus = 0;
        foreach(GameObject g in multipleTargets)
        {
            armorBonus++;
        }
        armorBonus /= 2;
        MaxSelectionRange = 10;
        UIPlayerInfo.UpdateCustomCommand("Select an ally to buff [max range 10]");
        currentAction = "SelectSingleAlly";
        while(target == null)
        {
            yield return new WaitForSeconds(0.2f);
        }
        target.SetCondition(Condition.TerrainArmor, armorBonus,true);
        CombatLog.Log(ActivePlayerStats.GetName() + " creates armor out of terrain for (" + armorBonus + ") turns!");
        ClearTiles();
        halfActions-= 2;
        TacticsAttack.DealStress(ActivePlayerStats, 2);
        Cancel();
    }

    // adept powers

    public void Reposition()
    {
        ActivePlayerStats.RemoveCondition(Condition.Momentum);
        CombatLog.Log(ActivePlayerStats.GetName() + " uses their momentum to attack and reposition in the same turn!");
        TacticsAttack.DealStress(ActivePlayerStats,2);
        SpendFreeAction();
        Cancel();
    }

    public void Dropkick()
    {
        ActivePlayerStats.RemoveCondition(Condition.Momentum);
        ActivePlayerStats.SetCondition(Condition.Dropkick, 0, true);
        SpendFreeAction();
        Cancel();
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
                d.Add(s,s);
            }
        }
        d.Add("End Turn","EndTurn");
        ConstructActions(d);
    }

    //is passed the action value of a button as an input
    override public void OnButtonPressed(string input)
    {
        TooltipSystem.hide();
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

    public void CalledCancel()
    {
        //to implement trait that avoids this penalty
        ActivePlayerStats.OnAttackFinished();
        Cancel();
    }
    
    public void BlastAttack()
    {
        UIPlayerInfo.UpdateCustomCommand("Select Blast Zone");
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
        UIPlayerInfo.UpdateCustomCommand("Select Attack Target");
        currentAction = "Attack";
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    }

    public void fireSS()
    {
        UIPlayerInfo.UpdateCustomCommand("Select Attack Target");
        currentAction = "Attack";
        FireRate = "SS";
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    }

    public void fireSA()
    {
        UIPlayerInfo.UpdateCustomCommand("Select Attack Target");
        currentAction = "Attack";
        FireRate = "SA";
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    }
    public void fireSAB()
    {
        UIPlayerInfo.UpdateCustomCommand("Select Attack Target");
        currentAction = "Attack";
        FireRate = "SAB";
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    }

    public void fireBF()
    {
        UIPlayerInfo.UpdateCustomCommand("Select Attack Target");
        currentAction = "Attack";
        FireRate = "BF";
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    }
    public void fireLB()
    {
        UIPlayerInfo.UpdateCustomCommand("Select Attack Target");
        currentAction = "Attack";
        FireRate = "LB";
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    } 
    public void fireFA()
    {
        UIPlayerInfo.UpdateCustomCommand("Select Attack Target");
        currentAction = "Attack";
        FireRate = "FA";
        PushToolTips();
        ActivePlayer.GetValidAttackTargets(ActiveWeapon);
        List<string> l = new List<string>{"Cancel"};
        ConstructActions(l);
    }

    public void fireFAB()
    {
        UIPlayerInfo.UpdateCustomCommand("Select Attack Target");
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
        ActivePlayerStats.RemoveItem(ActiveWeapon);
        ActivePlayerStats.Unequip(ActiveWeapon);
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

    public void Ready()
    {
        TokenDragBehavior.ToggleMovement(false);
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
        int baseDefenseDice = CurrentAttack.CalculateQuickDefenseBonus();
        UIPlayerInfo.UpdateCustomCommand("Target Can Choose to React \n[Die pool: " + baseDefenseDice +"] ");
        UIPlayerInfo.ShowActionsOnly(CurrentAttack.target);
        CurrentAttack.target.GetComponent<TacticsMovement>().PaintCurrentTile("selectableRunning");
        CombatLog.Log(CurrentAttack.target.GetName() + " has to react against an incoming attack!");
        List<string> l = new List<string>();
        //minions don't get reactions
        if(!CurrentAttack.target.myData.isMinion)
        {
            l.Add("TotalDefense");
            l.Add("Dodge");
            /*
            if(CurrentAttack.target.myData.hasTalent(TalentKey.BladeParry) && CurrentAttack.target.HoldingWeaponClass(WeaponClass.melee))
            {
                l.Add("BladeParry");
            }
            */
            if(CurrentAttack.ActiveWeapon.IsWeaponClass(WeaponClass.melee))
            {
                if(CurrentAttack.target.CanParry())
                {
                    l.Add("Parry");
                }
                l.Add("Block");
            }
        }
        l.Add("NoReaction");
        ConstructActions(l);
    }

    public void HitResolved()
    {
        if(CurrentAttack.attacker != null)
        {
            UIPlayerInfo.ShowAllInfo(CurrentAttack.attacker);
            CurrentAttack.attacker.RemoveCondition(Condition.Aiming);
        }
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
            PhotonNetwork.Destroy(g);
        }
        UIPlayerInfo.UpdateCustomCommand("");
    }

    public void ClearTiles()
    {
        foreach(GameObject g in multipleTargets)
        {
            Tile selectedTile;
            if(g.TryGetComponent<Tile>(out selectedTile))
            {
                GlobalManager.RemoveTile(selectedTile);
            }
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
        Dictionary<int, List<string>> output = new Dictionary<int, List<string>>();
        foreach(GameObject g in players)
        {
            PlayerStats selectedPlayer = g.GetComponent<PlayerStats>();
            List<string> currentTooltip = new List<string>();
            if(!string.IsNullOrEmpty(currentAction) && currentAction.Equals("Attack"))
            {
                currentTooltip = TacticsAttack.GenerateTooltip(selectedPlayer,ActivePlayerStats,ActiveWeapon,FireRate);
            }
            output.Add(selectedPlayer.GetID(), currentTooltip); 
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
        currentAction = "Move";
        ActiveWeapon = null;
        ActiveDrone = null;
        FireRate = null;
        target = null;
        ValidTargets = null;
        DirectionPointerInfo = Vector3.one;
        multipleTargetsLimit = 0;
        MaxSelectionRange = -1;
        multipleTargets = new List<GameObject>();
        GlobalManager.ClearBoard();
        StopAllCoroutines();
        if(ActivePlayer != null)
        {
            PushToolTips();
            ConstructActions();
            ActivePlayer.PaintCurrentTile("current");
            Move();
            UIPlayerInfo.UpdateDisplay(ActivePlayerStats, halfActions, freeActions);
        }
        TokenDragBehavior.ToggleMovement(true);
    }
}

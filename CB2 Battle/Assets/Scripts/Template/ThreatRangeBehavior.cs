using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

// The main behavior of threat range objects, note that this is seperate from the threat cone script.
// Where that script managed basic functions, this script knows what kind of threat range it is
public class ThreatRangeBehavior : MonoBehaviourPunCallbacks
{
    // The threat cone this script uses to find targets
    [SerializeField]
    public ThreatCone myRange;
    // A string key used to determine type of behavior
    public string threatType;
    // Reference to the gamemaster to send infromation back to 
    public TurnManager TurnManager;
    // Used to get hit information 
    private RaycastHit hit;
    // Refernce to the owner of this threat range, used to determine range in some cases
    private PlayerStats attacker;
    // Refernce to the weapon creating this threat range, also used to determine range and other properties
    private Weapon w;
    // toggles movement, by default threat ranges try to follow/look at the mouse so long as controllable = true
    bool controllable = true;
    // Reference to a cone shaped threat range object, used for overwatch, supressing fire, and flame attacks
    public GameObject ConeToken;
    // Reference to a sphere shaped threat range object, used exclusively for blast weapons
    public GameObject BlastToken;
    [SerializeField] private PhotonView pv;
    private Vector3 origin;
    private int ThrowRange;

    // Called on frame update, token movement and overwatch triggering happens here
    void Update()
    {
        if(myRange.draw)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //if the mouse is pointed at a thing
            if (Physics.Raycast(ray, out hit))
            {
                if(threatType.Equals("Blast") && controllable)
                {
                    float distance = Vector3.Distance(origin, hit.point);
                    if(distance <= ThrowRange)
                    {
                        gameObject.transform.position = hit.point + new Vector3(0, 0.1f, 0);
                    }
                }
                else if ( hit.collider.tag != "Player" && controllable )
                {
                    gameObject.transform.LookAt(hit.point);
                }
                CheckMouse();
            }
            float yRotation = transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0,yRotation,0);
        }
        else if(threatType.Equals("Overwatch"))
        {
            List<Transform> inRangeTargets = myRange.GetTargets();
            bool triggered = false;
            foreach(Transform t in inRangeTargets)
            {
                PlayerStats target = t.GetComponent<PlayerStats>();
                //if target is moving and an enemy!
                if(t.GetComponent<TacticsMovement>().moving && target.GetTeam() != attacker.GetTeam())
                {
                    Debug.Log("triggerd firing at " + w.getHighestROF() +" target "+ target.GetName() + " attacker " + attacker.GetName());
                    TurnManager.RollToHit(target,w.getHighestROF(),w,attacker);
                    triggered = true;
                }
            }
            if(triggered)
            {
                RemoveRange(attacker);
            }
        }
    }

    // Frame check for user left clicking 
    private void CheckMouse()
    {
        if(Input.GetMouseButtonUp(0) && controllable)
        {
            // standard check to make sure a left click on ui isn't interpreted as a left click for threat range
            bool hitUi = false;
            GameObject[] ui = GameObject.FindGameObjectsWithTag("Input");
            foreach(GameObject g in ui)
            {
                if(EventSystem.current.IsPointerOverGameObject())
                {
                    hitUi = true;
                }
            }
            if(!hitUi)
            {
                controllable = false;
                pv.RPC("RPC_MasterClick",RpcTarget.MasterClient, transform.position, transform.eulerAngles);
                if(!pv.IsMine)
                {
                    myRange.DestroyToken();
                }
            }
        }
    }

    [PunRPC]
    void RPC_MasterClick(Vector3 pos, Vector3 eul)
    {
        controllable = false;
        StartCoroutine(ClickDelay(pos,eul));
    }

    IEnumerator ClickDelay(Vector3 pos, Vector3 eul)
    {
        yield return new WaitForSeconds(0.5f);
        transform.position = pos;
        transform.eulerAngles = eul;
        List<Transform> selectedTargets = myRange.GetTargets();
        if(threatType.Equals("Supress"))
        {
            TurnManager.SupressingFire(selectedTargets);
            RemoveRange(attacker);
        }
        else if(threatType.Equals("Overwatch"))
        {
            attacker.SetCondition(Condition.Overwatch, 1, true);
            CombatLog.Log(attacker.GetName() + " establishes a kill zone!");
            myRange.Draw(false);
            TurnManager.OverwatchFinished();
        }
        else if(threatType.Equals("Flame"))
        { 
            TurnManager.FlameAttack(selectedTargets);
            RemoveRange(attacker);
        }
        else if(threatType.Equals("Blast"))
        {
            StartCoroutine(Scatter());
        }
    }

    public void SetParameters(string type, Weapon w, PlayerStats attacker)
    {
        
        TurnManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<TurnManager>();
        threatType = type;
        Vector3 dimensions;
        if(type.Equals("Blast"))
        {
            Destroy(ConeToken);
            BlastToken.SetActive(true);
            myRange = BlastToken.GetComponent<ThreatCone>();
            myRange.StrictLOS = true;
            dimensions = new Vector3(w.GetBlast() * 2,w.GetBlast() * 2,w.GetBlast() * 2);
        }
        else
        {
            Destroy(BlastToken);
            ConeToken.SetActive(true);
            myRange = ConeToken.GetComponent<ThreatCone>();
            myRange.avoidOwner = attacker.transform;
            // flame attacks and semi auto spray use narrower ranges
            dimensions = new Vector3(15,10,15);
        }
        myRange.transform.localScale = dimensions;
        ThrowRange = w.getRange(attacker);
        origin = attacker.transform.position; 
        this.attacker = attacker;
        this.w = w;
        Photon.Realtime.Player clientTarget = DmMenu.GetOwner(attacker);
        //send additional info to owner of the attacker
        pv.RPC("RPC_Parameter",RpcTarget.Others, dimensions, type, attacker.GetID(), ThrowRange, clientTarget.ActorNumber);
    }

    [PunRPC]
    void RPC_Parameter(Vector3 scale, string type, int attackerID, int ThrowRange, int ownerID)
    {
        Photon.Realtime.Player owningPlayer = PhotonNetwork.CurrentRoom.GetPlayer(ownerID);
        if(PhotonNetwork.LocalPlayer == owningPlayer)
        {
            PlayerStats attacker = PlayerSpawner.IDtoPlayer(attackerID);
            this.origin = attacker.transform.position;
            this.ThrowRange = ThrowRange;
            threatType = type;
            if(type.Equals("Blast"))
            {
                Destroy(ConeToken);
                BlastToken.SetActive(true);
                myRange = BlastToken.GetComponent<ThreatCone>();
                myRange.StrictLOS = true;
                BlastToken.transform.localScale = scale;
            }
            else
            {
                Destroy(BlastToken);
                ConeToken.SetActive(true);
                ConeToken.transform.localScale = scale;
                myRange = ConeToken.GetComponent<ThreatCone>();
                myRange.avoidOwner = attacker.transform;
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void RemoveRange(PlayerStats owner)
    {
        if(owner == attacker)
        {
            myRange.Clear();
            PhotonNetwork.Destroy(gameObject);
        }
    }

    IEnumerator Scatter()
    {
        RollResult ScatterRoll = attacker.AbilityCheck(w.Template.WeaponSkill.skillKey, w.Template.WeaponSkill.derivedAttribute, AttributeKey.PhysicalLimit, null , 2,0);
        while(!ScatterRoll.Completed())
        {
            yield return new WaitForSeconds (0.5f);
        }
        if(!ScatterRoll.Passed())
        {
            int distance = Random.Range(1,6);
            //SR rule more hits = less scatter
            distance -= ScatterRoll.GetHits();
            if(distance < 0)
            {
                distance = 0;
            }
            transform.rotation = Quaternion.Euler(0, Random.Range(0,360),0);
            gameObject.transform.Translate(transform.forward.normalized * distance); 
            Photon.Realtime.Player clientTarget = DmMenu.GetOwner(attacker);
            CombatLog.Log("By failing the ballistic test, " + attacker.GetName() +"'s " + w.GetName() + " scatters in a random direction!");    
        }
        yield return new WaitForSeconds (0.2f);
        TurnManager.BlastAttack(myRange.GetTargets());
        if(w.Template.Lethal)
        {
            List<Transform> Tiles = myRange.GetCover();
            foreach(Transform t in Tiles)
            {
                Tile currentTile = BoardBehavior.GetTile(t.position);
                currentTile.HitCover(w);
            }
        }
    }
}

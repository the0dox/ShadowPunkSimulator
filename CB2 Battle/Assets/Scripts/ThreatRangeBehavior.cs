using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;

// The main behavior of threat range objects, note that this is seperate from the threat cone script.
// Where that script managed basic functions, this script knows what kind of threat range it is
public class ThreatRangeBehavior : MonoBehaviour
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
        if(!pv.IsMine || myRange.draw)
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
                else if ( hit.collider.tag == "Tile" && controllable )
                {
                    gameObject.transform.LookAt(hit.point);
                }
                CheckMouse();
            }
            float yRotation = transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0,yRotation,0);
        }
        else if(pv.IsMine && threatType.Equals("Overwatch"))
        {
            List<Transform> inRangeTargets = myRange.GetTargets();
            foreach(Transform t in inRangeTargets)
            {
                PlayerStats target = t.GetComponent<PlayerStats>();
                //if target is moving and an enemy!
                if(t.GetComponent<TacticsMovement>().moving && target.GetTeam() != attacker.GetTeam())
                {
                    if(w.CanFire("Auto"))
                    {
                        TurnManager.RollToHit(t.GetComponent<PlayerStats>(),"Auto",w,attacker);
                    }
                    else if(w.CanFire("Semi"))
                    {
                        TurnManager.RollToHit(t.GetComponent<PlayerStats>(),"Semi",w,attacker);
                    }
                    if(!target.hasCondition("Pinned"))
                    {
                        target.AbilityCheck("WP",-20,"Suppression");
                    }
                    
                    RemoveRange(attacker);
                }
            }
        }
    }

    // Frame check for user left clicking 
    private void CheckMouse()
    {
        if(Input.GetMouseButtonUp(0))
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
                List<Transform> selectedTargets = myRange.GetTargets();
                List<int> ids = new List<int>();
                foreach(Transform t in selectedTargets)
                {
                    ids.Add(t.GetComponent<PlayerStats>().GetID());
                }
                pv.RPC("RPC_MasterClick",RpcTarget.MasterClient, ids, transform.position, transform.eulerAngles);
                if(!pv.IsMine)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    [PunRPC]
    void RPC_MasterClick(List<int> ids, Vector3 pos, Vector3 eul)
    {
        transform.position = pos;
        transform.eulerAngles = eul;
        List<Transform> selectedTargets = new List<Transform>();
        foreach(int i in ids)
        {
            selectedTargets.Add(PlayerSpawner.IDtoPlayer(i).transform);
        }
        controllable = false;
        if(threatType.Equals("Supress"))
        {
            TurnManager.SupressingFire(selectedTargets);
            Destroy(gameObject);
        }
        else if(threatType.Equals("Overwatch"))
        {
            myRange.draw = false;
            TurnManager.OverwatchFinished();
        }
        else if(threatType.Equals("Flame"))
        { 
            TurnManager.FlameAttack(selectedTargets);
            Destroy(gameObject);
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
            if(type.Equals("Flame") || !w.CanFire("Auto"))
            {
                dimensions = new Vector3((float) w.getRange(attacker) * 0.66f,w.getRange(attacker),w.getRange(attacker));
            }
            else
            {
                dimensions = new Vector3(w.getRange(attacker)/2,w.getRange(attacker)/2,w.getRange(attacker)/2);
            }
        }
        myRange.transform.localScale = dimensions;
        ThrowRange = w.getRange(attacker);
        origin = attacker.transform.position; 
        this.attacker = attacker;
        this.w = w;
        pv.RPC("RPC_Parameter",RpcTarget.Others, dimensions, type, origin, ThrowRange);
    }

    [PunRPC]
    void RPC_Parameter(Vector3 scale, string type, Vector3 origin, int ThrowRange)
    {
        this.origin= origin;
        this.ThrowRange = ThrowRange;
        threatType = type;
        if(type.Equals("Blast"))
        {
            Destroy(ConeToken);
            BlastToken.SetActive(true);
            BlastToken.transform.localScale = scale;
            Destroy(BlastToken.GetComponent<ThreatCone>());
        }
        else
        {
            Destroy(BlastToken);
            ConeToken.SetActive(true);
            ConeToken.transform.localScale = scale;
            Destroy(ConeToken.GetComponent<ThreatCone>());
        }
    }

    public void RemoveRange(PlayerStats owner)
    {
        if(owner == attacker)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    IEnumerator Scatter()
    {
        Debug.Log("scattering");
        RollResult ScatterRoll = attacker.AbilityCheck("BS",0);
        while(!ScatterRoll.Completed())
        {
            yield return new WaitForSeconds (0.5f);
        }
        if(!ScatterRoll.Passed())
        {
            int distance = Random.Range(1,6);
            transform.rotation = Quaternion.Euler(0, Random.Range(0,360),0);
            gameObject.transform.Translate(transform.forward.normalized * distance);
            CombatLog.Log("By failing the ballistic test, " + attacker.GetName() +"'s " + w.GetName() + " scatters in a random direction!");    
        }
        yield return new WaitForSeconds (0.5f);
        List<Transform> inRangeTargets = myRange.GetTargets();
        TurnManager.BlastAttack(inRangeTargets);

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreatRangeBehavior : MonoBehaviour
{
    [SerializeField]
    public ThreatRange myRange;
    public string threatType;
    public TurnManager TurnManager;
    private RaycastHit hit;
    private PlayerStats attacker;
    private Weapon w;
    bool controllable = true;
    public GameObject BlastToken;
    void Update()
    {
        if(myRange.draw)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //if the mouse is pointed at a thing
            if (Physics.Raycast(ray, out hit))
            {
                if(threatType.Equals("Blast") && hit.collider.tag == "Tile" && controllable)
                {
                    gameObject.transform.position = hit.point + new Vector3(0, 0.1f, 0);
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
        else if(threatType.Equals("Overwatch"))
        {
            List<Transform> inRangeTargets = myRange.GetTargets();
            foreach(Transform t in inRangeTargets)
            {
                PlayerStats target = t.GetComponent<PlayerStats>();
                //if target is moving and an enemy!
                if(t.GetComponent<TacticsMovement>().moving && target.GetTeam() != attacker.GetTeam())
                {
                    TurnManager.RollToHit(t.GetComponent<PlayerStats>(),"Auto",w,attacker);
                    if(!target.AbilityCheck("WP",-20).Passed())
                    {
                        target.SetCondition("Pinned",0,true);
                    }
                    RemoveRange(attacker);
                }
            }
        }
    }

    private void CheckMouse()
    {
        if(Input.GetMouseButtonUp(0))
        {
            List<Transform> selectedTargets = myRange.GetTargets();
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
                controllable = false; 
                if (!attacker.AbilityCheck("BS",w.RangeBonus(gameObject.transform, attacker)).Passed())
                {
                    Debug.Log("Scatter!");
                    StartCoroutine(Scatter(Random.Range(1,10)));
                }
                else
                {
                    TurnManager.BlastAttack(selectedTargets);
                }
            }
            else
            {
                Debug.Log("Error! invalid threat type!");
                Destroy(gameObject);
            }
        }
    }

    public void SetParameters(string type, Weapon w, PlayerStats attacker)
    {
        TurnManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<TurnManager>();
        threatType = type;
        BlastToken.SetActive(type.Equals("Blast"));
        if(type.Equals("Flame"))
        {
            myRange.viewAngle = 30f;
            int range = w.getRange(attacker);
            myRange.viewRadius = range;
        }
        else if(type.Equals("Blast"))
        {
            myRange.viewAngle = 360f;
            int blastRange = w.GetBlast();
            myRange.viewRadius = blastRange;
            BlastToken.transform.localScale = new Vector3(blastRange*2,blastRange*2,blastRange*2);
            myRange.ShowDisk = false;
        }
        else
        {
            myRange.viewRadius = w.getRange(attacker)/2;
        }
        this.attacker = attacker;
        this.w = w;
    }

    public void RemoveRange(PlayerStats owner)
    {
        if(owner == attacker)
        {
            Destroy(gameObject);
        }
    }

    IEnumerator Scatter(int distance)
    {
        Debug.Log("scattering " + distance );
        Debug.Log("moving");
        transform.rotation = Quaternion.Euler(0, Random.Range(0,360),0);
        gameObject.transform.Translate(transform.forward.normalized * distance);
        distance--; 
        yield return new WaitForSeconds (1f);
        TurnManager.BlastAttack(myRange.GetTargets());
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Basic threat range this object is doesn't know what kind of threat range it is, it only functions to collect targets 
// within its range to threat range behavior
public class ThreatCone : MonoBehaviour
{
    // Contains the transforms of each player gameobject within range
	private List<Transform> visibleTargets = new List<Transform>();
    // List of transforms that line of sight applies 
    private List<Transform> validTargets = new List<Transform>();
    // Toggles wether the threat range (transparent red area) is visible to the player, determined by threatrangebehavior
    public bool draw = true;
    // Toggled to use the stricter los rules for blast weapons
    public bool StrictLOS = false;
    // Reference to the mesh so that it isn't lost when draw is enabled/disabled
    Mesh MyMesh;
    // In certain threat ranges, the owner cannot be targeted, in which case their transform is saved here so that the cone knows to exclude it
    public Transform avoidOwner;

    // Called on first frame, starts a delayed check to save memory
    void Start()
    {
        MyMesh = GetComponent<MeshFilter>().mesh;
        StartCoroutine(LOSDelay());
    }

    public void Draw(bool enabled)
    {
        if(!enabled)
        {
            GetComponent<MeshFilter>().mesh = null;
        }
        else
        {
            GetComponent<MeshFilter>().mesh = MyMesh;
        }
        draw = enabled;
    }

    IEnumerator LOSDelay()
    {
        while(true)
        {
            yield return new WaitForSeconds(0.2f);
            CheckLOS();
        }
    }

    private void CheckLOS()
    {
        validTargets = new List<Transform>();
        bool paint = false;
        foreach(Transform t in visibleTargets)
        {
            if(ValidContact(t))
            {
                validTargets.Add(t);
                if(draw)
                {
                    paint = true;
                }
            }
            t.GetComponent<PlayerStats>().PaintTarget(paint);
        }
    }

    // Called every frame another collider is within the threat range, adds every player object within the range
    // But only if line of sight can be drawn between the origin and the target
    void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            // los is expensive and should only be called on players
            if(contact.otherCollider.tag == "Player")
            {
                // checks los between players so long as they aren't excluded
                if( ValidContact(contact.otherCollider.transform) && contact.otherCollider.transform != avoidOwner)
                {
                    if(!visibleTargets.Contains(contact.otherCollider.transform) )
                    {
                        
                        visibleTargets.Add(contact.otherCollider.transform);
                    }
                }
            }
        }
        //Debug.Log(visibleTargets.Count);
    }

    void OnCollisionExit(Collision collision)
    {
        // los is expensive and should only be called on players
        if(collision.gameObject.tag == "Player")
        {
            Transform myTrans = collision.transform;
            if(visibleTargets.Contains(myTrans))
            {
                myTrans.GetComponent<PlayerStats>().PaintTarget(false);
                visibleTargets.Remove(myTrans);
                Debug.Log(myTrans.gameObject.name + "has left my view");
            }
        }
    }

    private bool ValidContact(Transform target)
    {
        if(StrictLOS || avoidOwner == null)
        {
            return !Physics.Linecast(gameObject.transform.position, target.position, LayerMask.GetMask("Obstacle"));
        }
        else
        {
            return TacticsAttack.HasLOS(target.gameObject, avoidOwner.gameObject);
        }

    }

    // Returns the transforms of all players within the threat range to the behavior class
    public List<Transform> GetTargets()
    {
        CheckLOS();
        foreach(Transform t in visibleTargets)
        {
            t.GetComponent<PlayerStats>().PaintTarget(false);
        }
        return visibleTargets;
    }

    // used by behavior class to destroy the visual component of the token
    public void DestroyToken()
    {
        Destroy(gameObject);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Basic threat range this object is doesn't know what kind of threat range it is, it only functions to collect targets 
// within its range to threat range behavior
public class ThreatCone : MonoBehaviour
{
    // Contains the transforms of each player gameobject within range
	private List<Transform> visibleTargets = new List<Transform>();
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
        StartCoroutine(ThreatEunmerator());
    }

    // Psuedo update that empties the visible targets if the cone is moved, also where draw is checked/applied
    IEnumerator ThreatEunmerator()
    {
        while(true)
        {
            if(!draw)
            {
                GetComponent<MeshFilter>().mesh = null;
            }
            else
            {
                GetComponent<MeshFilter>().mesh = MyMesh;
            }
            visibleTargets = new List<Transform>();
            yield return new WaitForSeconds (0.02f);
        }
    }

    // Called every frame another collider is within the threat range, adds every player object within the range
    // But only if line of sight can be drawn between the origin and the target
    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            // los is expensive and should only be called on players
            if(contact.otherCollider.tag == "Player")
            {
                // checks los between players so long as they aren't excluded
                if( ValidContact(contact) && contact.otherCollider.transform != avoidOwner)
                {
                    if(!visibleTargets.Contains(contact.otherCollider.transform) )
                    {
                        visibleTargets.Add(contact.otherCollider.transform);
                        if(draw)
                        {
                            contact.otherCollider.GetComponent<PlayerStats>().PaintTarget();
                        }
                    }
                }
            }
            
        }
    }

    private bool ValidContact(ContactPoint contact)
    {
        if(StrictLOS)
        {
            return !Physics.Linecast(gameObject.transform.position, contact.otherCollider.transform.position, LayerMask.GetMask("Obstacle"));
        }
        else
        {
            return TacticsAttack.HasLOS(contact.otherCollider.gameObject, avoidOwner.gameObject);
        }
    }

    // Returns the transforms of all players within the threat range to the behavior class
    public List<Transform> GetTargets()
    {
        return visibleTargets;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreatCone : MonoBehaviour
{
	private List<Transform> visibleTargets = new List<Transform>();
    public bool draw = true;
    Mesh MyMesh;
    Collision myCollision;
    public Transform avoidOwner;

    void Start()
    {
        MyMesh = GetComponent<MeshFilter>().mesh;
        StartCoroutine(ThreatEunmerator());
    }

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

    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 myPOV = gameObject.transform.position;
            Vector3 TargetPOV = contact.otherCollider.transform.position;
            if(!Physics.Linecast(myPOV, TargetPOV, LayerMask.GetMask("Obstacle")) && contact.otherCollider.tag.Equals("Player") && (avoidOwner == null || contact.otherCollider.transform != avoidOwner))
            {
                if(!visibleTargets.Contains(contact.otherCollider.transform))
                {
                    Debug.Log("adding");
                    visibleTargets.Add(contact.otherCollider.transform);
                }
                else
                {
                    Debug.Log("already added");
                }
                contact.otherCollider.GetComponent<PlayerStats>().PaintTarget();
            }
        }
    }

    public List<Transform> GetTargets()
    {
        return visibleTargets;
    }
}

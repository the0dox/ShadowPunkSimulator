using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class OverworldMovement : MonoBehaviourPunCallbacks
{

    private Vector3 mOffset;


    private float mZCoord;

    private PhotonView pv;

    void Start()
    {
        pv = GetComponent<PhotonView>();
    }

    [PunRPC]
    void RPC_Drop(Vector3 pos)
    {
        transform.position = pos;
    }

    void OnMouseDown()

    {

        mZCoord = Camera.main.WorldToScreenPoint(

            gameObject.transform.position).z;



        // Store offset = gameobject world pos - mouse world pos

        mOffset = gameObject.transform.position - GetMouseAsWorldPoint();

    }
    
    void Update()
    {
        
        if(Input.GetMouseButtonUp(0))
        {
            mZCoord = gameObject.transform.position.z;
            mOffset = new Vector3(0,0,0);
            StartCoroutine(UpdateDelay());
        }
        else
        {
            transform.position = new Vector3(transform.position.x,0,transform.position.z);
        }
    }

    IEnumerator UpdateDelay()
    {
        yield return new WaitForSeconds(0.5f);
        pv.RPC("RPC_Drop",RpcTarget.Others,transform.position);
    }


    private Vector3 GetMouseAsWorldPoint()

    {

        // Pixel coordinates of mouse (x,y)

        Vector3 mousePoint = Input.mousePosition;



        // z coordinate of game object on screen

        mousePoint.z = mZCoord;



        // Convert it to world points

        return Camera.main.ScreenToWorldPoint(mousePoint);

    }



    void OnMouseDrag()

    {

        transform.position = GetMouseAsWorldPoint() + mOffset;

    }
}

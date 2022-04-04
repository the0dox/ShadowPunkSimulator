using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// simple class used to determine a direction usually placed on a specific player
public class DirectionSelectorBehavior : MonoBehaviour
{
    // used for static reference
    private static GameObject instance;
    // used for multiplayer 
    [SerializeField] private PhotonView pv;
    // snaps view to 4 directions
    [SerializeField] private bool snap = true;

    // called every frame, tracks player mouse movement 
    void Update()
    {   
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //if the mouse is pointed at a thing
        if (Physics.Raycast(ray, out hit))
        {
            gameObject.transform.LookAt(hit.point);
            if(snap)
            {
                float y = transform.eulerAngles.y;
                float modifiedY;
                modifiedY = y%360;
                if(modifiedY < 0)
                {
                    modifiedY += 360;
                }
                if(modifiedY >= 315 || modifiedY < 45)
                {
                    transform.eulerAngles = new Vector3(0,0,0);
                }
                else if(modifiedY > 45 && modifiedY < 135)
                {
                    transform.eulerAngles = new Vector3(0,90,0);
                }
                else if(modifiedY > 135 && modifiedY < 225)
                {
                    transform.eulerAngles = new Vector3(0,180,0);
                }
                else
                {
                    transform.eulerAngles = new Vector3(0,270,0);
                }
            }
        }
        CheckMouse();
    }

    // called when created, sets position on pos and creates
    public void SetLocation(Vector3 pos, Photon.Realtime.Player owner)
    {
        instance = this.gameObject;
        transform.position = pos;
        pv.RPC("RPC_SetLocation",RpcTarget.Others, pos, owner.ActorNumber);
    }

    public static void RemovePointer()
    {
        PhotonNetwork.Destroy(instance);
    }

    [PunRPC]
    void RPC_SetLocation(Vector3 pos, int actorID)
    {
        // if this is my player let me control this
        if(PhotonNetwork.LocalPlayer.ActorNumber == actorID)
        {
            transform.position = pos;
        }
        // else hide me
        else
        {
            gameObject.SetActive(false);
        }
    }

    // Check for left click and send y direction info back to server 
    private void CheckMouse()
    {
        if(Input.GetMouseButtonUp(0))
        {
            pv.RPC("RPC_Send_Direction_Info", RpcTarget.MasterClient, transform.eulerAngles);
        }
    }

    [PunRPC]
    void RPC_Send_Direction_Info(Vector3 Eulers)
    {
        TurnManager.instance.DirectionPointerInfo = Eulers;
        PhotonNetwork.Destroy(gameObject);
    }
}

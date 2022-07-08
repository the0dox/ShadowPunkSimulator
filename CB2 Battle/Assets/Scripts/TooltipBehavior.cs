using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;

public class TooltipBehavior : MonoBehaviour
{
    [SerializeField] private GameObject background;
    [SerializeField] private GameObject textInput; 
    [SerializeField] private TurnManager TurnMaster; 
    [SerializeField] Canvas myCanvas;
    [SerializeField] private PhotonView pv;
    private static PhotonView spv;
    
    void Start()
    {
        spv = pv;
    }

    // Update is called once per frame
    void Update()
    {
        if(!EventSystem.current.IsPointerOverGameObject())
        {
            RaycastHit hit;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                if(hit.collider.tag == "Player" && !TooltipSystem.active())
                {
                    TooltipTrigger myTrigger;
                    if(hit.collider.gameObject.TryGetComponent<TooltipTrigger>(out myTrigger))
                    {
                        myTrigger.ShowTooltip();
                        StartCoroutine(tooltipCheckDelay());
                    }
                }
            }  
        }   
    }

    IEnumerator tooltipCheckDelay()
    {
        bool active = true;
        while(active)
        {   
            yield return new WaitForSeconds(0.2f);
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                if(hit.collider.tag != "Player")
                {
                    active = false;
                }
            }  
            else
            {
                active = false;
            }
        }
        TooltipSystem.hide();
    }

    public static void UpdateToolTips(Dictionary<int, List<string>> newtt)
    {
        foreach(KeyValuePair<int,List<string>> kvp in newtt)
        {
            string[] value = kvp.Value.ToArray();
            spv.RPC("RPC_UpdateTooltip", RpcTarget.All, kvp.Key, value);
        }
        
    }

    [PunRPC]
    void RPC_UpdateTooltip(int playerID, string[] newtt)
    {
        PlayerStats token = PlayerSpawner.IDtoPlayer(playerID);
        TooltipTrigger tooltip = token.GetComponent<TooltipTrigger>();
        tooltip.content = "";
        for (int i = 0; i < newtt.Length; i++)
        {
            tooltip.content += newtt[i] + "\n";
        }
    }

}

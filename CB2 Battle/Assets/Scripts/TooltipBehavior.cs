using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class TooltipBehavior : MonoBehaviour
{
    [SerializeField] private GameObject background;
    [SerializeField] private GameObject textInput; 
    [SerializeField] private TurnManager TurnMaster; 
    [SerializeField] Canvas myCanvas;
    [SerializeField] private PhotonView pv;
    private static PhotonView spv;

    private static Dictionary<Vector3,List<string>> ToolTips = new Dictionary<Vector3, List<string>>();
    
    void Start()
    {
        spv = pv;
    }

    // Update is called once per frame
    void Update()
    {
        ResetSize();
        textInput.SetActive(false);
        background.SetActive(false);
        RaycastHit hit;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            Vector3 hitPos = hit.collider.transform.position;
            if(ToolTips.ContainsKey(hitPos))
            {
                SetText(ToolTips[hitPos]);
            }
        }  
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(myCanvas.transform as RectTransform, Input.mousePosition, myCanvas.worldCamera, out pos);
        transform.position = myCanvas.transform.TransformPoint(pos);
    }

    public static void UpdateToolTips(Dictionary<Vector3, List<string>> newtt)
    {
        spv.RPC("RPC_Clear",RpcTarget.All);
        foreach(KeyValuePair<Vector3,List<string>> kvp in newtt)
        {
            Vector3 key = kvp.Key;
            string[] value = kvp.Value.ToArray();
            spv.RPC("RPC_UpdateTooltip", RpcTarget.All, key, value);
        }
        
    }
    [PunRPC]
    void RPC_Clear()
    {
        ToolTips = new Dictionary<Vector3, List<string>>();
    }

    [PunRPC]
    void RPC_UpdateTooltip(Vector3 newpos, string[] newtt)
    {
        List<string>translatedTT = new List<string>();
        for (int i = 0; i < newtt.Length; i++)
        {
            translatedTT.Add(newtt[i]);
        }
        ToolTips.Add(newpos, translatedTT);
    }

    public void SetText(List<string> input)
    {
        textInput.SetActive(true);
        background.SetActive(true);
        Text ToolTipText = textInput.GetComponent<Text>();
        ToolTipText.text = "";
        foreach(string s in input)
        {
            ToolTipText.text += s;
            ToolTipText.text += "\n";
            IncreaseSize();
        }
    }

    public void ResetSize()
    {
        background.GetComponent<Image>().rectTransform.sizeDelta = new Vector2(110,0); 
    }
    
    public void IncreaseSize()
    {
        textInput.GetComponent<Text>().rectTransform.sizeDelta += new Vector2(0,55f); 
        background.GetComponent<Image>().rectTransform.sizeDelta += new Vector2(0,55f); 
    }
}

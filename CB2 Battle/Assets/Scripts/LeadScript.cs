using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class LeadScript : MonoBehaviourPunCallbacks
{
    [SerializeField] private float MaxHours;
    [SerializeField] private float CompletedHours = 0;
    private Vector3 dividerPos = new Vector3(0,-3.5f,0);
    [SerializeField] private GameObject DividerImage;
    [SerializeField] private InputField Title;
    [SerializeField] private Text TextTime;
    [SerializeField] private Image pieChart;
    private PlayerStats[] players;
    private static Vector3 activityDisplacement = new Vector3(0, 200, 0);
    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;
    [SerializeField] private GameObject player3;
    private PhotonView pv;

    void Update()
    {
        int difference = (int)(MaxHours - CompletedHours);
        if(difference / 24 > 0)
        {
            TextTime.text = (difference / 24) + " days remaining";
        }
        else
        {
            TextTime.text = (MaxHours - CompletedHours) + " hours remaining";
        }
        pieChart.fillAmount = (CompletedHours/MaxHours);
    }
    public void UpdateLead(int modifier, int time, PlayerStats[] players, string skill, string name)
    {
        Title.text = name;
        this.players = players;
        MaxHours = time;
        PlayerStats mainPlayer = players[0];
        
    }



    public void SetLead( string name, float progress, float time, int count)
    {
        pv = GetComponent<PhotonView>();
        pv.RPC("RPC_SetLead", RpcTarget.All, name,progress,time,count);
    }

    [PunRPC]
    void RPC_SetLead(string name, float progress, float time, int count)
    {
        Debug.Log(name);
        Title.text = name;
        this.MaxHours = time;
        this.CompletedHours = progress;
        transform.SetParent(GameObject.FindGameObjectWithTag("LeadParent").transform);
        transform.localPosition = new Vector3();
        transform.localPosition -= new Vector3(0, 200 * (count + 1),0);
    }

    //depreciated
    public void Init()
    {
        float angleIncrementer = -360f/(float)MaxHours;
        for(int i = 0; i < MaxHours; i++)
        {
            GameObject newLine = Instantiate(DividerImage) as GameObject;
            newLine.transform.SetParent(gameObject.transform);
            newLine.transform.localPosition = dividerPos;
            newLine.transform.Rotate(0,0, i * angleIncrementer, Space.Self);
        }
    }

    public bool Completed()
    {
        return CompletedHours >= MaxHours;
    }


    public void IncrementTime()
    {
        pv.RPC("RPC_Increment",RpcTarget.All);
    }
    [PunRPC]
    void RPC_Increment()
    {
        CompletedHours ++;
    }

    public float GetCompletedHours()
    {
        return CompletedHours;
    }

    public float GetMaxHours()
    {
        return MaxHours;
    }

    public string getName()
    {
        return Title.text;
    }

    public void Delete()
    {
        ActionQueueDisplay.RemoveActivity(this.gameObject);
    }
}

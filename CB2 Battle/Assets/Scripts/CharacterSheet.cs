using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

// UI window used for editing characters, can take save data or active map tokens
public class CharacterSheet : MonoBehaviourPunCallbacks
{
    // Reference to all Inputfieldscripts so that the charactersheet only accesses its own inputs
    [SerializeField] private List<InputFieldScript> EntryList;
    // main page reference
    [SerializeField] private GameObject page1;
    // equipment page reference
    [SerializeField] private GameObject page2;
    // reference for editing savedata
    private static CharacterSaveData ActivePlayer; 
    // an indvidual field for displaying weapon stats
    [SerializeField] private GameObject WeaponDisplay;
    // unique inputfield that saves the players name
    [SerializeField] private InputField NameField;
    [SerializeField] private PhotonView pv;
    [SerializeField] private TrackerSheet HealthMonitor;
    [SerializeField] private TrackerSheet StunMonitor;
    [SerializeField] private TrackerSheet EdgeMonitor;

    public bool MasterActive = false;
    public bool ClientActive = false;
    public bool MasterActiveOnce = false;
    public bool ClientActiveOnce = false;


    private PlayerStats CurrentToken;
    private int NPCHealth = -1;
    // Called when created downloads player data onto the sheet and freezes the screen
    public void Init(){
        DmMenu.ActiveCharacterSheet = this;
        CameraButtons.UIFreeze(true);
        transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform);
        transform.localPosition = new Vector3();
        transform.localScale = new Vector3(1,1,1);

    }

    // Uploading data is different depending on if we are editing save data of a map token
    public void UpdateStatsOut()
    {
        /* if this is called client side send my changes to the server
        if(!pv.IsMine)
        {
            pv.RPC("RPC_SyncStatsOut",RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, NameField.text, ActivePlayer.CompileStats(), ActivePlayer.skillSpecialization, ActivePlayer.compileEquipment().ToArray(), NPCHealth, ActivePlayer.CompileTalents());
        }
        */
        // if this is server side apply the changes
            // apply changes to npcs
        if(CurrentToken != null && CurrentToken.team != 0)
        {
            CurrentToken.NPCHealth = this.NPCHealth;
            CurrentToken.OnDownload();
        }
        // apply changes to an individual player
        else
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach(GameObject p in players)
            {
                PlayerStats currentPlayer = p.GetComponent<PlayerStats>();
                if(currentPlayer.myData.Equals(ActivePlayer))
                {
                    currentPlayer.OnDownload();
                }
            }
        }
        PhotonNetwork.Destroy(gameObject);
    }

    // playerstats activeplayer in the initative queue
    // callingplayer id: reference to the player that called for a charactersheet
    // Always called first and on the master client, gets the activeplayer and sends their stats back
    // to the player that called for a charactersheet
    public void UpdateStatsIn(PlayerStats input, int callingPlayerID)
    {
        CurrentToken = input;
        UpdateStatsIn(input.myData,callingPlayerID);
    }
    // Doownloads data from savedata
    public void UpdateStatsIn(CharacterSaveData input, int callingPlayerID)
    {
        // NPCS track health seperately per token
        if(CurrentToken != null && input.team != 0)
        {
            NPCHealth = CurrentToken.getWounds();
        }

        // always build the sheet on the master side
        if(!MasterActiveOnce)
        {
            MasterActiveOnce = true;
            ActivePlayer = input;
            UpdateStatsIn();
        }

        Photon.Realtime.Player CallingPlayer = PhotonNetwork.CurrentRoom.GetPlayer(callingPlayerID);
        //server side edit, no need for transfer notice this is always called by master so isMine doesn't work here
        if(CallingPlayer == pv.Owner)
        {
            MasterActive = true;
            Init();
        }
        // client side edit, transfer charactershetet
        else
        { 
            ClientActive = true;
            if(!ClientActiveOnce)
            {
                ClientActiveOnce = true;
                pv.RPC("RPC_SyncStats", CallingPlayer, input.playername, input.CompileStats(), input.skillSpecialization, input.compileEquipment().ToArray(), NPCHealth, input.CompileTalents());
            }
            else
            {
                pv.RPC("RPC_DontSync",CallingPlayer);
            }

        }
    }

    // Generic info that both kinds of download needs to know
    public void UpdateStatsIn(){
        NameField.text = ActivePlayer.playername;
        ActivePlayer.CalculateCharacteristics();
        StunMonitor.SetResource(ActivePlayer.GetAttribute(AttributeKey.SDamage));
        EdgeMonitor.SetResource(ActivePlayer.GetAttribute(AttributeKey.Edge));

        if(NPCHealth >= 0)
        {
            HealthMonitor.SetResource(NPCHealth);
        }
        else
        {
            HealthMonitor.SetResource(ActivePlayer.GetAttribute(AttributeKey.PDamage));
        }
        UpdateInputFields();
        SkillAdder.DownloadOwner(ActivePlayer);
        ItemAdder.DownloadOwner(ActivePlayer);
        TalentAdder.DownloadOwner(ActivePlayer);
        page2.SetActive(false);
    }


    // Bars
    public void AddHealth()
    {
        if(NPCHealth >= 0)
        {
            if(NPCHealth < 18)
            {
                HealthMonitor.IncrementValue();
                NPCHealth++;
            }
        }
        else
        {
            if(ActivePlayer.attribues[(int)AttributeKey.PDamage] < 18)
            {
                HealthMonitor.IncrementValue();
                ActivePlayer.attribues[(int)AttributeKey.PDamage]++;
            }
        }
    }

    public void DecreaseHealth()
    {
        if(NPCHealth >= 0)
        {
            if(NPCHealth > 0)
            {
                HealthMonitor.SubtractValue();
                NPCHealth--;
            }
        }
        else
        {
            if(ActivePlayer.attribues[(int)AttributeKey.PDamage] > 0)
            {
                HealthMonitor.SubtractValue();
                ActivePlayer.attribues[(int)AttributeKey.PDamage]--;
            }
        }
    }

    public void AddStun()
    {
        if(ActivePlayer.attribues[(int)AttributeKey.SDamage] < 12)
        {
            StunMonitor.IncrementValue();
            ActivePlayer.attribues[(int)AttributeKey.SDamage]++;
        }
    }

    public void DecreaseStun()
    {
        if(ActivePlayer.attribues[(int)AttributeKey.SDamage] > 0)
        {
            StunMonitor.SubtractValue();
            ActivePlayer.attribues[(int)AttributeKey.SDamage]--;
        }
    }

    public void AddEdge()
    {
        if(ActivePlayer.attribues[(int)AttributeKey.CurrentEdge] < 7)
        {
            EdgeMonitor.IncrementValue();
            ActivePlayer.attribues[(int)AttributeKey.CurrentEdge]++;
        }
    }

    public void DecreaseEdge()
    {
        if(ActivePlayer.attribues[(int)AttributeKey.CurrentEdge] > 0)
        {
            EdgeMonitor.SubtractValue();
            ActivePlayer.attribues[(int)AttributeKey.CurrentEdge]--;
        }
    }

    // Change Name
    public void OnNameUpdated()
    {
        string newName = NameField.text;
        pv.RPC("RPC_OnNameUpdated", RpcTarget.All, newName);
    }
    [PunRPC]
    void RPC_OnNameUpdated(string newName)
    {
        if(pv.IsMine)
        {
            ActivePlayer.playername = newName;
        }
        NameField.text = newName;
    }

    // sends new value for a given attribute to the server
    public void UpdatedAttribute(AttributeKey key, int value)
    {
        pv.RPC("RPC_UpdatedAttributeMaster", RpcTarget.MasterClient, (int)key, value);
    } 

    // server recieves new value, calculates new attributes, and sends the new attributes back
    [PunRPC]
    void RPC_UpdatedAttributeMaster(int key, int value)
    {
        ActivePlayer.SetAttribute((AttributeKey)key,value,true);
        UpdateInputFields();
        SkillAdder.UpdateSkillFields();
        pv.RPC("RPC_UpdatedAttributeClient", RpcTarget.Others, ActivePlayer.CompileStats());
    }

    // New calculations are sent back to clients
    [PunRPC]
    void RPC_UpdatedAttributeClient(string[] newCharacteristics)
    {
        ActivePlayer.DecompileStats(newCharacteristics);
        UpdateInputFields();
        SkillAdder.UpdateSkillFields();
    }

    private void UpdateInputFields()
    {
        foreach (InputFieldScript t in EntryList)
        {
            t.UpdateValue(ActivePlayer,this);
        }
        HealthMonitor.SetMaximum(ActivePlayer.GetAttribute(AttributeKey.PhysicalHealth));
        StunMonitor.SetMaximum(ActivePlayer.GetAttribute(AttributeKey.StunHealth));
        EdgeMonitor.SetMaximum(ActivePlayer.GetAttribute(AttributeKey.Edge));
    }


    // Master sends this to the client to be edited
    [PunRPC]
    void RPC_SyncStats(string name, string[] characteristics, Dictionary<string,int> specalizations, string[] equipment, int newNPCHealth, Dictionary<int,bool> newTalents)
    {
        List<string> convertedArray = new List<string>();
        for(int i = 0; i < equipment.Length; i++)
        {
            convertedArray.Add(equipment[i]);
        }
        ActivePlayer = new CharacterSaveData(name, characteristics, specalizations, convertedArray,newTalents);
        if(newNPCHealth >= 0)
        {
            Debug.Log("Unique npc health" + newNPCHealth);
            NPCHealth = newNPCHealth;
        }
        Init();
        UpdateStatsIn();
    }

    [PunRPC]
    void RPC_DontSync()
    {
        Init();
    }
    
    // when exiting as either master or client
    public void OnExit()
    {
        CameraButtons.UIFreeze(false);
        TooltipSystem.hide();
        transform.SetParent(null);
        pv.RPC("RPC_OnExit", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    void RPC_OnExit(int callingPlayerID)
    {
        // If the master called Onexit
        if(callingPlayerID == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            MasterActive = false;
        }
        // if the client called Onexit
        else
        {
            ClientActive = false;
        }
        // if neither is active, then close
        if(!MasterActive && !ClientActive)
        {
            Debug.Log("no one is active on sheet, deleting");
            UpdateStatsOut();
        }
    }

    /* Client sends this to the master to be saved 
    [PunRPC]
    void RPC_SyncStatsOut(int callingPlayerID, string newName, string[] newCharacteristics, Dictionary<string,int> newSpecalizations, string[] newequipment, int newNPCHealth, Dictionary<int,bool> newTalents)
    {
        DmMenu.RemoveClientSheet(callingPlayerID);
        ActivePlayer.playername = newName;
        ActivePlayer.skillSpecialization = newSpecalizations;
        List<string> convertedArray = new List<string>();
        for(int i = 0; i < newequipment.Length; i++)
        {
            convertedArray.Add(newequipment[i]);
        }
        ActivePlayer.decompileEquipment(convertedArray);
        ActivePlayer.DecompileStats(newCharacteristics);
        ActivePlayer.DecompileTalents(newTalents);
        NPCHealth = newNPCHealth;
        UpdateStatsOut();
    }
    */
}

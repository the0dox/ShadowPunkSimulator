using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private bool offlineMode = false;
    [SerializeField] private Text descriptionTextObject;
    [SerializeField] private string description;

    private void Start()
    {
        descriptionTextObject.text = description + Application.version;
        PhotonNetwork.OfflineMode = offlineMode;
        if(offlineMode)
        {
            PhotonNetwork.LoadLevel("Lobby");
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.LoadLevel("Lobby");
    }
}

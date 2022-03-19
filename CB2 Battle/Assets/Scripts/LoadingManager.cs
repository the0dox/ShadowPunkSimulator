using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class LoadingManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private bool offlineMode = false;

    private void Start()
    {
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

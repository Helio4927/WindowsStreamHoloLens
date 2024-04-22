using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public delegate void DelegateConnected();
    public event DelegateConnected EventConnected;
    public delegate void DelegateDisconnected();
    public event DelegateDisconnected EventDisconnected;
    public delegate void DelegateJoinedLobby();
    public event DelegateJoinedLobby EventJoinedLobby;
    public delegate void DelegateRoomListUpdated();
    public event DelegateRoomListUpdated EventRoomListUpdated;
    public delegate void DelegateJoinRoom();
    public event DelegateJoinRoom EventJoinRoom;
    public delegate void DelegateJoinRoomFailed();
    public event DelegateJoinRoomFailed EventJoinRoomFailed;
    public delegate void DelegateLeftRoom();
    public event DelegateLeftRoom EventLeftRoom;
    public delegate void DelegateCreatedRoom(string roomName);
    public event DelegateCreatedRoom EventCreatedRoom;
    public delegate void DelegateCreateRoomFailed();
    public event DelegateCreateRoomFailed EventCreateRoomFailed;
    public delegate void DelegateOtherEnteredRoom();
    public event DelegateOtherEnteredRoom EventOtherEnteredRoom;
    public delegate void DelegateOtherLeftRoom();
    public event DelegateOtherLeftRoom EventOtherLeftRoom;

    public const string localRoomNameKey = "KEY_LOCALROOMNAME";
    private List<string> roomNames = new List<string>();
    public string localRoomName;

    private static NetworkManager instance;

    private void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
            localRoomName = PlayerPrefs.GetString(localRoomNameKey, "test");
        }
    }

    public static NetworkManager GetInstance()
    {
        return instance;
    }

    public void Connect()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.OfflineMode = false;
            PhotonNetwork.GameVersion = Application.version;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        //PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.JoinLobby(TypedLobby.Default);
        if (EventConnected != null)
        {
            EventConnected.Invoke();
        }
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        if (EventJoinedLobby != null)
        {
            EventJoinedLobby.Invoke();
        }
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        if (EventCreatedRoom != null)
        {
            EventCreatedRoom.Invoke(localRoomName);
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);
        Logger.GetInstance().LogError(message);
        if (EventCreateRoomFailed != null)
        {
            EventCreateRoomFailed.Invoke();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);
        if (EventDisconnected != null)
        {
            EventDisconnected.Invoke();
        }
    }

    public void Disconnect()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        if (EventLeftRoom != null)
        {
            EventLeftRoom.Invoke();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);
        Disconnect();
    }

    public bool IsConnected()
    {
        return PhotonNetwork.IsConnected;
    }

    public bool IsMasterClient()
    {
        return PhotonNetwork.IsMasterClient;
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        base.OnRoomListUpdate(roomList);
        roomNames = roomList.Select(x => x.Name).ToList();
        if (EventRoomListUpdated != null)
        {
            EventRoomListUpdated.Invoke();
        }
    }

    public void CreateOrJoinRoom()
    {
        if (roomNames.Contains(localRoomName))
        {
            JoinRoom(localRoomName);
        }
        else
        {
            CreateRoom(localRoomName);
        }
    }

    public void CreateRoom(string roomName)
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;
        roomOptions.IsVisible = true;
        roomOptions.IsOpen = true;
        PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    public void JoinRoom(string roomName)
    {
        if (!PhotonNetwork.InRoom && roomName.Length != 0)
        {
            PhotonNetwork.JoinRoom(roomName);
        }
        else
        {
            EventJoinRoomFailed.Invoke();
        }
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        if (EventJoinRoom != null)
        {
            EventJoinRoom.Invoke();
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRandomFailed(returnCode, message);
        if (EventJoinRoomFailed != null)
        {
            EventJoinRoomFailed.Invoke();
        }
        Logger.GetInstance().LogError(message);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (EventOtherEnteredRoom != null)
        {
            EventOtherEnteredRoom.Invoke();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        if (EventOtherLeftRoom != null)
        {
            EventOtherLeftRoom.Invoke();
        }
    }

    public void RPC(MonoBehaviour mb, string method, params object[] parameters)
    {
        if (IsConnected())
        {
            PhotonView photonView = mb.GetComponent<PhotonView>();
            if (photonView)
            {
                photonView.RPC(method, RpcTarget.Others, parameters);
            }
        }
    }

    public bool IsMainPlayer()
    {
        return !IsConnected() || IsMasterClient();
    }

    public int GetPlayersNumber()
    {
        return PhotonNetwork.PlayerList.Length;
    }
}

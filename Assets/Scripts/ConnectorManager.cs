using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ConnectorManager : MonoBehaviour
{
    public UnityEvent<string> chatMessageReceived = new UnityEvent<string>();

    private void Start()
    {
        NetworkManager.GetInstance().EventConnected += () => UpdateConnectingStatusText("Connected to server");
        NetworkManager.GetInstance().EventJoinedLobby += () => ConnectedToLobby();
        NetworkManager.GetInstance().EventRoomListUpdated += () => GotRoomNames();
        NetworkManager.GetInstance().EventCreatedRoom += (name) => UpdateConnectingStatusText("Waiting for other user");
        NetworkManager.GetInstance().EventJoinRoom += () => JoinRoom();
        NetworkManager.GetInstance().EventOtherEnteredRoom += () => OtherEnteredRoom();
        NetworkManager.GetInstance().EventOtherLeftRoom += () => OtherLeftRoom();
        NetworkManager.GetInstance().EventCreateRoomFailed += () => RetryCreateOrJoin("Room creation failed");
        NetworkManager.GetInstance().EventJoinRoomFailed += () => RetryCreateOrJoin("Room join failed");
        NetworkManager.GetInstance().Connect();
    }

    #region ServerConnection
    private bool gotRoomNames;

    private void UpdateConnectingStatusText(string s)
    {
        Logger.GetInstance().Log(s);
    }

    private void ConnectedToLobby()
    {
        gotRoomNames = false;
        UpdateConnectingStatusText("Connected to lobby");
    }


    private void GotRoomNames()
    {
        if (!gotRoomNames)
        {
            gotRoomNames = true;
            CreateOrJoinRoom();
        }
    }

    private void CreateOrJoinRoom()
    {
        Logger.GetInstance().Log("Room: " + NetworkManager.GetInstance().localRoomName);
        UpdateConnectingStatusText("Joining");
        NetworkManager.GetInstance().CreateOrJoinRoom();
    }

    private void RetryCreateOrJoin(string errorText)
    {
        UpdateConnectingStatusText(errorText + ", trying in 5 seconds");
        Invoke(nameof(CreateOrJoinRoom), 5);
    }

    private void JoinRoom()
    {
        UpdateConnectingStatusText("Connected");
        if (NetworkManager.GetInstance().GetPlayersNumber() > 1)
        {
            if (textureStreamer)
            {
                textureStreamer.gameObject.SetActive(true);
            }
        }
    }

    private void OtherEnteredRoom()
    {
        UpdateConnectingStatusText("Other user connected");
        if (NetworkManager.GetInstance().GetPlayersNumber() > 1)
        {
            if (textureStreamer)
            {
                textureStreamer.gameObject.SetActive(true);
            }
        }
    }

    private void OtherLeftRoom()
    {
        UpdateConnectingStatusText("Other user disconnected");
        if (NetworkManager.GetInstance().GetPlayersNumber() < 2)
        {
            if (textureStreamer)
            {
                textureStreamer.gameObject.SetActive(false);
            }
        }
    }
    #endregion

    #region video
    public TextureStreamer textureStreamer;

    #endregion

    #region chat
    public void AddMessageToChat(string message)
    {
        NetworkManager.GetInstance().RPC(this, nameof(RPCAddMessageToChat), message);
    }

    [PunRPC]
    protected void RPCAddMessageToChat(string message)
    {
        chatMessageReceived.Invoke(message);
    }
    #endregion
}
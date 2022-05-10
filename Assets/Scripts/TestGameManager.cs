using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RyfiGames.Controller;

public class TestGameManager : MonoBehaviour, IControllerListener
{
    public string eventName;
    public string eventParam;
    public bool triggerEvent;
    public TestJoystick joystick;
    public List<JoinAttempt> joinAttempts = new List<JoinAttempt>();
    public List<ControllerPlayer> players = new List<ControllerPlayer>();

    public void OnAcceptedPlayer(ControllerPlayer player)
    {
        players.Add(player);
        print($"Player Accepted: {player.username}, {player.userID}, {player.mode}");
    }

    public void OnConnectToServer()
    {
        print("connected to server");
    }

    public void OnControllerData(ControllerPlayer player)
    {
        joystick.stickX = player.controllerData.joysticks[0].x;
        joystick.stickY = -player.controllerData.joysticks[0].y;
    }

    public void OnError(string message)
    {
        print(message);
    }

    public void OnHostSuccess(string pin)
    {
        print("host success. Pin: " + pin);
    }

    public void OnJoinAttempt(JoinAttempt joinAttempt)
    {
        joinAttempts.Add(joinAttempt);
        print($"Join Attempt: {joinAttempt.username}, {joinAttempt.joinAttemptID}, {joinAttempt.mode}");
    }

    public void OnPlayerDisconnect(ControllerPlayer player)
    {
        players.Remove(player);
        print($"Player Disconnect: {player.username}, {player.userID}, {player.mode}");
    }

    // Start is called before the first frame update
    void Start()
    {
        RyfiControllerManager.AddListener(this);
        RyfiControllerManager.InitializeConnection();
    }

    private void EventTrigger()
    {
        switch (eventName)
        {
            case "requestHost":
                RyfiControllerManager.RequestRoom();
                break;
            case "acceptPlayer":
                joinAttempts[0].AcceptPlayer();
                joinAttempts.RemoveAt(0);
                break;
            case "kickPlayer":
                players[0].KickPlayer();
                break;
            case "closeRoom":
                RyfiControllerManager.CloseRoom();
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (triggerEvent)
        {
            triggerEvent = false;
            EventTrigger();
        }
    }
}

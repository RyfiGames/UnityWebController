using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quobject.SocketIoClientDotNet.Client;
using Unity.WebRTC;
using System.Text;

namespace RyfiGames.Controller
{
    public class RyfiControllerManager : MonoBehaviour
    {
        public static RyfiControllerManager singleton;
        public string gameID;
        public Camera streamCamera;
        public static MediaStream camStream;
        public static MediaStream audioStream;
        private static string serverURL = "https://controller.y33t.net:443";
        private static Socket socket;
        private static List<EventHolder> runNextFrame = new List<EventHolder>();

        private static List<IControllerListener> listeners = new List<IControllerListener>();
        private static List<JoinAttempt> joinAttempts = new List<JoinAttempt>();
        private static List<ControllerPlayer> players = new List<ControllerPlayer>();

        void Awake()
        {
            if (RyfiControllerManager.singleton)
            {
                Destroy(gameObject);
            }
            else
            {
                RyfiControllerManager.singleton = this;
                DontDestroyOnLoad(gameObject);

                WebRTC.Initialize(EncoderType.Software);
                camStream = streamCamera.CaptureStream(1280, 720, 1000000);
                audioStream = Audio.CaptureStream();
                StartCoroutine(WebRTC.Update());
            }
        }

        public static void AddListener(IControllerListener listener)
        {
            listeners.Add(listener);
        }
        public static void RemoveListener(IControllerListener listener)
        {
            listeners.Remove(listener);
        }

        public static void InitializeConnection()
        {
            socket = IO.Socket(serverURL);

            socket.On(Socket.EVENT_ERROR, (err) =>
            {
                runNextFrame.Add(new EventHolder("error", err.ToString()));
            });
            socket.On(Socket.EVENT_CONNECT_ERROR, (err) =>
            {
                runNextFrame.Add(new EventHolder("connerror", err.ToString()));
            });
            socket.On(Socket.EVENT_CONNECT_TIMEOUT, (err) =>
            {
                runNextFrame.Add(new EventHolder("connerror", err.ToString()));
            });
            socket.On("errorMessage", new ListenerImpl((info, status) =>
            {
                runNextFrame.Add(new EventHolder("error", info.ToString()));
            }));

            socket.Once(Socket.EVENT_CONNECT, () =>
            {
                runNextFrame.Add(new EventHolder("connect"));
            });

            socket.On("hostFail", (reason) =>
            {
                runNextFrame.Add(new EventHolder("hostFail", reason.ToString()));
            });
            socket.On("hostConfirm", (pin) =>
            {
                runNextFrame.Add(new EventHolder("hostConfirm", pin.ToString()));
            });
            socket.On("joinAttempt", new ListenerImpl((username, jaid, mode) =>
            {
                runNextFrame.Add(new EventHolder("joinAttempt", jaid.ToString(), username.ToString(), mode.ToString()));
            }));
            socket.On("acceptedPlayer", new ListenerImpl((username, userID, mode) =>
            {
                runNextFrame.Add(new EventHolder("acceptedPlayer", userID.ToString(), username.ToString(), mode.ToString()));
            }));
            socket.On("disconnectPlayer", (userID) =>
            {
                runNextFrame.Add(new EventHolder("disconnectPlayer", userID.ToString()));
            });

            socket.On("rtcAnswer", new ListenerImpl((userID, sdp) =>
            {
                runNextFrame.Add(new EventHolder("rtcAnswer", userID.ToString(), sdp.ToString()));
            }));
            socket.On("rtcAnswerCandidate", new ListenerImpl((userID, candidate, sdpMid, sdpMLineIndex) =>
            {
                runNextFrame.Add(new EventHolder("rtcAnswerCandidate", userID.ToString(), candidate.ToString(), sdpMid.ToString(), sdpMLineIndex.ToString()));
            }));
        }

        private static ControllerPlayer PlayerByUserID(string userID)
        {
            foreach (ControllerPlayer player in players)
            {
                if (player.userID == userID)
                {
                    return player;
                }
            }
            return null;
        }

        public static void RequestRoom()
        {
            socket.Emit("requestHost", singleton.gameID);
        }

        public static void AcceptPlayer(JoinAttempt joinAttempt)
        {
            socket.Emit("acceptPlayer", joinAttempt.joinAttemptID);
            joinAttempts.Remove(joinAttempt);
        }

        public static void RejectPlayer(JoinAttempt joinAttempt)
        {
            joinAttempts.Remove(joinAttempt);
        }

        public void CreateRTCOffer(ControllerPlayer player)
        {
            StartCoroutine(player.CreateOffer());
        }

        public static void KickPlayer(ControllerPlayer player)
        {
            socket.Emit("kickPlayer", player.userID);
        }

        public static void CloseRoom()
        {
            socket.Emit("closeRoom");
            foreach (ControllerPlayer player in players)
            {
                player.DisconnectPlayer();
            }
            joinAttempts.Clear();
        }

        public static void ControllerDataUpdated(ControllerPlayer player)
        {
            foreach (IControllerListener listener in listeners)
            {
                listener.OnControllerData(player);
            }
        }

        void Start()
        {
            if (!RTCAudioReader.audioReaderActive)
            {
                Debug.LogWarning("No RTCAudioReader detected, this component is required to be with the AudioListener to stream audio.");
            }
        }

        public static void ShareError(string err)
        {
            foreach (IControllerListener listener in listeners)
            {
                listener.OnError("Error: " + err);
            }
        }

        private void RunHeldEvent(EventHolder e)
        {
            ControllerPlayer player = null;
            switch (e.eventName)
            {
                case "error":
                    foreach (IControllerListener listener in listeners)
                    {
                        listener.OnError("Error: " + e.parameters[0]);
                    }
                    break;
                case "connerror":
                    foreach (IControllerListener listener in listeners)
                    {
                        listener.OnError("Connection Error: " + e.parameters[0]);
                    }
                    break;
                case "connect":
                    foreach (IControllerListener listener in listeners)
                    {
                        listener.OnConnectToServer();
                    }
                    break;
                case "hostFail":
                    string message = "Host Request Failed: " + e.parameters[0];
                    foreach (IControllerListener listener in listeners)
                    {
                        listener.OnError(message);
                    }
                    break;
                case "hostConfirm":
                    foreach (IControllerListener listener in listeners)
                    {
                        listener.OnHostSuccess(e.parameters[0]);
                    }
                    break;
                case "joinAttempt":
                    JoinAttempt joinAttempt = new JoinAttempt(e.parameters[0], e.parameters[1], e.parameters[2]);
                    joinAttempts.Add(joinAttempt);
                    foreach (IControllerListener listener in listeners)
                    {
                        listener.OnJoinAttempt(joinAttempt);
                    }
                    break;
                case "acceptedPlayer":
                    player = new ControllerPlayer(e.parameters[0], e.parameters[1], e.parameters[2], socket);
                    players.Add(player);
                    foreach (IControllerListener listener in listeners)
                    {
                        listener.OnAcceptedPlayer(player);
                    }
                    break;
                case "disconnectPlayer":
                    player = PlayerByUserID(e.parameters[0]);
                    player.DisconnectPlayer();
                    players.Remove(player);
                    foreach (IControllerListener listener in listeners)
                    {
                        listener.OnPlayerDisconnect(player);
                    }
                    break;
                case "rtcAnswer":
                    var answerDesc = new RTCSessionDescription();
                    answerDesc.sdp = e.parameters[1];
                    answerDesc.type = RTCSdpType.Answer;
                    PlayerByUserID(e.parameters[0]).SetDesc(answerDesc);
                    break;
                case "rtcAnswerCandidate":
                    RTCIceCandidateInit t = new RTCIceCandidateInit();
                    t.candidate = e.parameters[1];
                    t.sdpMid = e.parameters[2];
                    t.sdpMLineIndex = int.Parse(e.parameters[3]);
                    RTCIceCandidate rtcCandidate = new RTCIceCandidate(t);
                    PlayerByUserID(e.parameters[0]).AddICE(rtcCandidate);
                    break;
            }
        }

        void Update()
        {
            foreach (EventHolder e in runNextFrame)
            {
                RunHeldEvent(e);
            }
            runNextFrame.Clear();
        }

        void OnDestroy()
        {
            foreach (ControllerPlayer player in players)
            {
                player.DisconnectPlayer();
            }
            WebRTC.Dispose();
            socket.Disconnect();
        }
    }

    public interface IControllerListener
    {
        void OnConnectToServer();
        void OnError(string message);
        void OnHostSuccess(string pin);
        void OnJoinAttempt(JoinAttempt joinAttempt);
        void OnAcceptedPlayer(ControllerPlayer player);
        void OnPlayerDisconnect(ControllerPlayer player);
        void OnControllerData(ControllerPlayer player);
    }

    public class JoinAttempt
    {
        public string joinAttemptID;
        public string username;
        public string mode;

        public JoinAttempt(string joinAtteptID, string username, string mode)
        {
            this.joinAttemptID = joinAtteptID;
            this.username = username;
            this.mode = mode;
        }

        public void AcceptPlayer()
        {
            RyfiControllerManager.AcceptPlayer(this);
        }
        public void RejectPlayer()
        {
            RyfiControllerManager.RejectPlayer(this);
        }
    }

    public class ControllerPlayer
    {
        public string userID;
        public string username;
        public string mode;
        public ControllerData controllerData;
        private Socket signal;
        private RTCPeerConnection pc;
        private RTCDataChannel dataChannel;
        public ControllerPlayer(string userID, string username, string mode, Socket socket)
        {
            this.userID = userID;
            this.username = username;
            this.mode = mode;
            controllerData = new ControllerData();
            signal = socket;
            PeerInit();
        }
        private void PeerInit()
        {
            var config = GetSelectedSdpSemantics();
            pc = new RTCPeerConnection(ref config);
            pc.OnIceCandidate = (e) =>
            {
                if (e != null)
                {
                    signal.Emit("rtcHostCandidate", userID, e.Candidate, e.SdpMid, e.SdpMLineIndex);
                }
            };
            dataChannel = pc.CreateDataChannel("controllerData");
            dataChannel.OnOpen = () =>
            {
                dataChannel.OnMessage = ReceiveData;
            };
            if (mode == "screenshare" || mode == "both")
            {
                foreach (var track in RyfiControllerManager.camStream.GetTracks())
                {
                    pc.AddTrack(track);
                }
                foreach (var track in RyfiControllerManager.audioStream.GetTracks())
                {
                    pc.AddTrack(track);
                }
            }
            RyfiControllerManager.singleton.CreateRTCOffer(this);
        }
        public IEnumerator CreateOffer()
        {
            RTCOfferAnswerOptions opt = default;
            var offer = pc.CreateOffer(ref opt);
            yield return offer;
            if (offer.IsError)
            {
                RyfiControllerManager.ShareError(offer.Error.ToString());
            }
            var sessionDesc = offer.Desc;
            pc.SetLocalDescription(ref sessionDesc);
            signal.Emit("rtcOffer", userID, sessionDesc.sdp);
        }
        public void SetDesc(RTCSessionDescription desc)
        {
            pc.SetRemoteDescription(ref desc);
        }
        public void AddICE(RTCIceCandidate ice)
        {
            pc.AddIceCandidate(ice);
        }
        private void ReceiveData(byte[] bytes)
        {
            string data = Encoding.Default.GetString(bytes);
            controllerData.Update(data);
            RyfiControllerManager.ControllerDataUpdated(this);
        }
        public void KickPlayer()
        {
            RyfiControllerManager.KickPlayer(this);
        }
        public void DisconnectPlayer()
        {
            dataChannel.Close();
            pc.Close();
        }

        private static RTCConfiguration GetSelectedSdpSemantics()
        {
            RTCConfiguration config = default;
            config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun1.l.google.com:19302", "stun:stun2.l.google.com:19302" } } };

            return config;
        }
    }

    public class ControllerData
    {
        public List<Vector2> joysticks;
        public List<bool> buttons;
        public ControllerData()
        {
            joysticks = new List<Vector2>();
            buttons = new List<bool>();
        }
        public void Update(string data)
        {
            joysticks.Clear();
            buttons.Clear();
            string[] sdata = data.Split(',');

            try
            {
                for (int i = 0; i < sdata.Length; i++)
                {
                    if (bool.TryParse(sdata[i], out bool pressed))
                    {
                        buttons.Add(pressed);
                    }
                    else if (float.TryParse(sdata[i], out float x))
                    {
                        i++;
                        Vector2 joystick = new Vector2(x, float.Parse(sdata[i]));
                        joysticks.Add(joystick);
                    }
                }
            }
            catch
            {
                Debug.LogError("Controller data could not be updated, error parsing controller data");
            }
        }
    }

    public struct EventHolder
    {
        public string eventName;
        public string[] parameters;
        public EventHolder(string name, params string[] parameters)
        {
            eventName = name;
            this.parameters = parameters;
        }
    }
}
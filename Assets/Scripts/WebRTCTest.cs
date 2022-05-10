using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using Quobject.SocketIoClientDotNet.Client;
using Newtonsoft.Json;
using System.Text;
using RyfiGames.Controller;

public class WebRTCTest : MonoBehaviour
{
    public Camera streamCam;
    public TestJoystick testJoystick;

    private RTCPeerConnection pc;
    private MediaStream camStream;

    // Websocket Stuff
    private string serverURL = "https://controller.y33t.net:443";
    private Socket socket;

    private RTCDataChannel dataChannel;

    private bool startCall;

    void Awake()
    {
        WebRTC.Initialize(EncoderType.Software);
    }
    // Start is called before the first frame update
    void Start()
    {
        OpenWebsocket();
        camStream = streamCam.CaptureStream(1280, 720, 1000000);

        // Create local peer
        var config = GetSelectedSdpSemantics();
        pc = new RTCPeerConnection(ref config);
        pc.OnIceCandidate = (e) =>
        {
            if (e != null)
            {
                socket.Emit("rtcHostCandidate", e.Candidate, e.SdpMid, e.SdpMLineIndex);
                print("sent ICE candidate");
            }
        };
        foreach (var track in camStream.GetTracks())
        {
            pc.AddTrack(track);
        }
        dataChannel = pc.CreateDataChannel("test");
        dataChannel.OnOpen = OpenDataChannel;
        StartCoroutine(WebRTC.Update());

    }

    private void OpenDataChannel()
    {
        print("data channel open");
        dataChannel.Send("Never gonna give you up :D");
        dataChannel.OnMessage = ReceiveData;
    }

    private IEnumerator StartCall()
    {
        RTCOfferAnswerOptions opt = default;
        var offer = pc.CreateOffer(ref opt);
        yield return offer;
        if (offer.IsError)
        {
            print(offer.Error);
        }
        var sessionDesc = offer.Desc;
        print(sessionDesc.sdp);
        pc.SetLocalDescription(ref sessionDesc);
        socket.Emit("hostPeerData", sessionDesc.sdp, sessionDesc.type.ToString());
    }

    void OpenWebsocket()
    {
        socket = IO.Socket(serverURL);

        socket.On(Socket.EVENT_ERROR, (err) =>
            {
                Debug.LogWarning("Error: " + err);
            });
        socket.On(Socket.EVENT_CONNECT_ERROR, (err) =>
        {
            Debug.LogWarning("Connection Error: " + err);
        });
        socket.On(Socket.EVENT_CONNECT_TIMEOUT, (err) =>
        {
            Debug.LogWarning("Connection Error: " + err);
        });

        socket.Once(Socket.EVENT_CONNECT, () =>
        {
            startCall = true;
            Debug.Log("Connected");
        });

        socket.On("rtcAnswer", (sdp) =>
        {
            var answerDesc = new RTCSessionDescription();
            answerDesc.sdp = sdp.ToString();
            answerDesc.type = RTCSdpType.Answer;
            pc.SetRemoteDescription(ref answerDesc);
        });

        socket.On("rtcAnswerCandidate", new ListenerImpl((candidate, sdpMid, sdpMLineIndex) =>
        {
            RTCIceCandidateInit t = new RTCIceCandidateInit();
            t.candidate = candidate.ToString();
            t.sdpMid = sdpMid.ToString();
            t.sdpMLineIndex = int.Parse(sdpMLineIndex.ToString());
            RTCIceCandidate rtcCandidate = new RTCIceCandidate(t);
            pc.AddIceCandidate(rtcCandidate);
            print("got ICE candidate");
        }));
    }

    void ReceiveData(byte[] bytes)
    {
        string data = Encoding.Default.GetString(bytes);
        string[] sdata = data.Split(',');
        float x = float.Parse(sdata[0]);
        float y = -float.Parse(sdata[1]);
        testJoystick.SetStick(x, y);
    }

    // Update is called once per frame
    void Update()
    {
        if (startCall)
        {
            startCall = false;
            StartCoroutine(StartCall());
        }
    }

    void OnDestroy()
    {
        WebRTC.Dispose();
        socket.Disconnect();
    }

    private static RTCConfiguration GetSelectedSdpSemantics()
    {
        RTCConfiguration config = default;
        config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun1.l.google.com:19302", "stun:stun2.l.google.com:19302" } } };

        return config;
    }
}
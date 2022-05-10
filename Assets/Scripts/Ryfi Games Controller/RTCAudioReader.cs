using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;

namespace RyfiGames.Controller
{
    public class RTCAudioReader : MonoBehaviour
    {
        public static bool audioReaderActive;
        void Awake()
        {
            if (!gameObject.GetComponent<AudioListener>())
            {
                Debug.LogWarning("RTCAudioReader should be put on a gameobject with an audio listener.");
            }
            else
            {
                audioReaderActive = true;
            }
        }
        private void OnAudioFilterRead(float[] data, int channels)
        {
            Audio.Update(data, data.Length);
        }
    }
}
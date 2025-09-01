using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SteamVoiceReceiver : MonoBehaviour
{
    private byte[] receiveBuffer = new byte[4096];
    private byte[] decompressBuffer = new byte[8192];
    private Queue<float> sampleQueue = new Queue<float>();
    private AudioSource audioSource;

    private const int SampleRate = 48000;
    private const int MaxBufferedSeconds = 2;
    private const int Channel = 0;

    void Start()
    {
        SteamNetworking.AllowP2PPacketRelay(true); // 🔧 NAT destekli iletişim

        audioSource = GetComponent<AudioSource>();
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        audioSource.spatialBlend = currentScene == "Lobby" ? 0f : 1f;
        audioSource.spatialBlend = 1f;
        
        audioSource.loop = true;
        audioSource.volume = 1f;
        audioSource.playOnAwake = true;
        audioSource.Play(); // OnAudioFilterRead için gerekli

        Debug.Log("🎧 VoiceReceiver initialized.");
    }

    void Update()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        audioSource.spatialBlend = currentScene == "Lobby" ? 0f : 1f;

        if (currentScene == "Lobby")
        {
            audioSource.spatialBlend = 1f;
            audioSource.spatialize = true;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 20f;
        }

        while (SteamNetworking.IsP2PPacketAvailable(out uint packetSize, Channel))
        {
            if (packetSize == 0 || packetSize > receiveBuffer.Length)
            {
                Debug.LogWarning($"⚠ Invalid packet size: {packetSize}");
                continue;
            }

            if (!SteamNetworking.ReadP2PPacket(receiveBuffer, packetSize, out uint bytesRead, out CSteamID sender, Channel))
            {
                Debug.LogWarning("❌ Failed to read P2P packet.");
                continue;
            }

            Debug.Log($"📥 Received voice packet from {sender}, {bytesRead} bytes");

            var result = SteamUser.DecompressVoice(
                receiveBuffer,
                bytesRead,
                decompressBuffer,
                (uint)decompressBuffer.Length,
                out uint pcmBytes,
                SampleRate
            );

            if (result == EVoiceResult.k_EVoiceResultOK && pcmBytes > 0)
            {
                Debug.Log($"🔊 Decompressed voice: {pcmBytes} bytes ({pcmBytes / 2} samples)");

                int samples = (int)(pcmBytes / 2);
                for (int i = 0; i < samples; i++)
                {
                    short sample = BitConverter.ToInt16(decompressBuffer, i * 2);
                    float normalized = sample / 32768f;
                    sampleQueue.Enqueue(normalized);
                }

                // Max buffer kontrolü
                int maxSamples = SampleRate * MaxBufferedSeconds;
                while (sampleQueue.Count > maxSamples)
                {
                    sampleQueue.Dequeue();
                }
            }
            else
            {
                Debug.LogWarning($"⚠ Decompress failed or empty: {result}, pcmBytes={pcmBytes}");
            }
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i += channels)
        {
            float sample = sampleQueue.Count > 0 ? sampleQueue.Dequeue() : 0f;

            for (int c = 0; c < channels; c++)
            {
                data[i + c] = sample;
            }
        }
    }

    void OnDestroy()
    {
        Debug.Log("🎧 VoiceReceiver destroyed.");
    }
}

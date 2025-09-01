using Steamworks;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class SteamVoiceSender : MonoBehaviour
{
    public bool EnableLoopbackTest = false;

    private byte[] voiceBuffer = new byte[4096];
    private byte[] decompressBuffer = new byte[8192];
    private Queue<float> loopbackBuffer = new Queue<float>();

    private float sendInterval = 0.02f; // 20ms
    private bool lastSentSilentFrame = false;

    private AudioSource loopbackAudio;
    private const int SampleRate = 48000;

    private const int MaxLoopbackBufferSize = SampleRate * 2; // 2 saniyelik buffer sınırı

    void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("⚠ SteamManager not initialized on Start.");
            return;
        }

        SteamUser.StartVoiceRecording();
        Debug.Log("🎙 Voice recording started.");

        if (EnableLoopbackTest)
        {
            loopbackAudio = GetComponent<AudioSource>();
            loopbackAudio.clip = AudioClip.Create("LoopbackClip", SampleRate, 1, SampleRate, true);
            loopbackAudio.loop = true;
            loopbackAudio.spatialBlend = 0;
            loopbackAudio.volume = 1f;
            loopbackAudio.Play();
            Debug.Log("🔁 Loopback test enabled.");
        }

        StartCoroutine(FixedIntervalCapture());
    }

    IEnumerator FixedIntervalCapture()
    {
        var wait = new WaitForSeconds(sendInterval);
        while (true)
        {
            CaptureAndSendVoice();
            yield return wait;
        }
    }

    void CaptureAndSendVoice()
    {
        if (!SteamManager.Initialized)
            return;

        var available = SteamUser.GetAvailableVoice(out _);
        if (available != EVoiceResult.k_EVoiceResultOK)
            return;

        var result = SteamUser.GetVoice(true, voiceBuffer, (uint)voiceBuffer.Length, out uint written);
        if (result != EVoiceResult.k_EVoiceResultOK)
            return;

        if (written > 0)
        {
            SendVoiceToLobbyMembers(voiceBuffer, written);
            lastSentSilentFrame = false;

            if (EnableLoopbackTest)
            {
                var decompressResult = SteamUser.DecompressVoice(
                    voiceBuffer,
                    written,
                    decompressBuffer,
                    (uint)decompressBuffer.Length,
                    out uint pcmBytes,
                    SampleRate
                );

                if (decompressResult == EVoiceResult.k_EVoiceResultOK && pcmBytes > 0)
                {
                    int sampleCount = (int)(pcmBytes / 2);
                    for (int i = 0; i < sampleCount; i++)
                    {
                        short sample = System.BitConverter.ToInt16(decompressBuffer, i * 2);
                        float floatSample = Mathf.Clamp(sample / 32768f, -1f, 1f);
                        loopbackBuffer.Enqueue(floatSample);
                    }

                    // Çok büyük buffer, ses gecikmesi yapabilir → temizle
                    if (loopbackBuffer.Count > MaxLoopbackBufferSize)
                    {
                        Debug.LogWarning("⚠ Loopback buffer overflow, resetting to prevent echo/glitch.");
                        loopbackBuffer.Clear(); // Drift veya eko oluşmadan sıfırla
                    }
                }
            }
        }
        else if (!lastSentSilentFrame)
        {
            byte[] silentFrame = new byte[] { 0 };
            SendVoiceToLobbyMembers(silentFrame, 1);
            lastSentSilentFrame = true;
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!EnableLoopbackTest)
            return;

        for (int i = 0; i < data.Length; i += channels)
        {
            float sample = loopbackBuffer.Count > 0 ? loopbackBuffer.Dequeue() : 0f;

            for (int c = 0; c < channels; c++)
                data[i + c] = sample;
        }

        // Eğer yeterli veri yoksa sessizlik ekle → underrun önleme
        while (loopbackBuffer.Count < data.Length)
            loopbackBuffer.Enqueue(0f);
    }

    void SendVoiceToLobbyMembers(byte[] data, uint size)
    {
        if (SteamLobby.Instance == null || SteamLobby.Instance.CurrentLobbyID == 0)
        {
            Debug.LogWarning("❗ Cannot send voice: Lobby info not available.");
            return;
        }

        CSteamID myId = SteamUser.GetSteamID();
        CSteamID lobbyId = new CSteamID(SteamLobby.Instance.CurrentLobbyID);
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);

        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i);
            if (memberId != myId)
            {
                bool success = SteamNetworking.SendP2PPacket(
                    memberId,
                    data,
                    size,
                    EP2PSend.k_EP2PSendUnreliableNoDelay
                );

                if (!success)
                {
                    Debug.LogWarning($"❌ Failed to send voice to: {memberId}");
                }
            }
        }
    }

    void OnDestroy()
    {
        if (SteamManager.Initialized)
        {
            SteamUser.StopVoiceRecording();
            Debug.Log("🛑 Voice recording stopped.");
        }
    }
}

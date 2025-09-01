using UnityEngine;
using Steamworks;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class MicLoopbackTest : MonoBehaviour
{
    private byte[] _compressed = new byte[4096];
    private byte[] _decompressed = new byte[8192];
    private Queue<float> _pcmBuffer = new Queue<float>();

    private AudioSource _audioSource;
    private int _sampleRate = 48000;

    void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steamworks not initialized.");
            return;
        }

        _audioSource = GetComponent<AudioSource>();
        _audioSource.spatialBlend = 0f;
        _audioSource.loop = true;
        _audioSource.volume = 1f;
        _audioSource.Play();

        Debug.Log("Voice recording started.");
        SteamUser.StartVoiceRecording();
    }

    void Update()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("Steam not initialized during Update.");
            return;
        }

        uint available;
        var result = SteamUser.GetAvailableVoice(out available);


        if (result == EVoiceResult.k_EVoiceResultOK && available > 0)
        {
            uint bytesWritten;
            bool voiceResult = SteamUser.GetVoice(true, _compressed, (uint)_compressed.Length, out bytesWritten) == EVoiceResult.k_EVoiceResultOK;

            Debug.Log($"SteamUser.GetVoice result: {voiceResult}, bytesWritten: {bytesWritten}");

            if (voiceResult && bytesWritten > 0)
            {
                uint pcmBytes;
                var decompressResult = SteamUser.DecompressVoice(
                    _compressed,
                    bytesWritten,
                    _decompressed,
                    (uint)_decompressed.Length,
                    out pcmBytes,
                    (uint)_sampleRate
                );

                Debug.Log($"DecompressVoice result: {decompressResult}, pcmBytes: {pcmBytes}");

                if (decompressResult == EVoiceResult.k_EVoiceResultOK && pcmBytes > 0)
                {
                    int samples = (int)(pcmBytes / 2);
                    Debug.Log($"Decoded PCM samples: {samples}");

                    for (int i = 0; i < samples; i++)
                    {
                        short sample = BitConverter.ToInt16(_decompressed, i * 2);
                        float normalized = sample / 32768f;
                        _pcmBuffer.Enqueue(normalized);
                    }

                    Debug.Log($"PCM buffer count: {_pcmBuffer.Count}");

                    // buffer overflow koruması
                    while (_pcmBuffer.Count > _sampleRate * 2)
                    {
                        _pcmBuffer.Dequeue();
                    }
                }
                else
                {
                    Debug.LogWarning("Decompression failed or no PCM data.");
                }
            }
            else
            {
                Debug.LogWarning("SteamUser.GetVoice failed or no bytes written.");
            }
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i += channels)
        {
            float sample = _pcmBuffer.Count > 0 ? _pcmBuffer.Dequeue() : 0f;
            for (int c = 0; c < channels; c++)
            {
                data[i + c] = sample;
            }
        }
    }

    void OnDestroy()
    {
        if (SteamManager.Initialized)
        {
            SteamUser.StopVoiceRecording();
            Debug.Log("Voice recording stopped.");
        }
    }
}

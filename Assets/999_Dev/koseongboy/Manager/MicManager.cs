using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MicManager : MonoBehaviour
{
    private AudioSource audioSource;
    [SerializeField]
    private bool isMicOn = false;
    float[] data = new float[256];
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(isMicOn)
        {
            audioSource.GetOutputData(data, 0);
            double db = GetDecibel();
            if(db > -20f)
            {
                Debug.Log("음성 감지됨. 데시벨: " + (int)db + "dB");
            }
            
        }

    }

    public void StartMicListen()
    {
        audioSource = GetComponent<AudioSource>();
        if(Microphone.devices.Length > 0)
        {
            string mic = Microphone.devices[0];
            Debug.Log("사용중인 마이크: " + mic);
            audioSource.clip = Microphone.Start(mic, true, 10, 44100); // 마이크, 10초짜리 루프녹음, 샘플레이트 44100Hz
            audioSource.loop = true;
            while(Microphone.GetPosition(mic) <= 0) {} // 마이크 대기
            audioSource.Play();
            isMicOn = true;
        }
        else
        {
            Debug.LogError("마이크 장치를 찾을 수 없습니다.");
        }
    }
    
    public void StopMicListen()
    {
        isMicOn = false;
        audioSource.Stop();
        audioSource.clip = null;

        Microphone.End(null);
        if(!audioSource.isPlaying)
        {
            Debug.Log("녹음 종료");
        } else
        {
            Debug.LogError("녹음이 종료되지 않았습니다.");
        }
    }

    double GetDecibel()
    {
        double sum = 0f;
        foreach(var sample in data)
        {
            sum += sample * sample;
        }
        double rms = Math.Sqrt(sum / data.Length);
        double decibel = 20*Math.Log10(rms / 0.1f);

        return decibel;
    }
}

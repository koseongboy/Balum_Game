using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class EtriPronunciationAPI : MonoBehaviour
{
    [Header("ETRI 발급 API 키")]
    public string accessKey = "fe65fc0e-d7c7-44cf-9349-9ff5347ff2fa"; 

    // 1. JSON 변환을 위해 ETRI API 규격에 맞는 데이터 클래스 준비
    [Serializable]
    public class PronunciationRequest
    {
        public ArgumentData argument;
    }

    [Serializable]
    public class ArgumentData
    {
        public string language_code;
        public string script; // 발음 평가를 받을 대본
        public string audio;  // 음성 파일 (Base64 인코딩)
    }

    void Start()
    {
        // 테스트용 가짜 데이터: 실제로는 유니티 마이크로 녹음된 WAV 파일의 byte[] 데이터가 들어가야 해!
        byte[] dummyAudioData = new byte[] { 0x00, 0x01, 0x02 }; 
        string targetScript = "안녕하세요"; // 읽을 문장

        StartCoroutine(SendPronunciationData(dummyAudioData, targetScript));
    }

    IEnumerator SendPronunciationData(byte[] audioBytes, string scriptText)
    {
        // 요청 보낼 ETRI 한국어 발음평가 API 주소
        string url = "http://epretx.etri.re.kr:8000/api/WiseASR_PronunciationKor";

        // 2. C# 객체를 생성하고 JsonUtility를 사용해 JSON 문자열로 변환
        PronunciationRequest requestData = new PronunciationRequest
        {
            argument = new ArgumentData
            {
                language_code = "korean", 
                script = scriptText,
                audio = Convert.ToBase64String(audioBytes) // 핵심! 오디오 데이터를 Base64 포맷으로 인코딩해야 해
            }
        };

        string jsonBody = JsonUtility.ToJson(requestData);

        // 3. UnityWebRequest 세팅 (JSON 원본을 POST로 보낼 때의 정석 방법)
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            // JSON 문자열을 byte 배열로 변환해서 업로드 핸들러에 장착해줘
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // 4. 헤더 세팅 (ETRI API 필수 요구 사항)
            request.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");
            request.SetRequestHeader("Authorization", accessKey);

            Debug.Log("서버에 음성 데이터 전송 중...");

            // 서버 응답 대기 (비동기)
            yield return request.SendWebRequest();

            Debug.Log("통신 대기 끝! 에러 체크 시작합니다.");

            // 5. 통신 결과 확인
            if (request.result == UnityWebRequest.Result.ConnectionError || 
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"통신 에러: {request.error}");
                Debug.LogError($"서버 상세 에러: {request.downloadHandler.text}");
            }
            else
            {
                Debug.Log("✅ 통신 성공!");
                Debug.Log($"서버 응답 JSON: {request.downloadHandler.text}");
                
                // 여기서 서버 응답(JSON)을 다시 파싱해서 점수를 빼오면 돼!
            }
        }
    }
}
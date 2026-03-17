using System;
using System.Text;
using System.Collections;
using System.IO;
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
        
    }

    public void StartConnection()
    {
        // 테스트용 가짜 데이터: 실제로는 유니티 마이크로 녹음된 WAV 파일의 byte[] 데이터가 들어가야 해!
        string base64AudioData = EncodeWavToBase64("./records/Test");
        string targetScript = "안녕하세요"; // 읽을 문장
        if(base64AudioData != null)
        {
            StartCoroutine(SendPronunciationData(base64AudioData, targetScript));
        } else
        {
            Debug.LogError("음성 데이터가 없습니다.");
        }


    }

    IEnumerator SendPronunciationData(string base64AudioData, string scriptText)
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
                audio = base64AudioData
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

    private string EncodeWavToBase64(string filename)
    {
        if (!filename.ToLower().EndsWith(".wav")) {
			filename += ".wav";
		}

		var filePath = Path.Combine(Application.persistentDataPath, filename);
        // 1. 해당 경로에 파일이 진짜 있는지 확인
        if (!File.Exists(filePath))
        {
            Debug.LogError($"파일을 찾을 수 없습니다! 경로를 확인해 주세요: {filePath}");
            return null;
        }

        try
        {
            // 2. WAV 파일을 통째로 읽어서 byte[] 배열로 가져오기
            byte[] audioBytes = File.ReadAllBytes(filePath);

            // 3. byte[] 배열을 Base64 형태의 긴 문자열로 변환 (이게 핵심!)
            string base64String = Convert.ToBase64String(audioBytes);
            
            Debug.Log("✅ WAV 파일 Base64 인코딩 성공!");
            return base64String;
        }
        catch (Exception e)
        {
            Debug.LogError($"파일을 읽고 변환하는 중 에러가 발생했습니다: {e.Message}");
            return null;
        }
    }

}
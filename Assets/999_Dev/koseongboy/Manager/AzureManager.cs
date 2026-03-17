using System.Threading.Tasks;
using UnityEngine;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;

public class AzureManager : MonoBehaviour
{
    // JSON 구조와 똑같은 클래스를 하나 만들어줘
    [System.Serializable]
    private class SecretData
    {
        public string azureApiKey;
        public string azureRegion;
    }
    public string referenceText = "안녕하세요 반갑습니다"; 

    private string subscriptionKey;
    private string region;

    void Start()
    {
        LoadSecrets();
    }

    private void LoadSecrets()
    {
        // 🚨 수정됨: "secrets" -> "secret" 으로 변경! (확장자는 쓰면 안 됨)
        TextAsset secretFile = Resources.Load<TextAsset>("secrets");

        if (secretFile != null)
        {
            SecretData secrets = JsonUtility.FromJson<SecretData>(secretFile.text);
            subscriptionKey = secrets.azureApiKey;
            region = secrets.azureRegion;
            
            Debug.Log($"✅ API 키 로드 성공! 길이: {subscriptionKey?.Length}");
        }
        else
        {
            Debug.LogError("🚨 Resources 폴더에서 'secret' (또는 secret.json) 파일을 찾을 수 없습니다!");
        }
    }
    
    // ... (나머지 발음 평가 코드는 동일하게 사용) ...
    
    // 예시: UI 버튼 클릭 시 이 함수를 실행하도록 연결하면 돼!
    public async void StartPronunciationTest()
    {
        // 유저가 읽어야 할 문장 (대본)
        

        Debug.Log("🎤 마이크 셋업 중...");

        // 1. 기본 Speech 설정 (한국어 인식 세팅)
        var speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
        speechConfig.SpeechRecognitionLanguage = "ko-KR"; 

        // 2. 발음 평가 전용 설정
        var pronunciationConfig = new PronunciationAssessmentConfig(
            referenceText,
            GradingSystem.HundredMark, // 100점 만점 기준
            Granularity.Phoneme,       // '음소' 단위까지 아주 잘게 쪼개서 평가
            enableMiscue: true         // 대본에 없는 단어를 말하거나 빼먹은 것도 체크 (MisCue)
        );
        
        // (선택) 억양, 말하기 리듬까지 평가하고 싶다면 켬!
        pronunciationConfig.EnableProsodyAssessment(); 

        // 3. 기기의 기본 마이크에서 오디오를 가져오도록 설정
        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        
        // 4. 음성 인식기(Recognizer) 생성 후, 발음 평가 규칙 덮어씌우기
        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);
        pronunciationConfig.ApplyTo(recognizer);


        Debug.Log("🗣️ 마이크가 켜졌습니다. 지금 문장을 읽어주세요!");

        // 5. 마이크로 음성 듣기 시작! (말을 멈추면 자동으로 인식을 종료하고 결과를 가져옴)
        var speechRecognitionResult = await recognizer.RecognizeOnceAsync();

        // 6. 결과 확인
        if (speechRecognitionResult.Reason == ResultReason.RecognizedSpeech)
        {
            // 인식된 음성 결과에서 '발음 평가 점수'만 쏙 뽑아오기
            var pronResult = PronunciationAssessmentResult.FromResult(speechRecognitionResult);

            Debug.Log($"✅ 유저가 실제 말한 문장: {speechRecognitionResult.Text}");
            Debug.Log($"🏆 전체 발음 점수 (Accuracy): {pronResult.AccuracyScore}");
            Debug.Log($"🌊 유창성 (Fluency): {pronResult.FluencyScore}");
            Debug.Log($"🎯 단어 빼먹지 않았는지 (Completeness): {pronResult.CompletenessScore}");
            Debug.Log($"🎵 억양 및 리듬 (Prosody): {pronResult.ProsodyScore}");
        }
        else if (speechRecognitionResult.Reason == ResultReason.Canceled)
        {
            var cancellation = CancellationDetails.FromResult(speechRecognitionResult);
            Debug.LogError($"에러 발생: {cancellation.ErrorDetails}");
        }
    }
}
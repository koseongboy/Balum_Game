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
    public PronunciationVisualizer visualizer;

    void Start()
    {
        LoadSecrets();
    }

    private void LoadSecrets()
    {
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
        // pronunciationConfig.EnableProsodyAssessment(); 

        // 3. 기기의 기본 마이크에서 오디오를 가져오도록 설정
        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        
        // 4. 음성 인식기(Recognizer) 생성 후, 발음 평가 규칙 덮어씌우기
        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);
        pronunciationConfig.ApplyTo(recognizer);

        // 1. 유저가 말을 시작했을 때 (옵션)
        recognizer.SpeechStartDetected += (s, e) =>
        {
            Debug.Log("🎙️ [인식 중] 유저가 말을 시작했습니다...");
        };

        // 2. 유저가 말을 끝마쳐서 마이크 녹음이 끝났을 때 (핵심!)
        recognizer.SpeechEndDetected += (s, e) =>
        {
            // 이 시점이 바로 녹음이 끝나고, 서버의 점수 응답을 기다리기 시작하는 시점이에요!
            Debug.Log("🛑 [녹음 완료] 오디오 입력이 끝났습니다! 서버에 평가를 요청하고 기다립니다...");
        };

        recognizer.Canceled += (s, e) =>
        {
            Debug.LogError($"🛑 통신 강제 취소됨! [이유: {e.Reason}] 상세 에러: {e.ErrorDetails}");
        };


        Debug.Log("🗣️ 마이크가 켜졌습니다. 지금 문장을 읽어주세요!");

        // 5. 마이크로 음성 듣기 시작! (말을 멈추면 자동으로 인식을 종료하고 결과를 가져옴)
        var speechRecognitionResult = await recognizer.RecognizeOnceAsync();

        // 6. 결과 확인
        if (speechRecognitionResult.Reason == ResultReason.RecognizedSpeech)
        {
            string rawJson = speechRecognitionResult.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
            Debug.Log($"📦 서버 원본 JSON 데이터:\n{rawJson}");
            // 인식된 음성 결과에서 '발음 평가 점수'만 쏙 뽑아오기
            var pronResult = PronunciationAssessmentResult.FromResult(speechRecognitionResult);

            Debug.Log($"✅ 유저가 실제 말한 문장: {speechRecognitionResult.Text}");
            Debug.Log($"🏆 전체 발음 점수 (Accuracy): {pronResult.AccuracyScore}");
            Debug.Log($"🌊 유창성 (Fluency): {pronResult.FluencyScore}");
            Debug.Log($"🎯 단어 빼먹지 않았는지 (Completeness): {pronResult.CompletenessScore}");
            // Debug.Log($"🎵 억양 및 리듬 (Prosody): {pronResult.ProsodyScore}");
            Debug.Log("🔍 --- 단어 및 음소(Phoneme) 단위 상세 분석 ---");

                // 문장을 단어 단위로 쪼개서 반복
                foreach (var word in pronResult.Words)
                {
                    // 단어별 점수와 상태(잘못 읽었는지, 빼먹었는지 등) 출력
                    Debug.Log($"📍 단어: [{word.Word}] | 정확도: {word.AccuracyScore}점 | 상태: {word.ErrorType}");

                    // 해당 단어를 다시 음소(Phoneme) 단위로 쪼개서 반복
                    foreach (var phoneme in word.Phonemes)
                    {
                        // 음소별 점수 출력
                        Debug.Log($"   -> 음소: '{phoneme.Phoneme}' | 정확도: {phoneme.AccuracyScore}점");
                    }
                }
                
                Debug.Log("-------------------------------------------");
                if (speechRecognitionResult.Reason == ResultReason.RecognizedSpeech)
                {
                
                    // 시각화 스크립트 실행!
                    if (visualizer != null)
                    {
                        visualizer.VisualizeResults(pronResult);
                    }
                }
        }
        else if (speechRecognitionResult.Reason == ResultReason.Canceled)
        {
            var cancellation = CancellationDetails.FromResult(speechRecognitionResult);
            Debug.LogError($"에러 발생: {cancellation.ErrorDetails}");
        }
    }
}
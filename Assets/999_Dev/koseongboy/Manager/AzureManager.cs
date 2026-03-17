using System.Threading.Tasks;
using UnityEngine;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;

public class AzureManager : MonoBehaviour
{
    [Header("Azure API 설정")]
    public string subscriptionKey = "7JgRPCb950YrIhN1Sr2acPCgDDRk40uJgA0WIxrDfI0wubYKEK2VJQQJ99CCACNns7RXJ3w3AAAYACOGKkub";
    public string region = "koreacentral"; // 여기에 복사한 지역을 넣어주세요 (예: koreacentral)
    
    // 예시: UI 버튼 클릭 시 이 함수를 실행하도록 연결하면 돼!
    public async void StartPronunciationTest()
    {
        // 유저가 읽어야 할 문장 (대본)
        string referenceText = "안녕하세요 반갑습니다"; 

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
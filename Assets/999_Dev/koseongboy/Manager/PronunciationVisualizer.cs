using System.Collections.Generic;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PronunciationVisualizer : MonoBehaviour
{
    [Header("UI 연결")]
    public Transform sentenceContainer;         // 어절들이 나열될 부모 (Horizontal Layout Group)
    public GameObject wordGroupPrefab;          // '단어 텍스트 + 게이지 부모'가 묶인 프리팹
    public GameObject phonemeSegmentPrefab;     // 게이지 바 조각 프리팹 (Image)

    [Header("점수별 색상 설정")]
    public Color colorGood = Color.green;       // 80점 이상
    public Color colorWarn = Color.yellow;      // 60~79점
    public Color colorBad = Color.red;          // 60점 미만

    public void VisualizeResults(PronunciationAssessmentResult azureResult)
    {
        // 1. 기존에 생성된 UI 찌꺼기 싹 지우기 초기화
        foreach (Transform child in sentenceContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. 서버에서 분류해 준 '단어(어절)' 단위로 반복
        foreach (var wordData in azureResult.Words)
        {
            // 어절 컨테이너(글자+게이지칸) 생성
            GameObject wordObj = Instantiate(wordGroupPrefab, sentenceContainer);
            
            // 어절 텍스트 세팅 (예: "안녕하세요")
            TextMeshProUGUI wordText = wordObj.GetComponentInChildren<TextMeshProUGUI>();
            if (wordText != null) wordText.text = wordData.Word;

            // 해당 어절 안에 있는 게이지 부모 컨테이너 찾기
            Transform gaugeContainer = wordObj.transform.Find("GaugeContainer");
            
            if (gaugeContainer != null)
            {
                // 3. 해당 어절에 속한 '음소' 데이터 개수만큼 게이지 바 조각 생성
                foreach (var phoneme in wordData.Phonemes)
                {
                    GameObject segment = Instantiate(phonemeSegmentPrefab, gaugeContainer);
                    Image img = segment.GetComponent<Image>();

                    // 점수에 따라 게이지 조각 색상 칠하기
                    float score = (float)phoneme.AccuracyScore;
                    if (score >= 80) img.color = colorGood;
                    else if (score >= 60) img.color = colorWarn;
                    else img.color = colorBad;
                }
            }
        }
    }
}
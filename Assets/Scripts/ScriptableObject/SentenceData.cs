using UnityEngine;

[CreateAssetMenu(fileName = "New SentenceData", menuName = "GameData/SentenceData")]
public class SentenceData : ScriptableObject, ICSVData
{
    public int Id;
    public string Theme;
    public int Difficulty;
    public string Sentence;
   public int GetId()
   {
       return Id;
   }
}

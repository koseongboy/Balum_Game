using UnityEngine;

[CreateAssetMenu(fileName = "New csv_Example", menuName = "GameData/csv_Example")]
public class csv_Example : ScriptableObject, ICSVData
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

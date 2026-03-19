using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public class CSVLoader : MonoBehaviour
{
#region Fields & Properties
        private static string csvFolderPath = "Assets/CSV/"; // csv 파일들
        private static string soScriptPath = "Assets/Scripts/ScriptableObject/"; // object 클래스들 경로
        private static string soAssetPath = "Assets/Resources/ScriptableObject/"; // object 객체들 경로
        
        #endregion

        #region Singleton & initialization
        public static CSVLoader Instance { get; private set; }
        public CSVLoader()
        {
            if (Instance != null)
            {
                return;
            }
            Instance = this;
        }
        #endregion

        #region getters & setters

        #endregion

        #region public methods

        [MenuItem("Tools/Generate ScriptableObject from CSV")]
        
        public static void GenerateScriptableObjects() {
            if (!Directory.Exists(csvFolderPath)) {
                Debug.LogError("[CSVManger] CSV 폴더를 찾을 수 없습니다. : " + csvFolderPath);
                return;
            }
        
            string[] csvFilePaths = Directory.GetFiles(csvFolderPath, "*.tsv");
            foreach ( string filePath in csvFilePaths )
            {
                // CSV 파일 이름(확장자 제외)을 가져옵니다. 이 이름을 바탕으로 클래스 및 에셋을 생성합니다.
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                // Debug.Log("[CSVLoader] CSV FileName : "+fileName);

                // 1. CSV 파일의 헤더와 첫 번째 데이터 행을 읽습니다.
                string[] lines = File.ReadAllLines(filePath);
                if (lines.Length <= 1) continue; // 헤더만 있거나, 비어있으면 continue

                string[] headers = lines[0].Split('\t').Select(s => s.Trim()).ToArray();
                string[] firstDataRow = lines[1].Split('\t').Select(s => s.Trim()).ToArray();

                // 2. CSV 파일로부터 스크립터블 오브젝트 클래스를 생성
                GenerateScriptableObjectClass(fileName, headers, firstDataRow);

                // 3. Unity 에디터에 컴파일 요청
                AssetDatabase.Refresh();

                // 4. 해당 클래스의 인스턴스를 생성하고 데이터 채우기
                CreateOrUpdateSOAssets(fileName, headers, lines);
            }

            // 모든 작업이 끝난 후, 변경된 에셋을 저장합니다.
            AssetDatabase.SaveAssets();
            Debug.Log("[CSVLoader] CSV 파일 자동 변환이 완료되었습니다.");
        }
        #endregion

        #region private methods
        // 새로운 스크립터블 오브젝트 클래스 생성
        private static void GenerateScriptableObjectClass(string soClassName, string[] headers, string[] firstDataRow)
        {
            string classFilePath = $"{soScriptPath}{soClassName}.cs";
        
            // 클래스 폴더가 없으면, 새로 생성
            if (!Directory.Exists(soScriptPath))    Directory.CreateDirectory(soScriptPath);
            
            // 이미 클래스 파일이 존재하면, 경고를 띄우고 생성 과정을 건너뜀
            if (!File.Exists(classFilePath)) {
                Debug.Log($"새로운 클래스 {soClassName}를 생성합니다.");            
            }
            else {
                return;
            }
            
            // 데이터 타입 추론
            string[] dataTypes = InferDataTypes(firstDataRow);

            // StringBuilder로 클래스 파일의 내용 구성
            StringBuilder classContent = new StringBuilder();
            classContent.AppendLine("using UnityEngine;");
            classContent.AppendLine();
            classContent.AppendLine($"[CreateAssetMenu(fileName = \"New {soClassName}\", menuName = \"GameData/{soClassName}\")]");
            classContent.AppendLine($"public class {soClassName} : ScriptableObject, ICSVData");
            classContent.AppendLine("{");

            for (int i = 0; i < headers.Length; i++)
            {
                classContent.AppendLine($"    public {dataTypes[i]} {headers[i]};");
            }
            
            classContent.AppendLine("   public int GetId()");
            classContent.AppendLine("   {");
            classContent.AppendLine("       return Id;");
            classContent.AppendLine("   }");
            
            classContent.AppendLine("}");

            // 파일에 클래스 내용을 작성
            File.WriteAllText(classFilePath, classContent.ToString());
            Debug.Log($"[CVSManager]새로운 C# 스크립트가 생성되었습니다: {classFilePath}");
        }
        
        // 각 컬럼의 데이터 타입 추론
        private static string[] InferDataTypes(string[] firstDataRow)
        {
            string[] dataTypes = new string[firstDataRow.Length];
            for (int i = 0; i < firstDataRow.Length; i++)
            {
                string value = firstDataRow[i];
                if (value.Equals("true", System.StringComparison.OrdinalIgnoreCase) || 
                    value.Equals("false", System.StringComparison.OrdinalIgnoreCase))
                {
                    dataTypes[i] = "bool";
                }
                else if (int.TryParse(value, out _))
                {
                    dataTypes[i] = "int";
                }
                else if (float.TryParse(value, out _))
                {
                    dataTypes[i] = "float";
                }
                else
                {
                    dataTypes[i] = "string";
                }
            }
            return dataTypes;
        }

    
        // 스크립터블 오브젝트 에셋을 생성하거나 업데이트
        private static void CreateOrUpdateSOAssets(string soClassName, string[] headers, string[] lines) {
            // 리플렉션을 사용하여 SO 클래스의 'Type'을 가져옵니다.
            // 이는 새로 생성된 클래스를 런타임에 동적으로 찾기 위함입니다.
            // GetType()을 사용할 때는 네임스페이스를 포함해야 할 수 있습니다. (예: "MyGame.ItemData")
            // C#의 기본 어셈블리(Assembly-CSharp)에서 타입을 찾습니다.
            Type soType = Assembly.Load("Assembly-CSharp").GetType(soClassName);

            if (soType == null)
            {
                // 네임스페이스가 있는 경우를 대비해 다시 찾아봅니다. (예시)
                soType = Assembly.Load("Assembly-CSharp").GetType("Game.Data." + soClassName);
            }
            if (soType == null)
            {
                Debug.LogError($"클래스 {soClassName}을(를) 찾을 수 없습니다. 컴파일 오류를 확인하세요.");
                return;
            }

            // SO 에셋을 저장할 폴더 경로를 설정하고, 폴더가 없으면 생성합니다.
            string assetFolderPath = $"{soAssetPath}{soClassName}/";
            if (!Directory.Exists(assetFolderPath))
            {
                Directory.CreateDirectory(assetFolderPath);
                AssetDatabase.Refresh(); // 새로 만든 폴더를 에디터에 알려줍니다.
            }

            // CSV의 모든 데이터 행을 순회하며 에셋을 처리합니다. (첫 번째 행은 헤더이므로 건너뜁니다.)
            for (int i = 1; i < lines.Length; i++)
            {
                // 쉼표로 분리하여 각 데이터 값을 가져옵니다.
                string[] values = lines[i].Split('\t').Select(s => s.Trim()).ToArray();

                // 에셋의 이름을 두 번째 컬럼의 값으로 설정합니다.
                string assetName = values[1];
                string assetPath = $"{assetFolderPath}{assetName}.asset";

                // 해당 경로에 이미 SO 에셋이 있는지 확인합니다.
                ScriptableObject soInstance = AssetDatabase.LoadAssetAtPath(assetPath, soType) as ScriptableObject;

                if (soInstance == null)
                {
                    // 존재하지 않으면, 새로운 SO 인스턴스를 생성하고 에셋으로 저장합니다.
                    soInstance = ScriptableObject.CreateInstance(soType) as ScriptableObject;
                    AssetDatabase.CreateAsset(soInstance, assetPath);
                }

                // 리플렉션을 사용하여 CSV 데이터 값을 SO의 필드에 할당합니다.
                for (int j = 0; j < headers.Length; j++)
                {
                    // 헤더(컬럼 이름)를 통해 해당 필드를 찾습니다.
                    FieldInfo field = soType.GetField(headers[j]);

                    if (field == null) continue;
                    
                    try
                    {
                        string csvValue = values[j];

                        if (field.FieldType == typeof(List<string>))
                        {
                            // 세미콜론(;)을 기준으로 분리하고, 각 항목의 앞뒤 공백을 제거하여 리스트로 만듦
                            List<string> listValues = csvValue
                                .Split(';')
                                .Select(s => s.Trim())
                                // 빈 문자열이 생기는 경우를 제거 (예: "item1;;item3")
                                .Where(s => !string.IsNullOrEmpty(s))
                                .ToList(); 
                            field.SetValue(soInstance, listValues);
                        }
                        // 2. List<string>이 아니라면, 기존의 단일 값 변환 로직을 사용
                        else
                        {
                            if (string.IsNullOrEmpty(csvValue)) // 비어있는 컬럼
                            {
                                continue;
                            }
                            // CSV 값을 필드의 실제 타입에 맞게 변환하여 할당합니다.
                            object convertedValue = Convert.ChangeType(csvValue, field.FieldType);
                            field.SetValue(soInstance, convertedValue);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"데이터 변환 오류 발생! CSV 값: '{values[j]}', 대상 필드: '{field.Name}', 오류: {e.Message}");
                    }
                }

                // 스크립트에서 변경된 내용을 유니티가 인식하도록 알려줍니다. (매우 중요!)
                EditorUtility.SetDirty(soInstance);
            }
        }
        #endregion

        #region Unity event methods
        #endregion
}

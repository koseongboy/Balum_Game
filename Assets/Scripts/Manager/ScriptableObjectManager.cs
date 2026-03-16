using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Manager
{
    public class ScriptableObjectManager
    {
        #region Fields & Properties
        private readonly string rootPath = "ScriptableObject";
        private Dictionary<string, Dictionary<int, ICSVData>> allDatas;
        #endregion

        #region Singleton & initialization
    
        private static ScriptableObjectManager instance;
        public static ScriptableObjectManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ScriptableObjectManager();
                }
                return instance;
            }
        }

        private ScriptableObjectManager()
        {
            allDatas = new Dictionary<string, Dictionary<int, ICSVData>>();
            LoadData();
        }
        #endregion
    
        #region getters & setters
        public Dictionary<int, ICSVData> GetAllData<T>()
        {
            return allDatas[typeof(T).Name];
        }

        /// <summary>
        /// <param name="id">int입니다! string이 아니어요.</param>
        /// <typeparam name="T">꼬리에 _를 꼭 붙여줘야합니다!!</typeparam>
        /// <returns>id값이 잘못될 경우, null을 return합니다.</returns>
        /// </summary>
        public T GetData<T>(int id)
        {
            Type t = typeof(T);
            if (allDatas[t.Name].TryGetValue(id, out var value))
            {
                return (T)value;
            }
            throw new Exception($"No Data with id : {id}, in "+t.Name);
        }
        #endregion

        #region public methods
        #endregion

        #region private methods
    
        private void LoadData()
        {
            allDatas.Clear();
            List<Type> dataTypesToLoad = Assembly.GetAssembly(typeof(ICSVData))
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ICSVData)) || typeof(ICSVData).IsAssignableFrom(t))
                .Where(t => !t.IsAbstract)
                .Where(t => !t.IsGenericTypeDefinition)
                .ToList();
        
            foreach (Type dataType in dataTypesToLoad)
            {
                string path = rootPath + "/" + dataType.Name;
                ScriptableObject[] objs = Resources.LoadAll<ScriptableObject>(path);
                allDatas.Add(dataType.Name, new Dictionary<int, ICSVData>());

                // 폴더 내 각 SO에 대해
                foreach (var so in objs)
                {
                    // 자동으로 탐색된 타입(dataType)과 로드된 객체의 타입(so.GetType())이 일치하는지 확인
                    if (so.GetType() != dataType) continue;
                    if (so is ICSVData data)
                    {
                        Dictionary<int, ICSVData> currentDB = allDatas[dataType.Name];
                        if (!currentDB.TryAdd(data.GetId(), data))
                        {
                            Debug.LogError($"[Data Error] Type: {dataType.Name}의 ID {data.GetId()}가 중복되었습니다.");
                        }
                    }
                }
            }
        }
        #endregion

    
        #region DEV
        [ContextMenu("PrintAll in Dictionary")]
        private void PrintAll_Dev()
        {
            foreach (KeyValuePair<string, Dictionary<int, ICSVData>> dict in allDatas)
            {
                foreach (var kvp in dict.Value)
                {
                    Debug.Log(kvp.Key+" : "+kvp.Value);
                }
            }
        }

        [ContextMenu("GetterTest_Dev")]
        private void GetterTest_Dev()
        {
        }
        #endregion
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GateManager : MonoBehaviour, ISaveManager
{
    public static GateManager instance;

    [SerializeField] private List<GateData> gateDatabase;

    private void Awake()
    {
        if (instance != null)
            Destroy(instance.gameObject);
        else
            instance = this;
    }

    public void DefaultGates()
    {
        foreach (GateData gate in gateDatabase)
        {
            gate.isLocked = true;
        }
    }

    public void GateUnlocked(GateData _data)
    {
        foreach (GateData gate in gateDatabase)
        {
            if (gate != null)
            {
                if (gate.gateId == _data.gateId)
                    gate.isLocked = _data.isLocked;
            }         
        }
        SaveManager.instance.SaveGame();
    }

    public void LoadData(GameData _data)
    {
        foreach (KeyValuePair<string, bool> pair in _data.gates)
        {
            foreach (GateData gate in gateDatabase)
            {
                if (gate != null)
                {
                    if(gate.gateId == pair.Key)
                        gate.isLocked = pair.Value;
                }
            }
        }
    }

    public void SaveData(ref GameData _data)
    {
        _data.gates.Clear();

        foreach (GateData gate in gateDatabase)
        {
            if (gate != null)
            {
                _data.gates.Add(gate.gateId, gate.isLocked);
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Fill up gate data base")]
    private void FillUpItemDataBase() => gateDatabase = new List<GateData>(GetItemDataBase());

    private List<GateData> GetItemDataBase()
    {
        List<GateData> dataBase = new List<GateData>();
        string[] assetName = AssetDatabase.FindAssets("", new[] { "Assets/Data/Gate" });

        foreach (string SOName in assetName)
        {
            var SOpath = AssetDatabase.GUIDToAssetPath(SOName);
            var itemData = AssetDatabase.LoadAssetAtPath<GateData>(SOpath);
            dataBase.Add(itemData);
        }

        return dataBase;
    }
#endif
}

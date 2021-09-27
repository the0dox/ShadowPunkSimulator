using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem
{
    public static void SavePlayer(CharacterSaveData data)
    {
        CreateSaveFile();
        BinaryFormatter formatter = new BinaryFormatter();
        
        string path = Application.persistentDataPath +"/save_data/characters/" + data.playername + ".data";
        FileStream stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream,data);
        stream.Close();
    }

    public static List<CharacterSaveData> LoadPlayer()
    {
        CreateSaveFile();
        List<CharacterSaveData> output = new List<CharacterSaveData>();
        string path = Application.persistentDataPath + "/save_data/characters";
        BinaryFormatter formatter = new BinaryFormatter();
        foreach(string file in Directory.EnumerateFiles(path))
        {
            FileStream stream = new FileStream(file,FileMode.Open);
            CharacterSaveData data = formatter.Deserialize(stream) as CharacterSaveData;
            stream.Close();
            output.Add(data);
        }
        return output;
    }

    public static void SaveScene(SceneSaveData data)
    {
        CreateSaveFile();
        BinaryFormatter formatter = new BinaryFormatter();
        
        string path = Application.persistentDataPath +"/save_data/scenes/" + data.GetName() + ".data";
        FileStream stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream,data);
        stream.Close();
    }

    public static List<SceneSaveData> LoadScenes()
    {
        CreateSaveFile();
        List<SceneSaveData> output = new List<SceneSaveData>();
        string path = Application.persistentDataPath + "/save_data/scenes";
        BinaryFormatter formatter = new BinaryFormatter();
        foreach(string file in Directory.EnumerateFiles(path))
        {
            FileStream stream = new FileStream(file,FileMode.Open);
            SceneSaveData data = formatter.Deserialize(stream) as SceneSaveData;
            stream.Close();
            output.Add(data);
        }
        return output;
    }

    private static void CreateSaveFile()
    {
        if(!Directory.Exists(Application.persistentDataPath + "/save_data"))
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/save_data");
        }
        if(!Directory.Exists(Application.persistentDataPath + "/save_data/characters"))
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/save_data/characters");
        }
        if(!Directory.Exists(Application.persistentDataPath + "/save_data/scenes"))
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/save_data/scenes");
        }
    }
}

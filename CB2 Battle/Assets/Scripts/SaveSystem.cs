using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Photon.Pun;

public class SaveSystem : MonoBehaviourPunCallbacks
{
    private static List<CharacterSaveData> PlayerFiles = new List<CharacterSaveData>();
    private static List<string> PlayerNames;
    [SerializeField] private PhotonView pv;
    private static PhotonView spv;

    void Start()
    {
        if(pv.IsMine)
        {
            CreateSaveFile();
            string path = Application.persistentDataPath + "/save_data/characters";
            BinaryFormatter formatter = new BinaryFormatter();
            foreach(string file in  Directory.EnumerateFiles(path))
            {
                FileStream stream = new FileStream(file,FileMode.Open);
                CharacterSaveData data = formatter.Deserialize(stream) as CharacterSaveData;
                stream.Close();
                PlayerFiles.Add(data);
            }
            foreach(CharacterSaveData csd in PlayerFiles)
            {
                csd.OnLoad();
            }
            //pv.RPC("RPC_Recieve_Players",RpcTarget.AllBuffered,convertedFiles.ToArray());
        }
    }

    public static void SavePlayer(CharacterSaveData data)
    {
        data.OnSave();
        CreateSaveFile();
        BinaryFormatter formatter = new BinaryFormatter();
        
        string path = Application.persistentDataPath +"/save_data/characters/" + data.playername + ".data";
        FileStream stream = new FileStream(path, FileMode.Create);
        formatter.Serialize(stream,data);
        stream.Close();
    }

    public static List<CharacterSaveData> LoadPlayer()
    {
        
        return PlayerFiles;
    }

    /*
    [PunRPC]
    void RPC_Recieve_Players(string[] players)
    {
        PlayerFiles = new List<string>();
        for(int i = 0; i < players.Length; i++)
        {
            PlayerFiles.Add(players[i]);
        }
    }
    */

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

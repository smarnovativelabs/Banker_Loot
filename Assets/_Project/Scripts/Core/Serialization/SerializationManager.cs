using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;
using UnityEngine;

public static class SerializationManager
{
    public static string projectName = "/ParcelSim";
    public static void Save(object saveData,string _fileName)
    {
        BinaryFormatter formatter = GetBinaryFormatter();
//#if UNITY_ANDROID
//        string path = Application.persistentDataPath + _fileName + ".meta";
//#else
         if (!Directory.Exists(Application.persistentDataPath + projectName))
        {
            Directory.CreateDirectory(Application.persistentDataPath + projectName);
        }
        string path = Application.persistentDataPath + projectName +"/" + _fileName + ".meta";

//#endif
        FileStream file = File.Create(path);
        formatter.Serialize(file, saveData);
        file.Close();
    }
    public static object LoadFile(string _fileName)
    {
//#if UNITY_ANDROID
//        string path = Application.persistentDataPath + _fileName + ".meta";

//#else
         if (!Directory.Exists(Application.persistentDataPath + projectName))
        {
            return null;
        }
        string path = Application.persistentDataPath + projectName + "/" + _fileName + ".meta";

//#endif
        if (!File.Exists(path))
        {
            return null;
        }
        BinaryFormatter formatter = GetBinaryFormatter();
        FileStream file = File.Open(path, FileMode.Open);
        try
        {
            object loadFile = formatter.Deserialize(file);
            file.Close();
            return loadFile;
        }
        catch
        {
            Debug.LogError("Failed to load Data");
            file.Close();
            return null;
        }
    }

    public static object Load(string _fileName)
    {
#if UNITY_ANDROID
        string path = Application.persistentDataPath + _fileName + ".meta";
#else
        if (!Directory.Exists(Application.persistentDataPath + projectName))
        {
            return null;
        }
        string path = Application.persistentDataPath + projectName + "/" + _fileName + ".meta";
#endif
        if (!File.Exists(path))
        {
            return null;
        }
        BinaryFormatter formatter = GetBinaryFormatter();
        FileStream file = File.Open(path, FileMode.Open);
        try
        {
            object loadFile = formatter.Deserialize(file);
            file.Close();
            return loadFile;
        }
        catch
        {
            Debug.LogError("Failed to load Data");
            file.Close();
            return null;
        }
    }

    public static void DeleteFile(string _fileName)
    {
#if UNITY_ANDROID
        string path = Application.persistentDataPath + _fileName + ".meta";
#else
        if (!Directory.Exists(Application.persistentDataPath + projectName))
        {
            Directory.CreateDirectory(Application.persistentDataPath + projectName);
        }
        string path = Application.persistentDataPath + projectName + "/" + _fileName + ".meta";

#endif
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
    public static bool IsFileSaved(string _fileName)
    {
        string path = Application.persistentDataPath + _fileName + ".meta";
        return File.Exists(path);

    }

    public static BinaryFormatter GetBinaryFormatter()
    {
        BinaryFormatter formatter = new BinaryFormatter();

        SurrogateSelector selector = new SurrogateSelector();

        Vector3Surrogate v3Surrogate = new Vector3Surrogate();
        QuaternionSurrogate quaternionSurrogate = new QuaternionSurrogate();
        selector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), v3Surrogate);
        selector.AddSurrogate(typeof(Quaternion), new StreamingContext(StreamingContextStates.All), quaternionSurrogate);

        formatter.SurrogateSelector = selector;

        return formatter;
    }
}

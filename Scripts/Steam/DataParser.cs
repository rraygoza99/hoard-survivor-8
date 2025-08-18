using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Godot;

public class DataParser
{
    public static event Action<Dictionary<string, string>> OnChatMessage;
    public static event Action<Dictionary<string, string>> OnReadyMessage;
    public static event Action<Dictionary<string, string>> OnGameStartMessage;
    public static event Action<Dictionary<string, string>> OnPlayerUpdate;
    public static Dictionary<string, string> ParseData(IntPtr data, int size){
        GD.Print("Parsing data of size: " + size);
        byte[] managedArray = new byte[size];
        Marshal.Copy(data, managedArray, 0, size);
        var str = System.Text.Encoding.Default.GetString(managedArray);
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(str);
       
    }

    public static void ProcessData(IntPtr data, int size){
        var dict = ParseData(data, size);

        switch (dict["DataType"])
        {
            case "ChatMessage":
                GD.Print("Chat message received: " + dict["Message"]);
                OnChatMessage.Invoke(dict);
                break;
            case "Ready":
                OnReadyMessage.Invoke(dict);
                break;
            case "StartGame":
                OnGameStartMessage.Invoke(dict);
                break;  
            case "UpdatePlayer":
                OnPlayerUpdate.Invoke(dict);
                break;
            default:
                break;
        }
    }
}

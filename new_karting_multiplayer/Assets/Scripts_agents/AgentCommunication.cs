using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AgentCommunication : MonoBehaviour
{
    IEnumerator SendActionToAgent(string action)
    {
        string url = "http://localhost:5000/api/agent/action";
        string jsonPayload = "{\"action\": \"" + action + "\"}";

        // Crea una richiesta POST con JSON
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            // Invia la richiesta e aspetta la risposta
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Response: " + www.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error: " + www.error);
            }
        }
    }

    public void SendAction(string action)
    {
        StartCoroutine(SendActionToAgent(action));
    }

}


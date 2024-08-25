using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AgentCommunication : MonoBehaviour
{
    IEnumerator SendActionToAgent(string action)
    {
        string url = "http://localhost:5000/api/agent/action";
        WWWForm form = new WWWForm();
        form.AddField("action", action);

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
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


using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AgentResponseHandler : MonoBehaviour
{
    private string agentUrl = "http://localhost:5000/api/agent/response";

    void Start()
    {
        StartCoroutine(GetAgentResponse());
    }

    IEnumerator GetAgentResponse()
    {
        while (true)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(agentUrl))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Agent response received: " + www.downloadHandler.text);
                }
                else
                {
                    Debug.LogError("Error receiving agent response: " + www.error);
                }
            }

            yield return new WaitForSeconds(5); // Attendi 5 secondi prima di richiedere di nuovo
        }
    }
}

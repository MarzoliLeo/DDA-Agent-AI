using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Mirror;
using System;

public class CheckpointTracker : NetworkBehaviour
{
    private Objective objective; // Riferimento all'oggetto Objective per accedere ai checkpoint
    private string agentUrl = "http://localhost:5000/api/agent/checkpoint";
    public string playerId; // ID univoco del giocatore
    private NetworkIdentity networkIdentity; // Riferimento al NetworkIdentity del giocatore


    void Start()
    {
        //if (!isLocalPlayer) return; // Assicurati che solo il client locale esegua questo codice

        /*objective = GetComponent<Objective>();
        if (objective == null)
        {
            Debug.LogError("Objective component not found on the GameObject.");
        }
        else
        {
            Debug.Log("Objective component found: " + objective.GetType().Name);
        }*/
        
    
    }

    // Metodo chiamato quando un checkpoint viene raccolto [src: TargetObject.cs].
    public void OnCheckpointCollected(GameObject player)
    {

        //if (!isLocalPlayer) return; // Assicurati che solo il client locale esegua questo codice

        // Ottieni l'id del giocatore.
        playerId = player.GetInstanceID().ToString("X");

        StartCoroutine(SendCheckpointUpdate(playerId, 1)); //because in Flask they are summed up, so 1 by 1.
    }

    IEnumerator SendCheckpointUpdate(String playerId, int checkpointCount)
    {
        // Include l'ID del giocatore nel payload JSON
        string jsonPayload = "{\"player_id\": \"" + playerId + "\", \"checkpoints\": " + checkpointCount + "}";
        Debug.Log("Sending this payload: " + jsonPayload);

        using (UnityWebRequest www = new UnityWebRequest(agentUrl, "POST"))
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Checkpoint data sent to agent: " + checkpointCount);
            }
            else
            {
                Debug.LogError("Error sending checkpoint data: " + www.error);
            }
        }
    }
}




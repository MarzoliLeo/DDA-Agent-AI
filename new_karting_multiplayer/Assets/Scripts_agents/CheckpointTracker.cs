using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class CheckpointTracker : MonoBehaviour
{
    private Objective objective; // Riferimento all'oggetto Objective per accedere ai checkpoint
    private string agentUrl = "http://localhost:5000/api/agent/checkpoint";

    private int lastCheckpointCount = 0; // Traccia il numero di checkpoint raccolti

    void Start()
    {
        objective = GetComponent<Objective>();
        if (objective == null)
        {
            Debug.LogError("Objective component not found on the GameObject.");
        }
        else
        {
            Debug.Log("Objective component found: " + objective.GetType().Name);
            lastCheckpointCount = objective.NumberOfPickupsTotal; // Inizializza con il numero totale di checkpoint
        }
    }

    void Update()
    {
        if (objective != null)
        {
            int remainingCheckpoints = objective.NumberOfPickupsRemaining;

            // Calcola il numero di checkpoint raccolti
            int currentCheckpointCount = objective.NumberOfPickupsTotal - remainingCheckpoints;

            // Invia i dati solo se il numero di checkpoint raccolti è cambiato
            if (currentCheckpointCount > lastCheckpointCount)
            {
                lastCheckpointCount = currentCheckpointCount;
                StartCoroutine(SendCheckpointUpdate(currentCheckpointCount));
            }
        }
    }

    IEnumerator SendCheckpointUpdate(int checkpointCount)
    {
        string jsonPayload = "{\"checkpoints\": " + checkpointCount + "}";

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


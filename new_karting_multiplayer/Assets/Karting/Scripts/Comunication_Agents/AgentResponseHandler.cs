using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using KartGame.KartSystems;
using System.Linq;

public class AgentResponseHandler : MonoBehaviour
{
    private const int PLAYERS_TO_APPLY_DDA_BELOW_WHAT_RANK = 4;
    private const float INCREMENT_VALUE = 10f; 
    private string agentPredictionUrl = "http://localhost:5000/api/agent/prediction";
    private CheckpointTracker checkpointTracker;


    void OnEnable()
    {
        CheckpointTracker.OnCheckpointTrackerEnabled += OnCheckpointTrackerEnabled;
    }

    void OnDisable()
    {
        CheckpointTracker.OnCheckpointTrackerEnabled -= OnCheckpointTrackerEnabled;
    }

    void OnCheckpointTrackerEnabled()
    {
        checkpointTracker = FindObjectOfType<CheckpointTracker>();

        if (checkpointTracker != null)
        {
            Debug.Log("CheckpointTracker found!");
            StartCoroutine(GetAgentPrediction());
        }
        else
        {
            Debug.LogError("CheckpointTracker still not found.");
        }
    }
    

    IEnumerator GetAgentPrediction()
    {
        while (true)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(agentPredictionUrl))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Agent prediction response received: " + www.downloadHandler.text);

                    // Parse the JSON response
                    AgentPredictionResponse response = JsonUtility.FromJson<AgentPredictionResponse>(www.downloadHandler.text);
                    
                    if (response != null && response.prediction > 0)
                    {
                        ApplyDDAToKarts();
                    }
                }
                else
                {
                    Debug.LogError("Error receiving agent prediction: " + www.error);
                }
            }

            yield return new WaitForSeconds(5); // Attendi 5 secondi prima di richiedere di nuovo
        }
    }

    void ApplyDDAToKarts()
    {
        // Controlla tutti i kart nel checkpointTracker
        Dictionary<ArcadeKart, string> karts = checkpointTracker.kartIdMap;
        
        // Filtra i kart con rank inferiore a 4
        var kartsBelowRank = karts.Keys.Where(kart =>
        {
            //calcola il rank dei kart nella scena di gioco...
            float distanceToFinish = Vector3.Distance(kart.transform.position, checkpointTracker.finishLineTransform.position);
            int rank = 1 + checkpointTracker.kartIdMap.Keys.Count(k => Vector3.Distance(k.transform.position, checkpointTracker.finishLineTransform.position) < distanceToFinish);
            return rank > 1 && rank <= PLAYERS_TO_APPLY_DDA_BELOW_WHAT_RANK; // ...Ma ritorna solo quelli con rank inferiore a 4
        });

        foreach (var kart in kartsBelowRank)
        {
            ArcadeKart.Stats stats = kart.baseStats;

            // Modifica la velocità in base alla predizione
            stats.TopSpeed += stats.TopSpeed * INCREMENT_VALUE;  // Aumenta la TopSpeed in base al valore della predizione
            //stats.Acceleration += stats.Acceleration * INCREMENT_VALUE;  // Aumenta l'accelerazione

            // Aggiorna le statistiche del kart
            kart.baseStats = stats;

            Debug.Log($"Updated stats for Kart {kart.name}. New top speed: {kart.baseStats.TopSpeed}, New acceleration: {kart.baseStats.Acceleration}");
        }
    }

    // Classe di supporto per parsare la risposta JSON della predizione
    [System.Serializable]
    public class AgentPredictionResponse
    {
        public float prediction;
    }
}





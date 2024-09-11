using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Mirror;
using System;
using KartGame.KartSystems;
using System.Collections.Generic;

public class CheckpointTracker : NetworkBehaviour
{
    private Objective objective; // Riferimento all'oggetto Objective per accedere ai checkpoint
    private string agentUrl = "http://localhost:5000/api/agent/checkpoint";
    private int checkpointCount = 0; // Contatore dei checkpoint raccolti
    private Dictionary<ArcadeKart, string> kartIdMap = new Dictionary<ArcadeKart, string>(); // Dizionario per mappare ogni kart con il suo ID

    private float lastUpdateTime = 0f;  // Per inviare aggiornamenti periodici

    public override void OnStartServer()
    {
        // Sottoscrivi all'evento di spawn degli oggetti dal NetworkManager
        NetworkManager.singleton.OnServerAddPlayerEvent += OnPlayerAdded;
    }

    public override void OnStopServer()
    {
        // Rimuovi la sottoscrizione per evitare memory leaks
        NetworkManager.singleton.OnServerAddPlayerEvent -= OnPlayerAdded;
    }

    // Metodo chiamato quando viene aggiunto un giocatore (quando il prefab viene spawnato)
    private void OnPlayerAdded(NetworkConnectionToClient conn)
    {
        Debug.Log("SERVER STARTEDDDDDD!!!!!!!");
        Debug.Log("Player added: " + conn.identity.gameObject.name);
        GameObject playerObject = conn.identity.gameObject;
        RegisterKart(playerObject);
    }

    private void RegisterKart(GameObject playerObject)
    {
        ArcadeKart kart = playerObject.GetComponentInChildren<ArcadeKart>(); // Cerca ArcadeKart nel prefab del player
        if (kart != null && !kartIdMap.ContainsKey(kart))
        {
            string playerId = Guid.NewGuid().ToString(); // Genera un ID univoco per il giocatore
            kartIdMap.Add(kart, playerId);
            Debug.Log($"Kart {kart.name} registrato con ID: {playerId}");
        }
        else
        {
            Debug.LogWarning("Nessun ArcadeKart trovato nel prefab del player.");
        }
    }

    void Start()
    {
        //if (!isLocalPlayer) return; // Assicurati che solo il client locale esegua questo codice

        objective = FindObjectOfType<Objective>();
        //ArcadeKart[] arcadeKarts = FindObjectsOfType<ArcadeKart>();
        if (objective == null /*|| arcadeKarts.Length == 0*/)
        {
            Debug.LogError("Something's wrong with references not found on the GameObject.");
        }
        else
        {
            Debug.Log("Objective component found: " + objective.GetType().Name);
            /*foreach (ArcadeKart kart in arcadeKarts)
            {
                string playerId = Guid.NewGuid().ToString(); // Genera un ID univoco per il giocatore
                kartIdMap.Add(kart, playerId);
                Debug.Log("Found Kart: " + kart.name + ", Assigned Player ID: " + playerId);
            }*/

            checkpointCount = objective.NumberOfActivePickupsRemaining();
        }
    }

    void Update()
    {
        //if (!isLocalPlayer) return;  //!Esegui solo per il client locale

        // Esegui l'invio dei dati al server Flask ogni 5 secondi
        if (Time.time - lastUpdateTime > 5f)
        {
            checkpointCount = objective.NumberOfPickupsTotal -  objective.NumberOfActivePickupsRemaining();
            foreach (var entry in kartIdMap)
            {
                ArcadeKart kart = entry.Key;
                string playerId = entry.Value;

                // Invia i dati del giocatore per ciascun kart
                SendPlayerData(playerId, kart, checkpointCount);
            }

            lastUpdateTime = Time.time;
        }
    }

    void SendPlayerData(String playerId, ArcadeKart kart, int checkpointCount)
    {
        // Recupera i dati dal kart
        ArcadeKart.Stats stats = kart.baseStats;
        float currentSpeed = kart.GetMaxSpeed();
        bool isDrifting = kart.IsDrifting;

        // Crea un payload JSON con i dati del kart
        string jsonPayload = "{\"player_id\": \"" + playerId + "\", \"checkpoints\": " + checkpointCount +
                             ", \"current_speed\": " + currentSpeed +
                             ", \"top_speed\": " + stats.TopSpeed +
                             ", \"acceleration\": " + stats.Acceleration +
                             ", \"is_drifting\": " + isDrifting + "}";

        Debug.Log("Sending this payload for Kart: " + kart.name + ": " + jsonPayload);
        StartCoroutine(SendDataToServer(jsonPayload));
    }

    IEnumerator SendDataToServer(string jsonPayload)
    {
        using (UnityWebRequest www = new UnityWebRequest(agentUrl, "POST"))
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Player data sent successfully.");
            }
            else
            {
                Debug.LogError("Error sending player data: " + www.error);
            }
        }
    }

    // Messaggio per quando un kart viene spawnato (necessario per la comunicazione di Mirror)
    public struct AddKartMessage : NetworkMessage
    {
        public uint kartNetId;
    }
}




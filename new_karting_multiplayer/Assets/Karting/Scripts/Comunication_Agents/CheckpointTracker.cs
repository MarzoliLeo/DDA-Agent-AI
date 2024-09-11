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

//********************************************************************

    /*public override void OnStartServer()
    {
        // Aggiungi un callback per quando un nuovo oggetto viene spawnato
        NetworkServer.RegisterHandler<AddKartMessage>(OnKartSpawned);
        Debug.Log("CheckpointTracker: Server started.!!!!!!!");
    }
    
    private void OnKartSpawned(NetworkConnectionToClient conn, AddKartMessage msg)
    {
        NetworkIdentity networkIdentity = NetworkServer.spawned[msg.kartNetId];
        // Assicurati che l'oggetto spawnato abbia un ArcadeKart e che non sia già registrato
        RegisterKart(networkIdentity);
        Debug.Log("Kart spawned: " + networkIdentity.name);
    }

    private void RegisterKart(NetworkIdentity networkIdentity)
    {
        // Verifica se il prefab spawnato è un ArcadeKart
        GameObject spawnedObject = networkIdentity.gameObject;

        // Se l'oggetto è effettivamente un ArcadeKart, registra il kart
        ArcadeKart kart = spawnedObject.GetComponentInChildren<ArcadeKart>(); // Cerca il kart nei figli
        if (kart != null && !kartIdMap.ContainsKey(kart))
        {
            string playerId = Guid.NewGuid().ToString(); // Genera un ID univoco
            kartIdMap.Add(kart, playerId);    // Aggiungi alla mappa
            Debug.Log($"Kart {kart.name} registrato con ID: {playerId}");
        }
        else
        {
            Debug.LogWarning("SpawnedObject: " + spawnedObject.name);
            Debug.LogWarning("Nessun ArcadeKart trovato nel prefab istanziato.");
        }
    }
    */

    //********************************************************************
    /*public override void OnStartClient()
    {
        // Chiamato quando il client parte
        base.OnStartClient();
        Debug.Log("CheckpointTracker: Client started.");
        RegisterKartSpawnEvents();
    }*/

    /*void RegisterKartSpawnEvents()
    {
        // Gestione della registrazione di karts che vengono spawnati
        NetworkClient.RegisterHandler<AddKartMessage>(OnKartSpawned);
    }*/

    /*
    void OnKartSpawned(AddKartMessage msg)
    {
        ArcadeKart kart = NetworkServer.FindLocalObject(msg.kartNetId).GetComponent<ArcadeKart>();
        if (kart != null && !kartIdMap.ContainsKey(kart))
        {
            string playerId = Guid.NewGuid().ToString(); // Genera un ID univoco per il giocatore
            kartIdMap.Add(kart, playerId);
            Debug.Log("Kart spawned: " + kart.name + ", Assigned Player ID: " + playerId);
        }
    }*/

    void Start()
    {
        //if (!isLocalPlayer) return; // Assicurati che solo il client locale esegua questo codice

        /*if (isServer)
        {
            // Usa un ciclo per cercare tutti i karts già presenti nella scena
            foreach (NetworkIdentity identity in NetworkServer.spawned.Values)
            {
                RegisterKart(identity);
            }
        }*/

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
    // Metodo chiamato da CustomNetworkManager per registrare un nuovo kart con un ID univoco.
    // Questo poi periodicamente tramite Update() invia i dati al server Flask.
    public void RegisterNewKart(ArcadeKart kart, string playerId)
    {
        if (!kartIdMap.ContainsKey(kart))
        {
            kartIdMap.Add(kart, playerId);
            Debug.Log($"Nuovo kart registrato: {kart.name} con ID {playerId}");
        }
    }

    // Metodo chiamato quando un checkpoint viene raccolto [src: TargetObject.cs].
    public void OnCheckpointCollected(GameObject player)
    {

        //if (!isLocalPlayer) return; // Assicurati che solo il client locale esegua questo codice

        Debug.Log("TEMPORANEO");

        //StartCoroutine(SendCheckpointUpdate(playerId, 1)); //because in Flask they are summed up, so 1 by 1.
    }
    /*
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
    }*/

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




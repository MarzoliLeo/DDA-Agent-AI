using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Mirror;
using System;
using KartGame.KartSystems;
using System.Collections.Generic;
using System.Linq;

public class CheckpointTracker : NetworkBehaviour
{
    private const float INCREMENT_VALUE = 3.0f;
    private Objective objective; // Riferimento all'oggetto Objective per accedere ai checkpoint
    private string agentUrl = "http://localhost:5000/api/agent/data";
    private int checkpointCount = 0; // Contatore dei checkpoint raccolti
    private Dictionary<ArcadeKart, int> kartCheckpointMap = new Dictionary<ArcadeKart, int>(); // Mappa kart -> checkpoint raccolti
    public Dictionary<ArcadeKart, string> kartIdMap = new Dictionary<ArcadeKart, string>(); // Dizionario per mappare ogni kart con il suo ID
    private float lastUpdateTime = 0f;  // Per inviare aggiornamenti periodici
    public Transform finishLineTransform; // Posizione del traguardo
    public static event Action OnCheckpointTrackerEnabled; //evento generato quando il CheckpointTracker viene attivato nella scena.

    //[SerializeField]
    //private GameObject aiKartPrefab; // Riferimento pubblico al prefab del kart AI, da assegnare tramite l'Inspector


    void OnEnable()
    {
        // Lancia l'evento quando il CheckpointTracker viene attivato
        OnCheckpointTrackerEnabled?.Invoke();
    }

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
        Debug.Log("Player added: " + conn.identity.gameObject.name);
        GameObject playerObject = conn.identity.gameObject;
        RegisterKart(playerObject);
        AddAIKart();
    }


    // Metodo per istanziare un kart controllato dall'AI
    private void AddAIKart()
    {
        //Cerca tutti i kart AI presenti nella scena e li registra.
        ArcadeKart[] aiKarts = GameObject.FindObjectsOfType<ArcadeKart>();
        foreach (var entry in aiKarts)
        {
            RegisterKart(entry.gameObject);
            Debug.Log("AI Kart added: " + entry.name);
        }
        
    }


    private void RegisterKart(GameObject playerObject)
    {
        ArcadeKart kart = playerObject.GetComponentInChildren<ArcadeKart>(); // Cerca ArcadeKart nel prefab del player
        if (kart != null && !kartIdMap.ContainsKey(kart))
        {
            string playerId = Guid.NewGuid().ToString(); // Genera un ID univoco per il giocatore
            kartIdMap.Add(kart, playerId);
            kartCheckpointMap.Add(kart, 0);
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
        finishLineTransform = GameObject.FindGameObjectWithTag("StartFinishLine")?.transform; // Assicurati di avere un tag appropriato per il traguardo

        if (objective == null /*|| arcadeKarts.Length == 0*/)
        {
            Debug.LogError("Something's wrong with references not found on the GameObject.");
        }
        else
        {
            Debug.Log("Objective component found: " + objective.GetType().Name);
            checkpointCount = objective.NumberOfActivePickupsRemaining();
        }
    }

    void Update()
    {
        //if (!isLocalPlayer) return;  //!Esegui solo per il client locale

        // Esegui l'invio dei dati al server Flask ogni 5 secondi
        if (Time.time - lastUpdateTime > 5f)
        {
            //checkpointCount = objective.NumberOfPickupsTotal -  objective.NumberOfActivePickupsRemaining();
            foreach (var entry in kartIdMap)
            {
                ArcadeKart kart = entry.Key;
                string playerId = entry.Value;

                // Esegui l'invio dei dati del giocatore al server Flask per ciascun kart
                int kartCheckpointCount = kartCheckpointMap[kart]; // Numero di checkpoint per questo kart
                SendPlayerData(playerId, kart, kartCheckpointCount);
                //SendPlayerData(playerId, kart, checkpointCount);
            }

            lastUpdateTime = Time.time;
        }
    }

    // Metodo modificato per incrementare solo il checkpoint del kart specifico
    public void OnKartCollisionWithPickupObject(GameObject kart)
    {
        if (kart == null) return;

        ArcadeKart arcadeKart = kart.GetComponentInParent<ArcadeKart>();
        if (arcadeKart != null && kartCheckpointMap.ContainsKey(arcadeKart))
        {
            // Incrementa il contatore dei checkpoint solo per questo kart
            kartCheckpointMap[arcadeKart]++;
            Debug.Log($"Kart {arcadeKart.name} ha raccolto un checkpoint. Totale checkpoint: {kartCheckpointMap[arcadeKart]}");
        }
        else
        {
            Debug.LogWarning("Il kart non è registrato nel sistema.");
        }
    }

    //Metodo per aggiornare le statistiche del kart quando collide con un oggetto CrashObject.
    public void OnKartCollisionWithCrashObject(GameObject kart)
    {

        if (kart == null) return;
        Debug.Log("Kart collided with CrashObject: " + kart.name);
        ArcadeKart arcadeKart = kart.GetComponentInParent<ArcadeKart>();
        
        ArcadeKart.Stats stats = arcadeKart.baseStats;
        
        // Aumentiamo la velocità massima 
        stats.TopSpeed += INCREMENT_VALUE;  
        
        // Aggiorna le statistiche del kart
        arcadeKart.baseStats = stats;

        Debug.Log($"Updated stats for Kart {arcadeKart.name}. New top speed: {arcadeKart.baseStats.TopSpeed}");
    }

   void SendPlayerData(string playerId, ArcadeKart kart, int checkpointCount)
    {
        // Recupera i dati dal kart
        ArcadeKart.Stats stats = kart.baseStats;
        float currentSpeed = kart.GetMaxSpeed();
        Vector3 kartPosition = kart.transform.position;
        float distanceToFinish = Vector3.Distance(kartPosition, finishLineTransform.position);
        
        // Calcola la distanza dai kart davanti e dietro
        float distanceToFront = float.MaxValue;
        float distanceToBack = float.MaxValue;
        List<ArcadeKart> allKarts = kartIdMap.Keys.ToList();
        allKarts.Remove(kart);
        foreach (var otherKart in allKarts)
        {
            float distance = Vector3.Distance(kartPosition, otherKart.transform.position);
            // Supponiamo che il kart davanti sia più vicino al traguardo e il kart dietro sia più lontano
            if (Vector3.Dot(kart.transform.forward, otherKart.transform.position - kartPosition) > 0)
            {
                distanceToFront = Mathf.Min(distanceToFront, distance);
            }
            else
            {
                distanceToBack = Mathf.Min(distanceToBack, distance);
            }
        }

        // Calcola la posizione in classifica basata sulla distanza dal traguardo
        int rank = 1 + kartIdMap.Keys.Count(k => Vector3.Distance(k.transform.position, finishLineTransform.position) < distanceToFinish);

        // Crea un payload JSON con i dati del kart
        string jsonPayload = $"{{\"player_id\": \"{playerId}\", \"checkpoints\": {checkpointCount}," +
                             $" \"current_speed\": {currentSpeed}, \"top_speed\": {stats.TopSpeed}," +
                             $" \"acceleration\": {stats.Acceleration}, \"position\": {{\"x\": {kartPosition.x}, \"y\": {kartPosition.y}, \"z\": {kartPosition.z}}}," +
                             $" \"distance_to_front\": {distanceToFront}, \"distance_to_back\": {distanceToBack}," +
                             $" \"rank\": {rank}, \"distance_to_finish\": {distanceToFinish}}}";

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
}


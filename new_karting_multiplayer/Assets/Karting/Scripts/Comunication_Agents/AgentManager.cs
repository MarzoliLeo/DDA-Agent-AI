using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentManager : MonoBehaviour
{
    private static AgentManager _instance;
    public static AgentManager Instance { get { return _instance; } }

    private Dictionary<string, PlayerDataModel> playerData = new Dictionary<string, PlayerDataModel>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

        public void UpdatePlayerData(string playerId, float time, int checkpoint, Vector3 position)
    {
        if (!playerData.ContainsKey(playerId))
        {
            playerData[playerId] = new PlayerDataModel(playerId);
        }
        
        playerData[playerId].UpdateData(time, checkpoint, position);
    }

    public PlayerDataModel GetPlayerData(string playerId)
    {
        return playerData.ContainsKey(playerId) ? playerData[playerId] : null;
    }

    public Dictionary<string, PlayerDataModel> GetAllPlayerData()
    {
        return playerData;
    }
}

/* ESEMPIO DI AGGIORNAMENTO DATI GIOCATORE.
[Command]
public void CmdSendPlayerData(float time, int checkpoint, Vector3 position)
{
    GameManager.Instance.UpdatePlayerData(netId.ToString(), time, checkpoint, position);
}*/

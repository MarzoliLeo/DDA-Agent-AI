using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDataModel
{
    public string PlayerId { get; private set; }
    public float Time { get; private set; }
    public int Checkpoint { get; private set; }
    public Vector3 Position { get; private set; }

    public PlayerDataModel(string playerId)
    {
        PlayerId = playerId;
    }

    public void UpdateData(float time, int checkpoint, Vector3 position)
    {
        Time = time;
        Checkpoint = checkpoint;
        Position = position;
    }
}


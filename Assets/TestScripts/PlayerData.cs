using Newtonsoft.Json;
using UnityEngine;

public class PlayerData
{
    public string               player_name_ = "Player";
    public int                  player_id_ = 1;
    public int                  level_ = 1;
    public int                  score_ = 0;

    [JsonProperty]
    private float               health_ = 100f;
    
    [JsonProperty]
    private bool                is_alive_ = true;

    public float Health
    {
        get => health_;
        set => health_ = Mathf.Clamp(value, 0, 100);
    }

    public bool IsAlive
    {
        get => is_alive_;
        set => is_alive_ = value;
    }
}

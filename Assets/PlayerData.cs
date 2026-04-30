using Newtonsoft.Json;
using UnityEngine;

public class PlayerData
{
    public string               player_name_;
    public int                  player_id_;
    public int                  level_;
    public int                  score_;

    [JsonProperty]
    private float               health_;
    [JsonProperty]
    private bool                is_alive_;

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

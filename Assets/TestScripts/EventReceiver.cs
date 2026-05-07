using TMPro;
using UnityEngine;
using EventBus;

public class EventReceiver : MonoBehaviour
{
    [SerializeField] private TMP_Text               player_name_;
    [SerializeField] private TMP_Text               player_id_;
    [SerializeField] private TMP_Text               level_;
    [SerializeField] private TMP_Text               score_;
    [SerializeField] private TMP_Text               health_;

    public void Awake()
    {
        EventBus<UpdateUserInfoEvent, PlayerData>.OnEvent += UpdateInfo;
    }
    public void OnDestroy()
    {
        EventBus<UpdateUserInfoEvent, PlayerData>.OnEvent -= UpdateInfo;
    }
    public void UpdateInfo(PlayerData data)
    {
        player_name_.text = data.player_name_;
        player_id_.text = data.player_id_.ToString();
        level_.text = data.level_.ToString();
        score_.text = data.score_.ToString();
        health_.text = data.Health.ToString();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI runnersWinText;
    [SerializeField] private TextMeshProUGUI taggersWinText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI episodeText;

    private void Start()
    {
        // Subscribe to game events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreUpdate += UpdateScoreDisplay;
            GameManager.Instance.OnGameStart += OnGameStart;
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }
        
        // Initialize UI
        UpdateScoreDisplay(0, 0);
        UpdateTimer(0);
        UpdateEpisodeDisplay(0, 5);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreUpdate -= UpdateScoreDisplay;
            GameManager.Instance.OnGameStart -= OnGameStart;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameActive)
        {
            UpdateTimer(GameManager.Instance.RemainingTime);
        }
    }

    private void OnGameStart()
    {
        UpdateEpisodeDisplay(GameManager.Instance.CurrentEpisode, GameManager.Instance.MaxEpisodes);
    }

    public void UpdateScoreDisplay(int runnersWin, int taggersWin)
    {
        if (runnersWinText != null)
        {
            runnersWinText.text = $"Runners: {runnersWin}";
        }
        
        if (taggersWinText != null)
        {
            taggersWinText.text = $"Taggers: {taggersWin}";
        }
    }

    private void UpdateTimer(float timeRemaining)
    {
        if (timerText != null)
        {
            timerText.text = $"Time: {Mathf.FloorToInt(timeRemaining)}";
        }
    }

    private void UpdateEpisodeDisplay(int currentEpisode, int maxEpisodes)
    {
        if (episodeText != null)
        {
            if (maxEpisodes == int.MaxValue)
            {
                episodeText.text = $"Episode: {currentEpisode}/∞";
            }
            else
            {
                episodeText.text = $"Episode: {currentEpisode}/{maxEpisodes}";
            }
        }
    }
} 
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.MLAgents;
using System.Text;

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }

    // Environment parameters
    [System.Serializable]
    public class EnvironmentParameters
    {
        public float lesson;
        public float level_index;
        public float num_foodballs;
        public float num_runners;
        public float num_taggers;
        public float num_freezeballs;
        public float time_limit;
    }

    // Game stats
    [System.Serializable]
    public class GameStats
    {
        public int runnersWin;
        public int taggersWin;
        public float time;
        public int freezeballsCollected;
        public int wallballsCollected;
        public int totalFreezes;
        public int totalUnfreezes;
        public float episodeLength;
        public float freezeballCollectionPercent;
        public float wallballCollectionPercent;
        public int wallsUsed;
        public int freezeballsUsed;
        public int freezeballsHit;
    }

    // Event declarations
    public event Action<EnvironmentParameters> OnEnvironmentParametersLoaded;
    public event Action OnGameStart;
    public event Action OnGameEnd;
    public event Action<GameStats> OnEpisodeEnd;
    public event Action<int, int> OnScoreUpdate;

    // Public properties
    public EnvironmentParameters EnvParams { get; private set; }
    public GameStats CurrentStats { get; private set; }
    public int MaxEpisodes { get; private set; } = 5;
    public int CurrentEpisode { get; private set; } = 0;
    public float RemainingTime { get; private set; }
    public bool IsGameActive { get; private set; } = false;

    // References
    [SerializeField] private BoxCollider runnerSpawnArea;
    [SerializeField] private BoxCollider taggerSpawnArea;
    [SerializeField] private BoxCollider itemSpawnArea;
    [SerializeField] private GameObject runnerPrefab;
    [SerializeField] private GameObject taggerPrefab;
    [SerializeField] private GameObject freezeballPrefab;
    [SerializeField] private GameObject wallballPrefab;

    // Lists to track game objects
    private List<GameObject> runners = new List<GameObject>();
    private List<GameObject> taggers = new List<GameObject>();
    private List<GameObject> freezeballs = new List<GameObject>();
    private List<GameObject> wallballs = new List<GameObject>();

    // Add this list to store stats from each episode
    private List<GameStats> episodeStats = new List<GameStats>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize stats
        CurrentStats = new GameStats();
        
        // Check if Academy exists and set max episodes
           bool academyExists = Unity.MLAgents.Academy.IsInitialized;
   if (academyExists)
   {
       MaxEpisodes = int.MaxValue;
       Debug.Log("Academy found, setting unlimited episodes");
   }
   else
   {
       MaxEpisodes = 5;
       Debug.Log("Academy not found, setting max episodes to 5");
   }    
        // Load environment parameters
        LoadEnvironmentParameters();
    }

    private void Start()
    {
        StartNewEpisode();
    }

    private void Update()
    {
        if (IsGameActive)
        {
            RemainingTime -= Time.deltaTime;
            
            if (RemainingTime <= 0)
            {
                EndEpisode(true); // Runners win when time expires
            }
        }
    }

    private void LoadEnvironmentParameters()
    {
        try
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("environment_param");
            if (jsonFile != null)
            {
                EnvParams = JsonUtility.FromJson<EnvironmentParameters>(jsonFile.text);
                OnEnvironmentParametersLoaded?.Invoke(EnvParams);
                Debug.Log("Environment parameters loaded successfully");
            }
            else
            {
                Debug.LogError("Failed to load environment_param.json");
                // Default values if file not found
                EnvParams = new EnvironmentParameters
                {
                    lesson = 1.0f,
                    level_index = 0,
                    num_foodballs = 5,
                    num_runners = 3,
                    num_taggers = 2,
                    num_freezeballs = 5,
                    time_limit = 60
                };
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading environment parameters: {e.Message}");
            // Default values if parsing error
            EnvParams = new EnvironmentParameters
            {
                lesson = 1.0f,
                level_index = 0,
                num_foodballs = 5,
                num_runners = 3,
                num_taggers = 2,
                num_freezeballs = 5,
                time_limit = 60
            };
        }
    }

    public void StartNewEpisode()
    {
        if (CurrentEpisode >= MaxEpisodes)
        {
            ExportResultsToCSV();
            Debug.Log("Max episodes reached. Experiment complete.");
            return;
        }

        CurrentEpisode++;
        RemainingTime = EnvParams.time_limit;
        IsGameActive = true;
        
        // Reset stats for new episode
        ResetEpisodeStats();
        
        // Clear existing game objects
        ClearGameObjects();
        
        // Spawn game elements
        SpawnRunners((int)EnvParams.num_runners);
        SpawnTaggers((int)EnvParams.num_taggers);
        SpawnFreezeballs((int)EnvParams.num_freezeballs);
        SpawnWallballs((int)EnvParams.num_foodballs); // Using num_foodballs for wallballs
        
        OnGameStart?.Invoke();
    }

    private void ResetEpisodeStats()
    {
        // Reset only the stats that should be reset per episode
        CurrentStats.time = 0;
        CurrentStats.freezeballsCollected = 0;
        CurrentStats.wallballsCollected = 0;
        CurrentStats.totalFreezes = 0;
        CurrentStats.totalUnfreezes = 0;
        CurrentStats.episodeLength = 0;
        CurrentStats.freezeballCollectionPercent = 0;
        CurrentStats.wallballCollectionPercent = 0;
        CurrentStats.wallsUsed = 0;
        CurrentStats.freezeballsUsed = 0;
        CurrentStats.freezeballsHit = 0;
        
        // Note: We don't reset runnersWin and taggersWin as those are cumulative
    }

    private void ClearGameObjects()
    {
        foreach (var runner in runners)
        {
            if (runner != null) Destroy(runner);
        }
        
        foreach (var tagger in taggers)
        {
            if (tagger != null) Destroy(tagger);
        }
        
        foreach (var freezeball in freezeballs)
        {
            if (freezeball != null) Destroy(freezeball);
        }
        
        foreach (var wallball in wallballs)
        {
            if (wallball != null) Destroy(wallball);
        }
        
        runners.Clear();
        taggers.Clear();
        freezeballs.Clear();
        wallballs.Clear();
    }

    private void SpawnRunners(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (runnerSpawnArea == null) break;
            
            Vector3 spawnPos = GetRandomPositionInBounds(runnerSpawnArea.bounds);
            GameObject runner = Instantiate(runnerPrefab, spawnPos, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0));
            runners.Add(runner);
        }
    }

    private void SpawnTaggers(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (taggerSpawnArea == null) break;
            
            Vector3 spawnPos = GetRandomPositionInBounds(taggerSpawnArea.bounds);
            GameObject tagger = Instantiate(taggerPrefab, spawnPos, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0));
            taggers.Add(tagger);
        }
    }

    private void SpawnFreezeballs(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (itemSpawnArea == null) break;
            
            Vector3 spawnPos = GetRandomPositionInBounds(itemSpawnArea.bounds);
            GameObject freezeball = Instantiate(freezeballPrefab, spawnPos, Quaternion.identity);
            freezeballs.Add(freezeball);
        }
    }

    private void SpawnWallballs(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (itemSpawnArea == null) break;
            
            Vector3 spawnPos = GetRandomPositionInBounds(itemSpawnArea.bounds);
            GameObject wallball = Instantiate(wallballPrefab, spawnPos, Quaternion.identity);
            wallballs.Add(wallball);
        }
    }

    // Helper method to get random position within bounds
    private Vector3 GetRandomPositionInBounds(Bounds bounds)
    {
        return new Vector3(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            UnityEngine.Random.Range(bounds.max.y, bounds.max.y),
            UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
        );
    }

    public void RespawnFreezeball()
    {
        StartCoroutine(RespawnFreezeBallAfterDelay(2f));
    }

    public void RespawnWallball()
    {
        StartCoroutine(RespawnWallBallAfterDelay(2f));
    }

    private IEnumerator RespawnFreezeBallAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (itemSpawnArea != null)
        {
            Vector3 spawnPos = GetRandomPositionInBounds(itemSpawnArea.bounds);
            GameObject freezeball = Instantiate(freezeballPrefab, spawnPos, Quaternion.identity);
            freezeballs.Add(freezeball);
        }
    }

    private IEnumerator RespawnWallBallAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (itemSpawnArea != null)
        {
            Vector3 spawnPos = GetRandomPositionInBounds(itemSpawnArea.bounds);
            GameObject wallball = Instantiate(wallballPrefab, spawnPos, Quaternion.identity);
            wallballs.Add(wallball);
        }
    }

    public void EndEpisode(bool runnersWin)
    {
        if (!IsGameActive) return;
        
        IsGameActive = false;
        CurrentStats.episodeLength = EnvParams.time_limit - RemainingTime;
        
        if (runnersWin)
        {
            CurrentStats.runnersWin++;
        }
        else
        {
            CurrentStats.taggersWin++;
        }
        
        // Save a copy of the current episode's stats before starting the next episode
        GameStats episodeStat = new GameStats
        {
            runnersWin = runnersWin ? 1 : 0,
            taggersWin = runnersWin ? 0 : 1,
            time = CurrentStats.time,
            freezeballsCollected = CurrentStats.freezeballsCollected,
            wallballsCollected = CurrentStats.wallballsCollected,
            totalFreezes = CurrentStats.totalFreezes,
            totalUnfreezes = CurrentStats.totalUnfreezes,
            episodeLength = CurrentStats.episodeLength,
            freezeballCollectionPercent = CurrentStats.freezeballCollectionPercent,
            wallballCollectionPercent = CurrentStats.wallballCollectionPercent,
            wallsUsed = CurrentStats.wallsUsed,
            freezeballsUsed = CurrentStats.freezeballsUsed,
            freezeballsHit = CurrentStats.freezeballsHit
        };
        
        // Add to our episode stats list
        episodeStats.Add(episodeStat);
        
        OnScoreUpdate?.Invoke(CurrentStats.runnersWin, CurrentStats.taggersWin);
        OnEpisodeEnd?.Invoke(CurrentStats);
        OnGameEnd?.Invoke();
        
        // Start new episode after a delay
        Invoke(nameof(StartNewEpisode), 2f);
    }

    public bool CheckAllRunnersFrozen()
    {
        int frozenCount = 0;
        foreach (var runner in runners)
        {
            if (runner != null)
            {
                RunnerAgent runnerAgent = runner.GetComponent<RunnerAgent>();
                if (runnerAgent != null && runnerAgent.IsFrozen)
                {
                    frozenCount++;
                }
            }
        }
        
        return frozenCount == runners.Count && runners.Count > 0;
    }

    public void NotifyFreeze()
    {
        CurrentStats.totalFreezes++;
        
        if (CheckAllRunnersFrozen())
        {
            EndEpisode(false); // Taggers win
        }
    }

    public void NotifyUnfreeze()
    {
        CurrentStats.totalUnfreezes++;
    }

    public void NotifyFreezeBallCollected()
    {
        CurrentStats.freezeballsCollected++;
        CurrentStats.freezeballCollectionPercent = (float)CurrentStats.freezeballsCollected / EnvParams.num_freezeballs;
    }

    public void NotifyWallBallCollected()
    {
        CurrentStats.wallballsCollected++;
        CurrentStats.wallballCollectionPercent = (float)CurrentStats.wallballsCollected / EnvParams.num_foodballs;
    }

    public void NotifyWallUsed()
    {
        CurrentStats.wallsUsed++;
    }

    public void NotifyFreezeBallUsed()
    {
        CurrentStats.freezeballsUsed++;
    }

    public void NotifyFreezeBallHit()
    {
        CurrentStats.freezeballsHit++;
    }

    private void ExportResultsToCSV()
    {
        StringBuilder sb = new StringBuilder();
        
        // Add headers
        sb.AppendLine("Episode,RunnerWin,TaggerWin,Time,FreezeBallsCollected,WallBallsCollected,TotalFreezes,TotalUnfreezes,EpisodeLength,FreezeBallCollectionPercent,WallBallCollectionPercent,WallsUsed,FreezeBallsUsed,FreezeBallsHit");
        
        // Add data for each episode
        for (int i = 0; i < episodeStats.Count; i++)
        {
            GameStats stats = episodeStats[i];
            sb.AppendLine($"{i+1},{stats.runnersWin},{stats.taggersWin},{stats.time},{stats.freezeballsCollected},{stats.wallballsCollected},{stats.totalFreezes},{stats.totalUnfreezes},{stats.episodeLength},{stats.freezeballCollectionPercent},{stats.wallballCollectionPercent},{stats.wallsUsed},{stats.freezeballsUsed},{stats.freezeballsHit}");
        }
        
        // Add summary row
        sb.AppendLine("------- Summary -------");
        sb.AppendLine($"Total,{CurrentStats.runnersWin},{CurrentStats.taggersWin},{CurrentStats.time},{CurrentStats.freezeballsCollected},{CurrentStats.wallballsCollected},{CurrentStats.totalFreezes},{CurrentStats.totalUnfreezes},{CurrentStats.episodeLength},{CurrentStats.freezeballCollectionPercent},{CurrentStats.wallballCollectionPercent},{CurrentStats.wallsUsed},{CurrentStats.freezeballsUsed},{CurrentStats.freezeballsHit}");
        
        // Define file path with timestamp
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filePath = Path.Combine(Application.persistentDataPath, $"FreezeTagResults_{timestamp}.csv");
        
        try
        {
            // Write to file
            File.WriteAllText(filePath, sb.ToString());
            Debug.Log($"Results exported to {filePath}");
            
            // Also log the path to the Unity Editor console
            Debug.Log($"CSV file location: {Application.persistentDataPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to export results: {e.Message}");
        }
    }
} 
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Inference;
using Unity.MLAgents.Policies;
using System.Text;

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }

    // Game configuration from JSON
    [System.Serializable]
    public class Lesson
    {
        public string name;
        public float num_wallballs;
        public float num_runners;
        public float num_taggers;
        public float num_freezeballs;
        public float time_limit;
    }

    [System.Serializable]
    public class EnvironmentConfig
    {
        public List<Lesson> lessons = new List<Lesson>();
        public int currentLesson;
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
    public event Action<Lesson> OnLessonLoaded;
    public event Action OnGameStart;
    public event Action OnGameEnd;
    public event Action<GameStats> OnEpisodeEnd;
    public event Action<int, int> OnScoreUpdate;

    // Public properties
    public EnvironmentConfig Config { get; private set; }
    public Lesson CurrentLesson { get; private set; }
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

    
    // Model assets
    private NNModel runnerModel;
    private NNModel taggerModel;
    
    // Selected lesson index
    private int currentLessonIndex = 3; // Default to lesson 3 (Lesson1_hard)

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
            
            // Try to get the lesson from Academy
            try 
            {
                var academyParameters = Academy.Instance.EnvironmentParameters;
                if (academyParameters.GetWithDefault("lesson", -1) != -1)
                {
                    currentLessonIndex = Mathf.FloorToInt(academyParameters.GetWithDefault("lesson", currentLessonIndex));
                    Debug.Log($"Lesson index from Academy: {currentLessonIndex}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not get lesson from Academy: {e.Message}");
            }
        }
        else
        {
            MaxEpisodes = 5;
            Debug.Log("Academy not found, setting max episodes to 5");
            
            // Try to get parameters from PlayerPrefs (set by MainMenu)
            if (PlayerPrefs.HasKey("LessonType"))
            {
                currentLessonIndex = PlayerPrefs.GetInt("LessonType", currentLessonIndex);
                Debug.Log($"Lesson index from MainMenu: {currentLessonIndex}");
            }
            
            if (PlayerPrefs.HasKey("RunnerAgentModel"))
            {
                string modelName = PlayerPrefs.GetString("RunnerAgentModel", "");
                if (!string.IsNullOrEmpty(modelName))
                {
                    // Load the model from Resources/Models/Runner folder
                    runnerModel = Resources.Load<NNModel>($"Models/Runner/{modelName}");
                    if (runnerModel != null)
                    {
                        Debug.Log($"Loaded Runner Model: {modelName}");
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to load Runner Model: {modelName}");
                    }
                }
            }
            
            if (PlayerPrefs.HasKey("TaggerAgentModel"))
            {
                string modelName = PlayerPrefs.GetString("TaggerAgentModel", "");
                if (!string.IsNullOrEmpty(modelName))
                {
                    // Load the model from Resources/Models/Tagger folder
                    taggerModel = Resources.Load<NNModel>($"Models/Tagger/{modelName}");
                    if (taggerModel != null)
                    {
                        Debug.Log($"Loaded Tagger Model: {modelName}");
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to load Tagger Model: {modelName}");
                    }
                }
            }
        }
        
        // Load lesson configuration
        LoadLessonConfiguration();
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

    private void LoadLessonConfiguration()
    {
        try
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("environment_param");
            if (jsonFile != null)
            {
                // Parse the new JSON format
                Config = JsonUtility.FromJson<EnvironmentConfig>(jsonFile.text);
                
                // Verify we have lessons
                if (Config.lessons == null || Config.lessons.Count == 0)
                {
                    Debug.LogError("No lessons found in environment_param.json");
                    SetDefaultLesson();
                    return;
                }
                
                // Make sure index is valid
                currentLessonIndex = Mathf.Clamp(currentLessonIndex, 0, Config.lessons.Count - 1);
                
                // Set current lesson
                CurrentLesson = Config.lessons[currentLessonIndex];
                
                Debug.Log($"Loaded lesson: {CurrentLesson.name} (index {currentLessonIndex})");
                Debug.Log($"Parameters: Runners={CurrentLesson.num_runners}, Taggers={CurrentLesson.num_taggers}, " +
                         $"FreezeBalls={CurrentLesson.num_freezeballs}, WallBalls={CurrentLesson.num_wallballs}, " +
                         $"TimeLimit={CurrentLesson.time_limit}");
                
                OnLessonLoaded?.Invoke(CurrentLesson);
            }
            else
            {
                Debug.LogError("Failed to load environment_param.json");
                SetDefaultLesson();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading lesson configuration: {e.Message}");
            SetDefaultLesson();
        }
    }

    private void SetDefaultLesson()
    {
        // Create default lesson (Lesson1_hard)
        CurrentLesson = new Lesson
        {
            name = "Lesson1_hard",
            num_wallballs = 10.0f,
            num_runners = 5.0f,
            num_taggers = 4.0f,
            num_freezeballs = 10.0f,
            time_limit = 90.0f
        };
        
        Debug.Log("Using default lesson (Lesson1_hard)");
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
        RemainingTime = CurrentLesson.time_limit;
        IsGameActive = true;
        
        // Reset stats for new episode
        ResetEpisodeStats();
        
        // Clear existing game objects
        ClearGameObjects();
        
        // Spawn game elements
        SpawnRunners((int)CurrentLesson.num_runners);
        SpawnTaggers((int)CurrentLesson.num_taggers);
        SpawnFreezeballs((int)CurrentLesson.num_freezeballs);
        SpawnWallballs((int)CurrentLesson.num_wallballs);
        
        OnGameStart?.Invoke();
    }

    // Reset episode stats, clearing previous episode data
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

    // Clear all game objects from previous episode
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

    // Spawn runners with behavior parameters
    private void SpawnRunners(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (runnerSpawnArea == null) break;
            
            Vector3 spawnPos = GetRandomPositionInBounds(runnerSpawnArea.bounds);
            GameObject runner = Instantiate(runnerPrefab, spawnPos, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0));
            
            // Set behavior parameters
            var behaviorParams = runner.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
            if (behaviorParams != null)
            {
                // Set model if available
                if (runnerModel != null)
                {
                    behaviorParams.Model = runnerModel;
                }
                else
                {
                    // Set to null/none if no model specified
                    behaviorParams.Model = null;
                }
            }
            
            runners.Add(runner);
        }
    }

    // Spawn taggers with behavior parameters
    private void SpawnTaggers(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (taggerSpawnArea == null) break;
            
            Vector3 spawnPos = GetRandomPositionInBounds(taggerSpawnArea.bounds);
            GameObject tagger = Instantiate(taggerPrefab, spawnPos, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0));
            
            // Set behavior parameters
            var behaviorParams = tagger.GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
            if (behaviorParams != null)
            {
                // Set model if available
                if (taggerModel != null)
                {
                    behaviorParams.Model = taggerModel;
                }
                else
                {
                    // Set to null/none if no model specified
                    behaviorParams.Model = null;
                }
            }
            
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
        // Fixed y value to avoid spawning inside ground or too high
        float yPos = bounds.min.y + 1.0f; // Ensure agents are always 1 unit above the minimum height
        
        return new Vector3(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            yPos,
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
        CurrentStats.episodeLength = CurrentLesson.time_limit - RemainingTime;
        
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
        CurrentStats.freezeballCollectionPercent = (float)CurrentStats.freezeballsCollected / CurrentLesson.num_freezeballs;
    }

    public void NotifyWallBallCollected()
    {
        CurrentStats.wallballsCollected++;
        CurrentStats.wallballCollectionPercent = (float)CurrentStats.wallballsCollected / CurrentLesson.num_wallballs;
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

    // Add method to return to main menu
    public void ReturnToMainMenu()
    {
        // Optionally export results before leaving
        ExportResultsToCSV();
        
        // Load main menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
} 
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.Sentis;
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
        public int max_wallballs;
        public int max_freezeballs;
        public float wall_cooldown;
        public float shoot_cooldown;
        public float freezeball_speed;
        public float wall_lifetime;
        public float runner_speed_multiplier;
        public float freezeball_size_multiplier;
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
        public int wallsUsed;
        public int freezeballsUsed;
        public int freezeballsHit;
        public int freezeByTouch;
        public int frozenAgentsAtEndOfEpisode;
        public float totalTimeSpentFreezing;
        public float fastestUnfreeze;
        public float avgUnfreezeTime;
        public int totalWallHitsToTagger;
        public int totalWallHitsToFreezeBallProjectile;
        public int totalUnsuccessfulUnfreezeAttempts;
        public float longestSurviveFromUnfreeze;
        public float shortestSurviveFromUnfreeze;
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
    [SerializeField] private int maxEpisodes = 5;
    public int MaxEpisodes { get => maxEpisodes; private set => maxEpisodes = value; }
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
    private ModelAsset runnerModel;
    private ModelAsset taggerModel;
    
    // Heuristic mode settings
    private int heuristicMode = 0; // 0: None, 1: Runner, 2: Tagger
    
    // Selected lesson index
    private int currentLessonIndex = 3; // Default to lesson 3 (Lesson1_hard)

    // Variables for tracking unfreeze times
    private float totalUnfreezeTime = 0f;
    private int unfreezeCount = 0;

    // File path for the current run's CSV export
    private string csvFilePath;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            // Remove DontDestroyOnLoad to allow proper scene reloading
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize stats
        CurrentStats = new GameStats();
        
        // Generate a unique filename for this run
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string randomHash = GenerateRandomHash(8);
        csvFilePath = Path.Combine(Application.persistentDataPath, $"FreezeTagResults_{timestamp}_{randomHash}.csv");
        Debug.Log($"Results will be saved to: {csvFilePath}");
        
        // Initial load of lesson configuration (will be updated in Start if needed)
        LoadLessonConfiguration();
    }

    private void Start()
    {
        // Verify physics settings
        Debug.Log($"Physics: bounceThreshold={Physics.bounceThreshold}, defaultContactOffset={Physics.defaultContactOffset}");
        Debug.Log("Layer collision matrix: checking if any layers are ignoring collisions...");
        
        // Initialize lesson configuration with defaults first
        LoadLessonConfiguration();
        
        // Pre-spawn agents to register their communicators with Academy, but don't start the episode yet
        PreSpawnAgents();
        
        // Wait for ML-Agents Academy to initialize before proceeding with game logic
        StartCoroutine(WaitForAcademyAndContinue());
    }

    private void PreSpawnAgents()
    {
        // Spawn a single runner and tagger outside the play area to register communicators
        Debug.Log("Pre-spawning agents to register communicators...");
        
        // Spawn a runner
        if (runnerPrefab != null)
        {
            // Spawn at a position far below the play area (it will be destroyed later)
            Vector3 spawnPos = new Vector3(0, -100, 0);
            GameObject runner = Instantiate(runnerPrefab, spawnPos, Quaternion.identity);
            runners.Add(runner);
        }
        
        // Spawn a tagger
        if (taggerPrefab != null)
        {
            // Spawn at a position far below the play area (it will be destroyed later)
            Vector3 spawnPos = new Vector3(0, -100, 0);
            GameObject tagger = Instantiate(taggerPrefab, spawnPos, Quaternion.identity);
            taggers.Add(tagger);
        }
    }

    private IEnumerator WaitForAcademyAndContinue()
    {
        // Wait up to 2 seconds for Academy to initialize
        float timeoutDuration = 2.0f;
        float elapsedTime = 0f;
        
        Debug.Log("Waiting for ML-Agents Academy to initialize (timeout: 2 seconds)...");
        
        while (!Unity.MLAgents.Academy.IsInitialized && elapsedTime < timeoutDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        if (Unity.MLAgents.Academy.IsInitialized)
        {
            Debug.Log($"ML-Agents Academy initialized after {elapsedTime:F2} seconds");
        }
        else
        {
            Debug.LogWarning($"ML-Agents Academy did not initialize after waiting {timeoutDuration} seconds");
        }
        
        // Clean up pre-spawned agents
        ClearGameObjects();
        
        // Now check PlayerPrefs and update configuration
        CheckPlayerPrefs();
        
        // Start a new episode with properly initialized settings
        StartNewEpisode();
    }

    private void CheckPlayerPrefs()
    {
        // Check if Academy exists and if it's connected to a Python trainer
        bool academyExists = Unity.MLAgents.Academy.IsInitialized;
        bool isConnectedToPython = false;
        
        if (academyExists && Academy.Instance != null)
        {
            try 
            {
                // Try to detect if we're in training mode (connected to Python)
                // During training, the Academy will initialize differently than in inference
                isConnectedToPython = Academy.Instance.IsCommunicatorOn;
                
                Debug.Log($"Academy.IsInitialized: {academyExists}, IsCommunicatorOn: {isConnectedToPython}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error checking Academy connection: {e.Message}");
                isConnectedToPython = false;
            }
        }
        
        // Only set unlimited episodes if truly connected to Python trainer
        if (academyExists && isConnectedToPython)
        {
            Debug.Log($"Before change: MaxEpisodes = {MaxEpisodes}");
            MaxEpisodes = int.MaxValue;
            Debug.Log($"After change: Python trainer connected, setting unlimited episodes (MaxEpisodes = {MaxEpisodes})");
            
            // Try to get the lesson from Academy
            try 
            {
                var academyParameters = Academy.Instance.EnvironmentParameters;
                Debug.Log($"Academy parameters accessed successfully: {academyParameters != null}");
                if (academyParameters.GetWithDefault("lesson", -1) != -1)
                {
                    int newLessonIndex = Mathf.FloorToInt(academyParameters.GetWithDefault("lesson", currentLessonIndex));
                    
                    // Only reload lesson if it has changed
                    if (newLessonIndex != currentLessonIndex)
                    {
                        currentLessonIndex = newLessonIndex;
                        Debug.Log($"Updated lesson index from Academy: {currentLessonIndex}");
                        
                        // Reload the lesson configuration with the new index
                        LoadLessonConfiguration();
                        
                        // Academy settings take precedence, so we can return early
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Could not get lesson from Academy: {e.Message}");
            }
        }
        else
        {
            // Make sure MaxEpisodes is set to the inspector value if not in Python training
            Debug.Log($"No Python trainer detected, using standard episode limit: {maxEpisodes}");
        }
        
        // If Academy doesn't exist or doesn't specify a lesson, check PlayerPrefs
        // Always check for updated lesson type from PlayerPrefs
        if (PlayerPrefs.HasKey("LessonType"))
        {
            int newLessonIndex = PlayerPrefs.GetInt("LessonType", currentLessonIndex);
            
            // Only reload lesson if it has changed
            if (newLessonIndex != currentLessonIndex)
            {
                currentLessonIndex = newLessonIndex;
                Debug.Log($"Updated lesson index from PlayerPrefs: {currentLessonIndex}");
                
                // Reload the lesson configuration with the new index
                LoadLessonConfiguration();
            }
        }
        
        // Check for updated agent models
        if (PlayerPrefs.HasKey("RunnerAgentModel"))
        {
            string modelName = PlayerPrefs.GetString("RunnerAgentModel", "");
            if (!string.IsNullOrEmpty(modelName))
            {
                try
                {
                    // Log the exact path we're trying to load
                    string resourcePath = $"Models/Runner/{modelName}";
                    Debug.Log($"Attempting to load Runner model from resource path: {resourcePath}");
                    
                    // Check if the model exists as a resource
                    var modelTextAsset = Resources.Load<TextAsset>(resourcePath);
                    if (modelTextAsset == null)
                    {
                        Debug.LogWarning($"Runner model not found as TextAsset at path: {resourcePath}");
                    }
                    
                    // Try to load as ModelAsset (Sentis)
                    runnerModel = Resources.Load<ModelAsset>(resourcePath);
                    
                if (runnerModel != null)
                {
                        Debug.Log($"Successfully loaded Runner Model: {modelName}");
                }
                else
                {
                        Debug.LogWarning($"Failed to load Runner Model: {modelName} - Model loaded as null");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception loading Runner Model: {modelName} - {e.Message}\n{e.StackTrace}");
                    runnerModel = null;
                }
            }
        }
        
        if (PlayerPrefs.HasKey("TaggerAgentModel"))
        {
            string modelName = PlayerPrefs.GetString("TaggerAgentModel", "");
            if (!string.IsNullOrEmpty(modelName))
            {
                try
                {
                    // Log the exact path we're trying to load
                    string resourcePath = $"Models/Tagger/{modelName}";
                    Debug.Log($"Attempting to load Tagger model from resource path: {resourcePath}");
                    
                    // Check if the model exists as a resource
                    var modelTextAsset = Resources.Load<TextAsset>(resourcePath);
                    if (modelTextAsset == null)
                    {
                        Debug.LogWarning($"Tagger model not found as TextAsset at path: {resourcePath}");
                    }
                    
                    // Try to load as ModelAsset (Sentis)
                    taggerModel = Resources.Load<ModelAsset>(resourcePath);
                    
                if (taggerModel != null)
                {
                        Debug.Log($"Successfully loaded Tagger Model: {modelName}");
                }
                else
                {
                        Debug.LogWarning($"Failed to load Tagger Model: {modelName} - Model loaded as null");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception loading Tagger Model: {modelName} - {e.Message}\n{e.StackTrace}");
                    taggerModel = null;
                }
            }
        }
        
        // Get heuristic mode setting
        if (PlayerPrefs.HasKey("HeuristicMode"))
        {
            heuristicMode = PlayerPrefs.GetInt("HeuristicMode", 0);
            Debug.Log($"Heuristic Mode: {heuristicMode} (0: None, 1: Runner, 2: Tagger)");
        }
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
        
        // Temporary: Press Ctrl+M to clear model references
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.M))
        {
            ClearModelReferences();
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
        // Create default lesson (Default)
        CurrentLesson = new Lesson
        {
            name = "Default",
            num_wallballs = 10.0f,
            num_runners = 5.0f,
            num_taggers = 4.0f,
            num_freezeballs = 10.0f,
            time_limit = 90.0f,
            max_wallballs = 3,
            max_freezeballs = 3,
            wall_cooldown = 1f,
            shoot_cooldown = 1f,
            freezeball_speed = 4.0f,
            wall_lifetime = 3.0f,
            runner_speed_multiplier = 1.0f,
            freezeball_size_multiplier = 1.0f
        };
        
        Debug.Log("Using default lesson (Default)");
    }

    public void StartNewEpisode()
    {
        Debug.Log($"StartNewEpisode: CurrentEpisode={CurrentEpisode}, MaxEpisodes={MaxEpisodes}, Type={MaxEpisodes.GetType()}");
        if (CurrentEpisode >= MaxEpisodes)
        {
            ExportResultsToCSV(true);
            Debug.Log("Max episodes reached. Experiment complete.");
            return;
        }

        // Switch to overview camera at the start of a new episode
        if (CameraSystemManager.Instance != null)
        {
            CameraSystemManager.Instance.CleanupAgentList();
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
        CurrentStats.wallsUsed = 0;
        CurrentStats.freezeballsUsed = 0;
        CurrentStats.freezeballsHit = 0;
        CurrentStats.freezeByTouch = 0;
        CurrentStats.frozenAgentsAtEndOfEpisode = 0;
        CurrentStats.totalTimeSpentFreezing = 0f;
        CurrentStats.fastestUnfreeze = float.MaxValue; // Initialize to max value so any unfreeze will be faster
        CurrentStats.avgUnfreezeTime = 0f;
        CurrentStats.totalWallHitsToTagger = 0;
        CurrentStats.totalWallHitsToFreezeBallProjectile = 0;
        CurrentStats.totalUnsuccessfulUnfreezeAttempts = 0;
        CurrentStats.longestSurviveFromUnfreeze = 0f;
        CurrentStats.shortestSurviveFromUnfreeze = float.MaxValue;
        
        // Reset unfreeze tracking variables
        totalUnfreezeTime = 0f;
        unfreezeCount = 0;
        
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
        // Select one random runner for heuristic mode if enabled
        int heuristicRunnerIndex = -1;
        if (heuristicMode == 1 && count > 0)
        {
            heuristicRunnerIndex = UnityEngine.Random.Range(0, count);
            Debug.Log($"Runner {heuristicRunnerIndex} will be in heuristic mode");
        }
        
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
                
                // If this is the chosen heuristic runner, set behavior type to heuristic
                if (i == heuristicRunnerIndex)
                {
                    behaviorParams.BehaviorType = Unity.MLAgents.Policies.BehaviorType.HeuristicOnly;
                    runner.name = "Runner_Heuristic";
                    Debug.Log("Runner set to heuristic mode");
                }
            }
            
            // Add item counter to agent camera
            Transform cameraTransform = runner.transform.Find("AgentCamera");
            if (cameraTransform != null)
            {
                cameraTransform.gameObject.AddComponent<AgentItemCounter>();
            }
            
            runners.Add(runner);
            
            // After creating a runner
            AgentCameraController cameraController = runner.GetComponentInChildren<AgentCameraController>();
            if (cameraController != null && CameraSystemManager.Instance != null)
            {
                CameraSystemManager.Instance.RegisterAgentCamera(cameraController);
            }
        }
    }

    // Spawn taggers with behavior parameters
    private void SpawnTaggers(int count)
    {
        // Select one random tagger for heuristic mode if enabled
        int heuristicTaggerIndex = -1;
        if (heuristicMode == 2 && count > 0)
        {
            heuristicTaggerIndex = UnityEngine.Random.Range(0, count);
            Debug.Log($"Tagger {heuristicTaggerIndex} will be in heuristic mode");
        }
        
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
                
                // If this is the chosen heuristic tagger, set behavior type to heuristic
                if (i == heuristicTaggerIndex)
                {
                    behaviorParams.BehaviorType = Unity.MLAgents.Policies.BehaviorType.HeuristicOnly;
                    tagger.name = "Tagger_Heuristic";
                    Debug.Log("Tagger set to heuristic mode");
                }
            }
            
            // Add item counter to agent camera
            Transform cameraTransform = tagger.transform.Find("AgentCamera");
            if (cameraTransform != null)
            {
                cameraTransform.gameObject.AddComponent<AgentItemCounter>();
            }
            
            taggers.Add(tagger);
            
            // After creating a tagger
            AgentCameraController cameraController = tagger.GetComponentInChildren<AgentCameraController>();
            if (cameraController != null && CameraSystemManager.Instance != null)
            {
                CameraSystemManager.Instance.RegisterAgentCamera(cameraController);
            }
        }
    }

    private void SpawnFreezeballs(int count)
    {
        if (freezeballPrefab == null || itemSpawnArea == null) return;
        
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = GetRandomPositionInBounds(itemSpawnArea.bounds);
            GameObject freezeball = Instantiate(freezeballPrefab, spawnPosition, Quaternion.identity);
            
            // Apply size multiplier from current lesson
            if (CurrentLesson != null)
            {
                float sizeMultiplier = CurrentLesson.freezeball_size_multiplier;
                freezeball.transform.localScale *= sizeMultiplier;
                
                // Log the size change for debugging
                Debug.Log($"Freezeball size adjusted with multiplier: {sizeMultiplier}");
            }
            
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
        
        // Only respawn if the game is still active
        if (!IsGameActive) yield break;
        
        Vector3 spawnPosition = GetRandomPositionInBounds(itemSpawnArea.bounds);
        GameObject freezeball = Instantiate(freezeballPrefab, spawnPosition, Quaternion.identity);
        
        // Apply size multiplier from current lesson
        if (CurrentLesson != null)
        {
            float sizeMultiplier = CurrentLesson.freezeball_size_multiplier;
            freezeball.transform.localScale *= sizeMultiplier;
        }
        
        freezeballs.Add(freezeball);
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
            
            // Give victory reward to all runners
            foreach (var runner in runners)
            {
                if (runner != null)
                {
                    RunnerAgent runnerAgent = runner.GetComponent<RunnerAgent>();
                    if (runnerAgent != null)
                    {
                        // Team victory reward
                        runnerAgent.AddReward(3.0f);
                        Debug.Log("Rewarding runner for team victory: +3.0");
                    }
                }
            }
            
            // Give loss penalty to all taggers
            foreach (var tagger in taggers)
            {
                if (tagger != null)
                {
                    TaggerAgent taggerAgent = tagger.GetComponent<TaggerAgent>();
                    if (taggerAgent != null)
                    {
                        // Team loss penalty
                        taggerAgent.AddReward(-2.0f);
                        Debug.Log("Penalizing tagger for team loss: -2.0");
                    }
                }
            }
        }
        else
        {
            CurrentStats.taggersWin++;
            
            // Give victory reward to all taggers
            foreach (var tagger in taggers)
            {
                if (tagger != null)
                {
                    TaggerAgent taggerAgent = tagger.GetComponent<TaggerAgent>();
                    if (taggerAgent != null)
                    {
                        // Team victory reward
                        taggerAgent.AddReward(3.0f);
                        Debug.Log("Rewarding tagger for team victory: +3.0");
                    }
                }
            }
            
            // Give loss penalty to all runners
            foreach (var runner in runners)
            {
                if (runner != null)
                {
                    RunnerAgent runnerAgent = runner.GetComponent<RunnerAgent>();
                    if (runnerAgent != null)
                    {
                        // Team loss penalty
                        runnerAgent.AddReward(-2.0f);
                        Debug.Log("Penalizing runner for team loss: -2.0");
                    }
                }
            }
        }
        
        // Count frozen agents at the end of the episode
        int frozenCount = 0;
        foreach (var runner in runners)
        {
            if (runner != null)
            {
                RunnerAgent runnerAgent = runner.GetComponent<RunnerAgent>();
                if (runnerAgent != null)
                {
                    // Check if the runner is frozen
                    if (runnerAgent.IsFrozen)
                    {
                        frozenCount++;
                    }
                    else
                    {
                        // This is a surviving runner - trigger its OnDisable to report final survival time
                        // (we'll do this by temporarily disabling it and then re-enabling it)
                        bool wasActive = runner.activeSelf;
                        if (wasActive)
                        {
                            runner.SetActive(false);
                            runner.SetActive(true);
                        }
                    }
                }
            }
        }
        CurrentStats.frozenAgentsAtEndOfEpisode = frozenCount;
        
        // Ensure average unfreeze time is 0 if no unfreezes occurred
        if (unfreezeCount == 0)
        {
            CurrentStats.avgUnfreezeTime = 0f;
        }
        
        // Handle the case where shortest survival time was never set (no agent was frozen and unfrozen)
        if (CurrentStats.shortestSurviveFromUnfreeze == float.MaxValue)
        {
            CurrentStats.shortestSurviveFromUnfreeze = 0f;
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
            wallsUsed = CurrentStats.wallsUsed,
            freezeballsUsed = CurrentStats.freezeballsUsed,
            freezeballsHit = CurrentStats.freezeballsHit,
            freezeByTouch = CurrentStats.freezeByTouch,
            frozenAgentsAtEndOfEpisode = CurrentStats.frozenAgentsAtEndOfEpisode,
            totalTimeSpentFreezing = CurrentStats.totalTimeSpentFreezing,
            fastestUnfreeze = CurrentStats.fastestUnfreeze == float.MaxValue ? 0f : CurrentStats.fastestUnfreeze,
            avgUnfreezeTime = CurrentStats.avgUnfreezeTime,
            totalWallHitsToTagger = CurrentStats.totalWallHitsToTagger,
            totalWallHitsToFreezeBallProjectile = CurrentStats.totalWallHitsToFreezeBallProjectile,
            totalUnsuccessfulUnfreezeAttempts = CurrentStats.totalUnsuccessfulUnfreezeAttempts,
            longestSurviveFromUnfreeze = CurrentStats.longestSurviveFromUnfreeze,
            shortestSurviveFromUnfreeze = CurrentStats.shortestSurviveFromUnfreeze
        };
        
        // Add to our episode stats list
        episodeStats.Add(episodeStat);
        
        // Export metrics every 5 episodes
        if (CurrentEpisode % 5 == 0)
        {
            ExportResultsToCSV(false);
        }
        
        OnScoreUpdate?.Invoke(CurrentStats.runnersWin, CurrentStats.taggersWin);
        OnEpisodeEnd?.Invoke(CurrentStats);
        OnGameEnd?.Invoke();
        
        // Check if Academy has changed the lesson
        CheckAcademyLessonUpdate();
        
        // Start new episode after a delay
        Invoke(nameof(StartNewEpisode), 2f);
    }

    // Check if the Academy has updated the lesson parameter and apply if needed
    private void CheckAcademyLessonUpdate()
    {
        if (Unity.MLAgents.Academy.IsInitialized)
        {
            try
            {
                var academyParameters = Academy.Instance.EnvironmentParameters;
                float lessonParam = academyParameters.GetWithDefault("lesson", -1);
                
                if (lessonParam != -1)
                {
                    int newLessonIndex = Mathf.FloorToInt(lessonParam);
                    
                    if (newLessonIndex != currentLessonIndex)
                    {
                        Debug.Log($"Academy changed lesson from {currentLessonIndex} to {newLessonIndex} after episode {CurrentEpisode}");
                        currentLessonIndex = newLessonIndex;
                        LoadLessonConfiguration();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error checking Academy lesson parameter: {e.Message}");
            }
        }
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
    }

    public void NotifyWallBallCollected()
    {
        CurrentStats.wallballsCollected++;
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

    public void NotifyFreezeByTouch()
    {
        CurrentStats.freezeByTouch++;
    }

    public void AddFreezeTime(float freezeTime)
    {
        CurrentStats.totalTimeSpentFreezing += freezeTime;
    }

    public void CheckFastestUnfreeze(float unfreezeTime)
    {
        // Only track valid unfreeze times (greater than zero)
        if (unfreezeTime > 0)
        {
            // Update fastest unfreeze time if this one was faster
            if (unfreezeTime < CurrentStats.fastestUnfreeze)
            {
                CurrentStats.fastestUnfreeze = unfreezeTime;
            }
            
            // Add to total unfreeze time for average calculation
            totalUnfreezeTime += unfreezeTime;
            unfreezeCount++;
            
            // Calculate average unfreeze time
            if (unfreezeCount > 0)
            {
                CurrentStats.avgUnfreezeTime = totalUnfreezeTime / unfreezeCount;
            }
        }
    }

    public void NotifyWallHitByTagger()
    {
        CurrentStats.totalWallHitsToTagger++;
        Debug.Log($"Wall hit by tagger! Total hits: {CurrentStats.totalWallHitsToTagger}");
    }

    public void NotifyWallHitByFreezeBallProjectile()
    {
        CurrentStats.totalWallHitsToFreezeBallProjectile++;
        Debug.Log($"Wall hit by freeze ball projectile! Total hits: {CurrentStats.totalWallHitsToFreezeBallProjectile}");
    }

    public void NotifyUnsuccessfulUnfreezeAttempt()
    {
        CurrentStats.totalUnsuccessfulUnfreezeAttempts++;
    }

    private void ExportResultsToCSV(bool isFinalExport = true)
    {
        StringBuilder sb = new StringBuilder();
        
        // Add headers
        sb.AppendLine("Episode,RunnerWin,TaggerWin,Time,FreezeBallsCollected,WallBallsCollected,TotalFreezes,TotalUnfreezes,EpisodeLength,WallsUsed,FreezeBallsUsed,FreezeBallsHit,FreezeByTouch,FrozenAgentsAtEndOfEpisode,TotalTimeSpentFreezing,FastestUnfreeze,AvgUnfreezeTime,TotalWallHitsToTagger,TotalWallHitsToFreezeBallProjectile,TotalUnsuccessfulUnfreezeAttempts,LongestSurviveFromUnfreeze,ShortestSurviveFromUnfreeze");
        
        // Add data for each episode
        for (int i = 0; i < episodeStats.Count; i++)
        {
            GameStats stats = episodeStats[i];
            sb.AppendLine($"{i+1},{stats.runnersWin},{stats.taggersWin},{stats.time},{stats.freezeballsCollected},{stats.wallballsCollected},{stats.totalFreezes},{stats.totalUnfreezes},{stats.episodeLength},{stats.wallsUsed},{stats.freezeballsUsed},{stats.freezeballsHit},{stats.freezeByTouch},{stats.frozenAgentsAtEndOfEpisode},{stats.totalTimeSpentFreezing},{stats.fastestUnfreeze},{stats.avgUnfreezeTime},{stats.totalWallHitsToTagger},{stats.totalWallHitsToFreezeBallProjectile},{stats.totalUnsuccessfulUnfreezeAttempts},{stats.longestSurviveFromUnfreeze},{stats.shortestSurviveFromUnfreeze}");
        }
        
        // Add summary row
        sb.AppendLine("------- Summary -------");
        sb.AppendLine($"Total,{CurrentStats.runnersWin},{CurrentStats.taggersWin},{CurrentStats.time},{CurrentStats.freezeballsCollected},{CurrentStats.wallballsCollected},{CurrentStats.totalFreezes},{CurrentStats.totalUnfreezes},{CurrentStats.episodeLength},{CurrentStats.wallsUsed},{CurrentStats.freezeballsUsed},{CurrentStats.freezeballsHit},{CurrentStats.freezeByTouch},{CurrentStats.frozenAgentsAtEndOfEpisode},{CurrentStats.totalTimeSpentFreezing},{CurrentStats.fastestUnfreeze},{CurrentStats.avgUnfreezeTime},{CurrentStats.totalWallHitsToTagger},{CurrentStats.totalWallHitsToFreezeBallProjectile},{CurrentStats.totalUnsuccessfulUnfreezeAttempts},{CurrentStats.longestSurviveFromUnfreeze},{CurrentStats.shortestSurviveFromUnfreeze}");
        
        try
        {
            // Write to the same file each time
            File.WriteAllText(csvFilePath, sb.ToString());
            
            if (isFinalExport)
            {
                Debug.Log($"Final results exported to {csvFilePath}");
            }
            else
            {
                Debug.Log($"Progress checkpoint exported to {csvFilePath} (Episode {CurrentEpisode})");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to export results: {e.Message}");
        }
    }
    
    // Helper function to generate a random hash
    private string GenerateRandomHash(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        
        char[] hash = new char[length];
        for (int i = 0; i < length; i++)
        {
            hash[i] = chars[random.Next(chars.Length)];
        }
        
        return new string(hash);
    }
    
    // Add method to return to main menu
    public void ReturnToMainMenu()
    {
        // Export final results before leaving
        ExportResultsToCSV(true);
        
        // Clean up the singleton instance
        Instance = null;
        
        // Load main menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void UpdateSurvivalTimeAfterFreeze(float survivalTime)
    {
        // Update longest survival time if this one is longer
        if (survivalTime > CurrentStats.longestSurviveFromUnfreeze)
        {
            CurrentStats.longestSurviveFromUnfreeze = survivalTime;
        }
        
        // Update shortest survival time if this one is shorter
        // and is a valid time (greater than zero)
        if (survivalTime > 0 && survivalTime < CurrentStats.shortestSurviveFromUnfreeze)
        {
            CurrentStats.shortestSurviveFromUnfreeze = survivalTime;
        }
    }
    
    // Make sure to clean up the singleton reference when the GameManager is destroyed
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Add this method to your GameManager
    public void ClearModelReferences()
    {
        PlayerPrefs.DeleteKey("RunnerAgentModel");
        PlayerPrefs.DeleteKey("TaggerAgentModel");
        PlayerPrefs.Save();
        Debug.Log("Cleared model references from PlayerPrefs");
    }
} 
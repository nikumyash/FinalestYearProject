using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown lessonDropdown;
    [SerializeField] private TMP_InputField runnerModelInput;
    [SerializeField] private TMP_InputField taggerModelInput;
    [SerializeField] private Button playButton;
    
    // Lesson configuration
    private List<string> lessonNames = new List<string>();
    
    private void Start()
    {
        // Load lesson names from the environment_param.json file
        LoadLessonNames();
        
        // Set up dropdown with lesson options
        if (lessonDropdown != null)
        {
            lessonDropdown.ClearOptions();
            List<string> options = new List<string>();
            
            // Add each lesson name to the dropdown
            for (int i = 0; i < lessonNames.Count; i++)
            {
                options.Add(lessonNames[i]);
            }
            
            lessonDropdown.AddOptions(options);
            
            // Select previous value if available
            if (PlayerPrefs.HasKey("LessonDropdownIndex"))
            {
                int savedIndex = PlayerPrefs.GetInt("LessonDropdownIndex", 0);
                lessonDropdown.value = Mathf.Clamp(savedIndex, 0, options.Count - 1);
            }
        }
        
        // Load previous values for inputs if available
        if (runnerModelInput != null && PlayerPrefs.HasKey("RunnerAgentModel"))
        {
            runnerModelInput.text = PlayerPrefs.GetString("RunnerAgentModel", "");
        }
        
        if (taggerModelInput != null && PlayerPrefs.HasKey("TaggerAgentModel"))
        {
            taggerModelInput.text = PlayerPrefs.GetString("TaggerAgentModel", "");
        }
        
        // Set up play button
        if (playButton != null)
        {
            playButton.onClick.AddListener(StartGame);
        }
    }
    
    private void LoadLessonNames()
    {
        lessonNames.Clear();
        
        try
        {
            // Load the JSON file
            TextAsset jsonFile = Resources.Load<TextAsset>("environment_param");
            if (jsonFile != null)
            {
                // Parse the JSON
                GameManager.EnvironmentConfig config = JsonUtility.FromJson<GameManager.EnvironmentConfig>(jsonFile.text);
                
                // Extract lesson names
                if (config.lessons != null && config.lessons.Count > 0)
                {
                    foreach (var lesson in config.lessons)
                    {
                        lessonNames.Add(lesson.name);
                    }
                    
                    Debug.Log($"Loaded {lessonNames.Count} lesson names from environment_param.json");
                }
                else
                {
                    Debug.LogError("No lessons found in environment_param.json");
                    // Add a default lesson name
                    lessonNames.Add("Default Lesson");
                }
            }
            else
            {
                Debug.LogError("Failed to load environment_param.json");
                // Add a default lesson name
                lessonNames.Add("Default Lesson");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading lesson names: {e.Message}");
            // Add a default lesson name
            lessonNames.Add("Default Lesson");
        }
    }
    
    public void StartGame()
    {
        // Save selected values
        if (lessonDropdown != null)
        {
            int selectedIndex = lessonDropdown.value;
            PlayerPrefs.SetInt("LessonDropdownIndex", selectedIndex);
            
            // Store the lesson index directly
            PlayerPrefs.SetInt("LessonType", selectedIndex);
            Debug.Log($"Selected Lesson: {lessonNames[selectedIndex]} (index: {selectedIndex})");
        }
        
        if (runnerModelInput != null && !string.IsNullOrEmpty(runnerModelInput.text))
        {
            PlayerPrefs.SetString("RunnerAgentModel", runnerModelInput.text);
            Debug.Log($"Runner Agent Model: {runnerModelInput.text}");
        }
        
        if (taggerModelInput != null && !string.IsNullOrEmpty(taggerModelInput.text))
        {
            PlayerPrefs.SetString("TaggerAgentModel", taggerModelInput.text);
            Debug.Log($"Tagger Agent Model: {taggerModelInput.text}");
        }
        
        // Save preferences and load game scene
        PlayerPrefs.Save();
        SceneManager.LoadScene("Game");
    }
}
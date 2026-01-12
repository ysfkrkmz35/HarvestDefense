using UnityEngine;

/// <summary>
/// Game Systems Bootstrap
/// Creates all required managers and UI elements on startup
/// Attach this to a single GameObject in your scene to initialize everything
/// </summary>
public class GameSystemsBootstrap : MonoBehaviour
{
    #region ═══════ SERIALIZED FIELDS ═══════

    [Header("═══ SCENE SETTINGS ═══")]
    [Tooltip("Scene to load after login")]
    [SerializeField] private string gameSceneName = "Game_Main_Scene";

    [Header("═══ WHAT TO CREATE ═══")]
    [SerializeField] private bool createUserDataManager = true;
    [SerializeField] private bool createDaysSurvivedTracker = true;
    [SerializeField] private bool createLeaderboardManager = true;
    [Tooltip("Disable this - login is now integrated into main menu")]
    [SerializeField] private bool createLoginUI = false;
    [SerializeField] private bool createDaysHUD = true;
    [SerializeField] private bool createLeaderboardPanel = true;

    [Header("═══ UI SETTINGS ═══")]
    [Tooltip("Show login screen on start (set false if this is game scene)")]
    [SerializeField] private bool showLoginOnStart = true;

    [Header("═══ DEBUG ═══")]
    [SerializeField] private bool showDebugLogs = true;

    #endregion

    #region ═══════ RUNTIME REFERENCES ═══════

    private GameObject managersObj;
    private LoginUICreator loginCreator;
    private DaysHUDCreator daysCreator;
    private LeaderboardPanelCreator leaderboardCreator;

    #endregion

    #region ═══════ UNITY LIFECYCLE ═══════

    private void Awake()
    {
        // Create managers container
        if (createUserDataManager || createDaysSurvivedTracker || createLeaderboardManager)
        if (createUserDataManager || createDaysSurvivedTracker || createLeaderboardManager)
        {
            // 1. Try to use the existing persistent container if UserDataManager exists
            if (UserDataManager.Instance != null)
            {
                managersObj = UserDataManager.Instance.gameObject;
                if (showDebugLogs) Debug.Log($"[Bootstrap] ♻️ Reusing existing GameManagers from UserDataManager: '{managersObj.name}'");
            }
            // 2. Otherwise try to find by name (fallback)
            else
            {
                managersObj = GameObject.Find("GameManagers");
                if (managersObj == null)
                {
                    managersObj = new GameObject("GameManagers");
                    DontDestroyOnLoad(managersObj);
                    if (showDebugLogs) Debug.Log("[Bootstrap] 🆕 Created new GameManagers container");
                }
                else
                {
                    if (showDebugLogs) Debug.Log($"[Bootstrap] 🔍 Found existing GameManagers by name: '{managersObj.name}'");
                }
            }
        }

        // Create managers
        if (createUserDataManager && UserDataManager.Instance == null)
        {
            managersObj.AddComponent<UserDataManager>();
            if (showDebugLogs) Debug.Log("[Bootstrap] ✅ UserDataManager created");
        }

        if (createDaysSurvivedTracker)
        {
            if (DaysSurvivedTracker.Instance == null)
            {
                managersObj.AddComponent<DaysSurvivedTracker>();
                if (showDebugLogs) Debug.Log("[Bootstrap] ✅ DaysSurvivedTracker created");
            }
            else
            {
                // CRITICAL FIX: Check if the existing instance is on the correct persistent object
                if (UserDataManager.Instance != null && DaysSurvivedTracker.Instance.gameObject != UserDataManager.Instance.gameObject)
                {
                    Debug.LogWarning($"[Bootstrap] 🚨 Found Rogue DaysSurvivedTracker on '{DaysSurvivedTracker.Instance.gameObject.name}'. PROACTIVELY DESTROYING IT.");
                    
                    // Destroy the rogue component immediately so the new one can claim the Singleton Throne
                    DestroyImmediate(DaysSurvivedTracker.Instance);
                    
                    // Creates the new valid one on the correct persistent object (managersObj)
                    managersObj.AddComponent<DaysSurvivedTracker>();
                    Debug.Log("[Bootstrap] ♻️ Re-created DaysSurvivedTracker on valid persistent object.");
                }
                else
                {
                    // Verify the existing instance (it's on the right object, just make sure it's enabled)
                    Debug.LogWarning($"[Bootstrap] ⚠️ DaysSurvivedTracker already exists on object: '{DaysSurvivedTracker.Instance.gameObject.name}'");
                    if (!DaysSurvivedTracker.Instance.enabled)
                    {
                        Debug.LogWarning("[Bootstrap] ⚠️ Existing DaysSurvivedTracker was DISABLED! Enabling it now.");
                        DaysSurvivedTracker.Instance.enabled = true;
                    }
                }
            }
        }

        if (createLeaderboardManager && LeaderboardManager.Instance == null)
        {
            managersObj.AddComponent<LeaderboardManager>();
            if (showDebugLogs) Debug.Log("[Bootstrap] ✅ LeaderboardManager created");
        }
    }

    private void Start()
    {
        // Create UI elements
        if (createLoginUI)
        {
            CreateLoginUI();
        }

        if (createDaysHUD)
        {
            CreateDaysHUD();
        }

        if (createLeaderboardPanel)
        {
            CreateLeaderboardPanel();
        }

        // Apply saved data to game state if user is logged in
        if (UserDataManager.Instance != null && UserDataManager.Instance.IsLoggedIn)
        {
            if (showDebugLogs) Debug.Log("[Bootstrap] 📂 Waiting for systems before restoring progress...");
            StartCoroutine(ApplyGameStateDelayed());
        }

        if (showDebugLogs)
        {
            Debug.Log("[Bootstrap] ✅ All game systems initialized!");
        }

        // Dump ItemDatabase for debugging
        if (HappyHarvest.GameManager.Instance != null && HappyHarvest.GameManager.Instance.ItemDatabase != null)
        {
            Debug.Log($"[Bootstrap] 📚 ItemDatabase contains {HappyHarvest.GameManager.Instance.ItemDatabase.Entries.Count} items:");
            foreach (var item in HappyHarvest.GameManager.Instance.ItemDatabase.Entries)
            {
                if (item != null)
                    Debug.Log($"  - ID: '{item.UniqueID}' Name: '{item.name}'");
            }
        }
    }

    private System.Collections.IEnumerator ApplyGameStateDelayed()
    {
        // Wait a frame for all Start() methods to complete
        yield return null;
        
        // Wait until PlayerProgression is ready (max 2 seconds)
        float timeout = 2f;
        while ((PlayerProgression.Instance == null || HappyHarvest.UIHandler.Instance == null) && timeout > 0)
        {
            yield return new WaitForSeconds(0.1f);
            timeout -= 0.1f;
        }

        if (PlayerProgression.Instance == null) Debug.LogWarning("[Bootstrap] ⚠️ PlayerProgression missing after timeout");
        if (HappyHarvest.UIHandler.Instance == null) Debug.LogWarning("[Bootstrap] ⚠️ UIHandler missing after timeout");

        if (UserDataManager.Instance != null && UserDataManager.Instance.IsLoggedIn)
        {
            UserDataManager.Instance.ApplyToGameState();
            if (showDebugLogs) Debug.Log("[Bootstrap] ✅ Saved progress restored!");
        }
    }

    #endregion

    #region ═══════ UI CREATION ═══════

    private void CreateLoginUI()
    {
        // Skip login UI if user is already logged in
        if (UserDataManager.Instance != null && UserDataManager.Instance.IsLoggedIn)
        {
            if (showDebugLogs) Debug.Log("[Bootstrap] ⏭️ User already logged in, skipping login UI");
            
            // Apply saved data to game state
            UserDataManager.Instance.ApplyToGameState();
            return;
        }

        var loginObj = new GameObject("LoginUICreator");
        loginObj.transform.SetParent(transform);

        loginCreator = loginObj.AddComponent<LoginUICreator>();

        if (!showLoginOnStart)
        {
            // Hide login if not needed
            loginCreator.Hide();
        }

        if (showDebugLogs) Debug.Log("[Bootstrap] ✅ Login UI created");
    }

    private void CreateDaysHUD()
    {
        var hudObj = new GameObject("DaysHUDCreator");
        hudObj.transform.SetParent(transform);

        daysCreator = hudObj.AddComponent<DaysHUDCreator>();

        if (showDebugLogs) Debug.Log("[Bootstrap] ✅ Days HUD created");
    }

    private void CreateLeaderboardPanel()
    {
        var lbObj = new GameObject("LeaderboardPanelCreator");
        lbObj.transform.SetParent(transform);

        leaderboardCreator = lbObj.AddComponent<LeaderboardPanelCreator>();

        if (showDebugLogs) Debug.Log("[Bootstrap] ✅ Leaderboard Panel created");
    }

    #endregion

    #region ═══════ PUBLIC ACCESS ═══════

    public void ShowLogin()
    {
        loginCreator?.Show();
    }

    public void ShowLeaderboard()
    {
        leaderboardCreator?.Show();
    }

    public void ToggleLeaderboard()
    {
        leaderboardCreator?.Toggle();
    }

    #endregion

    #region ═══════ EDITOR TESTS ═══════

    [ContextMenu("🚀 Reinitialize All Systems")]
    private void ReinitializeAll()
    {
        // This would need scene reload for proper reinitialization
        Debug.Log("[Bootstrap] Please reload the scene to reinitialize");
    }

    #endregion
}

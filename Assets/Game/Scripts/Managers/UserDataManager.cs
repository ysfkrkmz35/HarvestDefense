using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using HappyHarvest;

/// <summary>
/// User Data Manager
/// Singleton that handles all user data persistence
/// - Save/Load via PlayerPrefs with JSON serialization
/// - Username validation
/// - Auto-save on key events
/// </summary>
public class UserDataManager : MonoBehaviour
{
    #region ═══════ SINGLETON ═══════

    public static UserDataManager Instance { get; private set; }

    #endregion

    #region ═══════ CONSTANTS ═══════

    private const string SAVE_FOLDER_NAME = "SaveData";
    private const string USERS_LIST_FILE = "users_list.json";
    private const string LEADERBOARD_FILE = "leaderboard.json";
    private const int MIN_USERNAME_LENGTH = 3;
    private const int MAX_USERNAME_LENGTH = 15;
    private const string VALID_USERNAME_PATTERN = @"^[a-zA-Z0-9_]+$";

    /// <summary>Path to save data folder (next to game executable or in persistent data)</summary>
    private static string SaveFolderPath
    {
        get
        {
            // Use persistent data path for broader compatibility
            string basePath = Application.persistentDataPath;
            return Path.Combine(basePath, SAVE_FOLDER_NAME);
        }
    }

    #endregion

    #region ═══════ SERIALIZED FIELDS ═══════

    [Header("═══ AUTO-SAVE SETTINGS ═══")]
    [Tooltip("Enable auto-save on day start")]
    [SerializeField] private bool autoSaveOnDayStart = true;

    [Tooltip("Enable auto-save on spell unlock")]
    [SerializeField] private bool autoSaveOnSpellUnlock = true;

    [Tooltip("Enable periodic auto-save")]
    [SerializeField] private bool enablePeriodicSave = true;

    [Tooltip("Auto-save interval in seconds")]
    [SerializeField] private float autoSaveInterval = 60f;

    [Header("═══ DEBUG ═══")]
    [SerializeField] private bool showDebugLogs = true;

    #endregion

    #region ═══════ RUNTIME STATE ═══════

    /// <summary>Currently logged in user data</summary>
    public UserSaveData CurrentUser { get; private set; }

    /// <summary>Current username (null if not logged in)</summary>
    public string CurrentUsername => CurrentUser?.username;

    /// <summary>Is a user currently logged in?</summary>
    public bool IsLoggedIn => CurrentUser != null;

    private Coroutine autoSaveCoroutine;

    /// <summary>Flag to prevent auto-saving before game state is restored</summary>
    private bool gameStateRestored = false;

    #endregion

    #region ═══════ EVENTS ═══════

    /// <summary>Fired when a user logs in. Parameter: user data</summary>
    public static event Action<UserSaveData> OnUserLoggedIn;

    /// <summary>Fired when game is saved. Parameter: user data</summary>
    public static event Action<UserSaveData> OnGameSaved;

    #endregion

    #region ═══════ UNITY LIFECYCLE ═══════

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
    }

    private void Start()
    {
        // Subscribe to save triggers
        if (autoSaveOnDayStart)
        {
            GameManager.OnDayStart += OnDayStart;
        }

        if (autoSaveOnSpellUnlock)
        {
            SpellManager.OnSpellUnlocked += OnSpellUnlocked;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            // Unsubscribe
            GameManager.OnDayStart -= OnDayStart;
            SpellManager.OnSpellUnlocked -= OnSpellUnlocked;
            Instance = null;
        }
    }

    #endregion

    #region ═══════ USERNAME VALIDATION ═══════

    /// <summary>
    /// Validation result with error message if invalid
    /// </summary>
    public struct ValidationResult
    {
        public bool isValid;
        public string errorMessage;

        public static ValidationResult Valid() => new ValidationResult { isValid = true, errorMessage = null };
        public static ValidationResult Invalid(string error) => new ValidationResult { isValid = false, errorMessage = error };
    }

    /// <summary>
    /// Validate a username for registration
    /// Checks length, characters, and uniqueness
    /// </summary>
    public ValidationResult ValidateUsername(string username)
    {
        // Null/empty check
        if (string.IsNullOrWhiteSpace(username))
        {
            return ValidationResult.Invalid("Username cannot be empty");
        }

        // Trim whitespace
        username = username.Trim();

        // Length check
        if (username.Length < MIN_USERNAME_LENGTH)
        {
            return ValidationResult.Invalid($"Username must be at least {MIN_USERNAME_LENGTH} characters");
        }

        if (username.Length > MAX_USERNAME_LENGTH)
        {
            return ValidationResult.Invalid($"Username cannot exceed {MAX_USERNAME_LENGTH} characters");
        }

        // Character validation (alphanumeric + underscore)
        if (!Regex.IsMatch(username, VALID_USERNAME_PATTERN))
        {
            return ValidationResult.Invalid("Username can only contain letters, numbers, and underscores");
        }

        return ValidationResult.Valid();
    }

    #endregion

    #region ═══════ USER EXISTENCE CHECK ═══════

    /// <summary>
    /// Check if a username already exists
    /// </summary>
    public bool UserExists(string username)
    {
        if (string.IsNullOrEmpty(username)) return false;

        string filePath = GetUserFilePath(username);
        return File.Exists(filePath);
    }

    /// <summary>Get the file path for a user's save data</summary>
    private string GetUserFilePath(string username)
    {
        return Path.Combine(SaveFolderPath, $"{username.ToLower()}.json");
    }

    /// <summary>Get the file path for users list</summary>
    private string GetUsersListPath()
    {
        return Path.Combine(SaveFolderPath, USERS_LIST_FILE);
    }

    /// <summary>Ensure save folder exists</summary>
    private void EnsureSaveFolderExists()
    {
        if (!Directory.Exists(SaveFolderPath))
        {
            Directory.CreateDirectory(SaveFolderPath);
            if (showDebugLogs) Debug.Log($"[UserDataManager] 📁 Created save folder: {SaveFolderPath}");
        }
    }

    /// <summary>
    /// Get list of all registered usernames
    /// </summary>
    public List<string> GetAllUsernames()
    {
        string filePath = GetUsersListPath();
        if (!File.Exists(filePath))
        {
            return new List<string>();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var wrapper = JsonUtility.FromJson<UsernameListWrapper>(json);
            return wrapper?.usernames ?? new List<string>();
        }
        catch (Exception e)
        {
            Debug.LogError($"[UserDataManager] ❌ Failed to read users list: {e.Message}");
            return new List<string>();
        }
    }

    [System.Serializable]
    private class UsernameListWrapper
    {
        public List<string> usernames = new List<string>();
    }

    private void AddUsernameToList(string username)
    {
        EnsureSaveFolderExists();
        var usernames = GetAllUsernames();
        if (!usernames.Contains(username.ToLower()))
        {
            usernames.Add(username.ToLower());
            var wrapper = new UsernameListWrapper { usernames = usernames };
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(GetUsersListPath(), json);
        }
    }

    #endregion

    #region ═══════ USER CREATE/LOAD ═══════

    /// <summary>
    /// Create a new user account
    /// Returns null if username is invalid or already exists
    /// </summary>
    public UserSaveData CreateNewUser(string username)
    {
        var validation = ValidateUsername(username);
        if (!validation.isValid)
        {
            Debug.LogWarning($"[UserDataManager] ❌ Invalid username: {validation.errorMessage}");
            return null;
        }

        if (UserExists(username))
        {
            Debug.LogWarning($"[UserDataManager] ❌ Username already exists: {username}");
            return null;
        }

        // Create new user data
        var userData = UserSaveData.CreateNew(username);
        
        // Save immediately
        SaveUserData(userData);
        AddUsernameToList(username);

        if (showDebugLogs)
        {
            Debug.Log($"[UserDataManager] ✅ Created new user: {username}");
        }

        return userData;
    }

    /// <summary>
    /// Load user data by username
    /// Returns null if user doesn't exist
    /// </summary>
    public UserSaveData LoadUser(string username)
    {
        if (string.IsNullOrEmpty(username)) return null;

        string filePath = GetUserFilePath(username);
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var userData = JsonUtility.FromJson<UserSaveData>(json);
            if (showDebugLogs)
            {
                Debug.Log($"[UserDataManager] 📂 Loaded user from: {filePath}");
                Debug.Log($"[UserDataManager] 📂 User data: {userData}");
            }
            return userData;
        }
        catch (Exception e)
        {
            Debug.LogError($"[UserDataManager] ❌ Failed to load user file: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Login a user - loads or creates based on existence
    /// </summary>
    public UserSaveData Login(string username)
    {
        username = username?.Trim();

        var validation = ValidateUsername(username);
        if (!validation.isValid)
        {
            Debug.LogWarning($"[UserDataManager] ❌ Login failed: {validation.errorMessage}");
            return null;
        }

        UserSaveData userData;

        if (UserExists(username))
        {
            // Existing user - load data
            userData = LoadUser(username);
            if (userData == null)
            {
                Debug.LogError($"[UserDataManager] ❌ Failed to load existing user: {username}");
                return null;
            }
            // For existing users, gameStateRestored stays false until ApplyToGameState is called
            gameStateRestored = false;
            if (showDebugLogs)
            {
                Debug.Log($"[UserDataManager] 🔓 Welcome back, {username}!");
            }
        }
        else
        {
            // New user - create account
            userData = CreateNewUser(username);
            if (userData == null)
            {
                return null;
            }
            // New users don't have data to restore, so auto-saving can start immediately
            gameStateRestored = true;
            if (showDebugLogs)
            {
                Debug.Log($"[UserDataManager] 🆕 Welcome, {username}! New account created.");
            }
        }

        // Set as current user
        CurrentUser = userData;
        userData.UpdateLastPlayed();

        // Start auto-save coroutine
        if (enablePeriodicSave && autoSaveCoroutine == null)
        {
            autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());
        }

        OnUserLoggedIn?.Invoke(userData);
        return userData;
    }

    #endregion

    #region ═══════ SAVE SYSTEM ═══════

    /// <summary>
    /// Save user data to PlayerPrefs
    /// </summary>
    public void SaveUserData(UserSaveData userData)
    {
        if (userData == null) return;

        EnsureSaveFolderExists();
        string filePath = GetUserFilePath(userData.username);
        string json = JsonUtility.ToJson(userData, true);
        
        try
        {
            File.WriteAllText(filePath, json);
            if (showDebugLogs)
            {
                Debug.Log($"[UserDataManager] 💾 Saved to: {filePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[UserDataManager] ❌ Failed to save: {e.Message}");
        }
    }

    /// <summary>
    /// Save current user's game state
    /// </summary>
    public void SaveCurrentUser()
    {
        if (CurrentUser == null)
        {
            Debug.LogWarning("[UserDataManager] ⚠️ No user logged in, cannot save");
            return;
        }

        CurrentUser.CollectFromCurrentGameState();
        SaveUserData(CurrentUser);
        
        // Update leaderboard file
        UpdateLeaderboard();
        
        OnGameSaved?.Invoke(CurrentUser);
    }

    /// <summary>
    /// Auto-save coroutine for periodic saves
    /// </summary>
    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);
            
            if (IsLoggedIn)
            {
                if (showDebugLogs)
                {
                    Debug.Log("[UserDataManager] ⏰ Periodic auto-save triggered");
                }
                SaveCurrentUser();
            }
        }
    }

    #endregion

    #region ═══════ EVENT HANDLERS ═══════

    private void OnDayStart()
    {
        // Don't auto-save until game state is restored (prevents overwriting saved data with defaults)
        if (!gameStateRestored)
        {
            if (showDebugLogs) Debug.Log("[UserDataManager] ⏸️ Skipping auto-save - waiting for game state restore");
            return;
        }

        if (IsLoggedIn && autoSaveOnDayStart)
        {
            if (showDebugLogs)
            {
                Debug.Log("[UserDataManager] 🌅 Day started - auto-saving");
            }
            SaveCurrentUser();
        }
    }

    private void OnSpellUnlocked(SpellData spell)
    {
        if (IsLoggedIn && autoSaveOnSpellUnlock)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[UserDataManager] ✨ Spell unlocked ({spell?.spellName}) - auto-saving");
            }
            SaveCurrentUser();
        }
    }

    #endregion

    #region ═══════ LEADERBOARD DATA ═══════

    /// <summary>
    /// Leaderboard entry for a single user
    /// </summary>
    [System.Serializable]
    public class LeaderboardEntry
    {
        public string nickname;
        public int level;
        public int coins;
        public int daysSurvived;
        public List<string> unlockedSpells;
        public string lastPlayed;
    }

    /// <summary>
    /// Wrapper for leaderboard JSON
    /// </summary>
    [System.Serializable]
    public class LeaderboardData
    {
        public string lastUpdated;
        public List<LeaderboardEntry> players;
    }

    /// <summary>
    /// Get all users' save data for leaderboard
    /// </summary>
    public List<UserSaveData> GetAllUsers()
    {
        var users = new List<UserSaveData>();
        var usernames = GetAllUsernames();

        foreach (var username in usernames)
        {
            var userData = LoadUser(username);
            if (userData != null)
            {
                users.Add(userData);
            }
        }

        return users;
    }

    /// <summary>
    /// Update the leaderboard.json file with all users' data
    /// Called automatically on every save
    /// </summary>
    public void UpdateLeaderboard()
    {
        EnsureSaveFolderExists();
        
        var leaderboard = new LeaderboardData
        {
            lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            players = new List<LeaderboardEntry>()
        };

        // Load all users and create leaderboard entries
        var allUsers = GetAllUsers();
        foreach (var user in allUsers)
        {
            var entry = new LeaderboardEntry
            {
                nickname = user.username,
                level = user.level,
                coins = user.gold,
                daysSurvived = user.daysSurvived,
                unlockedSpells = user.unlockedSpellIds ?? new List<string>(),
                lastPlayed = user.lastPlayedAt
            };
            leaderboard.players.Add(entry);
        }

        // Sort by level descending, then by days survived
        leaderboard.players.Sort((a, b) => 
        {
            int levelCompare = b.level.CompareTo(a.level);
            if (levelCompare != 0) return levelCompare;
            return b.daysSurvived.CompareTo(a.daysSurvived);
        });

        // Write to file
        string filePath = Path.Combine(SaveFolderPath, LEADERBOARD_FILE);
        try
        {
            string json = JsonUtility.ToJson(leaderboard, true);
            File.WriteAllText(filePath, json);
            if (showDebugLogs)
            {
                Debug.Log($"[UserDataManager] 🏆 Leaderboard updated: {filePath} ({leaderboard.players.Count} players)");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[UserDataManager] ❌ Failed to update leaderboard: {e.Message}");
        }
    }

    /// <summary>
    /// Get path to leaderboard file
    /// </summary>
    public string GetLeaderboardPath()
    {
        return Path.Combine(SaveFolderPath, LEADERBOARD_FILE);
    }

    #endregion

    #region ═══════ APPLY TO GAME STATE ═══════

    /// <summary>
    /// Apply saved data to current game state
    /// Call after loading user to restore their progress
    /// </summary>
    public void ApplyToGameState()
    {
        if (CurrentUser == null)
        {
            Debug.LogWarning("[UserDataManager] ⚠️ No user to apply");
            return;
        }

        // Apply to PlayerProgression
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.Instance.SetValues(CurrentUser.level, CurrentUser.currentXP, CurrentUser.gold);
            if (showDebugLogs)
            {
                Debug.Log($"[UserDataManager] 📊 Applied progress: Level {CurrentUser.level}, XP {CurrentUser.currentXP}, Gold {CurrentUser.gold}");
            }
        }

        // Also apply gold to HappyHarvest's coin system
        if (HappyHarvest.GameManager.Instance != null && HappyHarvest.GameManager.Instance.Player != null)
        {
            HappyHarvest.GameManager.Instance.Player.Coins = CurrentUser.gold;
            if (showDebugLogs)
            {
                Debug.Log($"[UserDataManager] 💰 Applied coins to HappyHarvest: {CurrentUser.gold}");
            }
        }

        // Apply to DaysSurvivedTracker
        if (DaysSurvivedTracker.Instance != null)
        {
            DaysSurvivedTracker.Instance.SetDaysSurvived(CurrentUser.daysSurvived);
        }

        // Apply player position
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && (CurrentUser.lastPositionX != 0 || CurrentUser.lastPositionY != 0))
        {
            player.transform.position = new Vector3(CurrentUser.lastPositionX, CurrentUser.lastPositionY, player.transform.position.z);
            if (showDebugLogs)
            {
                Debug.Log($"[UserDataManager] 📍 Restored position: ({CurrentUser.lastPositionX}, {CurrentUser.lastPositionY})");
            }
        }

        // Restore unlocked spells
        if (SpellManager.Instance != null)
        {
            if (CurrentUser.unlockedSpellIds != null && CurrentUser.unlockedSpellIds.Count > 0)
            {
                SpellManager.Instance.SetUnlockedSpellsByName(CurrentUser.unlockedSpellIds);
                
                // Restore equipped slots
                if (CurrentUser.equippedSpellSlots != null && CurrentUser.equippedSpellSlots.Length == 4)
                {
                    SpellManager.Instance.SetEquippedSpellsByName(CurrentUser.equippedSpellSlots);
                    if (showDebugLogs)
                    {
                        Debug.Log($"[UserDataManager] ✨ Restored equipped slots: {string.Join(", ", CurrentUser.equippedSpellSlots)}");
                    }
                }
                
                if (showDebugLogs)
                {
                    Debug.Log($"[UserDataManager] ✨ Restored {CurrentUser.unlockedSpellIds.Count} unlocked spells");
                }
            }
            else
            {
                if (showDebugLogs) Debug.Log("[UserDataManager] ⚠️ No unlocked spells found in save data.");
            }
        }
        else
        {
             Debug.LogError("[UserDataManager] ❌ SpellManager.Instance is NULL! Cannot restore spells.");
        }

        // Restore inventory
        if (HappyHarvest.GameManager.Instance != null && HappyHarvest.GameManager.Instance.Player != null)
        {
            if (CurrentUser.inventoryItems != null && CurrentUser.inventoryItems.Count > 0)
            {
                HappyHarvest.GameManager.Instance.Player.Inventory.Load(CurrentUser.inventoryItems);
                
                // Refresh UI - Force it!
                if (HappyHarvest.UIHandler.Instance != null)
                {
                    HappyHarvest.UIHandler.UpdateInventory(HappyHarvest.GameManager.Instance.Player.Inventory);
                     if (showDebugLogs)
                    {
                        Debug.Log($"[UserDataManager] 📦 Restored {CurrentUser.inventoryItems.Count} inventory slots and updated UI.");
                        
                        // Detailed log
                        int itemCount = 0;
                        foreach(var item in CurrentUser.inventoryItems)
                        {
                            if(item != null) itemCount++;
                        }
                        Debug.Log($"[UserDataManager] 📦 Actual items count in save: {itemCount}");
                    }
                }
                else
                {
                     // Try to find it if Instance is null (unlikely but possible if Awake hasn't run or something)
                     var handler = FindAnyObjectByType<HappyHarvest.UIHandler>();
                     if (handler != null)
                     {
                         HappyHarvest.UIHandler.UpdateInventory(HappyHarvest.GameManager.Instance.Player.Inventory);
                         Debug.Log("[UserDataManager] 📦 Restored inventory and updated UI (found via FindAnyObjectByType).");
                     }
                     else
                     {
                         Debug.LogError("[UserDataManager] ❌ UIHandler not found! Inventory loaded but UI not updated.");
                     }
                }
            }
            else
            {
                 if (showDebugLogs) Debug.Log("[UserDataManager] ⚠️ Inventory list in save data is empty.");
            }
        }
        else
        {
            Debug.LogError("[UserDataManager] ❌ GameManager or Player is NULL! Cannot restore inventory.");
        }

        // Mark game state as restored - now auto-saving is allowed
        gameStateRestored = true;
        if (showDebugLogs)
        {
            Debug.Log("[UserDataManager] ✅ Game state restored - auto-saving now enabled");
        }
    }

    #endregion

    #region ═══════ EDITOR TESTS ═══════

    [ContextMenu("💾 Test: Save Current User")]
    private void TestSave()
    {
        SaveCurrentUser();
    }

    [ContextMenu("📊 Test: Print All Users")]
    private void TestPrintUsers()
    {
        var users = GetAllUsers();
        Debug.Log($"[UserDataManager] Total users: {users.Count}");
        foreach (var user in users)
        {
            Debug.Log($"  - {user}");
        }
    }

    [ContextMenu("🗑️ Test: Clear All Data (DANGEROUS)")]
    private void TestClearAllData()
    {
        if (Directory.Exists(SaveFolderPath))
        {
            Directory.Delete(SaveFolderPath, true);
            Debug.Log($"[UserDataManager] 🗑️ Deleted save folder: {SaveFolderPath}");
        }
        else
        {
            Debug.Log("[UserDataManager] ⚠️ No save folder to delete");
        }
    }

    [ContextMenu("📁 Open Save Folder")]
    private void OpenSaveFolder()
    {
        EnsureSaveFolderExists();
        Debug.Log($"[UserDataManager] 📁 Save folder: {SaveFolderPath}");
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.RevealInFinder(SaveFolderPath);
        #endif
    }

    #endregion
}

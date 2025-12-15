using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YG;

/// <summary>
/// Main tutorial system manager
/// Handles quest progression, state tracking, and integration with game systems
/// </summary>
public class TutorialManager : MonoBehaviour
{
    private static TutorialManager instance;
    public static TutorialManager Instance => instance;
    
    [Header("Quest Locations (Exclamation Marks)")]
    [Tooltip("Transform for exclamation mark for quest 1, 4, 6, 9, 10 (workbench)")]
    [SerializeField] private Transform workbenchLocation;
    [Tooltip("Transform for exclamation mark for quest 2, 7 (shop computer)")]
    [SerializeField] private Transform shopComputerLocation;
    [Tooltip("Transform for exclamation mark for quest 3, 8 (part spawn point)")]
    [SerializeField] private Transform partSpawnLocation;
    [Tooltip("Transform for exclamation mark for quest 5 (blowtorch)")]
    [SerializeField] private Transform blowtorchLocation;
    [Tooltip("Transform for exclamation mark for quest 11 (shooting targets)")]
    [SerializeField] private Transform shootingTargetsLocation;
    [Tooltip("Transform for exclamation mark for quest 12 (location door)")]
    [SerializeField] private Transform locationDoorLocation;
    
    [Header("Exclamation Mark Prefab")]
    [SerializeField] private GameObject exclamationMarkPrefab;
    
    [Header("Audio")]
    [Tooltip(@"Audio clips for quests (24 total: 12 en + 12 ru).
Format: [index] = Quest number + language

[0] = 1 en  [1] = 1 ru  [2] = 2 en  [3] = 2 ru
[4] = 3 en  [5] = 3 ru  [6] = 4 en  [7] = 4 ru
[8] = 5 en  [9] = 5 ru  [10] = 6 en  [11] = 6 ru
[12] = 7 en  [13] = 7 ru  [14] = 8 en  [15] = 8 ru
[16] = 9 en  [17] = 9 ru  [18] = 10 en  [19] = 10 ru
[20] = 11 en  [21] = 11 ru  [22] = 12 en  [23] = 12 ru")]
    [SerializeField] private AudioClip[] questAudioClips = new AudioClip[24];
    
    [Header("References")]
    [SerializeField] private TutorialQuestUI questUI;
    
    // State
    private TutorialQuest currentQuest = TutorialQuest.None;
    private bool isInitialized = false;
    private bool tutorialCompleted = false;
    
    // Exclamation marks
    private Dictionary<TutorialQuest, TutorialExclamationMark> exclamationMarks = new Dictionary<TutorialQuest, TutorialExclamationMark>();
    private TutorialExclamationMark currentExclamationMark;
    
    // Quest tracking flags
    private bool quest1Completed = false;
    private bool quest2Completed = false;
    private bool quest3Completed = false;
    private bool quest4Completed = false;
    private bool quest5Completed = false;
    private bool quest6Completed = false;
    private bool quest7Completed = false;
    private bool quest8Completed = false;
    private bool quest9Completed = false;
    private bool quest10Completed = false;
    private bool quest11Completed = false;
    private bool quest12Completed = false;
    
    // Initial money for quest 11 detection
    private int initialMoneyForQuest11 = 0;
    
    // Events
    public System.Action<TutorialQuest> OnQuestStarted;
    public System.Action<TutorialQuest> OnQuestCompleted;
    
    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (instance != null && instance != this)
        {
            Debug.LogWarning("TutorialManager: Another instance exists. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Move to root if parented
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
    }
    
    private void Start()
    {
        // Wait for GameManager and SaveSystemManager initialization
        StartCoroutine(WaitForInitialization());
    }
    
    private IEnumerator WaitForInitialization()
    {
        // Wait for GameManager
        while (GameManager.Instance == null)
        {
            yield return null;
        }
        
        // Wait for GameManager to be initialized
        while (!GameManager.Instance.IsInitialized)
        {
            yield return null;
        }
        
        // Wait for SaveSystemManager to load data
        if (SaveSystemManager.Instance != null)
        {
            while (!SaveSystemManager.Instance.IsLoadComplete)
            {
                yield return null;
            }
        }
        
        // Additional wait to ensure all systems are ready
        yield return new WaitForSeconds(0.1f);
        
        // Now initialize tutorial
        InitializeTutorial();
    }
    
    /// <summary>
    /// Initialize tutorial system after game is fully loaded
    /// </summary>
    private void InitializeTutorial()
    {
        if (isInitialized) return;
        
        Debug.Log("TutorialManager: Initializing tutorial system...");
        
        // Load tutorial progress from save
        LoadTutorialProgress();
        
        // Find TutorialQuestUI if not assigned
        if (questUI == null)
        {
            if (GameplayHUD.Instance != null)
            {
                questUI = GameplayHUD.Instance.GetComponent<TutorialQuestUI>();
            }
            
            if (questUI == null)
            {
                questUI = FindFirstObjectByType<TutorialQuestUI>();
            }
        }
        
        // Auto-find locations if not assigned
        AutoFindLocations();
        
        // Create exclamation marks
        CreateExclamationMarks();
        
        // Subscribe to events for quest tracking
        SubscribeToEvents();
        
        // Start tutorial if needed
        if (!tutorialCompleted)
        {
            if (currentQuest != TutorialQuest.None && currentQuest != TutorialQuest.Completed)
            {
                // Load quest from save - continue from where we left off
                StartQuest(currentQuest);
            }
            else if (currentQuest == TutorialQuest.None)
            {
                // No save data - start from quest 1
                StartQuest(TutorialQuest.CreateGun);
            }
        }
        else
        {
            // Tutorial completed - hide UI
            if (questUI != null)
            {
                questUI.Hide();
            }
            HideAllExclamationMarks();
        }
        
        isInitialized = true;
    }
    
    /// <summary>
    /// Load tutorial progress from save data
    /// </summary>
    private void LoadTutorialProgress()
    {
        if (!YG2.isSDKEnabled || YG2.saves == null)
        {
            Debug.Log("TutorialManager: YG2 not initialized or no save data. Starting fresh tutorial.");
            currentQuest = TutorialQuest.None;
            tutorialCompleted = false;
            return;
        }
        
        int savedQuestIndex = YG2.saves.tutorialQuestIndex;
        
        // Check if this is a new game (no save data)
        // savedQuestIndex will be -1 (None) if no tutorial progress was ever saved
        if (savedQuestIndex == (int)TutorialQuest.None)
        {
            // New game or tutorial not started yet - start from quest 1
            currentQuest = TutorialQuest.None;
            tutorialCompleted = false;
            Debug.Log("TutorialManager: New game detected (no tutorial progress) - starting from quest 1.");
        }
        else if (savedQuestIndex == (int)TutorialQuest.Completed)
        {
            // Tutorial completed (explicitly set to Completed after finishing all quests)
            tutorialCompleted = true;
            currentQuest = TutorialQuest.Completed;
            Debug.Log("TutorialManager: Tutorial already completed.");
        }
        else if (savedQuestIndex >= 0 && savedQuestIndex <= 11)
        {
            // Load checkpoint quest from save
            TutorialQuest checkpointQuest = (TutorialQuest)savedQuestIndex;
            
            // Verify this is actually a checkpoint quest
            if (!IsCheckpointQuest(checkpointQuest))
            {
                // Invalid checkpoint - start fresh
                Debug.LogWarning($"TutorialManager: Saved quest index {savedQuestIndex} is not a checkpoint. Starting fresh.");
                currentQuest = TutorialQuest.None;
                tutorialCompleted = false;
                return;
            }
            
            // Continue from the quest after the checkpoint
            // This is safe because we only save checkpoints that are guaranteed to be completed
            TutorialQuest nextQuest = GetNextQuest(checkpointQuest);
            currentQuest = nextQuest;
            tutorialCompleted = false;
            Debug.Log($"TutorialManager: Loaded checkpoint - Quest {savedQuestIndex + 1} ({checkpointQuest}), continuing from Quest {(int)nextQuest + 1} ({nextQuest})");
        }
        else
        {
            // Invalid save data - start fresh
            currentQuest = TutorialQuest.None;
            tutorialCompleted = false;
            Debug.LogWarning($"TutorialManager: Invalid tutorial quest index in save: {savedQuestIndex}. Starting fresh.");
        }
    }
    
    /// <summary>
    /// Check if a quest is a checkpoint (quests 1, 4, 6, 9, 11, 12)
    /// </summary>
    private bool IsCheckpointQuest(TutorialQuest quest)
    {
        // Checkpoints: CreateGun (0), AttachBarrel (3), WeldBarrel (5), AttachMag (8), ShootTargets (10), EnterRange (11)
        return quest == TutorialQuest.CreateGun || 
               quest == TutorialQuest.AttachBarrel || 
               quest == TutorialQuest.WeldBarrel || 
               quest == TutorialQuest.AttachMag || 
               quest == TutorialQuest.ShootTargets ||
               quest == TutorialQuest.EnterRange;
    }
    
    /// <summary>
    /// Get the last completed checkpoint quest that is guaranteed to be completed
    /// Logic: If current quest is not a checkpoint, return the last checkpoint BEFORE it
    /// (because if we're on a non-checkpoint quest, it means the previous checkpoint was completed)
    /// If current quest is a checkpoint, we haven't completed it yet, so return the previous checkpoint
    /// </summary>
    private TutorialQuest GetLastCheckpointQuest()
    {
        // If tutorial is completed, return Completed
        if (tutorialCompleted || currentQuest == TutorialQuest.Completed)
        {
            return TutorialQuest.Completed;
        }
        
        // Find the last checkpoint BEFORE current quest (not including current quest)
        // This is the checkpoint that is guaranteed to be completed
        TutorialQuest lastCheckpoint = TutorialQuest.None;
        for (int i = 0; i < (int)currentQuest; i++)  // Note: < instead of <= to exclude current quest
        {
            TutorialQuest quest = (TutorialQuest)i;
            if (IsCheckpointQuest(quest))
            {
                lastCheckpoint = quest;
            }
        }
        
        return lastCheckpoint;
    }
    
    /// <summary>
    /// Update tutorial progress in memory (will be saved by autosave system)
    /// Only saves checkpoint quests (1, 4, 6, 9, 11, 12)
    /// Saves the last checkpoint that is guaranteed to be completed (before current quest)
    /// </summary>
    private void SaveTutorialProgress()
    {
        if (!YG2.isSDKEnabled || YG2.saves == null)
        {
            Debug.LogWarning("TutorialManager: Cannot update progress - YG2 not initialized.");
            return;
        }
        
        // Update progress in memory - will be saved by autosave system
        if (tutorialCompleted)
        {
            // Save Completed status (12) to distinguish from new game (-1)
            YG2.saves.tutorialQuestIndex = (int)TutorialQuest.Completed;
        }
        else
        {
            // Save only the last checkpoint quest that is guaranteed to be completed
            // This is the last checkpoint BEFORE current quest (not including current quest)
            TutorialQuest checkpointQuest = GetLastCheckpointQuest();
            if (checkpointQuest == TutorialQuest.None)
            {
                // No checkpoint reached yet, don't save progress (keep as -1 for new game)
                YG2.saves.tutorialQuestIndex = (int)TutorialQuest.None;
            }
            else
            {
                YG2.saves.tutorialQuestIndex = (int)checkpointQuest;
            }
        }
        
        Debug.Log($"TutorialManager: Updated tutorial progress in memory - Current quest: {currentQuest}, Checkpoint quest index: {YG2.saves.tutorialQuestIndex} (will be saved by autosave)");
    }
    
    /// <summary>
    /// Auto-find quest locations if not assigned in Inspector
    /// </summary>
    private void AutoFindLocations()
    {
        // Find workbench
        if (workbenchLocation == null)
        {
            Workbench workbench = FindFirstObjectByType<Workbench>();
            if (workbench != null)
            {
                workbenchLocation = workbench.transform;
            }
        }
        
        // Find shop computer
        if (shopComputerLocation == null)
        {
            ShopComputer shopComputer = FindFirstObjectByType<ShopComputer>();
            if (shopComputer != null)
            {
                shopComputerLocation = shopComputer.transform;
            }
        }
        
        // Find part spawn point
        if (partSpawnLocation == null)
        {
            if (PartSpawner.Instance != null && PartSpawner.Instance.SpawnPoint != null)
            {
                partSpawnLocation = PartSpawner.Instance.SpawnPoint;
            }
        }
        
        // Find blowtorch
        if (blowtorchLocation == null)
        {
            Blowtorch blowtorch = FindFirstObjectByType<Blowtorch>();
            if (blowtorch != null)
            {
                ItemPickup pickup = blowtorch.GetComponent<ItemPickup>();
                if (pickup != null)
                {
                    blowtorchLocation = pickup.transform;
                }
            }
        }
        
        // Find location door
        if (locationDoorLocation == null)
        {
            LocationDoor door = FindFirstObjectByType<LocationDoor>();
            if (door != null)
            {
                locationDoorLocation = door.transform;
            }
        }
        
        // Note: shootingTargetsLocation should be assigned manually in Inspector
        // as it's a custom location chosen by the user
    }
    
    /// <summary>
    /// Create exclamation marks for all quest locations
    /// </summary>
    private void CreateExclamationMarks()
    {
        if (exclamationMarkPrefab == null)
        {
            Debug.LogWarning("TutorialManager: Exclamation mark prefab not assigned!");
            return;
        }
        
        // Create marks for each location type
        CreateExclamationMarkForLocation(TutorialQuest.CreateGun, workbenchLocation);
        CreateExclamationMarkForLocation(TutorialQuest.BuyBarrel, shopComputerLocation);
        CreateExclamationMarkForLocation(TutorialQuest.TakeBarrel, partSpawnLocation);
        CreateExclamationMarkForLocation(TutorialQuest.AttachBarrel, workbenchLocation);
        CreateExclamationMarkForLocation(TutorialQuest.TakeBlowtorch, blowtorchLocation);
        CreateExclamationMarkForLocation(TutorialQuest.WeldBarrel, workbenchLocation);
        CreateExclamationMarkForLocation(TutorialQuest.BuyMag, shopComputerLocation);
        CreateExclamationMarkForLocation(TutorialQuest.TakeMag, partSpawnLocation);
        CreateExclamationMarkForLocation(TutorialQuest.AttachMag, workbenchLocation);
        CreateExclamationMarkForLocation(TutorialQuest.TakeGun, workbenchLocation);
        CreateExclamationMarkForLocation(TutorialQuest.ShootTargets, shootingTargetsLocation);
        CreateExclamationMarkForLocation(TutorialQuest.EnterRange, locationDoorLocation);
    }
    
    /// <summary>
    /// Create exclamation mark for a specific quest location
    /// </summary>
    private void CreateExclamationMarkForLocation(TutorialQuest quest, Transform location)
    {
        if (location == null || exclamationMarkPrefab == null) return;
        
        // Check if mark already exists for this location
        if (exclamationMarks.ContainsKey(quest))
        {
            return;
        }
        
        GameObject markObj = Instantiate(exclamationMarkPrefab, location);
        markObj.transform.localPosition = Vector3.zero;
        
        TutorialExclamationMark mark = markObj.GetComponent<TutorialExclamationMark>();
        if (mark == null)
        {
            mark = markObj.AddComponent<TutorialExclamationMark>();
        }
        
        exclamationMarks[quest] = mark;
        mark.Hide(); // Hide initially
    }
    
    /// <summary>
    /// Subscribe to game events for quest tracking
    /// </summary>
    private void SubscribeToEvents()
    {
        // Quest 1: WeaponSlotManager.SlotsChanged
        if (WeaponSlotManager.Instance != null)
        {
            WeaponSlotManager.Instance.SlotsChanged += OnSlotsChanged;
        }
        
        // Quest 2, 7: PurchaseConfirmationUI - need to hook into purchase
        // We'll check PartSpawner spawns or monitor shop purchases
        
        // Quest 3, 5, 8, 10: InteractionHandler - need to add event or check currentItem
        // We'll use Update polling for now
        
        // Quest 4, 9: Workbench InstallPart - need to check WeaponBody state
        // We'll use Update polling to check mounted weapon parts
        
        // Quest 6: WeldingSystem - check IsWelded property
        // We'll use Update polling
        
        // Quest 11: MoneySystem.OnMoneyChanged
        if (MoneySystem.Instance != null)
        {
            MoneySystem.Instance.OnMoneyChanged += OnMoneyChanged;
        }
        
        // Quest 12: LocationManager.OnLocationChangedEvent
        if (LocationManager.Instance != null)
        {
            LocationManager.Instance.OnLocationChangedEvent += OnLocationChanged;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (WeaponSlotManager.Instance != null)
        {
            WeaponSlotManager.Instance.SlotsChanged -= OnSlotsChanged;
        }
        
        if (MoneySystem.Instance != null)
        {
            MoneySystem.Instance.OnMoneyChanged -= OnMoneyChanged;
        }
        
        if (LocationManager.Instance != null)
        {
            LocationManager.Instance.OnLocationChangedEvent -= OnLocationChanged;
        }
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameInitialized -= InitializeTutorial;
        }
        
        if (instance == this)
        {
            instance = null;
        }
    }
    
    private void Update()
    {
        if (!isInitialized || tutorialCompleted) return;
        
        // Poll-based quest completion checks (for systems without events)
        CheckQuestCompletion();
    }
    
    /// <summary>
    /// Check quest completion through polling
    /// </summary>
    private void CheckQuestCompletion()
    {
        switch (currentQuest)
        {
            case TutorialQuest.TakeBarrel:
                CheckTakeBarrelQuest();
                break;
            case TutorialQuest.AttachBarrel:
                CheckAttachBarrelQuest();
                break;
            case TutorialQuest.TakeBlowtorch:
                CheckTakeBlowtorchQuest();
                break;
            case TutorialQuest.WeldBarrel:
                CheckWeldBarrelQuest();
                break;
            case TutorialQuest.TakeMag:
                CheckTakeMagQuest();
                break;
            case TutorialQuest.AttachMag:
                CheckAttachMagQuest();
                break;
            case TutorialQuest.TakeGun:
                CheckTakeGunQuest();
                break;
            case TutorialQuest.EnterRange:
                // Проверяем локацию каждый кадр, чтобы завершить квест сразу при загрузке на стрельбище
                // (даже если событие OnLocationChanged сработало, пока было открыто полноэкранное окно)
                CheckEnterRangeQuest();
                break;
        }
    }
    
    /// <summary>
    /// Start a quest
    /// </summary>
    private void StartQuest(TutorialQuest quest)
    {
        if (quest == TutorialQuest.None || quest == TutorialQuest.Completed)
        {
            return;
        }
        
        currentQuest = quest;
        
        // Get localized quest text
        string questKey = $"tutorial.quest.{(int)quest + 1}";
        string questText = LocalizationHelper.Get(questKey, GetDefaultQuestText(quest), GetDefaultQuestText(quest));
        
        // Show quest UI (text is already updated by SwitchQuestAnimation if called from CompleteQuest)
        if (questUI != null)
        {
            questUI.ShowQuest(questText);
        }
        
        // Show exclamation mark
        ShowExclamationMark(quest);
        
        // Play quest audio
        PlayQuestAudio(quest);
        
        // Initialize quest-specific tracking
        if (quest == TutorialQuest.ShootTargets && MoneySystem.Instance != null)
        {
            initialMoneyForQuest11 = MoneySystem.Instance.CurrentMoney;
        }
        
        // Save progress
        SaveTutorialProgress();
        
        // Force auto-save when quest 10 (TakeGun) becomes active
        if (quest == TutorialQuest.TakeGun)
        {
            if (SaveSystemManager.Instance != null)
            {
                SaveSystemManager.Instance.TriggerAutoSaveOnReturn();
                Debug.Log("TutorialManager: Forced auto-save triggered for quest 10 (TakeGun)");
            }
            else
            {
                Debug.LogWarning("TutorialManager: Cannot trigger auto-save - SaveSystemManager.Instance is null");
            }
        }
        
        OnQuestStarted?.Invoke(quest);
        
        Debug.Log($"TutorialManager: Started quest {quest} ({questText})");
    }
    
    /// <summary>
    /// Continue to next quest (called after UI animation completes)
    /// </summary>
    private void ContinueToNextQuest(TutorialQuest nextQuest)
    {
        currentQuest = nextQuest;
        
        // Show exclamation mark
        ShowExclamationMark(nextQuest);
        
        // Play quest audio
        PlayQuestAudio(nextQuest);
        
        // Initialize quest-specific tracking
        if (nextQuest == TutorialQuest.ShootTargets && MoneySystem.Instance != null)
        {
            initialMoneyForQuest11 = MoneySystem.Instance.CurrentMoney;
        }
        
        // Save progress
        SaveTutorialProgress();
        
        // Force auto-save when quest 10 (TakeGun) becomes active
        if (nextQuest == TutorialQuest.TakeGun)
        {
            if (SaveSystemManager.Instance != null)
            {
                SaveSystemManager.Instance.TriggerAutoSaveOnReturn();
                Debug.Log("TutorialManager: Forced auto-save triggered for quest 10 (TakeGun)");
            }
            else
            {
                Debug.LogWarning("TutorialManager: Cannot trigger auto-save - SaveSystemManager.Instance is null");
            }
        }
        
        OnQuestStarted?.Invoke(nextQuest);
        
        Debug.Log($"TutorialManager: Continued to quest {nextQuest}");
    }
    
    /// <summary>
    /// Complete current quest and move to next
    /// </summary>
    private void CompleteQuest()
    {
        if (currentQuest == TutorialQuest.None || currentQuest == TutorialQuest.Completed)
        {
            return;
        }
        
        TutorialQuest completedQuest = currentQuest;
        
        // Hide exclamation mark first (marker не должен висеть поверх окна)
        HideExclamationMark(completedQuest);
        
        // Если сейчас открыт полноэкранный UI, ждём его закрытия и только потом показываем анимации
        if (IsFullscreenBlocked())
        {
            StartCoroutine(WaitForFullscreenAndRunQuestTransition(completedQuest));
        }
        else
        {
            RunQuestTransition(completedQuest);
        }
        
        OnQuestCompleted?.Invoke(completedQuest);
        
        Debug.Log($"TutorialManager: Completed quest {completedQuest}");
    }
    
    /// <summary>
    /// Complete current quest and skip specified quests, jumping directly to target quest
    /// </summary>
    private void CompleteQuestWithSkip(TutorialQuest[] questsToSkip, TutorialQuest targetQuest)
    {
        if (currentQuest == TutorialQuest.None || currentQuest == TutorialQuest.Completed)
        {
            return;
        }
        
        TutorialQuest completedQuest = currentQuest;
        
        // Hide exclamation mark first
        HideExclamationMark(completedQuest);
        
        // Mark skipped quests as completed
        foreach (TutorialQuest skippedQuest in questsToSkip)
        {
            OnQuestCompleted?.Invoke(skippedQuest);
            Debug.Log($"TutorialManager: Skipped quest {skippedQuest}");
        }
        
        // Если сейчас открыт полноэкранный UI, ждём его закрытия и только потом показываем анимации
        if (IsFullscreenBlocked())
        {
            StartCoroutine(WaitForFullscreenAndRunQuestTransitionWithSkip(completedQuest, targetQuest));
        }
        else
        {
            RunQuestTransitionWithSkip(completedQuest, targetQuest);
        }
        
        OnQuestCompleted?.Invoke(completedQuest);
        
        Debug.Log($"TutorialManager: Completed quest {completedQuest}, skipped {questsToSkip.Length} quests, jumping to {targetQuest}");
    }
    
    /// <summary>
    /// Выполнить переход к целевому квесту с анимациями (пропуская промежуточные квесты)
    /// </summary>
    private void RunQuestTransitionWithSkip(TutorialQuest completedQuest, TutorialQuest targetQuest)
    {
        if (questUI != null)
        {
            questUI.CompleteQuestAnimation(() =>
            {
                // Switch directly to target quest
                questUI.SwitchQuestAnimation(GetQuestText(targetQuest), () =>
                {
                    // Continue with target quest (UI already updated)
                    ContinueToNextQuest(targetQuest);
                });
            });
        }
        else
        {
            // No UI - just move to target quest
            StartQuest(targetQuest);
        }
    }
    
    /// <summary>
    /// Короутина: ждём выхода из полноэкранного окна и только после этого запускаем анимации перехода с пропуском квестов
    /// </summary>
    private IEnumerator WaitForFullscreenAndRunQuestTransitionWithSkip(TutorialQuest completedQuest, TutorialQuest targetQuest)
    {
        // Ждём пока разблокируется полноэкранный UI
        while (IsFullscreenBlocked())
        {
            yield return null;
        }
        
        // После закрытия полноэкранного окна проверяем локацию для квеста 12
        // (на случай, если событие смены локации сработало, пока окно было открыто)
        if (completedQuest == TutorialQuest.EnterRange)
        {
            CheckEnterRangeQuest();
            // Если квест завершился, выходим (RunQuestTransition вызовется из CompleteQuest)
            if (quest12Completed)
            {
                yield break;
            }
        }
        
        RunQuestTransitionWithSkip(completedQuest, targetQuest);
    }
    
    /// <summary>
    /// Выполнить переход к следующему квесту с анимациями (уже вне полноэкранных окон)
    /// </summary>
    private void RunQuestTransition(TutorialQuest completedQuest)
    {
        if (questUI != null)
        {
            questUI.CompleteQuestAnimation(() =>
            {
                // Move to next quest
                TutorialQuest nextQuest = GetNextQuest(completedQuest);
                
                if (nextQuest == TutorialQuest.Completed)
                {
                    // Tutorial completed
                    CompleteTutorial();
                }
                else
                {
                    // Switch to next quest
                    questUI.SwitchQuestAnimation(GetQuestText(nextQuest), () =>
                    {
                        // Continue with next quest (UI already updated)
                        ContinueToNextQuest(nextQuest);
                    });
                }
            });
        }
        else
        {
            // No UI - just move to next quest
            TutorialQuest nextQuest = GetNextQuest(completedQuest);
            
            if (nextQuest == TutorialQuest.Completed)
            {
                CompleteTutorial();
            }
            else
            {
                StartQuest(nextQuest);
            }
        }
    }
    
    /// <summary>
    /// Короутина: ждём выхода из полноэкранного окна и только после этого запускаем анимации перехода
    /// </summary>
    private IEnumerator WaitForFullscreenAndRunQuestTransition(TutorialQuest completedQuest)
    {
        // Ждём пока разблокируется полноэкранный UI
        while (IsFullscreenBlocked())
        {
            yield return null;
        }
        
        // После закрытия полноэкранного окна проверяем локацию для квеста 12
        // (на случай, если событие смены локации сработало, пока окно было открыто)
        if (completedQuest == TutorialQuest.EnterRange)
        {
            CheckEnterRangeQuest();
            // Если квест завершился, выходим (RunQuestTransition вызовется из CompleteQuest)
            if (quest12Completed)
            {
                yield break;
            }
        }
        
        RunQuestTransition(completedQuest);
    }
    
    /// <summary>
    /// Проверка: сейчас открыт какой-либо полноэкранный UI, перекрывающий HUD?
    /// </summary>
    private bool IsFullscreenBlocked()
    {
        // На стрельбище AdManager блокируется, но это не полноэкранное окно
        // Проверяем только если мы в мастерской
        bool isInWorkshop = LocationManager.Instance != null && 
                           LocationManager.Instance.CurrentLocation == LocationManager.LocationType.Workshop;
        
        // 1) Блок от AdManager (используется для таймера рекламы и полноэкранных окон)
        // Но только если мы в мастерской (на стрельбище блок не означает полноэкранное окно)
        if (isInWorkshop && AdManager.Instance != null && AdManager.Instance.IsAdTimerBlocked)
        {
            return true;
        }
        
        // 2) Общий контекст HUD — если кто-то запросил скрытие HUD, значит на экране полноэкранный UI
        if (GameplayUIContext.HasInstance && GameplayUIContext.Instance.IsHudHidden)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Complete entire tutorial
    /// </summary>
    private void CompleteTutorial()
    {
        tutorialCompleted = true;
        currentQuest = TutorialQuest.Completed;
        
        // Hide quest UI
        if (questUI != null)
        {
            questUI.Hide();
        }
        
        // Hide all exclamation marks
        HideAllExclamationMarks();
        
        // Save progress
        SaveTutorialProgress();
        
        Debug.Log("TutorialManager: Tutorial completed!");
    }
    
    /// <summary>
    /// Get next quest after current one
    /// </summary>
    private TutorialQuest GetNextQuest(TutorialQuest current)
    {
        int nextIndex = (int)current + 1;
        if (nextIndex >= (int)TutorialQuest.Completed)
        {
            return TutorialQuest.Completed;
        }
        return (TutorialQuest)nextIndex;
    }
    
    /// <summary>
    /// Get quest text (localized)
    /// </summary>
    private string GetQuestText(TutorialQuest quest)
    {
        string questKey = $"tutorial.quest.{(int)quest + 1}";
        return LocalizationHelper.Get(questKey, GetDefaultQuestText(quest), GetDefaultQuestText(quest));
    }
    
    /// <summary>
    /// Get default English quest text
    /// </summary>
    private string GetDefaultQuestText(TutorialQuest quest)
    {
        return quest switch
        {
            TutorialQuest.CreateGun => "Create a new gun at the workbench",
            TutorialQuest.BuyBarrel => "Buy a barrel at the computer",
            TutorialQuest.TakeBarrel => "Take the barrel",
            TutorialQuest.AttachBarrel => "Attach the barrel to the gun",
            TutorialQuest.TakeBlowtorch => "Take blowtorch",
            TutorialQuest.WeldBarrel => "Weld the barrel with blowtorch",
            TutorialQuest.BuyMag => "Buy a mag at the computer",
            TutorialQuest.TakeMag => "Take the mag",
            TutorialQuest.AttachMag => "Attach the mag to the gun",
            TutorialQuest.TakeGun => "Take the gun from workbench",
            TutorialQuest.ShootTargets => "Shoot some targets",
            TutorialQuest.EnterRange => "Go to the door and enter shooting range",
            _ => "Unknown quest"
        };
    }
    
    /// <summary>
    /// Show exclamation mark for quest
    /// </summary>
    private void ShowExclamationMark(TutorialQuest quest)
    {
        // Hide current mark
        if (currentExclamationMark != null)
        {
            currentExclamationMark.Hide();
        }
        
        // Show new mark
        if (exclamationMarks.TryGetValue(quest, out TutorialExclamationMark mark))
        {
            mark.Show();
            currentExclamationMark = mark;
        }
    }
    
    /// <summary>
    /// Hide exclamation mark for quest
    /// </summary>
    private void HideExclamationMark(TutorialQuest quest)
    {
        if (exclamationMarks.TryGetValue(quest, out TutorialExclamationMark mark))
        {
            mark.Hide();
            if (currentExclamationMark == mark)
            {
                currentExclamationMark = null;
            }
        }
    }
    
    /// <summary>
    /// Hide all exclamation marks
    /// </summary>
    private void HideAllExclamationMarks()
    {
        foreach (var mark in exclamationMarks.Values)
        {
            mark.Hide();
        }
        currentExclamationMark = null;
    }
    
    /// <summary>
    /// Play quest audio based on current language
    /// </summary>
    private void PlayQuestAudio(TutorialQuest quest)
    {
        if (questAudioClips == null || questAudioClips.Length < 24) return;
        
        // Determine language (0 = en, 1 = ru)
        int languageIndex = 0;
        if (LocalizationManager.Instance != null)
        {
            languageIndex = LocalizationManager.Instance.CurrentLanguage == "ru" ? 1 : 0;
        }
        
        // Calculate clip index: questIndex * 2 + languageIndex
        int questIndex = (int)quest;
        int clipIndex = questIndex * 2 + languageIndex;
        
        if (clipIndex >= 0 && clipIndex < questAudioClips.Length && questAudioClips[clipIndex] != null)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(questAudioClips[clipIndex], volume: 0.8f);
            }
        }
    }
    
    // Event Handlers
    
    /// <summary>
    /// Quest 1: Weapon slots changed (weapon created)
    /// </summary>
    private void OnSlotsChanged()
    {
        if (currentQuest == TutorialQuest.CreateGun && !quest1Completed)
        {
            if (WeaponSlotManager.Instance != null && WeaponSlotManager.Instance.OccupiedCount > 0)
            {
                quest1Completed = true;
                CompleteQuest();
            }
        }
    }
    
    /// <summary>
    /// Quest 2, 7: Check for part purchases (monitor PartSpawner or shop)
    /// </summary>
    private void CheckPartPurchase(PartType partType, TutorialQuest expectedQuest)
    {
        if (currentQuest == expectedQuest)
        {
            if (expectedQuest == TutorialQuest.BuyBarrel && partType == PartType.Barrel && !quest2Completed)
            {
                quest2Completed = true;
                CompleteQuest();
            }
            else if (expectedQuest == TutorialQuest.BuyMag && partType == PartType.Magazine && !quest7Completed)
            {
                quest7Completed = true;
                CompleteQuest();
            }
        }
    }
    
    /// <summary>
    /// Quest 3: Check if barrel was picked up
    /// </summary>
    private void CheckTakeBarrelQuest()
    {
        if (currentQuest != TutorialQuest.TakeBarrel || quest3Completed) return;
        
        InteractionHandler handler = FindFirstObjectByType<InteractionHandler>();
        if (handler != null && handler.CurrentItem != null)
        {
            WeaponPart part = handler.CurrentItem.GetComponent<WeaponPart>();
            if (part != null && part.Type == PartType.Barrel)
            {
                quest3Completed = true;
                CompleteQuest();
            }
        }
    }
    
    /// <summary>
    /// Quest 4: Check if barrel is attached to weapon on workbench
    /// </summary>
    private void CheckAttachBarrelQuest()
    {
        if (currentQuest != TutorialQuest.AttachBarrel || quest4Completed) return;
        
        Workbench workbench = FindFirstObjectByType<Workbench>();
        if (workbench != null && workbench.MountedWeapon != null)
        {
            WeaponPart barrel = workbench.MountedWeapon.GetPart(PartType.Barrel);
            if (barrel != null)
            {
                quest4Completed = true;
                CompleteQuest();
            }
        }
    }
    
    /// <summary>
    /// Quest 5: Check if blowtorch was picked up
    /// </summary>
    private void CheckTakeBlowtorchQuest()
    {
        if (currentQuest != TutorialQuest.TakeBlowtorch || quest5Completed) return;
        
        InteractionHandler handler = FindFirstObjectByType<InteractionHandler>();
        if (handler != null && handler.CurrentItem != null)
        {
            Blowtorch blowtorch = handler.CurrentItem.GetComponent<Blowtorch>();
            if (blowtorch != null)
            {
                quest5Completed = true;
                CompleteQuest();
            }
        }
    }
    
    /// <summary>
    /// Quest 6: Check if barrel is welded
    /// </summary>
    private void CheckWeldBarrelQuest()
    {
        if (currentQuest != TutorialQuest.WeldBarrel || quest6Completed) return;
        
        Workbench workbench = FindFirstObjectByType<Workbench>();
        if (workbench != null && workbench.MountedWeapon != null)
        {
            WeaponPart barrel = workbench.MountedWeapon.GetPart(PartType.Barrel);
            if (barrel != null)
            {
                WeldingSystem welding = barrel.GetComponent<WeldingSystem>();
                if (welding != null && welding.IsWelded)
                {
                    quest6Completed = true;
                    
                    // Check if weapon already has a magazine attached
                    // If yes, skip quests 7, 8, 9 and go directly to quest 10
                    WeaponPart magazine = workbench.MountedWeapon.GetPart(PartType.Magazine);
                    if (magazine != null)
                    {
                        // Magazine already attached - skip quests 7, 8, 9
                        quest7Completed = true; // BuyMag
                        quest8Completed = true; // TakeMag
                        quest9Completed = true; // AttachMag
                        
                        // Complete quest 6 and jump directly to quest 10
                        CompleteQuestWithSkip(questsToSkip: new[] { TutorialQuest.BuyMag, TutorialQuest.TakeMag, TutorialQuest.AttachMag }, targetQuest: TutorialQuest.TakeGun);
                    }
                    else
                    {
                        // No magazine - continue normally
                        CompleteQuest();
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Quest 8: Check if magazine was picked up
    /// </summary>
    private void CheckTakeMagQuest()
    {
        if (currentQuest != TutorialQuest.TakeMag || quest8Completed) return;
        
        InteractionHandler handler = FindFirstObjectByType<InteractionHandler>();
        if (handler != null && handler.CurrentItem != null)
        {
            WeaponPart part = handler.CurrentItem.GetComponent<WeaponPart>();
            if (part != null && part.Type == PartType.Magazine)
            {
                quest8Completed = true;
                CompleteQuest();
            }
        }
    }
    
    /// <summary>
    /// Quest 9: Check if magazine is attached to weapon on workbench
    /// </summary>
    private void CheckAttachMagQuest()
    {
        if (currentQuest != TutorialQuest.AttachMag || quest9Completed) return;
        
        Workbench workbench = FindFirstObjectByType<Workbench>();
        if (workbench != null && workbench.MountedWeapon != null)
        {
            WeaponPart magazine = workbench.MountedWeapon.GetPart(PartType.Magazine);
            if (magazine != null)
            {
                quest9Completed = true;
                CompleteQuest();
            }
        }
    }
    
    /// <summary>
    /// Quest 10: Check if gun was taken from workbench
    /// </summary>
    private void CheckTakeGunQuest()
    {
        if (currentQuest != TutorialQuest.TakeGun || quest10Completed) return;
        
        InteractionHandler handler = FindFirstObjectByType<InteractionHandler>();
        if (handler != null && handler.CurrentItem != null)
        {
            WeaponBody weaponBody = handler.CurrentItem.GetComponent<WeaponBody>();
            if (weaponBody != null)
            {
                quest10Completed = true;
                CompleteQuest();
            }
        }
    }
    
    /// <summary>
    /// Quest 11: Money changed (any money earned from targets)
    /// </summary>
    private void OnMoneyChanged(int newMoney)
    {
        if (currentQuest == TutorialQuest.ShootTargets && !quest11Completed)
        {
            // Check if money increased (earned from targets)
            // Initialize on first check or when quest starts
            if (initialMoneyForQuest11 == 0)
            {
                initialMoneyForQuest11 = newMoney;
            }
            else if (newMoney > initialMoneyForQuest11)
            {
                quest11Completed = true;
                CompleteQuest();
            }
        }
        else if (currentQuest != TutorialQuest.ShootTargets)
        {
            // Reset initial money when not on quest 11
            initialMoneyForQuest11 = 0;
        }
    }
    
    /// <summary>
    /// Quest 12: Location changed to testing range
    /// </summary>
    private void OnLocationChanged(LocationManager.LocationType newLocation)
    {
        if (currentQuest == TutorialQuest.EnterRange && !quest12Completed)
        {
            if (newLocation == LocationManager.LocationType.TestingRange)
            {
                quest12Completed = true;
                // Завершаем квест сразу при загрузке на стрельбище, игнорируя блок AdManager
                CompleteQuestImmediately();
            }
        }
    }
    
    /// <summary>
    /// Quest 12: Check if we're on testing range (called after fullscreen closes)
    /// This handles the case when location change event fired while fullscreen was open
    /// </summary>
    private void CheckEnterRangeQuest()
    {
        if (currentQuest == TutorialQuest.EnterRange && !quest12Completed)
        {
            if (LocationManager.Instance != null && 
                LocationManager.Instance.CurrentLocation == LocationManager.LocationType.TestingRange)
            {
                quest12Completed = true;
                // Завершаем квест сразу при загрузке на стрельбище, игнорируя блок AdManager
                // (на стрельбище AdManager блокируется, но это не полноэкранное окно)
                CompleteQuestImmediately();
            }
        }
    }
    
    /// <summary>
    /// Завершить квест сразу, без проверки полноэкранных окон (для квеста 12 на стрельбище)
    /// </summary>
    private void CompleteQuestImmediately()
    {
        if (currentQuest == TutorialQuest.None || currentQuest == TutorialQuest.Completed)
        {
            return;
        }
        
        TutorialQuest completedQuest = currentQuest;
        
        // Hide exclamation mark first
        HideExclamationMark(completedQuest);
        
        // Запускаем анимации сразу, без проверки полноэкранных окон
        RunQuestTransition(completedQuest);
        
        OnQuestCompleted?.Invoke(completedQuest);
        
        Debug.Log($"TutorialManager: Completed quest {completedQuest} immediately (no fullscreen check)");
    }
    
    // Public API
    
    /// <summary>
    /// Check if tutorial is blocking taking weapon from workbench (quest 10)
    /// </summary>
    public bool IsQuestBlockingTakeWeapon()
    {
        return currentQuest != TutorialQuest.TakeGun && 
               currentQuest != TutorialQuest.Completed && 
               currentQuest != TutorialQuest.None;
    }
    
    /// <summary>
    /// Check if tutorial is blocking interaction with shop computer (before quest 1)
    /// </summary>
    public bool IsQuestBlockingShopComputer()
    {
        // If tutorial is not initialized yet, block interaction
        if (!isInitialized)
        {
            return true;
        }
        
        // Block until quest 1 (CreateGun) is reached or started
        // Block if currentQuest is None (new game, before quest 1 starts)
        // Also block if currentQuest is CreateGun but quest 1 is not yet completed
        // (to prevent interaction during quest 1 execution)
        // Allow interaction once quest 1 (CreateGun) is completed or any quest after it
        if (currentQuest == TutorialQuest.None)
        {
            return true;
        }
        
        // If we're on quest 1, check if it's completed
        if (currentQuest == TutorialQuest.CreateGun)
        {
            // Block if quest 1 is not completed yet
            return !quest1Completed;
        }
        
        // Allow interaction for all other quests (quest 2 and beyond)
        return false;
    }
    
    /// <summary>
    /// Check if auto-save should be blocked (quest 10 or quest 11, before quest 12)
    /// </summary>
    public bool IsAutoSaveBlocked()
    {
        if (!isInitialized || tutorialCompleted)
        {
            return false;
        }
        
        // Block if current quest is TakeGun (quest 10) - not yet completed
        if (currentQuest == TutorialQuest.TakeGun)
        {
            return true;
        }
        
        // Block if current quest is ShootTargets (quest 11) - not yet completed
        if (currentQuest == TutorialQuest.ShootTargets && !quest11Completed)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if tutorial is blocking interaction with door (before quest 12)
    /// </summary>
    public bool IsQuestBlockingDoor()
    {
        // If tutorial is not initialized yet, block interaction
        if (!isInitialized)
        {
            return true;
        }
        
        // If tutorial is completed, allow interaction
        if (tutorialCompleted || currentQuest == TutorialQuest.Completed)
        {
            return false;
        }
        
        // Block until quest 12 (EnterRange) is reached or started
        // Block on all quests before quest 12 (quests 1-11)
        // Allow interaction once quest 12 (EnterRange) is active
        if (currentQuest == TutorialQuest.EnterRange)
        {
            return false;
        }
        
        // Block on all other quests (1-11)
        return true;
    }
    
    /// <summary>
    /// Notify that a part was purchased (called from PurchaseConfirmationUI or ShopUI)
    /// </summary>
    public void NotifyPartPurchased(PartType partType)
    {
        CheckPartPurchase(partType, TutorialQuest.BuyBarrel);
        CheckPartPurchase(partType, TutorialQuest.BuyMag);
    }
    
    /// <summary>
    /// Get current quest
    /// </summary>
    public TutorialQuest CurrentQuest => currentQuest;
    
    /// <summary>
    /// Check if tutorial is completed
    /// </summary>
    public bool IsTutorialCompleted => tutorialCompleted;
    
    /// <summary>
    /// Check if tutorial system is initialized
    /// </summary>
    public bool IsInitialized => isInitialized;
}


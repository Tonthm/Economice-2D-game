using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// ============================================================
//  RandomEventSystem.cs
//  - สุ่มเหตุการณ์ทุก 5 เทิร์น แบบ Non-repeating
//  - ผู้เล่นเลือก Resolve (เสีย Gold) หรือ Ignore (เสีย Reliability)
//  - เรียก CheckEvent(turn) จาก TurnManager
// ============================================================

public enum EventOutcomeType { LoseGold, LoseReliability, LoseResource, GainGold, GainReliability }

[System.Serializable]
public class GameEvent
{
    public string title;
    [TextArea] public string description;

    [Header("Resolve (Fix) Cost")]
    public float resolveGoldCost;
    public string resolveResourceName;
    public int    resolveResourceQty;

    [Header("Outcome if Resolved")]
    public EventOutcomeType resolveOutcomeType;
    public float resolveOutcomeValue;      // บวก = ได้รับ | ลบ = สูญเสีย

    [Header("Outcome if Ignored")]
    public float ignoreReliabilityPenalty = 10f;
    public float ignoreGoldPenalty        = 0f;
}

public class RandomEventSystem : MonoBehaviour
{
    public static RandomEventSystem Instance { get; private set; }

    [Header("References")]
    public EconomySystem    economySystem;
    public ResourceInventory inventory;

    [Header("Event Trigger Interval")]
    public int eventEveryNTurns = 5;

    [Header("Events — configure in Inspector")]
    public List<GameEvent> allEvents = new List<GameEvent>();

    [Header("Event UI Panel")]
    public GameObject eventPanel;
    public TextMeshProUGUI eventTitleText;
    public TextMeshProUGUI eventDescText;
    public TextMeshProUGUI eventCostText;
    public Button resolveButton;
    public Button ignoreButton;

    private List<int> remainingPool = new List<int>();
    private GameEvent activeEvent;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (eventPanel != null) eventPanel.SetActive(false);
        if (allEvents.Count == 0) SetupDefaultEvents();
        RebuildPool();

        resolveButton?.onClick.AddListener(OnResolve);
        ignoreButton?.onClick.AddListener(OnIgnore);
    }

    void SetupDefaultEvents()
    {
        allEvents.Add(new GameEvent
        {
            title       = "🏚️ Building Collapse",
            description = "A weakened structure has collapsed! Send engineers to repair it or lose citizen trust.",
            resolveGoldCost = 500f,
            resolveResourceName = "Engineer",
            resolveResourceQty  = 1,
            resolveOutcomeType  = EventOutcomeType.GainReliability,
            resolveOutcomeValue = 5f,
            ignoreReliabilityPenalty = 15f
        });
        allEvents.Add(new GameEvent
        {
            title       = "🔥 Fire Outbreak",
            description = "Fire is spreading through the market district! Act quickly to contain it.",
            resolveGoldCost = 800f,
            resolveOutcomeType  = EventOutcomeType.GainReliability,
            resolveOutcomeValue = 5f,
            ignoreReliabilityPenalty = 20f,
            ignoreGoldPenalty = 300f
        });
        allEvents.Add(new GameEvent
        {
            title       = "🐹 Hamster Raiders",
            description = "A band of hamster brigands is demanding a toll. Pay them off or lose resources.",
            resolveGoldCost = 600f,
            resolveOutcomeType  = EventOutcomeType.GainReliability,
            resolveOutcomeValue = 3f,
            ignoreReliabilityPenalty = 10f,
            ignoreGoldPenalty = 400f
        });
        allEvents.Add(new GameEvent
        {
            title       = "🌊 Flash Flood",
            description = "Heavy rain has flooded lower parts of the village. Evacuate and repair infrastructure.",
            resolveGoldCost     = 700f,
            resolveResourceName = "Labor",
            resolveResourceQty  = 2,
            resolveOutcomeType  = EventOutcomeType.GainReliability,
            resolveOutcomeValue = 8f,
            ignoreReliabilityPenalty = 18f
        });
        allEvents.Add(new GameEvent
        {
            title       = "🎉 Harvest Festival",
            description = "A good season! Merchants offer bonus goods. Invest in supplies at reduced prices.",
            resolveGoldCost     = 0f,
            resolveOutcomeType  = EventOutcomeType.GainGold,
            resolveOutcomeValue = 1000f,
            ignoreReliabilityPenalty = 0f
        });
    }

    // ============================================================
    //  CHECK — เรียกจาก TurnManager.EndTurn()
    // ============================================================
    public void CheckEvent(int currentTurn)
    {
        if (currentTurn % eventEveryNTurns != 0) return;
        TriggerRandomEvent();
    }

    void TriggerRandomEvent()
    {
        if (remainingPool.Count == 0) RebuildPool();

        int poolIdx = Random.Range(0, remainingPool.Count);
        int evIdx   = remainingPool[poolIdx];
        remainingPool.RemoveAt(poolIdx);

        activeEvent = allEvents[evIdx];
        ShowEventPanel(activeEvent);
    }

    void RebuildPool()
    {
        remainingPool.Clear();
        for (int i = 0; i < allEvents.Count; i++) remainingPool.Add(i);
        Debug.Log("[Event] Pool reset");
    }

    // ============================================================
    //  UI
    // ============================================================
    void ShowEventPanel(GameEvent ev)
    {
        if (eventPanel == null) return;
        eventPanel.SetActive(true);

        if (eventTitleText != null) eventTitleText.text = ev.title;
        if (eventDescText  != null) eventDescText.text  = ev.description;

        string costStr = "";
        if (ev.resolveGoldCost > 0)          costStr += $"Gold: {ev.resolveGoldCost:N0}  ";
        if (!string.IsNullOrEmpty(ev.resolveResourceName) && ev.resolveResourceQty > 0)
            costStr += $"{ev.resolveResourceName}: x{ev.resolveResourceQty}";
        if (eventCostText != null) eventCostText.text = string.IsNullOrEmpty(costStr) ? "Free!" : costStr;

        // ตรวจว่ามีของพอ Resolve ไหม
        bool canResolve = economySystem.playerGold >= ev.resolveGoldCost &&
                          (string.IsNullOrEmpty(ev.resolveResourceName) ||
                           inventory.HasItems(ev.resolveResourceName, ev.resolveResourceQty));
        resolveButton.interactable = canResolve;
    }

    // ============================================================
    //  RESOLVE
    // ============================================================
    void OnResolve()
    {
        if (activeEvent == null) return;

        economySystem.playerGold -= activeEvent.resolveGoldCost;
        if (!string.IsNullOrEmpty(activeEvent.resolveResourceName) && activeEvent.resolveResourceQty > 0)
            inventory.RemoveItem(activeEvent.resolveResourceName, activeEvent.resolveResourceQty);

        ApplyOutcome(activeEvent.resolveOutcomeType, activeEvent.resolveOutcomeValue);
        Debug.Log($"[Event] Resolved: {activeEvent.title}");
        CloseEvent();
    }

    // ============================================================
    //  IGNORE
    // ============================================================
    void OnIgnore()
    {
        if (activeEvent == null) return;
        economySystem.reliability -= activeEvent.ignoreReliabilityPenalty;
        economySystem.reliability  = Mathf.Clamp(economySystem.reliability, -100f, 100f);
        economySystem.playerGold  -= activeEvent.ignoreGoldPenalty;
        Debug.Log($"[Event] Ignored: {activeEvent.title} | Reliability -{activeEvent.ignoreReliabilityPenalty}");
        CloseEvent();
    }

    void ApplyOutcome(EventOutcomeType type, float value)
    {
        switch (type)
        {
            case EventOutcomeType.GainGold:         economySystem.playerGold  += value; break;
            case EventOutcomeType.LoseGold:         economySystem.playerGold  -= value; break;
            case EventOutcomeType.GainReliability:  economySystem.reliability += value; break;
            case EventOutcomeType.LoseReliability:  economySystem.reliability -= value; break;
        }
        economySystem.reliability = Mathf.Clamp(economySystem.reliability, -100f, 100f);
    }

    void CloseEvent()
    {
        activeEvent = null;
        if (eventPanel != null) eventPanel.SetActive(false);
    }
}

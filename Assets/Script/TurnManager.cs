using UnityEngine;

// ============================================================
//  TurnManager_Updated.cs  ← แทนที่ TurnManager.cs เดิม
//  เพิ่ม hooks สำหรับ Week 3-4 Systems
//  (RandomEventSystem, ContractSystem, ShopSystem)
// ============================================================

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Turn State")]
    public int currentTurn = 0;
    public int maxAP = 5;
    public int currentAP = 5;

    [Header("References")]
    public EconomySystem    economySystem;
    public RandomEventSystem randomEventSystem;   // Week 3
    public ContractSystem   contractSystem;       // Week 3
    public ShopSystem       shopSystem;           // Week 2

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        currentAP   = maxAP;
        currentTurn = 0;
        Debug.Log("[TurnManager] Game Started | Turn: 0 | AP: 5");
    }

    // ============================================================
    //  END TURN — เรียกจากปุ่ม End Turn ใน HUD
    // ============================================================
    public void EndTurn()
    {
        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsVictory))
            return;

        currentTurn++;
        Debug.Log($"[TurnManager] Turn {currentTurn} Begin");

        // 1. Reset AP
        ResetAP();

        // 2. Economy (Tax + Fluctuation + Game Over Check)
        if (economySystem != null)
            economySystem.OnEndTurn();
        else
            Debug.LogWarning("[TurnManager] EconomySystem not assigned!");

        // 3. Contract income bonus
        contractSystem?.OnEndTurn();

        // 4. Shop price update (demand decay + recalc)
        shopSystem?.OnEndTurn();

        // 5. Random Events (every 5 turns)
        randomEventSystem?.CheckEvent(currentTurn);
    }

    // ============================================================
    //  AP MANAGEMENT
    // ============================================================
    void ResetAP()
    {
        currentAP = maxAP;
        Debug.Log($"[TurnManager] AP Reset to {maxAP}");
    }

    public bool UseAP(int amount)
    {
        if (currentAP < amount)
        {
            Debug.LogWarning($"[TurnManager] Not enough AP! Required: {amount} | Current: {currentAP}");
            return false;
        }
        currentAP -= amount;
        Debug.Log($"[TurnManager] Used {amount} AP | Remaining: {currentAP}");
        return true;
    }

    public bool HasEnoughAP(int amount) => currentAP >= amount;
}

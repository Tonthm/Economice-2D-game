using UnityEngine;

// ============================================================
//  TurnManager.cs
//  จัดการระบบผลัดรอบของเกม Economice
//  - นับ Turn
//  - Reset AP ทุกเทิร์น
//  - เรียก EconomySystem.OnEndTurn()
//  - TODO: เพิ่ม RandomEventSystem, ContractSystem ภายหลัง
// ============================================================

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Turn State")]
    public int currentTurn = 0;
    public int maxAP = 5;
    public int currentAP = 5;

    [Header("References")]
    public EconomySystem economySystem;

    // ============================================================
    //  SINGLETON
    // ============================================================
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        currentAP = maxAP;
        currentTurn = 0;
        Debug.Log("[TurnManager] Game Started | Turn: 0 | AP: 5");
    }

    // ============================================================
    //  END TURN — เรียกจากปุ่ม End Turn ใน UI
    // ============================================================
    public void EndTurn()
    {
        currentTurn++;
        Debug.Log($"[TurnManager] Turn {currentTurn} Begin");

        // 1. Reset AP
        ResetAP();

        // 2. รัน Economy (Tax, Fluctuation, Game Over Check)
        if (economySystem != null)
            economySystem.OnEndTurn();
        else
            Debug.LogWarning("[TurnManager] EconomySystem not assigned!");

        // TODO สัปดาห์ 3: เพิ่ม RandomEventSystem.CheckEvent(currentTurn)
        // TODO สัปดาห์ 3: เพิ่ม ContractSystem.OnEndTurn()
    }

    // ============================================================
    //  AP MANAGEMENT
    // ============================================================
    void ResetAP()
    {
        currentAP = maxAP;
        Debug.Log($"[TurnManager] AP Reset to {maxAP}");
    }

    // ใช้ AP — เรียกจาก ContractSystem ตอนซื้อ Contract
    // return true ถ้าใช้ได้, false ถ้า AP ไม่พอ
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

    // เช็คว่ามี AP พอไหม (ใช้ใน UI เพื่อ disable ปุ่มที่ใช้ AP ไม่พอ)
    public bool HasEnoughAP(int amount) => currentAP >= amount;
}

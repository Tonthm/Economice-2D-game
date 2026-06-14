using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using TMPro;
using UnityEngine.Serialization;

// ============================================================
//  EconomySystem.cs
//  ระบบเงินหลักของเกม Economice
//  - คำนวณรายได้ประชาชนจาก Building Level + Economy Fluctuation
//  - เก็บภาษีอัตโนมัติเมื่อ End Turn
//  - Manual Tax เพิ่มรายได้แต่ลด Reliability
//  - ขาย Resource คืน 70%
// ============================================================

public enum BuildingType { House, Market, Workshop }

[System.Serializable]
public class Building
{
    public BuildingType type;
    [Range(1, 3)] public int level = 1;

    // รายได้ Base ของประชาชนต่อหลัง (ก่อนคูณ economy modifier)
    // House: เน้นจำนวน | Market: รายได้สูง | Workshop: bonus ตอน economy ดี
    public float GetBaseIncome()
    {
        switch (type)
        {
            case BuildingType.House:
                return level switch { 1 => 100f, 2 => 180f, 3 => 280f, _ => 100f };
            case BuildingType.Market:
                return level switch { 1 => 200f, 2 => 350f, 3 => 520f, _ => 200f };
            case BuildingType.Workshop:
                return level switch { 1 => 140f, 2 => 240f, 3 => 370f, _ => 140f };
            default: return 0f;
        }
    }

    // Workshop ได้ bonus เพิ่มเมื่อ economy ดี (modifier > 1)
    public float GetEconomyBonus(float economyModifier)
    {
        if (type == BuildingType.Workshop && economyModifier > 1f)
            return GetBaseIncome() * (economyModifier - 1f) * 0.5f; // 50% ของส่วนเกิน
        return 0f;
    }
}

public class EconomySystem : MonoBehaviour
{
    [Header("State")]
    public float playerGold = 3000f;
    [FormerlySerializedAs("Gold")] [SerializeField] TextMeshProUGUI DisplayingGold;
    [Range(0f, 100f)] public float reliability = 30f;

    [Header("Buildings")]
    public List<Building> buildings = new List<Building>();

    [Header("Economy Fluctuation")]
    [Range(0.5f, 1.5f)] public float economyModifier = 1f; // สุ่มใหม่ทุก End Turn

    [Header("Tax Settings")]
    [Range(0f, 1f)] public float taxRate = 0.2f;             // ภาษีอัตโนมัติ 20% ของรายได้ประชาชน
    public float manualTaxAmount = 200f;                      // จำนวนเงินที่เก็บเพิ่มเมื่อกด Manual Tax
    public float manualTaxReliabilityPenalty = 5f;            // Reliability ลดต่อครั้งที่กด

    [Header("Sell Resource Settings")]
    [Range(0f, 1f)] public float sellBackRate = 0.70f;        // ขายคืน 70% ของราคาซื้อ

    [Header("Reliability Loss - Game Over")]
    public int reliabilityNegativeTurnCount = 0;              // นับเทิร์นที่ Reliability ติดลบ
    public int gameOverThreshold = 10;                        // Game Over ที่ 10 เทิร์น

    // ============================================================
    //  END TURN — เรียกจาก TurnManager เมื่อผู้เล่นกด End Turn
    // ============================================================
    public void OnEndTurn()
    {
        RollEconomyModifier();
        CollectAutoTax();
        CheckReliabilityGameOver();
    }

    // ============================================================x
    //  ECONOMY FLUCTUATION — สุ่ม modifier ใหม่ทุกเทิร์น
    //  0.5 = เศรษฐกิจแย่มาก | 1.0 = ปกติ | 1.5 = เศรษฐกิจดีมาก
    // ============================================================
    void RollEconomyModifier()
    {
        economyModifier = Random.Range(0.5f, 1.5f);
        Debug.Log($"[Economy] Economy Modifier this turn: {economyModifier:F2}");
    }

    // ============================================================
    //  AUTO TAX — เก็บอัตโนมัติทุก End Turn
    // ============================================================
    void CollectAutoTax()
    {
        float citizenIncome = CalculateTotalCitizenIncome();
        float tax = citizenIncome * taxRate;
        playerGold += tax;
        Debug.Log($"[Tax] Citizen Income: {citizenIncome:F0} | Auto Tax Collected: {tax:F0} | Gold: {playerGold:F0}");
    }

    // ============================================================
    //  รายได้รวมของประชาชน = ผลรวม (Base + EconomyBonus) ทุกหลัง
    // ============================================================
    public float CalculateTotalCitizenIncome()
    {
        float total = 0f;
        foreach (var b in buildings)
            total += b.GetBaseIncome() + b.GetEconomyBonus(economyModifier);

        // คูณ economy modifier กับ base income ด้วย (ส่งผลทุกหลัง)
        total *= economyModifier;
        return total;
    }

    // ============================================================
    //  MANUAL TAX — ผู้เล่นกดเก็บเพิ่มเอง (เรียกจากปุ่ม UI)
    // ============================================================
    public void CollectManualTax()
    {
        playerGold += manualTaxAmount;
        reliability -= manualTaxReliabilityPenalty;
        reliability = Mathf.Clamp(reliability, -100f, 100f);
        Debug.Log($"[Tax] Manual Tax: +{manualTaxAmount} Gold | Reliability: {reliability:F1}%");
    }

    // ============================================================
    //  SELL RESOURCE — ขายคืน Resource ที่ซื้อมา (70% ของราคาซื้อ)
    //  buyPrice = ราคาที่ซื้อมาในขณะนั้น
    //  quantity  = จำนวนที่ต้องการขายคืน
    // ============================================================
    public float SellResource(float buyPrice, int quantity)
    {
        float refund = buyPrice * quantity * sellBackRate;
        playerGold += refund;
        Debug.Log($"[Shop] Sold {quantity} units | Refund: {refund:F0} Gold | Gold: {playerGold:F0}");
        return refund;
    }

    // ============================================================
    //  RELIABILITY GAME OVER CHECK
    // ============================================================
    void CheckReliabilityGameOver()
    {
        if (reliability < 0f)
        {
            reliabilityNegativeTurnCount++;
            Debug.LogWarning($"[Reliability] Negative for {reliabilityNegativeTurnCount} turn(s)");

            if (reliabilityNegativeTurnCount >= gameOverThreshold)
                TriggerGameOver();
        }
        else
        {
            reliabilityNegativeTurnCount = 0; // reset ถ้า Reliability กลับมาบวก
        }
    }

    void TriggerGameOver()
    {
        Debug.LogError("[Game Over] Reliability was negative for 10 consecutive turns!");
        // TODO: เรียก GameManager.Instance.GameOver() ตรงนี้
    }

    // ============================================================
    //  VICTORY CHECK — เรียกหลังสร้างอนุสาวรีย์ชีส
    // ============================================================
    public bool CanBuildMonument(float monumentCost)
    {
        return playerGold >= monumentCost && reliability >= 70f;
        GameManager.Instance?.GameOver();
    }

    // ============================================================
    //  UPGRADE BUILDING — เรียกเมื่อผู้เล่น Upgrade สิ่งปลูกสร้าง
    //  และเพิ่ม Reliability
    // ============================================================
    public void UpgradeBuilding(Building building, float reliabilityGain)
    {
        if (building.level >= 3)
        {
            Debug.Log("[Building] Already max level!");
            return;
        }
        building.level++;
        reliability += reliabilityGain;
        reliability = Mathf.Clamp(reliability, -100f, 100f);
        Debug.Log($"[Building] Upgraded to Level {building.level} | Reliability: {reliability:F1}%");
    }

    void Update()
    {
        DisplayingGold.text = playerGold.ToString("N0");
    }
}

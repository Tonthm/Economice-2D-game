using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// ============================================================
//  BuildingManager.cs
//  ระบบจัดการ Building ทั้งหมดในแผนที่
//  - เพิ่ม Building ใหม่
//  - Upgrade Building (ใช้ Gold + AP)
//  - แสดง Popup รายละเอียด Building เมื่อกดคลิก
// ============================================================

[System.Serializable]
public class BuildingUpgradeCost
{
    public BuildingType type;
    public float[] costPerLevel = { 500f, 1200f }; // Lv1→2, Lv2→3
    public float[] reliabilityGain = { 5f, 10f };
    public int apCost = 1;
}

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("References")]
    public EconomySystem economySystem;
    public TurnManager   turnManager;

    [Header("Upgrade Costs")]
    public List<BuildingUpgradeCost> upgradeCosts = new List<BuildingUpgradeCost>();

    [Header("Upgrade UI Panel")]
    public GameObject upgradePanel;
    public TextMeshProUGUI upgradePanelTitle;
    public TextMeshProUGUI upgradePanelInfo;
    public Button upgradeConfirmButton;
    public Button upgradeCancelButton;

    private Building selectedBuilding;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
        upgradeConfirmButton?.onClick.AddListener(ConfirmUpgrade);
        upgradeCancelButton?.onClick.AddListener(CloseUpgradePanel);
    }

    // ============================================================
    //  เรียกเมื่อผู้เล่นคลิก Building บนแผนที่
    // ============================================================
    public void SelectBuilding(Building building)
    {
        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsVictory) return;
        selectedBuilding = building;
        ShowUpgradePanel(building);
    }

    void ShowUpgradePanel(Building b)
    {
        if (b.level >= 3)
        {
            Debug.Log("[Building] Max level!");
            return;
        }

        BuildingUpgradeCost costData = GetCostData(b.type);
        float cost = costData?.costPerLevel[b.level - 1] ?? 999f;
        float relGain = costData?.reliabilityGain[b.level - 1] ?? 5f;

        if (upgradePanelTitle != null) upgradePanelTitle.text = $"{b.type} (Lv {b.level} → {b.level + 1})";
        if (upgradePanelInfo  != null) upgradePanelInfo.text  =
            $"Cost: {cost:N0} Gold  |  AP: {costData?.apCost ?? 1}\n" +
            $"Reliability +{relGain}%\n" +
            $"Income: {b.GetBaseIncome():N0} → {GetNextLevelIncome(b):N0}";

        bool canAfford = economySystem.playerGold >= cost && turnManager.HasEnoughAP(costData?.apCost ?? 1);
        upgradeConfirmButton.interactable = canAfford;

        if (upgradePanel != null) upgradePanel.SetActive(true);
    }

    void ConfirmUpgrade()
    {
        if (selectedBuilding == null) return;
        BuildingUpgradeCost costData = GetCostData(selectedBuilding.type);
        float cost = costData?.costPerLevel[selectedBuilding.level - 1] ?? 999f;
        float relGain = costData?.reliabilityGain[selectedBuilding.level - 1] ?? 5f;
        int ap = costData?.apCost ?? 1;

        if (!turnManager.UseAP(ap)) { Debug.LogWarning("[Building] Not enough AP"); return; }
        if (economySystem.playerGold < cost) { Debug.LogWarning("[Building] Not enough Gold"); return; }

        economySystem.playerGold -= cost;
        economySystem.UpgradeBuilding(selectedBuilding, relGain);
        CloseUpgradePanel();
    }

    void CloseUpgradePanel()
    {
        selectedBuilding = null;
        if (upgradePanel != null) upgradePanel.SetActive(false);
    }

    // ============================================================
    //  HELPERS
    // ============================================================
    BuildingUpgradeCost GetCostData(BuildingType type) =>
        upgradeCosts.Find(c => c.type == type);

    float GetNextLevelIncome(Building b)
    {
        int nextLv = b.level + 1;
        switch (b.type)
        {
            case BuildingType.House:     return nextLv == 2 ? 180f : 280f;
            case BuildingType.Market:    return nextLv == 2 ? 350f : 520f;
            case BuildingType.Workshop:  return nextLv == 2 ? 240f : 370f;
            default: return 0f;
        }
    }

    // ============================================================
    //  เพิ่ม Building ใหม่ (เรียกจาก Project/Shop)
    // ============================================================
    public void AddBuilding(BuildingType type, int level = 1)
    {
        Building b = new Building { type = type, level = level };
        economySystem.buildings.Add(b);
        Debug.Log($"[Building] Added {type} Lv{level}");
    }
}

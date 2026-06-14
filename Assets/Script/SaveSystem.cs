using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ============================================================
//  SaveSystem.cs
//  บันทึก/โหลดข้อมูลเกมเป็น JSON ลงใน Application.persistentDataPath
//  รองรับ 2 สล็อต (SAVED#1, SAVED#2)
//  เรียกใช้: SaveSystem.Instance.Save(slot) / .Load(slot)
// ============================================================

[System.Serializable]
public class SaveData
{
    // Economy
    public float playerGold;
    public float reliability;
    public float economyModifier;
    public int   reliabilityNegativeTurnCount;

    // Turn
    public int currentTurn;
    public int currentAP;

    // Buildings
    public List<BuildingSaveEntry> buildings = new List<BuildingSaveEntry>();

    // Inventory
    public List<InventoryEntry> inventory = new List<InventoryEntry>();

    // Projects
    public List<ProjectSaveEntry> projects = new List<ProjectSaveEntry>();

    // Contracts
    public List<bool> contractsPurchased = new List<bool>();

    // Contract income bonus
    public float totalContractBonusPerTurn;

    // Difficulty
    public string difficulty;

    // Metadata
    public string saveDate;
    public int    saveTurn;
}

[System.Serializable]
public class BuildingSaveEntry
{
    public BuildingType type;
    public int level;
}

[System.Serializable]
public class ProjectSaveEntry
{
    public bool isCompleted;
    public List<int> deliveredQty = new List<int>(); // ตรงกับ index ของ requirements
}

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    [Header("References")]
    public EconomySystem  economySystem;
    public TurnManager    turnManager;
    public ResourceInventory inventory;
    public ProjectSystem  projectSystem;
    public ContractSystem contractSystem;
    public DifficultyManager difficultyManager;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ============================================================
    //  SAVE
    // ============================================================
    public void Save(int slot)
    {
        SaveData data = new SaveData
        {
            playerGold                   = economySystem.playerGold,
            reliability                  = economySystem.reliability,
            economyModifier              = economySystem.economyModifier,
            reliabilityNegativeTurnCount = economySystem.reliabilityNegativeTurnCount,
            currentTurn                  = turnManager.currentTurn,
            currentAP                    = turnManager.currentAP,
            totalContractBonusPerTurn    = GetContractBonus(),
            difficulty                   = difficultyManager != null ? difficultyManager.CurrentDifficulty : "Normal",
            saveDate                     = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            saveTurn                     = turnManager.currentTurn
        };

        // Buildings
        foreach (var b in economySystem.buildings)
            data.buildings.Add(new BuildingSaveEntry { type = b.type, level = b.level });

        // Inventory
        data.inventory = inventory.GetSaveData();

        // Projects
        foreach (var p in projectSystem.projects)
        {
            ProjectSaveEntry pe = new ProjectSaveEntry { isCompleted = p.isCompleted };
            foreach (var r in p.requirements) pe.deliveredQty.Add(r.deliveredQty);
            data.projects.Add(pe);
        }

        // Contracts
        foreach (var c in contractSystem.contracts)
            data.contractsPurchased.Add(c.isPurchased);

        string path = GetSavePath(slot);
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        Debug.Log($"[Save] Slot {slot} saved → {path}");
    }

    // ============================================================
    //  LOAD
    // ============================================================
    public bool Load(int slot)
    {
        string path = GetSavePath(slot);
        if (!File.Exists(path)) { Debug.LogWarning($"[Save] Slot {slot} not found"); return false; }

        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));

        // Economy
        economySystem.playerGold                   = data.playerGold;
        economySystem.reliability                  = data.reliability;
        economySystem.economyModifier              = data.economyModifier;
        economySystem.reliabilityNegativeTurnCount = data.reliabilityNegativeTurnCount;

        // Turn
        turnManager.currentTurn = data.currentTurn;
        turnManager.currentAP   = data.currentAP;

        // Buildings
        economySystem.buildings.Clear();
        foreach (var b in data.buildings)
            economySystem.buildings.Add(new Building { type = b.type, level = b.level });

        // Inventory
        inventory.LoadSaveData(data.inventory);

        // Projects
        for (int i = 0; i < Mathf.Min(data.projects.Count, projectSystem.projects.Count); i++)
        {
            projectSystem.projects[i].isCompleted = data.projects[i].isCompleted;
            for (int j = 0; j < Mathf.Min(data.projects[i].deliveredQty.Count,
                                           projectSystem.projects[i].requirements.Count); j++)
                projectSystem.projects[i].requirements[j].deliveredQty = data.projects[i].deliveredQty[j];
        }

        // Contracts
        for (int i = 0; i < Mathf.Min(data.contractsPurchased.Count, contractSystem.contracts.Count); i++)
            contractSystem.contracts[i].isPurchased = data.contractsPurchased[i];

        // Difficulty
        difficultyManager?.ApplyDifficulty(data.difficulty);

        Debug.Log($"[Save] Slot {slot} loaded from {path}");
        return true;
    }

    // ============================================================
    //  HELPERS
    // ============================================================
    public bool SlotExists(int slot) => File.Exists(GetSavePath(slot));

    public string GetSlotInfo(int slot)
    {
        string path = GetSavePath(slot);
        if (!File.Exists(path)) return "Empty";
        SaveData d = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        return $"Turn {d.saveTurn}  |  {d.saveDate}  |  {d.difficulty}";
    }

    string GetSavePath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"save_slot{slot}.json");

    float GetContractBonus()
    {
        float total = 0f;
        if (contractSystem == null) return 0f;
        foreach (var c in contractSystem.contracts)
            if (c.isPurchased) total += c.goldBonusPerTurn;
        return total;
    }
}

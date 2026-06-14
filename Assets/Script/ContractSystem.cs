using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// ============================================================
//  ContractSystem.cs
//  ระบบสัญญาจ้าง:
//  - ผู้เล่นซื้อ Contract ด้วย Gold + AP
//  - Contract ให้ Reliability / Income Bonus / Building ใหม่
//  - บาง Contract ทำให้ Reliability ลด (ประชาชนไม่เห็นด้วย)
// ============================================================

[System.Serializable]
public class ContractData
{
    public string contractName;
    [TextArea] public string description;
    public Sprite icon;

    public float goldCost;
    public int   apCost;
    public float reliabilityChange;   // บวก/ลบ
    public float goldBonusPerTurn;    // รายได้ต่อเทิร์นที่ได้เพิ่ม
    public BuildingType? addBuilding; // ถ้า != null จะเพิ่ม Building ใหม่ให้

    [HideInInspector] public bool isPurchased;
}

public class ContractSystem : MonoBehaviour
{
    public static ContractSystem Instance { get; private set; }

    [Header("References")]
    public EconomySystem  economySystem;
    public TurnManager    turnManager;
    public BuildingManager buildingManager;

    [Header("Contracts")]
    public List<ContractData> contracts = new List<ContractData>();

    [Header("UI")]
    public GameObject contractPanel;
    public Transform  contractListParent;
    public GameObject contractRowPrefab;

    // รายได้พิเศษสะสมจาก Contract ที่ซื้อแล้ว
    private float totalContractBonusPerTurn = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (contractPanel != null) contractPanel.SetActive(false);
        if (contracts.Count == 0) SetupDefaultContracts();
        BuildUI();
    }

    void SetupDefaultContracts()
    {
        contracts.Add(new ContractData
        {
            contractName    = "🚛 Transport Network",
            description     = "Build a cart road connecting villages. Boosts trade income every turn.",
            goldCost        = 3000f, apCost = 2,
            reliabilityChange = 8f,  goldBonusPerTurn = 200f
        });
        contracts.Add(new ContractData
        {
            contractName    = "🏢 Office Tower",
            description     = "A new administrative building. Citizens are unsure about it.",
            goldCost        = 4000f, apCost = 3,
            reliabilityChange = -5f, goldBonusPerTurn = 400f
        });
        contracts.Add(new ContractData
        {
            contractName    = "💨 Wind Farm",
            description     = "Clean energy for the village! Citizens love it.",
            goldCost        = 5000f, apCost = 3,
            reliabilityChange = 12f, goldBonusPerTurn = 300f
        });
        contracts.Add(new ContractData
        {
            contractName    = "📡 Signal Tower",
            description     = "Improve communications. Speeds up contract resolution.",
            goldCost        = 2500f, apCost = 2,
            reliabilityChange = 5f,  goldBonusPerTurn = 150f
        });
        contracts.Add(new ContractData
        {
            contractName    = "🏪 New Market",
            description     = "Add a new Market building to your village.",
            goldCost        = 2000f, apCost = 1,
            reliabilityChange = 10f, goldBonusPerTurn = 0f,
            addBuilding     = BuildingType.Market
        });
    }

    // ============================================================
    //  BUY CONTRACT
    // ============================================================
    public bool PurchaseContract(int idx)
    {
        if (idx < 0 || idx >= contracts.Count) return false;
        ContractData c = contracts[idx];
        if (c.isPurchased) { Debug.Log("[Contract] Already purchased"); return false; }

        if (economySystem.playerGold < c.goldCost)
        { Debug.LogWarning("[Contract] Not enough Gold"); return false; }

        if (!turnManager.UseAP(c.apCost))
        { Debug.LogWarning("[Contract] Not enough AP"); return false; }

        // Deduct cost
        economySystem.playerGold -= c.goldCost;

        // Apply reliability
        economySystem.reliability += c.reliabilityChange;
        economySystem.reliability  = Mathf.Clamp(economySystem.reliability, -100f, 100f);

        // Add income bonus
        totalContractBonusPerTurn += c.goldBonusPerTurn;

        // Add Building if applicable
        if (c.addBuilding.HasValue)
            buildingManager?.AddBuilding(c.addBuilding.Value);

        c.isPurchased = true;
        Debug.Log($"[Contract] Purchased: {c.contractName}");
        BuildUI();
        return true;
    }

    // ============================================================
    //  END TURN — รับรายได้จาก Contract
    // ============================================================
    public void OnEndTurn()
    {
        if (totalContractBonusPerTurn > 0)
        {
            economySystem.playerGold += totalContractBonusPerTurn;
            Debug.Log($"[Contract] Bonus income: +{totalContractBonusPerTurn:N0} Gold");
        }
    }

    // ============================================================
    //  UI
    // ============================================================
    public void OpenPanel()  { contractPanel?.SetActive(true);  BuildUI(); }
    public void ClosePanel() { contractPanel?.SetActive(false); }

    void BuildUI()
    {
        if (contractListParent == null || contractRowPrefab == null) return;
        foreach (Transform t in contractListParent) Destroy(t.gameObject);

        for (int i = 0; i < contracts.Count; i++)
        {
            int idx = i;
            GameObject row = Instantiate(contractRowPrefab, contractListParent);
            ContractRow cr = row.GetComponent<ContractRow>();
            cr?.Init(contracts[i], () => PurchaseContract(idx));
        }
    }
}

// ============================================================
//  ContractRow — component บน Prefab แต่ละ Contract
// ============================================================
public class ContractRow : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI costText;
    public Button          buyButton;

    public void Init(ContractData data, System.Action onBuy)
    {
        if (nameText != null) nameText.text = data.isPurchased ? $"✅ {data.contractName}" : data.contractName;
        if (descText != null) descText.text = data.description;
        if (costText != null)
        {
            string rel = data.reliabilityChange >= 0 ? $"+{data.reliabilityChange}" : $"{data.reliabilityChange}";
            costText.text = $"Cost: {data.goldCost:N0} 🧀  AP: {data.apCost}  Rel: {rel}%";
            if (data.goldBonusPerTurn > 0)
                costText.text += $"  Income/turn: +{data.goldBonusPerTurn:N0}";
        }
        if (buyButton != null)
        {
            buyButton.interactable = !data.isPurchased;
            buyButton.onClick.AddListener(() => onBuy());
        }
    }
}

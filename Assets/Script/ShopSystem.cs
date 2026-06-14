using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// ============================================================
//  ShopSystem.cs
//  ระบบร้านค้า:
//  - ราคาผันผวนตาม economyModifier และ demand (ซื้อมาก = แพงขึ้น)
//  - ขายคืน 70% ของราคาที่ซื้อมา (ผ่าน EconomySystem.SellResource)
//  - ใช้ AP ต่อการซื้อ 1 ครั้ง
// ============================================================

[System.Serializable]
public class ShopItem
{
    public string itemName;
    public Sprite icon;
    public float basePrice;          // ราคามาตรฐาน
    [HideInInspector] public float currentPrice;  // ราคาหลังผันผวน
    [HideInInspector] public float demandMultiplier = 1f; // เพิ่มเมื่อซื้อมาก

    public const float MaxPriceMultiplier = 3f; // เพดาน 3x

    public void RecalculatePrice(float economyModifier)
    {
        float raw = basePrice * economyModifier * demandMultiplier;
        currentPrice = Mathf.Clamp(raw, basePrice * 0.5f, basePrice * MaxPriceMultiplier);
    }

    // เพิ่ม demand เมื่อซื้อ, ค่อยๆ ลดกลับทุก End Turn
    public void OnBought(int qty)    => demandMultiplier = Mathf.Clamp(demandMultiplier + qty * 0.05f, 1f, 3f);
    public void DecayDemand()        => demandMultiplier = Mathf.Lerp(demandMultiplier, 1f, 0.2f);
}

public class ShopSystem : MonoBehaviour
{
    public static ShopSystem Instance { get; private set; }

    [Header("References")]
    public EconomySystem    economySystem;
    public TurnManager      turnManager;
    public ResourceInventory inventory;       // Week 2 script

    [Header("Shop Items")]
    public List<ShopItem> items = new List<ShopItem>();

    [Header("UI")]
    public GameObject shopPanel;
    public Transform  itemListParent;       // Layout Group ใส่ item rows
    public GameObject itemRowPrefab;        // Prefab: icon | name | price | qty +/- | Buy | Sell

    [Header("AP per purchase")]
    public int apPerBuy = 1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        RefreshPrices();
        BuildShopUI();
    }

    // ============================================================
    //  เปิด/ปิด Shop
    // ============================================================
    public void OpenShop()  { shopPanel?.SetActive(true);  RefreshPrices(); RebuildUI(); }
    public void CloseShop() { shopPanel?.SetActive(false); }

    // ============================================================
    //  BUY
    // ============================================================
    public bool BuyItem(int itemIndex, int qty)
    {
        if (itemIndex < 0 || itemIndex >= items.Count) return false;
        ShopItem item = items[itemIndex];
        float total = item.currentPrice * qty;

        if (economySystem.playerGold < total)
        { Debug.LogWarning("[Shop] Not enough Gold"); return false; }

        if (!turnManager.UseAP(apPerBuy))
        { Debug.LogWarning("[Shop] Not enough AP"); return false; }

        economySystem.playerGold -= total;
        inventory.AddItem(item.itemName, qty, item.currentPrice);
        item.OnBought(qty);

        Debug.Log($"[Shop] Bought {qty}x {item.itemName} for {total:N0} Gold");
        RebuildUI();
        return true;
    }

    // ============================================================
    //  SELL — ใช้ EconomySystem.SellResource (คืน 70%)
    // ============================================================
    public bool SellItem(int itemIndex, int qty)
    {
        if (itemIndex < 0 || itemIndex >= items.Count) return false;
        ShopItem item = items[itemIndex];

        float avgBuyPrice = inventory.GetAverageBuyPrice(item.itemName);
        int available = inventory.GetQuantity(item.itemName);
        if (available < qty) { Debug.LogWarning("[Shop] Not enough items to sell"); return false; }

        economySystem.SellResource(avgBuyPrice, qty);
        inventory.RemoveItem(item.itemName, qty);

        Debug.Log($"[Shop] Sold {qty}x {item.itemName}");
        RebuildUI();
        return true;
    }

    // ============================================================
    //  END TURN HOOK — เรียกจาก TurnManager หลัง OnEndTurn
    // ============================================================
    public void OnEndTurn()
    {
        foreach (var item in items)
        {
            item.DecayDemand();
            item.RecalculatePrice(economySystem.economyModifier);
        }
    }

    // ============================================================
    //  PRICE REFRESH
    // ============================================================
    void RefreshPrices()
    {
        foreach (var item in items)
            item.RecalculatePrice(economySystem.economyModifier);
    }

    // ============================================================
    //  UI BUILD (simple — instantiate rows)
    // ============================================================
    void BuildShopUI()
    {
        if (itemListParent == null || itemRowPrefab == null) return;
        foreach (Transform t in itemListParent) Destroy(t.gameObject);

        for (int i = 0; i < items.Count; i++)
        {
            int idx = i; // closure
            GameObject row = Instantiate(itemRowPrefab, itemListParent);
            ShopItemRow uiRow = row.GetComponent<ShopItemRow>();
            if (uiRow != null) uiRow.Init(items[i], () => BuyItem(idx, 1), () => SellItem(idx, 1));
        }
    }

    void RebuildUI() => BuildShopUI();
}

// ============================================================
//  ShopItemRow.cs (ใส่ใน Prefab row)
//  Assign ผ่าน Init() จาก ShopSystem
// ============================================================
public class ShopItemRow : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI stockText;
    public Button buyButton;
    public Button sellButton;

    public void Init(ShopItem item, System.Action onBuy, System.Action onSell)
    {
        if (nameText  != null) nameText.text  = item.itemName;
        if (priceText != null) priceText.text = $"{item.currentPrice:N0} 🧀";
        if (stockText != null) stockText.text = $"x{ShopSystem.Instance?.inventory?.GetQuantity(item.itemName) ?? 0}";
        buyButton?.onClick.AddListener(() => onBuy());
        sellButton?.onClick.AddListener(() => onSell());
    }
}

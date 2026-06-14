using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  ResourceInventory.cs
//  คลังทรัพยากรของผู้เล่น
//  - เก็บ quantity และ average buy price ต่อ item
//  - ใช้โดย ShopSystem และ ProjectSystem
// ============================================================

[System.Serializable]
public class InventoryEntry
{
    public string itemName;
    public int    quantity;
    public float  totalSpent;   // ใช้คำนวณ average buy price

    public float GetAverageBuyPrice() => quantity > 0 ? totalSpent / quantity : 0f;
}

public class ResourceInventory : MonoBehaviour
{
    public static ResourceInventory Instance { get; private set; }

    public List<InventoryEntry> entries = new List<InventoryEntry>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ============================================================
    //  ADD
    // ============================================================
    public void AddItem(string itemName, int qty, float buyPriceEach)
    {
        InventoryEntry e = GetOrCreate(itemName);
        e.quantity   += qty;
        e.totalSpent += buyPriceEach * qty;
    }

    // ============================================================
    //  REMOVE — return false ถ้าไม่พอ
    // ============================================================
    public bool RemoveItem(string itemName, int qty)
    {
        InventoryEntry e = GetEntry(itemName);
        if (e == null || e.quantity < qty) return false;
        float avgPrice = e.GetAverageBuyPrice();
        e.quantity   -= qty;
        e.totalSpent -= avgPrice * qty;
        e.totalSpent  = Mathf.Max(0, e.totalSpent);
        return true;
    }

    // ============================================================
    //  QUERY
    // ============================================================
    public int   GetQuantity(string itemName)        => GetEntry(itemName)?.quantity ?? 0;
    public float GetAverageBuyPrice(string itemName) => GetEntry(itemName)?.GetAverageBuyPrice() ?? 0f;
    public bool  HasItems(string itemName, int qty)  => GetQuantity(itemName) >= qty;

    // ============================================================
    //  HELPERS
    // ============================================================
    InventoryEntry GetEntry(string itemName) => entries.Find(e => e.itemName == itemName);

    InventoryEntry GetOrCreate(string itemName)
    {
        InventoryEntry e = GetEntry(itemName);
        if (e == null) { e = new InventoryEntry { itemName = itemName }; entries.Add(e); }
        return e;
    }

    // ============================================================
    //  SAVE / LOAD SUPPORT (เรียกจาก SaveSystem Week 4)
    // ============================================================
    public List<InventoryEntry> GetSaveData() => entries;

    public void LoadSaveData(List<InventoryEntry> data)
    {
        entries = data ?? new List<InventoryEntry>();
    }
}

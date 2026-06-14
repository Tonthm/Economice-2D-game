# Economice — Unity Setup Guide

## ไฟล์ทั้งหมดที่ได้รับ

```
Week1/
  GameManager.cs         ← GameOver + Victory logic (Singleton)
  HUDController.cs       ← อัปเดต UI ทุก Frame + ผูกปุ่มทั้งหมด
  GameOverScreen.cs      ← Panel Game Over
  VictoryScreen.cs       ← Panel Victory
  EconomySystem_Updated  ← แก้ TriggerGameOver() 1 บรรทัด (ดูข้างใน)

Week2/
  BuildingManager.cs     ← Upgrade Building + Popup
  ShopSystem.cs          ← ซื้อ/ขาย Resource + demand fluctuation
  ResourceInventory.cs   ← คลังทรัพยากรของผู้เล่น
  ProjectSystem.cs       ← ส่ง Resource ฟื้นฟูเมือง

Week3/
  RandomEventSystem.cs   ← Event ทุก 5 เทิร์น แบบ non-repeating
  ContractSystem.cs      ← ซื้อ Contract ด้วย Gold + AP

Week4/
  SaveSystem.cs          ← JSON Save/Load 2 สล็อต
  DifficultyManager.cs   ← Easy / Normal / Hard
  LoadGameMenu.cs        ← UI เลือก Save Slot
  TurnManager_Updated.cs ← แทนที่ TurnManager.cs เดิม (hook ทุก system)
```

---

## วิธี Setup ใน Unity (ทำตามลำดับ)

### Step 1 — แก้ EconomySystem.cs เดิม
ค้นหาฟังก์ชัน `TriggerGameOver()` แล้วเปลี่ยน:
```csharp
// TODO: เรียก GameManager.Instance.GameOver() ตรงนี้
```
เป็น:
```csharp
GameManager.Instance?.GameOver();
```

### Step 2 — แทนที่ TurnManager.cs
ใช้ `TurnManager_Updated.cs` แทน `TurnManager.cs` เดิม (rename ไฟล์เป็น `TurnManager.cs`)

### Step 3 — สร้าง GameObjects ใน Scene หลัก
สร้าง Empty GameObjects แล้ว Add Component ตามนี้:

| GameObject Name   | Components                              |
|-------------------|-----------------------------------------|
| GameManager       | GameManager                             |
| TurnManager       | TurnManager                             |
| EconomySystem     | EconomySystem                           |
| ResourceInventory | ResourceInventory                       |
| BuildingManager   | BuildingManager                         |
| ShopSystem        | ShopSystem                              |
| ProjectSystem     | ProjectSystem                           |
| RandomEventSystem | RandomEventSystem                       |
| ContractSystem    | ContractSystem                          |
| SaveSystem        | SaveSystem                              |
| DifficultyManager | DifficultyManager (DontDestroyOnLoad)   |
| HUD               | HUDController                           |

### Step 4 — Assign References ใน Inspector
**GameManager:**
- `economySystem` → EconomySystem GameObject
- `turnManager` → TurnManager GameObject
- `gameOverScreen` → GameOverScreen Panel
- `victoryScreen` → VictoryScreen Panel

**TurnManager (Updated):**
- `economySystem` → EconomySystem
- `randomEventSystem` → RandomEventSystem
- `contractSystem` → ContractSystem
- `shopSystem` → ShopSystem

**HUDController:**
- Assign ทุก TextMeshProUGUI และ Button ใน Inspector
- ผูก `endTurnButton` → ปุ่ม End Turn ใน Canvas

**ShopSystem:**
- `inventory` → ResourceInventory
- `economySystem` → EconomySystem
- `turnManager` → TurnManager

**ProjectSystem:**
- `economySystem`, `inventory`, `turnManager`

**RandomEventSystem:**
- `economySystem`, `inventory`

**ContractSystem:**
- `economySystem`, `turnManager`, `buildingManager`

**SaveSystem:**
- Assign ทุก references

### Step 5 — ตั้งค่า Shop Items ใน Inspector
ใน ShopSystem → Items ให้เพิ่ม:
- Cement: basePrice = 1000
- Lumber: basePrice = 930 (แสดงเป็น 85% ตาม economy)
- Engineer: basePrice = 500
- Wood: basePrice = 750
- Labor: basePrice = 300

### Step 6 — ตั้งค่า Building Upgrade Costs ใน Inspector
ใน BuildingManager → Upgrade Costs เพิ่ม:
- House:    [500, 1200] Gold | [5%, 10%] Reliability | AP: 1
- Market:   [800, 2000] Gold | [5%, 12%] Reliability | AP: 1
- Workshop: [600, 1500] Gold | [5%, 10%] Reliability | AP: 1

### Step 7 — สร้าง UI Panels
1. **HUD** (ซ้ายบน): Gold, Reliability, AP, Turn icons
2. **Shop Panel**: Tab SHOP / RESOURCES / PROJECT / CONTRACT
3. **Event Panel**: Title, Desc, Cost, [RESOLVE] [IGNORE]
4. **Upgrade Panel**: Title, Info, [UPGRADE] [CANCEL]
5. **Game Over Panel**: Message, [RETRY] [MENU]
6. **Victory Panel**: Message, [PLAY AGAIN] [MENU]
7. **Save/Load Panel**: Slot1 + Slot2 info + [LOAD] [SAVE]

### Step 8 — Difficulty Scene
สร้าง Scene ชื่อ "DifficultyMenu" มีปุ่ม 3 ปุ่ม:
```csharp
// ผูกแต่ละปุ่ม onClick:
DifficultyManager.Instance.SetDifficulty("Easy");
DifficultyManager.Instance.SetDifficulty("Normal");
DifficultyManager.Instance.SetDifficulty("Hard");
```

---

## Default Projects (สร้างอัตโนมัติถ้าไม่ตั้งใน Inspector)
1. Restore Town Hall → Cement x5, Labor x3 → Reliability +15%
2. Repair Market → Lumber x4, Engineer x2 → Reliability +10%
3. Rebuild Homes → Cement x3, Lumber x3, Labor x4 → Reliability +10%

## Default Contracts
1. Transport Network — 3000G, AP2, Rel+8, Income+200/turn
2. Office Tower — 4000G, AP3, Rel-5, Income+400/turn
3. Wind Farm — 5000G, AP3, Rel+12, Income+300/turn
4. Signal Tower — 2500G, AP2, Rel+5, Income+150/turn
5. New Market — 2000G, AP1, Rel+10, เพิ่ม Market Building

## Default Random Events (5 events, non-repeating)
- 🏚️ Building Collapse
- 🔥 Fire Outbreak
- 🐹 Hamster Raiders
- 🌊 Flash Flood
- 🎉 Harvest Festival
```

---

## Victory Condition Check
`EconomySystem.CanBuildMonument(cost)` ตรวจ:
- `playerGold >= monumentCost`
- `reliability >= 70f`

เรียกผ่าน `GameManager.TryBuildMonument()` จากปุ่ม Build Monument ใน HUD
(ปุ่มจะโผล่เมื่อ AllProjectsComplete == true → ตรวจจาก ProjectSystem)

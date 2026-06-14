using UnityEngine;
using TMPro;
using UnityEngine.UI;

// ============================================================
//  LoadGameMenu.cs
//  หน้าจอเลือก Save Slot เพื่อโหลดหรือบันทึก
//  วาง Script นี้บน Panel ที่มีปุ่ม Slot 1 และ Slot 2
// ============================================================

public class LoadGameMenu : MonoBehaviour
{
    [Header("References")]
    public SaveSystem saveSystem;

    [Header("Slot UI")]
    public TextMeshProUGUI slot1InfoText;
    public TextMeshProUGUI slot2InfoText;
    public Button slot1LoadButton;
    public Button slot2LoadButton;
    public Button slot1SaveButton;
    public Button slot2SaveButton;

    void OnEnable() => RefreshSlots();

    void RefreshSlots()
    {
        if (saveSystem == null) return;

        slot1InfoText.text = saveSystem.SlotExists(1) ? saveSystem.GetSlotInfo(1) : "— Empty —";
        slot2InfoText.text = saveSystem.SlotExists(2) ? saveSystem.GetSlotInfo(2) : "— Empty —";

        slot1LoadButton.interactable = saveSystem.SlotExists(1);
        slot2LoadButton.interactable = saveSystem.SlotExists(2);
    }

    public void OnLoadSlot1() { saveSystem.Load(1); gameObject.SetActive(false); }
    public void OnLoadSlot2() { saveSystem.Load(2); gameObject.SetActive(false); }
    public void OnSaveSlot1() { saveSystem.Save(1); RefreshSlots(); }
    public void OnSaveSlot2() { saveSystem.Save(2); RefreshSlots(); }

    public void OnClose() => gameObject.SetActive(false);
}

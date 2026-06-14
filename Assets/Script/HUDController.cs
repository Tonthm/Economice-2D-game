using UnityEngine;
using TMPro;
using UnityEngine.UI;

// ============================================================
//  HUDController.cs
//  อัปเดต UI หลักทุก Frame:
//  - Gold, Reliability, AP, Turn (ซ้ายบน / ขวาบน)
//  - ปุ่ม End Turn
//  - ปุ่ม Manual Tax
//  - ปุ่ม Build Monument (ปลดล็อกเมื่อครบเงื่อนไข)
// ============================================================

public class HUDController : MonoBehaviour
{
    [Header("References")]
    public EconomySystem economySystem;
    public TurnManager   turnManager;
    public GameManager   gameManager;

    [Header("HUD Text Elements")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI reliabilityText;
    public TextMeshProUGUI apText;
    public TextMeshProUGUI turnText;

    [Header("Buttons")]
    public Button endTurnButton;
    public Button manualTaxButton;
    public Button buildMonumentButton;

    [Header("Reliability Bar (optional)")]
    public Slider reliabilitySlider;

    void Start()
    {
        // ผูก Button Events
        endTurnButton.onClick.AddListener(OnEndTurnClicked);
        manualTaxButton.onClick.AddListener(OnManualTaxClicked);
        if (buildMonumentButton != null)
            buildMonumentButton.onClick.AddListener(OnBuildMonumentClicked);
    }

    void Update()
    {
        RefreshHUD();
    }

    // ============================================================
    //  REFRESH — อัปเดตค่าทุก Frame
    // ============================================================
    void RefreshHUD()
    {
        if (goldText        != null) goldText.text        = $"🧀 {economySystem.playerGold:N0}";
        if (reliabilityText != null) reliabilityText.text = $"♥ {economySystem.reliability:F0}%";
        if (apText          != null) apText.text          = $"AP: {turnManager.currentAP}";
        if (turnText        != null) turnText.text        = $"Turn: {turnManager.currentTurn}";

        if (reliabilitySlider != null)
        {
            reliabilitySlider.minValue = -100f;
            reliabilitySlider.maxValue = 100f;
            reliabilitySlider.value    = economySystem.reliability;
        }

        // ปุ่ม End Turn / Manual Tax ปิดเมื่อ Game Over หรือ Victory
        bool gameActive = !gameManager.IsGameOver && !gameManager.IsVictory;
        endTurnButton.interactable    = gameActive;
        manualTaxButton.interactable  = gameActive;

        // ปุ่ม Build Monument แสดงเมื่อครบเงื่อนไข
        if (buildMonumentButton != null)
            buildMonumentButton.gameObject.SetActive(
                gameActive && economySystem.CanBuildMonument(gameManager.monumentCost));
    }

    // ============================================================
    //  BUTTON CALLBACKS
    // ============================================================
    void OnEndTurnClicked()    => turnManager.EndTurn();
    void OnManualTaxClicked()  => economySystem.CollectManualTax();
    void OnBuildMonumentClicked() => gameManager.TryBuildMonument();
}

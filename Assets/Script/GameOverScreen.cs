using UnityEngine;
using TMPro;

// ============================================================
//  GameOverScreen.cs
//  Panel ที่โผล่เมื่อแพ้ (Reliability ติดลบ 10 เทิร์นติดกัน)
//  วาง GameObject นี้ไว้ใน Canvas แล้ว SetActive(false) ตั้งต้น
// ============================================================

public class GameOverScreen : MonoBehaviour
{
    public TextMeshProUGUI messageText;

    public void Show(int survivedTurns)
    {
        gameObject.SetActive(true);
        if (messageText != null)
            messageText.text = $"The villagers have lost faith in you...\nYou lasted {survivedTurns} turns.";
    }

    // ผูกกับปุ่ม Retry ใน Inspector
    public void OnRetryClicked()   => GameManager.Instance.RestartGame();
    public void OnMenuClicked()    => GameManager.Instance.GoToMainMenu();
}

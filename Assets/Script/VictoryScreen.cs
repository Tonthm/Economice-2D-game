using UnityEngine;
using TMPro;

// ============================================================
//  VictoryScreen.cs
//  Panel ที่โผล่เมื่อชนะ (สร้างอนุสาวรีย์ชีสสำเร็จ)
// ============================================================

public class VictoryScreen : MonoBehaviour
{
    public TextMeshProUGUI messageText;

    public void Show(int turns, float goldLeft)
    {
        gameObject.SetActive(true);
        if (messageText != null)
            messageText.text = $"The Cheese Monument stands tall!\nCompleted in {turns} turns.\nGold remaining: {goldLeft:N0} 🧀";
    }

    public void OnPlayAgainClicked() => GameManager.Instance.RestartGame();
    public void OnMenuClicked()      => GameManager.Instance.GoToMainMenu();
}

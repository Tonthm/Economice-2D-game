using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
//  DifficultyManager.cs
//  ตั้งค่าเริ่มต้นตามโหมดความยาก Easy / Normal / Hard
//  - เรียก SetDifficulty() จากหน้า Difficulty Selection
//  - เรียก ApplyDifficulty() ตอน Load ด้วย
// ============================================================

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("References")]
    public EconomySystem economySystem;
    public TurnManager   turnManager;
    public GameManager   gameManager;

    public string CurrentDifficulty { get; private set; } = "Normal";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ============================================================
    //  เรียกจากหน้า Difficulty Selection UI
    // ============================================================
    public void SetDifficulty(string difficulty)
    {
        CurrentDifficulty = difficulty;
        PlayerPrefs.SetString("SelectedDifficulty", difficulty);
        Debug.Log($"[Difficulty] Selected: {difficulty}");
        SceneManager.LoadScene("GameScene");
    }

    // ============================================================
    //  ใช้ตั้งค่า EconomySystem ตอนเริ่มเกมหรือ Load
    // ============================================================
    public void ApplyDifficulty(string difficulty)
    {
        CurrentDifficulty = difficulty;
        if (economySystem == null) return;

        switch (difficulty)
        {
            case "Easy":
                economySystem.playerGold  = 5000f;
                economySystem.reliability = 50f;
                economySystem.taxRate     = 0.25f;
                economySystem.gameOverThreshold = 15;
                if (gameManager != null) gameManager.monumentCost = 30000f;
                break;

            case "Hard":
                economySystem.playerGold  = 1500f;
                economySystem.reliability = 20f;
                economySystem.taxRate     = 0.15f;
                economySystem.gameOverThreshold = 5;
                if (gameManager != null) gameManager.monumentCost = 80000f;
                break;

            default: // Normal
                economySystem.playerGold  = 3000f;
                economySystem.reliability = 30f;
                economySystem.taxRate     = 0.20f;
                economySystem.gameOverThreshold = 10;
                if (gameManager != null) gameManager.monumentCost = 50000f;
                break;
        }
        Debug.Log($"[Difficulty] Applied: {difficulty} | Gold: {economySystem.playerGold} | Reliability: {economySystem.reliability}");
    }
}

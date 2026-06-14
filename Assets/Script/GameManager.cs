using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
//  GameManager.cs
//  จัดการสถานะหลักของเกม: GameOver และ Victory
//  - Singleton เข้าถึงได้จากทุก Script
//  - เรียก GameOver() จาก EconomySystem เมื่อ Reliability ติดลบนาน
//  - เรียก Victory() เมื่อผู้เล่นสร้างอนุสาวรีย์ชีสสำเร็จ
// ============================================================

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public EconomySystem economySystem;
    public TurnManager turnManager;
    public GameOverScreen gameOverScreen;   // assign ใน Inspector
    public VictoryScreen victoryScreen;     // assign ใน Inspector

    [Header("Victory Settings")]
    public float monumentCost = 50000f;

    public bool IsGameOver { get; private set; } = false;
    public bool IsVictory  { get; private set; } = false;

    // ============================================================
    //  SINGLETON
    // ============================================================
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ============================================================
    //  GAME OVER — เรียกจาก EconomySystem
    // ============================================================
    public void GameOver()
    {
        if (IsGameOver || IsVictory) return;
        IsGameOver = true;
        Debug.LogError("[GameManager] GAME OVER!");
        if (gameOverScreen != null)
            gameOverScreen.Show(turnManager.currentTurn);
    }

    // ============================================================
    //  VICTORY — เรียกเมื่อผู้เล่นกดสร้างอนุสาวรีย์ชีส
    // ============================================================
    public void TryBuildMonument()
    {
        if (IsGameOver || IsVictory) return;

        if (!economySystem.CanBuildMonument(monumentCost))
        {
            Debug.LogWarning("[GameManager] Cannot build monument: Not enough Gold or Reliability < 70%");
            // TODO: แสดง popup แจ้งเหตุผล
            return;
        }

        economySystem.playerGold -= monumentCost;
        IsVictory = true;
        Debug.Log("[GameManager] VICTORY! Monument built!");
        if (victoryScreen != null)
            victoryScreen.Show(turnManager.currentTurn, economySystem.playerGold);
    }

    // ============================================================
    //  SCENE HELPERS — ใช้กับปุ่ม Retry / Main Menu
    // ============================================================
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  ProjectSystem.cs
//  ระบบโครงการซ่อมแซม/พัฒนาเมือง
//  - แต่ละ Project ต้องการ Resource หลายชนิด
//  - ผู้เล่นส่ง Resource → ความคืบหน้าเพิ่ม
//  - เมื่อครบทุก Project → ปลดล็อกการสร้าง Monument
// ============================================================

[System.Serializable]
public class ResourceRequirement
{
    public string itemName;
    public int    requiredQty;
    [HideInInspector] public int deliveredQty;

    public bool IsMet => deliveredQty >= requiredQty;
    public int  Remaining => Mathf.Max(0, requiredQty - deliveredQty);
}

[System.Serializable]
public class ProjectData
{
    public string projectName;
    public List<ResourceRequirement> requirements = new List<ResourceRequirement>();
    public float reliabilityReward = 10f;
    [HideInInspector] public bool isCompleted;

    public float GetProgress()
    {
        if (requirements.Count == 0) return 1f;
        float met = 0;
        foreach (var r in requirements) met += Mathf.Clamp01((float)r.deliveredQty / r.requiredQty);
        return met / requirements.Count;
    }
}

public class ProjectSystem : MonoBehaviour
{
    public static ProjectSystem Instance { get; private set; }

    [Header("References")]
    public EconomySystem    economySystem;
    public ResourceInventory inventory;
    public TurnManager      turnManager;

    [Header("Projects — configure in Inspector")]
    public List<ProjectData> projects = new List<ProjectData>();

    [Header("UI")]
    public GameObject projectPanel;
    public Transform  projectListParent;
    public GameObject projectRowPrefab;

    public bool AllProjectsComplete => projects.TrueForAll(p => p.isCompleted);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (projectPanel != null) projectPanel.SetActive(false);
        // Default Projects หากไม่ได้ตั้งใน Inspector
        if (projects.Count == 0) SetupDefaultProjects();
        BuildUI();
    }

    void SetupDefaultProjects()
    {
        projects.Add(new ProjectData
        {
            projectName = "Restore Town Hall",
            reliabilityReward = 15f,
            requirements = new List<ResourceRequirement>
            {
                new ResourceRequirement { itemName = "Cement",   requiredQty = 5 },
                new ResourceRequirement { itemName = "Labor",    requiredQty = 3 }
            }
        });
        projects.Add(new ProjectData
        {
            projectName = "Repair Market",
            reliabilityReward = 10f,
            requirements = new List<ResourceRequirement>
            {
                new ResourceRequirement { itemName = "Lumber",   requiredQty = 4 },
                new ResourceRequirement { itemName = "Engineer", requiredQty = 2 }
            }
        });
        projects.Add(new ProjectData
        {
            projectName = "Rebuild Homes",
            reliabilityReward = 10f,
            requirements = new List<ResourceRequirement>
            {
                new ResourceRequirement { itemName = "Cement",   requiredQty = 3 },
                new ResourceRequirement { itemName = "Lumber",   requiredQty = 3 },
                new ResourceRequirement { itemName = "Labor",    requiredQty = 4 }
            }
        });
    }

    // ============================================================
    //  OPEN / CLOSE
    // ============================================================
    public void OpenPanel()  { projectPanel?.SetActive(true);  BuildUI(); }
    public void ClosePanel() { projectPanel?.SetActive(false); }

    // ============================================================
    //  ส่ง Resource เข้า Project
    //  itemName: ชนิด resource, projectIndex: index ใน list
    // ============================================================
    public bool ContributeResource(int projectIndex, string itemName, int qty)
    {
        if (projectIndex < 0 || projectIndex >= projects.Count) return false;
        ProjectData p = projects[projectIndex];
        if (p.isCompleted) return false;

        ResourceRequirement req = p.requirements.Find(r => r.itemName == itemName);
        if (req == null) return false;

        int toDeliver = Mathf.Min(qty, req.Remaining);
        if (toDeliver <= 0) return false;

        if (!inventory.HasItems(itemName, toDeliver))
        { Debug.LogWarning($"[Project] Not enough {itemName}"); return false; }

        inventory.RemoveItem(itemName, toDeliver);
        req.deliveredQty += toDeliver;

        Debug.Log($"[Project] {p.projectName}: delivered {toDeliver}x {itemName} | Progress: {p.GetProgress()*100:F0}%");

        CheckCompletion(projectIndex);
        BuildUI();
        return true;
    }

    void CheckCompletion(int idx)
    {
        ProjectData p = projects[idx];
        if (p.requirements.TrueForAll(r => r.IsMet))
        {
            p.isCompleted = true;
            economySystem.reliability += p.reliabilityReward;
            economySystem.reliability  = Mathf.Clamp(economySystem.reliability, -100f, 100f);
            Debug.Log($"[Project] '{p.projectName}' COMPLETE! Reliability +{p.reliabilityReward}%");

            if (AllProjectsComplete)
                Debug.Log("[Project] All projects done! Monument is now unlockable.");
        }
    }

    // ============================================================
    //  UI
    // ============================================================
    void BuildUI()
    {
        if (projectListParent == null || projectRowPrefab == null) return;
        foreach (Transform t in projectListParent) Destroy(t.gameObject);

        for (int i = 0; i < projects.Count; i++)
        {
            int idx = i;
            GameObject row = Instantiate(projectRowPrefab, projectListParent);
            ProjectRow pr = row.GetComponent<ProjectRow>();
            pr?.Init(projects[i], idx);
        }
    }
}

// ============================================================
//  ProjectRow — component บน Prefab แต่ละ Project
// ============================================================
public class ProjectRow : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI progressText;
    public Slider          progressBar;
    public TextMeshProUGUI requirementsText;
    public Button          contributeButton; // เปิด sub-panel หรือ contribute อัตโนมัติ

    private int projectIndex;

    public void Init(ProjectData data, int idx)
    {
        projectIndex = idx;
        if (nameText     != null) nameText.text      = data.isCompleted ? $"✅ {data.projectName}" : data.projectName;
        if (progressText != null) progressText.text  = $"{data.GetProgress()*100:F0}%";
        if (progressBar  != null) progressBar.value  = data.GetProgress();

        if (requirementsText != null)
        {
            string req = "";
            foreach (var r in data.requirements)
                req += $"{r.itemName}: {r.deliveredQty}/{r.requiredQty}  ";
            requirementsText.text = req.Trim();
        }

        if (contributeButton != null)
        {
            contributeButton.interactable = !data.isCompleted;
            contributeButton.onClick.AddListener(OnContributeClicked);
        }
    }

    // ส่งของที่มีใน inventory เข้า project อัตโนมัติ
    void OnContributeClicked()
    {
        ProjectData p = ProjectSystem.Instance.projects[projectIndex];
        foreach (var req in p.requirements)
        {
            int have = ResourceInventory.Instance.GetQuantity(req.itemName);
            if (have > 0 && !req.IsMet)
                ProjectSystem.Instance.ContributeResource(projectIndex, req.itemName, have);
        }
    }
}

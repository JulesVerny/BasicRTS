using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviour
{
    // ==============================================================================
    public enum GameDifficultyType { Easy, Medium, Hard };
    public static GameDifficultyType TheGameDifficulty;

    // Main Game Manager Class
    // ===============================================================================================
    public enum Faction {None, Player, Enemy};
   

    public enum RTSType { BaseHQ,Factory,Refinary, Tank, Humvee,Harvester, Gun, Laser}
   
    // ===============================================================================================
    [Header("Main Set Up")]
    [SerializeField]
    public BaseManager PlayerBase;
    [SerializeField]
    public BaseManager EnemyBase;
    [SerializeField]
    public Material PlayerMaterial;
    [SerializeField]
    public Material EnemyMaterial;

    // =====================
    [Header("Debug Items")]
    [SerializeField]
    GameObject TheAIDebugPanel;
    bool ShowingDebugPanel;

    // =================
   
    [SerializeField]
    public GameObject TestReference;
    // ===================================

    // ================
    NavMeshAgent TestReferenceAgent;

    private FogWarManager2 TheFogofWarManager;

    // ===============================================================================================
    private void Awake()
    {
        TestReferenceAgent = TestReference.GetComponent<NavMeshAgent>();

        TheFogofWarManager = FindFirstObjectByType<FogWarManager2>();


    } // Awake

    // 
    void Start()
    {

        // Instantite both Player ane Enemy bases
        PlayerBase.InstantiateBase(Faction.Player);

        EnemyBase.InstantiateBase(Faction.Enemy);

        ShowingDebugPanel = false;
        TheAIDebugPanel.gameObject.SetActive(false);

    } // Start()
    // ===============================================================================================
    // Update is called once per frame
    void Update()
    {
        // Have a Baisc D AI Debug Panel Show Hide

        // Basic Debug Panel
        if (Input.GetKeyUp(KeyCode.D))
        {
            if (ShowingDebugPanel)
            {
                TheAIDebugPanel.gameObject.SetActive(false);
                ShowingDebugPanel = false;
                TheFogofWarManager.DebugClearAllFog(false);
            }
            else
            {
                TheAIDebugPanel.gameObject.SetActive(true);
                ShowingDebugPanel = true;
                TheFogofWarManager.DebugClearAllFog(true); 
            }
        } // AI  D: Debug Key Pressed 
        // ==================================



    } // Update UI Interface 
      // ===============================================================================================


}

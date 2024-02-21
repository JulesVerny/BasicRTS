using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;
using System.Drawing;
using System.Collections.Specialized;

public class PlayerUserInterface : MonoBehaviour
{
    // The Player User Interface for the Main Play Scene
    // =====================================================================================
    // Multi Unit Selection Logic from https://gamedevacademy.org/rts-unity-tutorial/
    [Header("Selector Controls")]
    [SerializeField]
    public RectTransform selectionBox;
    private Vector2 startSelBoxPos;

    [Header("Base Configuration")]
    [SerializeField]
    BaseConfiguration AConfiguration;

    [Header("Command UI Controls")]
    [SerializeField]
    public TMP_Text CurrentBudgetText;
    [SerializeField]
    public TMP_Text FactoryCostText;
    [SerializeField]
    public TMP_Text RefinaryCostText;
    [SerializeField]
    public TMP_Text GunCostText;
    [SerializeField]
    public TMP_Text LaserCostText;
    [SerializeField]
    public TMP_Text HarvesterCostText;
    [SerializeField]
    public TMP_Text HumveeCostText;
    [SerializeField]
    public TMP_Text TankCostText;

    [SerializeField]
    public TMP_Text ResponseText;

    [Header("GUI Run Time Label Textures")]
    [SerializeField]
    public Texture2D HealthBarBorderTexture;
    [SerializeField]
    public Texture2D GoodHealthFillTexture;
    [SerializeField]
    public Texture2D MediumHealthFillTexture;
    [SerializeField]
    public Texture2D PoorHealthFillTexture;
    [SerializeField]
    public Texture2D RefinaryBorderTexture;
    [SerializeField]
    public Texture2D RefinaryFillTexture;
    // =================================
    List<RTSComponent> CurrentlySelectedRTSObjects;
    MountPoint CurrentlySelectedMountPoint; 
    List<RTSComponent> CurrentlyActionedRTSObjects;
    List<RTSComponent> AllRTSHealthObjects;

    BaseManager ThePlayerBase; 

    List<GameObject> OreFieldsList;
    float NextReviewTime;
    float ReviewPeriod = 1.0f;

    // Reference to the Game manager and FogOfWar Manager
    private GameManager TheGameManager;
    private FogWarManager2 TheFogofWarManager;
    bool PlayerHasQuit; 
    // ======================================================================================
    private void Awake()
    {
        // Find the Game Manager
        TheGameManager = FindFirstObjectByType<GameManager>();
        TheFogofWarManager = FindFirstObjectByType<FogWarManager2>();

        CurrentlySelectedRTSObjects = new List<RTSComponent>();
        CurrentlyActionedRTSObjects = new List<RTSComponent>();
        CurrentlySelectedMountPoint = null;
        AllRTSHealthObjects = new List<RTSComponent>();

        OreFieldsList = new List<GameObject>();
        PlayerHasQuit = false;

        ThePlayerBase = this.transform.GetComponent<GameManager>().PlayerBase;
    } // Awake
    // ======================================================================================
    void Start()
    {
        ResponseText.text = "Have Fun";

        // Read and Set Up the Cost Labels

        CurrentBudgetText.text = "£" + (AConfiguration.InitialBudget).ToString();

        FactoryCostText.text = "£" + (AConfiguration.CostOfFactory).ToString();
        RefinaryCostText.text = "£" + (AConfiguration.CostOfRefinary).ToString();
        GunCostText.text = "£" + (AConfiguration.CostOfGun).ToString();
        LaserCostText.text = "£" + (AConfiguration.CostOfLaser).ToString();
        HarvesterCostText.text = "£" + (AConfiguration.CostOfHavester).ToString();
        HumveeCostText.text = "£" + (AConfiguration.CostOfHumvee).ToString();
        TankCostText.text = "£" + (AConfiguration.CostOfTank).ToString();

        //  Find All the Ore Fields, and their 3D static Positions
        foreach (GameObject OreFieldGO in GameObject.FindGameObjectsWithTag("OreField"))
        {
            OreFieldsList.Add(OreFieldGO);
        }

        NextReviewTime = Time.time + ReviewPeriod;

    }  // Start
    // ======================================================================================
    // Update is called once per frame  - User Interface 
    //void Update()
    void OnGUI()
    {

        // Process Debug Key Strokes and Mouse behaviours
        // 
        
        // ================================================
        // Escape or Q For Quit
        if ((Input.GetKeyUp(KeyCode.Escape))&&(!PlayerHasQuit))
        {
            Debug.Log("Exit Game by Escape Button");
            MainMenuManager.LastGameResponse = "Player Has Quit";
            MainMenuManager.NumberAIWins++;
            PlayerPrefs.SetInt("Winner", 1);
            PlayerHasQuit = true; 
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            
        }  // Escape or Quit Q Check 
        // ===============================================


        // Mouse Actions
        //===========================================================
        // Get the ACTION: Right Button Down  : Assume Button 1
        if (Input.GetMouseButtonDown(1))
        {
            // Shoot a Ray from Camera, to intercept Scene (Terrain)
            Ray CameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit CameraRayHit;
            if (Physics.Raycast(CameraRay, out CameraRayHit))
            {
                // Check if Right Action Clicked upon an RTS Object
                if (CameraRayHit.transform.tag == "RTSGameObject")
                {
                    //Have Action Clicked upon RTS Game Object"
                    GameObject LeftClickedRTSObject = CameraRayHit.transform.gameObject;
                    RTSComponent LeftClickedRTSComponent= LeftClickedRTSObject.GetComponent< RTSComponent >(); 
                   
                    if(LeftClickedRTSComponent.TheFaction == GameManager.Faction.Enemy)
                    {
                        // Set the Target Indicator on
                        ClearAllActionedClickedObjects();
                        CurrentlyActionedRTSObjects.Add(LeftClickedRTSComponent); 
                        LeftClickedRTSComponent.SetTargetIndicated(true);

                        // And Set Off any Slected Units to Attack
                        PlayerAttackIndicatedUnits(); 
                    }
                } // Clicked Upon RTS Object
                else
                {
                    ClearAllActionedClickedObjects(); 
                    // Ray Cast Has Hit
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(CameraRayHit.point, out hit, 1.0f, NavMesh.AllAreas))
                    {
                        // Have Found onto a Nav Mesh Point.
                        foreach (RTSComponent RTSItem in CurrentlySelectedRTSObjects)
                        {
                            // User Interface Only Action Player Units
                            if ((RTSItem.TheFaction == GameManager.Faction.Player))
                            {
                                if ((RTSItem.RTSType == GameManager.RTSType.Humvee) || (RTSItem.RTSType == GameManager.RTSType.Tank) || (RTSItem.RTSType == GameManager.RTSType.Harvester))
                                {
                                    // Set the Sibling Nav Agent (via parent transform) Destinaton to the Camera Hit Point
                                    if (RTSItem != null)
                                    {
                                        // Clear Down any Enagement 
                                        if((RTSItem.RTSType == GameManager.RTSType.Humvee) || (RTSItem.RTSType == GameManager.RTSType.Tank))
                                        { 
                                            RTSItem.ClearDownCurrentEngagement();
                                        }
                                        // User Directed Move Action 
                                        RTSItem.SetNewDestination(hit.position);            
                                    }
                                } // Moveable Unit
                            } // Player Unit
                        } // For each currently selected RTS Item

                    } // Nav Mesh Point Found
                }
            }  // Ray cast Strike Strike
            ResponseText.text = "";
        } // Right ACTION Mouse Button
        // =========================================================
        // Get the SELECT: Left Button Down  : Assume Button 0
        if (Input.GetMouseButtonDown(0))
        {
            // Shoot a Ray from Camera, to intercept Scene (Terrain)
            Ray CameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit CameraRayHit;
            if (Physics.Raycast(CameraRay, out CameraRayHit))
            {
                // Have Hit Something
                if (CameraRayHit.transform.tag == "RTSGameObject")
                {
                    //Have Clicked on RTS Game Object so Clear Down all Existing Selected RTS Objects
                    ClearMountPointsSelected();
                    ClearDownRTSComponentSelections();

                    RTSComponent ClickedRTSObject = CameraRayHit.transform.GetComponent<RTSComponent>();
                    if ((!ClickedRTSObject.CurrentlySelected)  && (ClickedRTSObject.TheFaction == GameManager.Faction.Player))
                    {
                        ClickedRTSObject.SetSelection(true);
                        CurrentlySelectedRTSObjects.Add(ClickedRTSObject);
                    }
                    ResponseText.text = "";
                }  // Clicked an RTS Object
                else if (CameraRayHit.transform.tag == "RTSMountPoint")
                {
                    // Mount Point Clicked Upon
                    ClearMountPointsSelected();
                    MountPoint ClickedMountPoint = CameraRayHit.transform.GetComponent<MountPoint>();
                    ClearDownRTSComponentSelections();
                    if ((!ClickedMountPoint.HasTurret) && (ClickedMountPoint.TheFaction == GameManager.Faction.Player))
                    {
                        ClickedMountPoint.SetSelection(true);
                        CurrentlySelectedMountPoint = ClickedMountPoint;
                    }  // Mount Point
                    ResponseText.text = "";
                } // ===================
                else
                {                  
                    // Need to confirm Clicked above Main Command Bar - Before Clearing exisitng Selections
                    if (Input.mousePosition.y > 200)
                    {
                        // Have Clicked away [Above Command Bar]  so clear down all selections
                        ClearDownRTSComponentSelections();
                    }
                }  // Missed RTS Object
            }  // Ray Cast into Scene

            // Set up Multi Selection Box
            startSelBoxPos = Input.mousePosition;

        } // Left SELECT Mouse Button Down
            // =================================================================

        // Left Mouse Selection Mouse Up Event
        if (Input.GetMouseButtonUp(0))
        {
            ReleaseSelectionBox();
        } // Left Selection Mouse Up
        // =======================================================================
        // Left Mouse Drag Held Down: 
        if (Input.GetMouseButton(0))
        {
            // Update the Selection Box
            UpdateSelectionBox(Input.mousePosition);
        } // L Mouse Drag Held Down
          // =========================================================================

        // ==================================================================================================
        // Now the Main Display GUI Updates

        // Update the Current Budget Display
        //CurrentBudgetText.text = "£" + (this.transform.GetComponent<GameManager>().PlayerBase.CurrentBudget).ToString();
        CurrentBudgetText.text = "£" + ThePlayerBase.CurrentBudget.ToString(); 

        // Check the Budget Cost Colour vs Player Base Budgets for 
        if(ThePlayerBase.CurrentBudget>= AConfiguration.CostOfFactory) FactoryCostText.color = UnityEngine.Color.white;
        else FactoryCostText.color = UnityEngine.Color.red;

        if (ThePlayerBase.CurrentBudget >= AConfiguration.CostOfRefinary) RefinaryCostText.color = UnityEngine.Color.white;
        else RefinaryCostText.color = UnityEngine.Color.red;

        if (ThePlayerBase.CurrentBudget >= AConfiguration.CostOfGun) GunCostText.color = UnityEngine.Color.white;
        else GunCostText.color = UnityEngine.Color.red;

        if (ThePlayerBase.CurrentBudget >= AConfiguration.CostOfLaser) LaserCostText.color = UnityEngine.Color.white;
        else LaserCostText.color = UnityEngine.Color.red;

        if (ThePlayerBase.CurrentBudget >= AConfiguration.CostOfHavester) HarvesterCostText.color = UnityEngine.Color.white;
        else HarvesterCostText.color = UnityEngine.Color.red;

        if (ThePlayerBase.CurrentBudget >= AConfiguration.CostOfHumvee) HumveeCostText.color = UnityEngine.Color.white;
        else HumveeCostText.color = UnityEngine.Color.red;

        if (ThePlayerBase.CurrentBudget >= AConfiguration.CostOfTank) TankCostText.color = UnityEngine.Color.white;
        else TankCostText.color = UnityEngine.Color.red;


        // =========================================
        // Review Through all The Ore Fields
        foreach (GameObject OreField in OreFieldsList)
        {
            DrawOreFieldCapacitiy(OreField);
        }
        // =========================================

        // Update All the Health Bars
        if(Time.time> NextReviewTime)
        {
            ReviewUpdateRTSHealthItemsList(); 
            NextReviewTime = Time.time + ReviewPeriod;
        }
        foreach (RTSComponent RTSItem in AllRTSHealthObjects)
        {
            if(RTSItem!=null) DrawHealtBar(RTSItem);
        }
        // ==========================================================================

    }  // OnGUI
    // ======================================================================================
    void ClearAllActionedClickedObjects()
    {
        foreach (RTSComponent CurrentlyActionedRTSObject in CurrentlyActionedRTSObjects)
        { 
            CurrentlyActionedRTSObject.SetTargetIndicated(false);  
        }
        CurrentlyActionedRTSObjects.Clear(); 

    } // ClearActionedClickedObject
    // ======================================================================================
    void ClearMountPointsSelected()
    {
        // Clear All Mount Points
        foreach (GameObject MountGO in ThePlayerBase.MountPoints)
        {
            MountPoint AMountPoint = MountGO.GetComponent<MountPoint>();
            if (AMountPoint != null)
            {
                AMountPoint.SetSelection(false);
            }
        }
        CurrentlySelectedMountPoint = null;
    } // ClearMountPointsSelected
      //======================================================================================
    void ClearDownRTSComponentSelections()
    {
        foreach (RTSComponent rTSComponent in CurrentlySelectedRTSObjects) rTSComponent.SetSelection(false);
        CurrentlySelectedRTSObjects.Clear();
    }
    // ======================================================================================
    private void DrawOreFieldCapacitiy(GameObject TheOreField)
    {
        // Drawing GUI Rectangles !!!!
        // See https://forum.unity.com/threads/draw-a-simple-rectangle-filled-with-a-color.116348/

        // Note y direction is downscreen. 
        Vector3 OreField2DScreenPosition = Camera.main.WorldToScreenPoint(TheOreField.transform.position);

        // Check that Not In Fog of War  - If Its Enabled - Then do not continue To Display ore Field
        if (TheFogofWarManager.QueryFogEnabledAt(TheOreField.transform.position)) return;

        OreField2DScreenPosition.y = Screen.height - OreField2DScreenPosition.y;  // 

        var BorderStyle = new GUIStyle();
        BorderStyle.normal.background = RefinaryBorderTexture;
        GUI.Label(new Rect(OreField2DScreenPosition.x, (OreField2DScreenPosition.y-50),20,50), "", BorderStyle);

        // Map Ore Fill 0..10 => 3..47
        int OreCapacity = TheOreField.GetComponent<OrefieldManager>().CurrentFill;
        float DisplayFillHeight = OreCapacity * 4.25f;

        var FillStyle = new GUIStyle();
        FillStyle.normal.background = RefinaryFillTexture;
        GUI.Label(new Rect(OreField2DScreenPosition.x+2.0f, (OreField2DScreenPosition.y - DisplayFillHeight-3.0f), 16, DisplayFillHeight), "", FillStyle);

    }  // DrawOreFieldCapacitiy
    // ======================================================================================
    void ReviewUpdateRTSHealthItemsList()
    {
        AllRTSHealthObjects.Clear();

        foreach (GameObject GameObjectItem in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            AllRTSHealthObjects.Add(GameObjectItem.GetComponent<RTSComponent>());
        } // for each Game Object

    } // ReviewUpdateRTSHealthItemsList
    // ======================================================================================
    void DrawHealtBar(RTSComponent AnRTSItem)
    {
        // Get the Screen Location Note That y direction is downscreen. 
        Vector3 RTSItem2DScreenPosition = Camera.main.WorldToScreenPoint(AnRTSItem.transform.position);

        // Check that Not In Fog of War  - If Its Enabled - Then do not continue with health Bars
        if (TheFogofWarManager.QueryFogEnabledAt(AnRTSItem.transform.position)) return;

        RTSItem2DScreenPosition.y = Screen.height - RTSItem2DScreenPosition.y;  // 

        // Large RTS Items
        if ((AnRTSItem.RTSType == GameManager.RTSType.BaseHQ) || (AnRTSItem.RTSType == GameManager.RTSType.Factory) || (AnRTSItem.RTSType == GameManager.RTSType.Refinary) ||(AnRTSItem.RTSType == GameManager.RTSType.Harvester))
        {
           
            // Draw Large 50 x 15 Health Box At 75 Above -20 Left
            var BorderStyle = new GUIStyle();
            BorderStyle.normal.background = HealthBarBorderTexture;
            GUI.Label(new Rect(RTSItem2DScreenPosition.x-20.0f, (RTSItem2DScreenPosition.y - 75), 50, 15), "", BorderStyle);

            float DisplayFillWidth = AnRTSItem.HealthFraction * 46.0f;

            var FillStyle = new GUIStyle();
            FillStyle.normal.background = MediumHealthFillTexture;
            if (AnRTSItem.HealthFraction > 0.6f) FillStyle.normal.background = GoodHealthFillTexture;
            if (AnRTSItem.HealthFraction < 0.25f) FillStyle.normal.background = PoorHealthFillTexture;

            GUI.Label(new Rect(RTSItem2DScreenPosition.x -18.0f, (RTSItem2DScreenPosition.y - 73.0f), DisplayFillWidth, 11), "", FillStyle);

        } // large Health bar
        // ==============================
        // Smaller RTS Items
        if ((AnRTSItem.RTSType == GameManager.RTSType.Humvee) || (AnRTSItem.RTSType == GameManager.RTSType.Tank) || (AnRTSItem.RTSType == GameManager.RTSType.Gun) || (AnRTSItem.RTSType == GameManager.RTSType.Laser))
        {
            // Draw Small 25 x 10 Health Box At 35 Above -10 Left
            var BorderStyle = new GUIStyle();
            BorderStyle.normal.background = HealthBarBorderTexture;
            GUI.Label(new Rect(RTSItem2DScreenPosition.x-10.0f, (RTSItem2DScreenPosition.y - 35), 25, 10), "", BorderStyle);

            float DisplayFillWidth = AnRTSItem.HealthFraction * 23.0f;

            var FillStyle = new GUIStyle();
            FillStyle.normal.background = MediumHealthFillTexture;
            if (AnRTSItem.HealthFraction > 0.5f) FillStyle.normal.background = GoodHealthFillTexture;
            if (AnRTSItem.HealthFraction < 0.25f) FillStyle.normal.background = PoorHealthFillTexture;

            GUI.Label(new Rect(RTSItem2DScreenPosition.x -9.0f, (RTSItem2DScreenPosition.y - 34.0f), DisplayFillWidth, 8), "", FillStyle);

        } // Small Health Bar

    } // DrawHealtBar
    // =======================================================================================
    void UpdateSelectionBox(Vector2 curMousePosition)
    {
        // Update the Selection Box Widget
        // Set active display
        if (!selectionBox.gameObject.activeInHierarchy) selectionBox.gameObject.SetActive(true);

        // Now set up the dimensions according to difference from Start Pos
        float width = curMousePosition.x - startSelBoxPos.x;
        float height = curMousePosition.y - startSelBoxPos.y;
        selectionBox.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(height));
        selectionBox.anchoredPosition = startSelBoxPos + new Vector2(width / 2, height / 2);

    } // UpdateSelectionBox
        // ================================================================================================
    void ReleaseSelectionBox()
    {
        // Called when the Left Mouse Selection Button Released

        // First Clear down the Selection Box UI
        selectionBox.gameObject.SetActive(false);

        // Gte the 2D Screen Dimenion Bounds
        Vector2 Selmin = selectionBox.anchoredPosition - selectionBox.sizeDelta / 2;
        Vector2 Selmax = selectionBox.anchoredPosition + selectionBox.sizeDelta / 2;

        // Now perform the Search Against all GameObject Tagged as RTS Objects Projected into the 2D Screen Space
        foreach (GameObject GameObjectItem in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(GameObjectItem.transform.position);
            // Now check within the bounds
            if ((screenPos.x > Selmin.x) && (screenPos.x < Selmax.x) && (screenPos.y > Selmin.y) && (screenPos.y < Selmax.y))
            {
                // Add to the Selcted Units List

                RTSComponent GORTSComponent = GameObjectItem.GetComponent<RTSComponent>();
                // Can only Select Player Units  (Don't want Enely to be Actioned to Attack their own

                if (GORTSComponent.TheFaction == GameManager.Faction.Player)
                {
                    GORTSComponent.SetSelection(true);
                    CurrentlySelectedRTSObjects.Add(GORTSComponent);
                }
            }
        } // For each Possible RTS Game Object within Game

    } // ReleaseSelectionBox
    // ==================================================================================================
    // User Command Buttons

    public void FactoryBuildButtonClick()
    {
        //Debug.Log("Factory Build Button Click");
        if (ThePlayerBase.CurrentBudget - ThePlayerBase.TheBaseConfiguration.CostOfFactory >= 0)
        {
            ThePlayerBase.RebuildFactory();
            ResponseText.text = "Factory Created";
        }
        else
        {
            ResponseText.text = "Not Enough Budget";
        }

    } // FactoryBuildButtonClick
    // =========================================================================================
    public void RefinaryBuildButtonClick()
    {
        //Debug.Log("Refinary Build Button Click");
        if (ThePlayerBase.CurrentBudget - ThePlayerBase.TheBaseConfiguration.CostOfRefinary >= 0)
        {
            ThePlayerBase.RebuildRefinary();
            ResponseText.text = "Refinary Created";
        }
        else
        {
            ResponseText.text = "Not Enough Budget";
        }
    } // RefinaryBuildButtonClick
    // =====================================================================================
    public void GunBuildButtonClick()
    {
        // Check If a Mount is Selected
        if (CurrentlySelectedMountPoint == null)
        {
            ResponseText.text = "No Mount Selected";
            return;
        }
        if (ThePlayerBase.CurrentBudget - ThePlayerBase.TheBaseConfiguration.CostOfGun < 0)
        {
            ResponseText.text = "No Enough Budget";
            return;
        }
        // Got This Far so Presume Can Spawn a Gun System
        ThePlayerBase.SpawnGunSystem(CurrentlySelectedMountPoint.gameObject);
        ResponseText.text = "Gun Turret Created";

    } // GunBuildButtonClick
    // =====================================================================================
    public void LaserBuildButtonClick()
    {
        // Check If a Mount is Selected
        if (CurrentlySelectedMountPoint == null)
        {
            ResponseText.text = "No Mount Selected";
            return;
        }
        
        if (ThePlayerBase.CurrentBudget - ThePlayerBase.TheBaseConfiguration.CostOfLaser < 0)
        {
            ResponseText.text = "No Enough Budget";
            return;
        }
        // Got This Far so Presume Can Spawn a Laser System
        ThePlayerBase.SpawnLaserSystem(CurrentlySelectedMountPoint.gameObject);
        ResponseText.text = "Laser Created";
    } // LaserBuildButtonClick
    // ============================================
    public void HarvesterBuildButtonClick()
    {
        //Debug.Log("Harvester Build Button Click");
        if(ThePlayerBase.FactoryGameObject == null)
        {
            ResponseText.text = "Sorry No Factory !";
            return;
        }

        // Check if sufficient [Player] Budget Firt 
        if (ThePlayerBase.CurrentBudget - ThePlayerBase.TheBaseConfiguration.CostOfHavester >= 0)
        {
            ThePlayerBase.SpawnHarvester(false);
            ResponseText.text = "Harvester Created";
        }
        else
        {
            ResponseText.text = "Not Enough Budget";
        }
    } // HarvesterBuildButtonClick
    // ============================================
    public void HumveeBuildButtonClick()
    {
        //Debug.Log("Humvee Build Button Click");
       
        if (ThePlayerBase.FactoryGameObject == null)
        {
            ResponseText.text = "Sorry No Factory !";
            return;
        }

        if (ThePlayerBase.CurrentBudget - ThePlayerBase.TheBaseConfiguration.CostOfHumvee >= 0)
        {
            ThePlayerBase.SpawnHumvee();
            ResponseText.text = "Humvee Created";
        }
        else
        {
            ResponseText.text = "Not Enough Budget";
        }
    } // HumveeBuildButtonClick
    // ============================================
    public void TankBuildButtonClick()
    {
        //Debug.Log("Tank Build Button Click");
        if (ThePlayerBase.FactoryGameObject == null)
        {
            ResponseText.text = "Sorry No Factory !";
            return;
        }

        if (ThePlayerBase.CurrentBudget - ThePlayerBase.TheBaseConfiguration.CostOfTank >= 0)
        {
            ThePlayerBase.SpawnTank();
            ResponseText.text = "Tank Created";
        }
        else
        {
            ResponseText.text = "Not Enough Budget";
        }

    } // TankBuildButtonClick
    // ============================================
    public void AttackButtonClick()
    {
        Debug.Log("Attack Button Click");

    } // AttackButtonClick
    // ============================================
    public void RepairButtonClick()
    {
        Debug.Log("TODO Repair Button Click");

        // Check if ActonClicked Own Building

    } // RepairButtonClick
    // =================================================================================================
    void PlayerAttackIndicatedUnits()
    {
        // Just Confimr that the FIRST Action Indicated Object is Enemy
        if(CurrentlyActionedRTSObjects.Count>0)
        { 
            RTSComponent FirstIndicatedRTSObject = CurrentlyActionedRTSObjects[0];
            if (FirstIndicatedRTSObject.TheFaction == GameManager.Faction.Enemy)
            {
                // Then Send Selected Units to Attack The Actioned Enemy Object
                foreach (RTSComponent rTSComponent in CurrentlySelectedRTSObjects)
                {
                    if (rTSComponent != null)
                    {
                        // Only Allow target Nominations for Humvees and Tanks
                        if ((rTSComponent.TheFaction == GameManager.Faction.Player)&&((rTSComponent.RTSType == GameManager.RTSType.Humvee) || (rTSComponent.RTSType == GameManager.RTSType.Tank)))
                        {
                            // Set Unit Direct to Attack the Parent GameObject
                            GameObject ParentEnemyGO = FirstIndicatedRTSObject.transform.gameObject;
                            rTSComponent.DirectAttackOnly(ParentEnemyGO);
                        }
                    }
                } // for each RTS Component currenty Selected 
            } // Enemy Unit
        }
    } // PlayerAttackIndicatedUnits()
   // ==================================================================================================


    // ==================================================================================================

}

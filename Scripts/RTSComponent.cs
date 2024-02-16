using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

public class RTSComponent : MonoBehaviour
{
    public enum RTSUnitStates {IdleNone, ProtectingHarvester, PatrollingPoints,AttackMoveToInner, AttackMoveToMid,AttackMoveToOuter, EngagementPhase}
    public enum AttackMode { None, DirectTargetOnly, DegradeBaseDefences,DegradeLocalUnits,FreeForAll};
    // =========
    [Header("General Configuration")]
    [SerializeField]
    public GameManager.Faction TheFaction;

    [SerializeField]
    public RTSComponentConfiguration TheRTSComponentConfiguration;

    public RTSUnitStates CurrentUnitState;
    public AttackMode TheAttackMode; 
    public bool OnMissionAttack; 

    //===========
    [SerializeField]
    GameObject AssociatedModelMesh;
    [SerializeField]
    GameObject ModelMotionGO;
    [SerializeField]
    int TheSelectionMaterialSlot;

    [SerializeField]
    GameObject TheSelectorObject;
    public bool CurrentlySelected;

    [SerializeField]
    GameObject TheTargetIndicatorObject;
    
    [SerializeField]
    public GameManager.RTSType RTSType;

    private GameManager TheGameManager;
    private FogWarManager2 TheFogofWarManager;

    private WeaponsManager ThisUnitsOwnWeapon;
    private BaseManager ParentBase;
    // ========================
    // Health Management
    [Header("Health Management")]
    [SerializeField]
    GameObject SmokeHealthGO;
    [SerializeField]
    GameObject FireHealthGO;
    VisualEffect SmokeHealthEffect;
    VisualEffect FireHealthEffect;
    EnemyAIManager TheEnemyAIManager;

    // =====================
    // Sound Effects 
    [Header("Sound Effects")]
    [SerializeField] private AudioSource TheAudioPlayer;
    [SerializeField] private AudioClip BaseUnderAttackClip;
    [SerializeField] private AudioClip HarvesterUnderAttackClip;
    float NextWarningElapsedTime;
    float WarningElapsedPeriod = 30.0f; 

    // ====================
    [Header("Major Variables")]
    public int CurrentHealth;
    private int MaxPossibleHealth; 
    public float HealthFraction;
    NavMeshAgent TheNavMeshAgent;
    public GameObject TheDesignatedLocalTarget;

    public int TurretMountIndex; 
    // Periodic Review
    float NextReviewTime;
    float ReviewPeriod =1.0f; // Every Second

    // Path Management
    public List<int> RoutePath;
    public int NextPathIndex;
    HarvesterManager ProtectedHarvester; 
    // ====================================================================================
    private void Awake()
    {
        // Find the Game Manager
        TheGameManager = FindFirstObjectByType<GameManager>();

        TheFogofWarManager = FindFirstObjectByType<FogWarManager2>();

        TurretMountIndex = -1;
        // Set Up the Selector Object
        TheSelectorObject.SetActive(false);
        RoutePath = new List<int>();
        if ((RTSType == GameManager.RTSType.Humvee) || (RTSType == GameManager.RTSType.Harvester) || (RTSType == GameManager.RTSType.Tank))
        {
            TheNavMeshAgent = GetComponent<NavMeshAgent>();
        }

        TheEnemyAIManager = TheGameManager.GetComponent<EnemyAIManager>();

        SetSelection(false);
        SetTargetIndicated(false);
        NextWarningElapsedTime = Time.time;

    } // Awake
    // ====================================================================================
    void Start()
    {
        // Note Typically set the Faction By Instantiation 
        CurrentUnitState = RTSUnitStates.IdleNone;
        NextPathIndex = -1;
      
        ProtectedHarvester = null;
        TheDesignatedLocalTarget = null;
        TheAttackMode = AttackMode.None;
        OnMissionAttack = false; 
        RoutePath.Clear();
        // ==========================================
        // Set up the Nav Agents correct Speed
        if ((RTSType == GameManager.RTSType.Humvee) || (RTSType == GameManager.RTSType.Harvester) || (RTSType == GameManager.RTSType.Tank))
        {
            this.transform.GetComponent<NavMeshAgent>().speed = TheRTSComponentConfiguration.UnitSpeed;
        }
        // ============================================
        if ((RTSType == GameManager.RTSType.Humvee) || (RTSType == GameManager.RTSType.Tank))
        {
            ThisUnitsOwnWeapon = this.transform.GetComponent<WeaponsManager>();
        }
        // =============================================

        // Find the Parent Base
        ParentBase = null; 
        foreach (GameObject PossibleBaseGO in GameObject.FindGameObjectsWithTag("Base"))
        {
            BaseManager PossibleBase = PossibleBaseGO.GetComponent<BaseManager>();
            if (PossibleBase.BaseFaction == TheFaction)
            {
                ParentBase = PossibleBase; 
            }
        } // Search For Bases
        // ====================================

        // Adjust the Initial RTS Component Health as a Function of Game Difficulty
        if (TheFaction == GameManager.Faction.Player) MaxPossibleHealth = TheRTSComponentConfiguration.MaxHealth;
        else
        {
            if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Easy) MaxPossibleHealth = TheRTSComponentConfiguration.MaxHealth;
            if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Medium) MaxPossibleHealth = (int)(TheRTSComponentConfiguration.MaxHealth * 1.1f);
            if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Hard) MaxPossibleHealth =  (int)(TheRTSComponentConfiguration.MaxHealth * 1.25f);
        }

        CurrentHealth = MaxPossibleHealth;
        HealthFraction = (float)CurrentHealth / MaxPossibleHealth;

        // Set Up the Smoke and Fire Effects
        if (SmokeHealthGO != null) SmokeHealthEffect = SmokeHealthGO.GetComponent<VisualEffect>();
        if (FireHealthGO != null) FireHealthEffect = FireHealthGO.GetComponent<VisualEffect>();
        if (SmokeHealthEffect!=null) SmokeHealthEffect.Stop();
        if (FireHealthEffect != null) FireHealthEffect.Stop();
       
        // ==============================
        // Set Up periodic Review
        NextReviewTime = Time.time;

    } // Start()
    // ====================================================================================
    // Update is called once per frame
    void Update()
    {





    } // Update
    // ====================================================================================
    private void FixedUpdate()
    {
        // Pitch and Roll Correction for moving Units
        if ((RTSType == GameManager.RTSType.Harvester) || (RTSType == GameManager.RTSType.Tank ) ||  (RTSType == GameManager.RTSType.Humvee))
        {
            PitchRollCorrection();
        }
        // =========================================
        // Periodic Review
        if (Time.time > NextReviewTime) 
        {
            // Update the Fog of War , for Player RTS Items Only
            if(TheFaction == GameManager.Faction.Player) 
            {
                TheFogofWarManager.FriendlyCurrentlyAt(this.transform.position);
            }
            NextReviewTime = Time.time + ReviewPeriod; // Every Second

            // Check if any nearby Units
            AIUnitCheckNearbyTargets();

        }  // Periodic Review
        // =============================


        // If Currently Protecting Harvester, then need to move towards a Harvester
        if((CurrentUnitState== RTSUnitStates.ProtectingHarvester) && (ProtectedHarvester != null) && (TheFaction == GameManager.Faction.Enemy)) 
        {
            if(TheAttackMode == AttackMode.None) MoveTowardsHarvester();
        }

        // ==========================================
        // Need to Check Progress for Any Units Currently on War Path
        if((CurrentUnitState == RTSUnitStates.AttackMoveToInner) || (CurrentUnitState == RTSUnitStates.AttackMoveToMid) || (CurrentUnitState == RTSUnitStates.AttackMoveToOuter))
        {
            // Need to reveiw if Reached Next Tactical Point
            if (TheNavMeshAgent.remainingDistance < TheNavMeshAgent.stoppingDistance * 2.0f)
            { 
                Vector3 NextTPPosition = ParentBase.TacticalPoints[RoutePath[NextPathIndex]].transform.position;
                if(Vector3.Distance(NextTPPosition,this.transform.position) < 25.0f)
                {
                    // Then Have got Reasonably Close to the Next Tactical Point, So Move on to the Next Or Final Enagement
                    NextPathIndex++;
                    if (NextPathIndex < RoutePath.Count)
                    {
                        // Move onto the Next TP
                        MoveTowardsTP(RoutePath[NextPathIndex]);              
                        if (NextPathIndex == 1) CurrentUnitState = RTSUnitStates.AttackMoveToMid;
                        if (NextPathIndex == 2) CurrentUnitState = RTSUnitStates.AttackMoveToOuter;

                        // Need to Check that The DesignatedTarget still exists
                        if (TheEnemyAIManager.GetMissionAttackTarget() == null)
                        {
                            ConsiderPostEngagementAction();
                        }
                        
                    } // Still on Approach Path
                    else
                    {
                        // Have reached end of Attack path, So move Onto Final Enagemeent and Destination
                        if (TheEnemyAIManager.GetMissionAttackTarget() != null)
                        {
                            SetNewDestination(TheEnemyAIManager.GetMissionAttackTarget().transform.position);
                            ThisUnitsOwnWeapon.AssignWeaponToTarget(TheEnemyAIManager.GetMissionAttackTarget());
                            CurrentUnitState = RTSUnitStates.EngagementPhase;
                        }
                        else
                        {
                            // Designated Target No Longer Exists, It may Have been destroyed on Way 
                            ConsiderPostEngagementAction();
                        }
                    } // Reached End of Attack Path
                } // Tactical Point Distance Check

            } // Nav Agent Check
        }  // Unit Currently on War Path
        // =============================================
        if ((CurrentUnitState == RTSUnitStates.EngagementPhase) && (TheFaction == GameManager.Faction.Enemy))
        {
            if (TheDesignatedLocalTarget != null)
            {
                // Target Still exists so Should Still Pursue it. 
                if ((RTSType == GameManager.RTSType.Tank) || (RTSType == GameManager.RTSType.Humvee))
                {
                    // We only need to do for Enemy AI,  Leave Up to Player to choose
                    if (TheFaction == GameManager.Faction.Enemy)
                    {
                        SetNewDestination(TheDesignatedLocalTarget.transform.position);
                    }
                }
            }  // Target Still exists
            else
            {
                // So Looks Like the Target Has Just Been Destroyed or No Longer Exists
                ConsiderPostEngagementAction();

            } // No Target
        }  // RTSUnitStates.EngagementPhase Checks
        // ===============================================

    } // FixedUpdate
      // ==================================================================================
    void PitchRollCorrection()
    {
        Ray TerrainRay = new Ray(transform.position, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(TerrainRay, out hit, 50.0f, LayerMask.GetMask("Terrain")))
        {
            // Calculate Vehicle Pitch and Roll based on Terrain Normal  Hit Vector
            Vector3 TerrainNormal = hit.normal;
            float pitchAngle = Vector3.SignedAngle(Vector3.up, Vector3.ProjectOnPlane(TerrainNormal, transform.right), transform.right);
            float rollAngle = Vector3.SignedAngle(Vector3.up, Vector3.ProjectOnPlane(TerrainNormal, transform.forward), transform.forward);
            //Debug.Log("[Pitch&Roll]:     Pitch Angle: " + pitchAngle.ToString() + "  RollAngle: " + rollAngle.ToString());

            // Apply rotation to the corresponding Model Motion GO
            ModelMotionGO.transform.localRotation = Quaternion.Euler(pitchAngle, 0.0f, rollAngle);
        }
    } // PitchRollCorrection

    // =================================================================================================
    void ConsiderPostEngagementAction()
    {
        // ===================
        if ((TheAttackMode == AttackMode.None) || (TheAttackMode == AttackMode.DirectTargetOnly))
        {
            //Debug.Log("[INFO]: AI Unit Return to Base: No Attack Mode or DirectTarget"); 
            ReturnToBaseAndResetWeapons();
            return; 
        }
        // ===================
        // If there is still a Group Target - Then Start Enagement against That
        if (TheEnemyAIManager.GetMissionAttackTarget() != null)
        {
            ThisUnitsOwnWeapon.AssignWeaponToTarget(TheEnemyAIManager.GetMissionAttackTarget());
            CurrentUnitState = RTSUnitStates.EngagementPhase;
            SetNewDestination(TheEnemyAIManager.GetMissionAttackTarget().transform.position);
            return;
        } // Attack Group Target
        // ===================
        if (TheAttackMode == AttackMode.DegradeBaseDefences)
        {
            GameObject PossibleBaseDefenceTarget = FindNextNearbyBaseDefenceTarget();
            if(PossibleBaseDefenceTarget != null) 
            {
                ThisUnitsOwnWeapon.AssignWeaponToTarget(PossibleBaseDefenceTarget);
                CurrentUnitState = RTSUnitStates.EngagementPhase;
                TheEnemyAIManager.SetMissionAttackTarget(PossibleBaseDefenceTarget);
                SetNewDestination(PossibleBaseDefenceTarget.transform.position);
                return;
            }
        } // Degrade Base Defences
        // ==========================
        if (TheAttackMode == AttackMode.DegradeLocalUnits)
        {
            GameObject PossibleUnitTarget = FindNextNearbyUnitTarget();
            if (PossibleUnitTarget != null)
            {
                ThisUnitsOwnWeapon.AssignWeaponToTarget(PossibleUnitTarget);
                CurrentUnitState = RTSUnitStates.EngagementPhase;
                TheEnemyAIManager.SetMissionAttackTarget(PossibleUnitTarget);
                SetNewDestination(PossibleUnitTarget.transform.position);
                OnMissionAttack = false;    // Consider Main Mission Complete
                return;
            }
        } // Degrade any Local Units Defences

        // ==========================
        if (TheAttackMode == AttackMode.FreeForAll)
        {
            GameObject PossibleNearbyTarget = FindNextNearbyAnyTarget();
            if (PossibleNearbyTarget != null)
            {
                ThisUnitsOwnWeapon.AssignWeaponToTarget(PossibleNearbyTarget);
                CurrentUnitState = RTSUnitStates.EngagementPhase;
                TheEnemyAIManager.SetMissionAttackTarget(PossibleNearbyTarget);
                SetNewDestination(PossibleNearbyTarget.transform.position);
                OnMissionAttack = false;    // Consider Main Mission Complete
                return;
            }
        } // Degrade any Local Units Defences
        // ===================

        // Otherwsie does not appear to be any Global Attack Target or Local Targets, so Retunr to Base
        ReturnToBaseAndResetWeapons();

    } // ConsiderPostEngagementAction
    // ==================================================================================
    void ReturnToBaseAndResetWeapons()
    {
        SetNewDestination(ParentBase.transform.position);
        ClearDownCurrentEngagement();

    } // ReturnToBaseAndResetWeapons
    // ===================================================================================
    public void ClearDownCurrentEngagement()
    {
        ThisUnitsOwnWeapon.ReturnWeaponsToSurveillanceMode();
        CurrentUnitState = RTSUnitStates.IdleNone;
        TheDesignatedLocalTarget = null;
        TheAttackMode = AttackMode.None;
        // Note Can Be called upon By Player UI for Player Units. So need to caveat AI Mission Target Processing
        if (TheFaction == GameManager.Faction.Enemy)
        {
            TheEnemyAIManager.ClearMissionAttackTarget();
            OnMissionAttack = false;
        }
    } // ClearDownCurrentEngagement
    // ===================================================================================
    public void SetAffinity(GameManager.Faction TheAssignedAffinity)
    {
        // Set Affinity After being Created. 
        TheFaction = TheAssignedAffinity;

        // Note to change a single Material, within a List of Materials, need to Get and SET the whole Materials array back. 
        // Get the List of Materials with the Associated Model
        Material[] ModelMaterials = AssociatedModelMesh.GetComponent<Renderer>().materials;
        if (TheFaction == GameManager.Faction.Player)
        {
            ModelMaterials[TheSelectionMaterialSlot] = TheGameManager.PlayerMaterial;
        }
        if (TheFaction == GameManager.Faction.Enemy)
        {
            ModelMaterials[TheSelectionMaterialSlot] = TheGameManager.EnemyMaterial;
        }
        AssociatedModelMesh.GetComponent<Renderer>().materials = ModelMaterials;

        // If this unit is a Harvster - Need to set its Home Refinray
        if (RTSType == GameManager.RTSType.Harvester)
        {
            this.transform.GetComponent<HarvesterManager>().FindHomeRefinaryAndBase(TheFaction);
        }
    } // SetAffinity

    // ====================================================================================
    public void SetSelection(bool NewSelectionMode)
    {
        if(TheSelectorObject!=null) TheSelectorObject.SetActive(NewSelectionMode);
        CurrentlySelected = NewSelectionMode;
    } // SetSelection
    // ====================================================================================
    public void SetTargetIndicated(bool TargetIndicated)
    {
        if(TheTargetIndicatorObject!= null) TheTargetIndicatorObject.SetActive(TargetIndicated);
    }
    // =================================================================================
    public void SetAttackMode(AttackMode NewAttackMode)
    {
        TheAttackMode = NewAttackMode;
    } // SetAttackMode

    // =================================================================================
    public GameObject FindNearestViableOreField()
    {
        GameObject NearestOreField = null;
        // Confirm can only make call on Harveter Types
        if (RTSType == GameManager.RTSType.Harvester)
        {
            HarvesterManager TheHarvesterManager = this.transform.GetComponent<HarvesterManager>();
            NearestOreField= TheHarvesterManager.ReviewNextOreField();
        }
        return NearestOreField;

    } // FindNearestViableOreField
    // =================================================================================================
    public Vector3 FindHomeBaseReturnLocation()
    {
        Vector3 BaseLocation = Vector3.zero;
        if (ParentBase != null)
        {  
            Vector3 randomPoint = ParentBase.UnitMeetPoint + Random.insideUnitSphere * 10.0f;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 12.5f, NavMesh.AllAreas))
            {
                return (hit.position);
            }
            else return ParentBase.UnitMeetPoint;
        }
        return BaseLocation;
    } // FindHomeBaseReturnLocation
    // ==================================================================================================
    public void SetToProtectAHarvester()
    {
        // Find The AI Harvester
        foreach (GameObject PossibleRTSObject in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            RTSComponent PossibleRTSItem = PossibleRTSObject.GetComponent<RTSComponent>();
            if ((PossibleRTSItem.RTSType == GameManager.RTSType.Harvester) && (PossibleRTSItem.TheFaction == GameManager.Faction.Enemy))
            {
                ProtectedHarvester = PossibleRTSObject.GetComponent<HarvesterManager>();
                if (ProtectedHarvester != null)
                {
                    CurrentUnitState = RTSUnitStates.ProtectingHarvester;
                    TheAttackMode = AttackMode.None; 
                }
            }
        } // Search Across RTS Game Objects

    } // SetToProtectAHarvester
    // ==================================================================================================
    void MoveTowardsHarvester()
    {
        // Need to Direct the Unit Towards the Harvester if Moving Out, Harvestting (or Moving Back)
        if ((ProtectedHarvester.TheHarvesterState == HarvesterManager.HarvesterState.MovingOut) || (ProtectedHarvester.TheHarvesterState == HarvesterManager.HarvesterState.HarvestingOre))
        {
            // Check Distance to Harvester
            float DistanceToHarvester = Vector3.Distance(this.transform.position, ProtectedHarvester.transform.position);
            if (DistanceToHarvester > ThisUnitsOwnWeapon.TheWeaponWeaponConfiguration.WeaponsRange / 2.0f)
            {
                
                // Then Need to Move closer to the Harvester
                Vector3 CloseToHarvester = ProtectedHarvester.transform.position + Random.insideUnitSphere * 10.0f;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(CloseToHarvester, out hit, 10.0f, NavMesh.AllAreas))
                {
                    SetNewDestination(hit.position);
                }
                else SetNewDestination(ProtectedHarvester.transform.position);
            }
        } // Harevster Moving Out Or Harvesting
        // =======================================
    } // MoveTowardsHarvester
    // ====================================================================================================
    public void SetUnitOffOnPathToEnemyBase(List<int> AttackPath, GameObject DesignatedTargetGO)
    {
        // Check That a Unit and that target Still exists
        if (((RTSType == GameManager.RTSType.Tank) || (RTSType == GameManager.RTSType.Humvee)) && (DesignatedTargetGO != null))
        {
            // Copy Attack Path into Unit
            RoutePath.Clear();
            for (int i = 0; i < AttackPath.Count; i++)
            {
                RoutePath.Add(AttackPath[i]);
            }
            // Now Set Off Towards The First TP
            CurrentUnitState = RTSUnitStates.AttackMoveToInner;
            MoveTowardsTP(RoutePath[0]);
            NextPathIndex = 0;
            TheDesignatedLocalTarget = DesignatedTargetGO;
            ThisUnitsOwnWeapon.AssignWeaponToTarget(DesignatedTargetGO);
        } // Confirm Correct Unit path           

    } // SetUnitOffOnPathToEnemyBase
    // ======================================================================================================
    public void DirectAttackOnly(GameObject DesignatedTargetGO)
    {
        // (Go) Directly Attack a Unit
        if (DesignatedTargetGO != null)
        {
            //  Confirm a Unit, and Not A Defence Unit then need to Move Towards Target 
            if ((RTSType == GameManager.RTSType.Tank) || (RTSType == GameManager.RTSType.Humvee))
            {
                if(TheFaction == GameManager.Faction.Enemy)TheAttackMode = AttackMode.DirectTargetOnly; 
                SetNewDestination(DesignatedTargetGO.transform.position);
                TheDesignatedLocalTarget = DesignatedTargetGO;
                ThisUnitsOwnWeapon.AssignWeaponToTarget(DesignatedTargetGO);
                CurrentUnitState = RTSUnitStates.EngagementPhase;
            } // Tank or Humvee
        } // DesignatedTargetGO Still exists
    }   // DirectToAttackUnit
    // ====================================================================================================
    public void PartOfGroupDirectAttack(GameObject DesignatedTargetGO)
    {
        if ((DesignatedTargetGO != null)  && (TheFaction == GameManager.Faction.Enemy))
        {
            //  Confirm a Unit, and Not A Base Defence Unit then need to Move Towards Target 
            if ((RTSType == GameManager.RTSType.Tank) || (RTSType == GameManager.RTSType.Humvee))
            {
                SetNewDestination(DesignatedTargetGO.transform.position);
                TheDesignatedLocalTarget = DesignatedTargetGO;
                ThisUnitsOwnWeapon.AssignWeaponToTarget(DesignatedTargetGO);
                CurrentUnitState = RTSUnitStates.EngagementPhase;
            } // Tank or Humvee
        } // DesignatedTargetGO Still existys
    }   // PartOfGroupDirectAttack
    // ====================================================================================================

    // ====================================================================================================
    GameObject FindNextNearbyBaseDefenceTarget() 
    { 
        GameObject LocalTarget = null;

        // Find a FriendlyHarvester
        foreach (GameObject PossibleNextTargetGO in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            RTSComponent PossibleRTSItem = PossibleNextTargetGO.GetComponent<RTSComponent>();
            if (((PossibleRTSItem.RTSType == GameManager.RTSType.Gun) || (PossibleRTSItem.RTSType == GameManager.RTSType.Laser)) && (PossibleRTSItem.TheFaction == GameManager.Faction.Player))
            {
                // Check within 2.5 * Weapons Range
                if (Vector3.Distance(this.transform.position, PossibleNextTargetGO.transform.position) < ThisUnitsOwnWeapon.TheWeaponWeaponConfiguration.WeaponsRange * 2.5f)
                {
                    return PossibleNextTargetGO;
                }
            }
        } // Search Across RTS Game Objects

        return LocalTarget;
    } // FindNextNearbyBaseDefenceTarget
    // ==========================================
    GameObject FindNextNearbyUnitTarget()
    {
        GameObject LocalTarget = null;
        // Find a FriendlyHarvester
        foreach (GameObject PossibleNextTargetGO in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            RTSComponent PossibleRTSItem = PossibleNextTargetGO.GetComponent<RTSComponent>();
            if (((PossibleRTSItem.RTSType == GameManager.RTSType.Tank) || (PossibleRTSItem.RTSType == GameManager.RTSType.Humvee)) && (PossibleRTSItem.TheFaction == GameManager.Faction.Player))
            {
                // Check within 2.5 * Weapons Range
                if(Vector3.Distance(this.transform.position, PossibleNextTargetGO.transform.position)< ThisUnitsOwnWeapon.TheWeaponWeaponConfiguration.WeaponsRange*2.5f)
                {
                    return PossibleNextTargetGO; 
                }
            }
        } // Search Across RTS Game Objects

        return LocalTarget;
    } // FindNextNearbyBaseDefenceTarget
    // =========================================
    GameObject FindNextNearbyAnyTarget()
    {
        GameObject LocalTarget = null;
        // Find a FriendlyHarvester
        foreach (GameObject PossibleNextTargetGO in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            RTSComponent PossibleRTSItem = PossibleNextTargetGO.GetComponent<RTSComponent>();
            if (PossibleRTSItem.TheFaction == GameManager.Faction.Player)
            {
                // Check within 3.0 * Weapons Range
                if (Vector3.Distance(this.transform.position, PossibleNextTargetGO.transform.position) < ThisUnitsOwnWeapon.TheWeaponWeaponConfiguration.WeaponsRange * 3.0f)
                {
                    return PossibleNextTargetGO;
                }
            }
        } // Search Across RTS Game Objects

        return LocalTarget;
    } // FindNextNearbyBaseDefenceTarget
    // =====================================================================================================
    void AIReturnFireLocal(GameObject TheAttacker)
    {
        // Check Not Currently already in Engaged or a Directed Fire
        if((CurrentUnitState != RTSUnitStates.EngagementPhase) && ((TheAttackMode == AttackMode.None) || (TheAttackMode == AttackMode.DegradeLocalUnits) || (TheAttackMode == AttackMode.FreeForAll)))
        {
            // Check that its a Mobile Unit to respond and that Attacker Still exists
            if ((TheAttacker != null) && ((RTSType == GameManager.RTSType.Tank)||(RTSType == GameManager.RTSType.Humvee))) 
            {
                SetImmediateEngagement(TheAttacker);
            } // Viable Type to respond
        }
    } // AIReturnFireLocal
    // ======================================================================================================
    private void AIUnitCheckNearbyTargets()
    {
        // No Response if already in an Engagement Phase or if Not AI 
        if ((CurrentUnitState == RTSUnitStates.EngagementPhase) || (TheFaction == GameManager.Faction.Player)) return;
        // This Unit need to be a Mobile Tank or Humvee
        if (!((RTSType == GameManager.RTSType.Humvee) || (RTSType == GameManager.RTSType.Tank))) return;
        // Need to check that Not On Directed Attack Mission
        if (!((TheAttackMode == AttackMode.None) || (TheAttackMode == AttackMode.FreeForAll) || (TheAttackMode == AttackMode.DegradeLocalUnits))) return;

        // So confirmed that this is a Mobile AI Unit not in an engagemnt Pahse
        foreach (GameObject PossibleTargetGO in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            // Now Check if Hostile Player)  And either Tank, Humvee or Harvester 
            RTSComponent PossibleRTSTarget = PossibleTargetGO.GetComponent<RTSComponent>();
            if ((PossibleRTSTarget.TheFaction == GameManager.Faction.Player) && ((PossibleRTSTarget.RTSType == GameManager.RTSType.Humvee)|| (PossibleRTSTarget.RTSType == GameManager.RTSType.Tank)||(PossibleRTSTarget.RTSType == GameManager.RTSType.Harvester)))
            {
                float RangeFromThisUnit = Vector3.Distance(PossibleTargetGO.transform.position, this.transform.position);

                // Check within the Surveillance Region, which is 2.0 X Engagement Range
                if (RangeFromThisUnit < 4.0 * ThisUnitsOwnWeapon.TheWeaponWeaponConfiguration.WeaponsRange)
                {
                    SetImmediateEngagement(PossibleTargetGO);
                } // Range Check
            } // Check Enemy 
        }  // for all RTS Objects in Game

    } // AIUnitEngageNearbyTargets
    // ======================================================================================================
    void SetImmediateEngagement(GameObject TheTarget)
    {
        SetNewDestination(TheTarget.transform.position);
        TheDesignatedLocalTarget = TheTarget;
        ThisUnitsOwnWeapon.AssignWeaponToTarget(TheTarget);
        TheAttackMode = AttackMode.DegradeLocalUnits;
        CurrentUnitState = RTSUnitStates.EngagementPhase;
    } // SetImmediateEngagement
    // =======================================================================================================
    public void MoveTowardsTP(int TacticalPointIndex)
    {
        // Extract TestPoint Position
        Vector3 TPDestination  = ParentBase.TacticalPoints[TacticalPointIndex].transform.position + Random.insideUnitSphere * 5.0f;
        //Now Confirm Find on the Nav mesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(TPDestination, out hit, 10.0f, NavMesh.AllAreas))
        {
            SetNewDestination(hit.position);
        }
        else
        {
            //Debug.Log("[NOTE]: Note Unit Cannot Find TP Point on Nav Mesh: Setting Explicit");
            SetNewDestination(ParentBase.TacticalPoints[TacticalPointIndex].transform.position);
        }

    }   // MoveTowardsTP
    // ====================================================================================================
    public void SetNewDestination(Vector3 NewDestination)
    {
        // If a Humvee or Tank Types
        if ((RTSType == GameManager.RTSType.Humvee) || (RTSType == GameManager.RTSType.Tank))
        {
            this.transform.GetComponent<NavMeshAgent>().SetDestination(NewDestination);
        } // Unit Type

        // Review Harvester Destination
        if (RTSType == GameManager.RTSType.Harvester)
        {
            HarvesterManager TheHarvesterManager = this.transform.GetComponent<HarvesterManager>();
            TheHarvesterManager.SetMovingOutState(NewDestination);
        } // Harvester Destination

    } // SetNewDestination
    // =====================================================================================
    public void SetMountIndex(int mountIndex) 
    { 
        TurretMountIndex = mountIndex;
    }
    // =====================================================================================
    public void TakeHit(int HealthDamage, GameObject TheAttacker)
    {
        CurrentHealth = CurrentHealth - HealthDamage;
        HealthFraction = (float)CurrentHealth / MaxPossibleHealth;

        if (HealthFraction < 0.5f)
        {
            SmokeHealthEffect.Play();
            FireHealthEffect.Play();
            //Debug.Log("[DEBUG]: Smoke Started for " + this.gameObject.name +  " With health Fraction: " + HealthFraction.ToString());
        }
        else
        {
            // And Stop any Smoke etc if Health
            if (SmokeHealthEffect != null) SmokeHealthEffect.Stop();
            if (FireHealthEffect != null) FireHealthEffect.Stop();
        }

        // Check and Return Fire
        if (TheFaction == GameManager.Faction.Enemy) AIReturnFireLocal(TheAttacker);

        // ===================
        // However If The Player need to review Audio  "Under Attack" Warnings
        if (TheFaction == GameManager.Faction.Player)
        {
            if((RTSType == GameManager.RTSType.BaseHQ) || (RTSType == GameManager.RTSType.Refinary) || (RTSType == GameManager.RTSType.Factory) ||(RTSType == GameManager.RTSType.Gun) || (RTSType == GameManager.RTSType.Laser))
            {
                if (Time.time > NextWarningElapsedTime)
                {
                    TheAudioPlayer.PlayOneShot(BaseUnderAttackClip); 
                    NextWarningElapsedTime = Time.time + WarningElapsedPeriod; 
                }
            }
            if ((RTSType == GameManager.RTSType.Harvester) && (Time.time > NextWarningElapsedTime))
            {
                TheAudioPlayer.PlayOneShot(HarvesterUnderAttackClip);
                NextWarningElapsedTime = Time.time + WarningElapsedPeriod;
            }
        } // Player Audio "Under Attack" Warnings
        // ================================

        if (CurrentHealth <= 0)
        {
            //Debug.Log("[INFO]: " + RTSType.ToString()  + "   Has Just Been Killed");
            if (SmokeHealthEffect != null) SmokeHealthEffect.Stop();
            if (FireHealthEffect != null) FireHealthEffect.Stop();

            // ===============
            //Check if Turret or Laser, if So The Reset its Mount References
            if((RTSType == GameManager.RTSType.Gun) || (RTSType == GameManager.RTSType.Laser))
            {
                if(TurretMountIndex!=-1)
                {
                    ParentBase.MountPoints[TurretMountIndex].GetComponent<MountPoint>().ClearMount();
                    TurretMountIndex = -1; 
                }
            }  // Laser of Gun Mount References
            // ===============

            // Now Check if Game Over
            CheckIfGameOver();

            Destroy(this.transform.gameObject);
        }
        // ========================================
        // Need to Raise Attack Alerts for Attacks on AI Units  - But Note only if Unit (Tank, Humvee) As Attackers
        if ((ParentBase != null) && (TheAttacker != null) && (TheFaction == GameManager.Faction.Enemy))
        {
            if ((RTSType == GameManager.RTSType.BaseHQ) || (RTSType == GameManager.RTSType.Factory) || (RTSType == GameManager.RTSType.Refinary))
            {
                // Base Building Under An Attack
                ParentBase.CurrentAttacks = BaseManager.UnderAttackModes.BaseBuildingUnderAttack;
            }
            if ((RTSType == GameManager.RTSType.Gun) || (RTSType == GameManager.RTSType.Laser))
            {
                // Base Defence Under An Attack
                ParentBase.CurrentAttacks = BaseManager.UnderAttackModes.BaseDefenceUnderAttack;
            }
            if (((RTSType == GameManager.RTSType.Humvee) || (RTSType == GameManager.RTSType.Tank)) && (TheAttackMode != AttackMode.DirectTargetOnly))
            {
                // A Unit is Under An Attack
                ParentBase.CurrentAttacks = BaseManager.UnderAttackModes.UnitUnderAttack;
            }
            if (RTSType == GameManager.RTSType.Harvester)
            {
                // A Harvester is Under An Attack
                ParentBase.CurrentAttacks = BaseManager.UnderAttackModes.HarvesterUnderAttack;
            }

            ParentBase.TheAttackingUnit = TheAttacker;
        } // Enemy Item is being Attacked 
        // ===========================
    } // TakeHit
    // ==============================================================================================
    void CheckIfGameOver()
    {
        // Called when an RTS Game Object is killed off
        if(RTSType == GameManager.RTSType.BaseHQ)
        {
            if(TheFaction == GameManager.Faction.Player)
            {
                PlayerPrefs.SetInt("Winner", 1); 
                MainMenuManager.NumberAIWins++;
                MainMenuManager.LastGameResponse = "The AI Has Won";
                Debug.Log("The AI Has Won: Destryed Player HQ");
            } // Player Lost

            if (TheFaction == GameManager.Faction.Enemy)
            {
                PlayerPrefs.SetInt("Winner", 2);
                MainMenuManager.NumberPlayerWins++;
                MainMenuManager.LastGameResponse = "Well Done, Player Won";
                Debug.Log("The Player Has Won: Destroyed Enemy HQ");
            } // Player Won
            // Now return to the Main Menu Scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    } // CheckIfGameOver
    // ====================================================================================================

}

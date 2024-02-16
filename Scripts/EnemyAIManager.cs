using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class EnemyAIManager : MonoBehaviour
{
    // =========================
     
    // =========================

    public enum BuildQueStratgyType {None,Mixed,HumveeFest,HardTank,BuildEconomy,BuildDefences}
    public enum TargetOptionTypes {None, Harvester,HarvesterEscorts,BaseSpecific,BaseDefences,BaseGeneral}

    //  =========================================================================================
    [SerializeField] GameObject OwnEnemyBase;
    [SerializeField] GameObject PlayerBase;
    BaseManager OwnEnemyBaseManager; 

    // Local Controls
    List<GameManager.RTSType> CurrentBuildQueue;
    public BuildQueStratgyType CurrentAIBuildStrategy;
    List<GameObject> MountSequenceList;
    GameObject MissionAttackTarget = null;

    int NextBuildIndex; 
    float TacticalReviewPeriod = 2.0f;
    float NextTacticalReviewTime = 0.0f;
    float StrategyReviewPeriod = 60.0f;
    float NextStrategyReviewTime = 0.0f;
    float LastUnitSupportTime = 0.0f; 
    
    public bool BuildingEmergencyBudget;
    int TacticalRecoveryCount;
    int NextBudgetRecoveryTime; 

    // Tactical Indicators
    int NumberofAITanks;
    int NumberofPlayerTanks;
    int NumberofAIHumvees;
    int NumberofPlayerHumvees;
    int NumberofAIBaseDefences;
    int NumberofPlayerBaseDefences;

    int NumberofAITanksAtBase;
    int NumberofPlayerTanksAtBase;
    int NumberAIProtectingHarvesters;
    int NumberPlayerTanksProtectingHarvesters;
    int NumberOfPlayerHarvesters;
    int NumberofAIHarvesters; 
    List<GameObject> PlayerTankList;
    List<GameObject> UnitsOnCurrentMission;
    public bool BaseCurrentlyUnderAttack;
    public TargetOptionTypes TheProposedTargetType; 

    // Unit Defence Indicators
    int CountofAssignedUnits = 0;
    int AssumedNumberUnitsToSendToUnitDefence;

// =============================================
[Header("AI Debug UI ")]
    [SerializeField]
    public TMP_Text AIBudgetText;
    [SerializeField]
    public TMP_Text StrategyText;

    [SerializeField]
    public TMP_Text BuildIndexText;
    [SerializeField]
    public TMP_Text BuildingBudgetText;

    [SerializeField]
    public TMP_Text AttackCommentTB;

    [SerializeField]
    public TMP_Text NextBuildItemTB;

    [SerializeField]
    public TMP_Text GroupTargetTB;
    // =========================================================================
    private void Awake()
    {
        OwnEnemyBaseManager = OwnEnemyBase.GetComponent<BaseManager>();

        CurrentBuildQueue = new List<GameManager.RTSType>();
        CurrentAIBuildStrategy = BuildQueStratgyType.None;
        MountSequenceList = new List<GameObject>();
        PlayerTankList = new List<GameObject>();
        UnitsOnCurrentMission = new List<GameObject>(); 
        NextBuildIndex = -1; 

    } // Awake
    //  ========================================================================

    //  ========================================================================
    void Start()
    {
        // Randomise the AI Stratgy

        InitialBuildStrategyChoice();

        InitialiseBuildQueue();

        NextTacticalReviewTime = Time.time + TacticalReviewPeriod;
        NextStrategyReviewTime = Time.time + StrategyReviewPeriod*1.5f;  // Keep Early Strategy consistent   
        LastUnitSupportTime = Time.time;
        
        TheProposedTargetType = TargetOptionTypes.None;

        ResetBudgetRecovery(); 
        BaseCurrentlyUnderAttack = false;

        CountofAssignedUnits = 0;
        AssumedNumberUnitsToSendToUnitDefence = 0;

        // Initialise The Build Que
    } // Start
    //  ========================================================================
    // Update is called once per frame
    void Update()
    {
        


    } // 
    //  ========================================================================
    private void FixedUpdate()
    {
        //===============

        // Debug Review Group Target
        if (GetMissionAttackTarget() != null) GroupTargetTB.text = GetMissionAttackTarget().GetComponent<RTSComponent>().RTSType.ToString();
        else GroupTargetTB.text = "NO Mission Target"; 

        if (Time.time > NextTacticalReviewTime)
        {
            // Time to Perform Tactical Assessments 
            PerformTacticalAssessment();

            // Review If Any Current Base Attacks
            if (OwnEnemyBaseManager.CurrentAttacks == BaseManager.UnderAttackModes.None)
            {
                // No Attacks In play so Perform benign (inc Post Attack) Reviews
                ReviewEmergencyBuilds();
                ReviewHarvesterProtections();
            }
            else
            {
                // Some Attacks Appear to be In play
                ReviewAndRespondToAnyCurrentAttacks();
            }

            // ==============================================
            if (Time.time > NextStrategyReviewTime)
            {
                // ConsiderRecovery Budget After a Period
                
                // Just reset the Number of Units to Respond To a Unit Attack 
                DecideMaxUnitsToRespondUnitAttack(); 

                TacticalRecoveryCount++;
                if((TacticalRecoveryCount>= NextBudgetRecoveryTime) && (NumberOfPlayerHarvesters>0))
                {
                    BuildingEmergencyBudget = true; 
                }

                bool StrategyWasChanged = ReviewCurrentStrategy();

                if(StrategyWasChanged) UpdateBuildQueue();    // *** Only Revise Build Queue, if the Startegy Review Actually Changed
                
                NextStrategyReviewTime = Time.time + StrategyReviewPeriod;

                // Now Review Attack Options
                ReviewPossibleAttackOptions(); 

            } // Strategy Review Period Expired
            // ==============================================

            PerformNextBuild();
           
            // ==============================
            // Update the AI User Interface
            AIBudgetText.text = "£" + OwnEnemyBaseManager.CurrentBudget.ToString();
            StrategyText.text = CurrentAIBuildStrategy.ToString();
            BuildIndexText.text = NextBuildIndex.ToString();
            BuildingBudgetText.text = TacticalRecoveryCount.ToString() + " / " + NextBudgetRecoveryTime.ToString() + " : " + BuildingEmergencyBudget.ToString();
            NextBuildItemTB.text = CurrentBuildQueue[NextBuildIndex].ToString(); 
            // ================================
            // Clear Down Attack Assements for Review At next
            OwnEnemyBaseManager.ResetAttackAssessments();

            NextTacticalReviewTime = Time.time + TacticalReviewPeriod;
        } // Tactical Review Time Up

    } // FixedUpdate
    //  ========================================================================
   
    #region Strategy Assessments
    // =============================================================================
    void ResetBudgetRecovery()
    {
        BuildingEmergencyBudget = false;
        TacticalRecoveryCount = 0;
        NextBudgetRecoveryTime = Random.Range(6, 10);  // Delay before consider building Budget Reserve
    } // ResetBudgetRecovery
    // ====================================================================
    void PerformTacticalAssessment()
    {
        // Peform Unit and Defence Counts
        // Tactical Indicators
        NumberofAITanks = 0;
        NumberofPlayerTanks = 0;
        NumberofAIHumvees = 0;
        NumberofPlayerHumvees = 0;
        NumberofAIBaseDefences = 0;
        NumberofPlayerBaseDefences = 0;
        NumberAIProtectingHarvesters = 0;
        NumberOfPlayerHarvesters = 0;
        NumberofAIHarvesters = 0;

        PlayerTankList.Clear();
        foreach (GameObject PossibleRTSObject in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            RTSComponent PossibleRTSItem = PossibleRTSObject.GetComponent<RTSComponent>();
            if (PossibleRTSItem.TheFaction == GameManager.Faction.Player)
            {
                if (PossibleRTSItem.RTSType == GameManager.RTSType.Tank)
                {
                    PlayerTankList.Add(PossibleRTSObject); 
                    NumberofPlayerTanks++;
                }
                if (PossibleRTSItem.RTSType == GameManager.RTSType.Humvee) NumberofPlayerHumvees++;
                if (PossibleRTSItem.RTSType == GameManager.RTSType.Harvester) NumberOfPlayerHarvesters++; 
                if ((PossibleRTSItem.RTSType == GameManager.RTSType.Gun) || (PossibleRTSItem.RTSType == GameManager.RTSType.Laser)) NumberofPlayerBaseDefences++;

                if(Vector3.Distance(PossibleRTSObject.transform.position,PlayerBase.transform.position) < 150.0f)
                {
                    if (PossibleRTSItem.RTSType == GameManager.RTSType.Tank) NumberofPlayerTanksAtBase++;
                }
            }
            if (PossibleRTSItem.TheFaction == GameManager.Faction.Enemy)
            {
                if (PossibleRTSItem.RTSType == GameManager.RTSType.Tank) NumberofAITanks++;
                if (PossibleRTSItem.RTSType == GameManager.RTSType.Humvee) NumberofAIHumvees++;
                if (PossibleRTSItem.RTSType == GameManager.RTSType.Harvester) NumberofAIHarvesters++;
                if ((PossibleRTSItem.RTSType == GameManager.RTSType.Gun) || (PossibleRTSItem.RTSType == GameManager.RTSType.Laser)) NumberofAIBaseDefences++;

                if ((PossibleRTSItem.RTSType == GameManager.RTSType.Tank) || (PossibleRTSItem.RTSType == GameManager.RTSType.Humvee))
                {
                    if (PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.ProtectingHarvester) NumberAIProtectingHarvesters++;
                }
                if (Vector3.Distance(PossibleRTSObject.transform.position, OwnEnemyBase.transform.position) < 150.0f)
                {
                    if (PossibleRTSItem.RTSType == GameManager.RTSType.Tank) NumberofAITanksAtBase++;
                }
            }
        } // Search through All RTS Objects

        // Review Number of Player Tanks Protecting Their Harvesters
        NumberPlayerTanksProtectingHarvesters = NumberPlayerTanksProtectingHarvester();

    } // PerformTacticalAssessment
    // ===========================================================================================


    // ============================================================================================
    void DecideMaxUnitsToRespondUnitAttack()
    {
        // Perform a Pre Assessment on How Many Will send to respond to a Unit Under Attack. 
        CountofAssignedUnits = 0;
        AssumedNumberUnitsToSendToUnitDefence = Random.Range(1, (NumberofAITanks + NumberofAIHumvees) / 2);
    } // DecideMaxUnitsToRespondUnitAttack

    // ============================================================================================
    void ReviewAndRespondToAnyCurrentAttacks()
    {
        // Review each Type of Attack and Respond
        
        if (OwnEnemyBaseManager.CurrentAttacks == BaseManager.UnderAttackModes.BaseBuildingUnderAttack)
        {
            // Urgent Base Building So return Most(All) Units
            ReturnAvailableUnitsToDefendBase();

            ResetBudgetRecovery();    // Serious Attack, so Relinquish Budget to Build Units or Defences
        }

        if (OwnEnemyBaseManager.CurrentAttacks == BaseManager.UnderAttackModes.UnitUnderAttack)
        {
            // Reduce period of only doing a support Task Once every 30 seconds
            if (Time.time > LastUnitSupportTime + 30.0f)
            {
                // Send Some Units to Help Protect Unit
                ReturnSomeUnitsToDefendUnit();
                LastUnitSupportTime = Time.time;
            }
        }

        if (OwnEnemyBaseManager.CurrentAttacks == BaseManager.UnderAttackModes.HarvesterUnderAttack)
        {
            // Send available Tanks Units to Help Protect Harevster 
            SendToProtectHarvester();
        }

        // NOTE:  Don't bother to respond to bas Defence Attacks  : Assume Can defend themselves 


    } //  ReviewAndRespondToAnyCurrentAttacks()
    // ============================================================================================
    void ReviewEmergencyBuilds()
    {
        // Need to Confirm that need to
        // a) Ensure have a working Refinary - If Not build One
        // b) Ensure have a Harvester and Spawn One if necessary 
        // c) Ensure Have a Factory, if Not build One

        // d) Perform Any Repairs on HQ, Refinary and Factory: TODO

        // ===================
        // Confirm that Refinary Still Exists
        if(OwnEnemyBaseManager.RefinaryGameObject == null)
        {
            // We have Lost the Refinary - So perform a Rebuild  If we still have the Budget !
            //
            if (OwnEnemyBaseManager.CurrentBudget - OwnEnemyBaseManager.TheBaseConfiguration.CostOfRefinary >= 0)
            {
                OwnEnemyBaseManager.RebuildRefinary();
                // Can reset Budget Recovery 
                BuildingEmergencyBudget = false;
                TacticalRecoveryCount = 0;
                NextBudgetRecoveryTime = Random.Range(1, 4);
                ReviewCurrentStrategy();  // Prompt a Review of Strategy and Revise Build Que
                UpdateBuildQueue();
            }
            else Debug.Log("[INFO]: Enemy Cannot Afford to Rebuild Refinary !");
        }  // Check If Refinary Still Exists
        // ===================
        // Confirm we Have any Harevster Harvester 
        if((NumberofAIHarvesters<1) && (OwnEnemyBaseManager.RefinaryGameObject != null))
        {
            if (OwnEnemyBaseManager.CurrentBudget - OwnEnemyBaseManager.TheBaseConfiguration.CostOfHavester >= 0)
            {
                OwnEnemyBaseManager.SpawnHarvester(false);
                ReviewCurrentStrategy();  // Prompt a Review of Strategy and Revise Build Que
                UpdateBuildQueue();
            }
            else Debug.Log("[INFO]: Enemy Cannot Afford to Spwan a Harvester !");
        }  // Check If we have a Harvester
        // ==================
        if (OwnEnemyBaseManager.FactoryGameObject == null)
        {
            // We have Lost the Factory - So perform a Rebuild  If we still have the Budget !
            //
            if (OwnEnemyBaseManager.CurrentBudget - OwnEnemyBaseManager.TheBaseConfiguration.CostOfFactory >= 0)
            {
                OwnEnemyBaseManager.RebuildFactory();
                ReviewCurrentStrategy();  // Prompt a Review of Strategy
            }
            else Debug.Log("[INFO]: Enemy Cannot Afford to Rebuild Factory");
        }  // Check If Factory Still Exists
        // ====================

    } // ReviewEmergencyBuilds
    // ============================================================================================
    bool ReviewCurrentStrategy()
    {
        bool StrategyChanged = false;
        BuildQueStratgyType PreviousStrategy = CurrentAIBuildStrategy;
        int RandomChoice100 = Random.Range(0, 100);

        // ================================================================================
        // Monitor Relative Build Growth Advantage, Harvester, really just want to gain advantage, not stay building too many more Harvesters !
        if (CurrentAIBuildStrategy == BuildQueStratgyType.BuildEconomy)
        {
            if(NumberofAIHarvesters > NumberOfPlayerHarvesters+1)
            {
                // An Excess of AI Harvesters - Spend on More Tanks instead
                CurrentAIBuildStrategy = BuildQueStratgyType.HardTank;
                StrategyChanged = true;
                return StrategyChanged;
            }
            else if(NumberofAIHarvesters> NumberOfPlayerHarvesters)
            {
                // Have built a Harvester superiority, so can change to a new Policy away from Harvester / Economy Strategy
                ReviseBuildStrategyChoice();
                if (CurrentAIBuildStrategy != PreviousStrategy) StrategyChanged = true;
                return StrategyChanged;
            }
        }  // On Economy Strategy
        else
        {
            // Need to check that Player is not heading building superior Economy 
            if ((NumberofAIHarvesters < NumberOfPlayerHarvesters) && (NumberofAITanks>=2))
            {
                // Really need to advance focus upon Harvester economoy
                if (RandomChoice100 > 25)
                {
                    CurrentAIBuildStrategy = BuildQueStratgyType.BuildEconomy;
                    StrategyChanged = true;
                    return StrategyChanged;
                }
            } // Player has Economic Growth Advantage
        }  // Not on Build

        // Otherwsie we would typically is to Stay with current Strategy              
        if (RandomChoice100 < 33) ReviseBuildStrategyChoice();
        // Else Otherwise we will stay as we are !

        // Note if Near to Player Defeat then Can relax the Own Emergency Build - to release more funds
        if ((NumberOfPlayerHarvesters < 1) && (NumberofPlayerHumvees < 1) && (NumberofPlayerTanks < 1))
        {
            ResetBudgetRecovery();
        }

        if (CurrentAIBuildStrategy != PreviousStrategy) StrategyChanged = true;
        return StrategyChanged;
    } // ReviewStrategy
    // ===========================================================================
    private void InitialBuildStrategyChoice()
    {
        // The Initial Choice is heavily skewed towards Humvees, tanks or Economy Only
        int RandomChoice100 = Random.Range(0, 100);
        if (RandomChoice100 < 40) CurrentAIBuildStrategy = BuildQueStratgyType.HumveeFest;
        if ((RandomChoice100 >= 40) && (RandomChoice100 < 70)) CurrentAIBuildStrategy = BuildQueStratgyType.HardTank; ;
        if (RandomChoice100 >= 70) CurrentAIBuildStrategy = BuildQueStratgyType.BuildEconomy;

    } // InitialStrategyChoice
    // ============================================================================
    private void ReviseBuildStrategyChoice()
    {
        // General Random Assignment
        int RandomChoice100 = Random.Range(0, 100);
        if (RandomChoice100 < 15) CurrentAIBuildStrategy = BuildQueStratgyType.Mixed;
        if ((RandomChoice100 >= 15) && (RandomChoice100 < 40)) CurrentAIBuildStrategy = BuildQueStratgyType.HumveeFest;
        if ((RandomChoice100 >= 40) && (RandomChoice100 < 65)) CurrentAIBuildStrategy = BuildQueStratgyType.HardTank;
        if ((RandomChoice100 >= 65) && (RandomChoice100 < 80))
        {
            // Review wheatehr to Progress a Harvester/ Economy
            if (CurrentAIBuildStrategy != BuildQueStratgyType.BuildEconomy) CurrentAIBuildStrategy = BuildQueStratgyType.BuildEconomy;
            else CurrentAIBuildStrategy = BuildQueStratgyType.HardTank;
        } // Build Economy Range
        if ((RandomChoice100 >= 80) && (RandomChoice100 <= 100))
        {
            // Review Build Base Defences 
            if (FindNextMount() > 0) CurrentAIBuildStrategy = BuildQueStratgyType.BuildDefences;
            else
            {
                // However base Defence are complete, So concentrate on Tanks or other Options
                RandomChoice100 = Random.Range(0, 100);
                if (RandomChoice100 < 60) CurrentAIBuildStrategy = BuildQueStratgyType.HardTank;
                if ((RandomChoice100 >= 60) && (RandomChoice100 < 90)) CurrentAIBuildStrategy = BuildQueStratgyType.HumveeFest;
                if (RandomChoice100 > 90) CurrentAIBuildStrategy = BuildQueStratgyType.BuildEconomy;
            }
        } // Builkd Defences Range
        // ===========================

        // Now Consider Some modifiers 
        RandomChoice100 = Random.Range(0, 100);    
        // Check Player defences against Humvees
        if(((NumberofPlayerTanks-1)>NumberofAITanks)  || (NumberofPlayerBaseDefences>1))
        {
            // Looks like Player growing strong defences - so Modify  emphasis on Humvees, towards more on Tanks instead
            if(CurrentAIBuildStrategy == BuildQueStratgyType.HumveeFest)
            {
                if(RandomChoice100>33) CurrentAIBuildStrategy = BuildQueStratgyType.HardTank;
            }
        } // Check Against Humvees 
        // ========================
        // Check against a possible Player Humvee Rush
        if ((NumberofPlayerHumvees > 4) && ((NumberAIProtectingHarvesters<2) || (NumberofAIBaseDefences<3) || (NumberofAITanks<3)))
        {
            // Suggests an increase emphasis on either Tank building
            if((CurrentAIBuildStrategy != BuildQueStratgyType.HardTank)|| (CurrentAIBuildStrategy != BuildQueStratgyType.BuildDefences))
            {
                if (RandomChoice100 > 40) CurrentAIBuildStrategy = BuildQueStratgyType.HardTank;
            }
        } // Check Against a Humvee Rush 
        // ================================
        // Review if Player is very weak (c.f. No Harvesters)  => Focus on Tanks
        if((NumberOfPlayerHarvesters<1) && (NumberofAIHarvesters>0))
        {
            // Then the player has little to no economoy build so can focus on Tanks
            if (RandomChoice100 > 20) CurrentAIBuildStrategy = BuildQueStratgyType.HardTank;
        }

    } // ReviseBuildStrategyChoice
    // ===========================================================================
    void ReviewHarvesterProtections()
    {
        // Need to Review and Assign Harvester Protection Squads, default 1 or 2. Increase if Player has Units
        int ProtectionLevel = Random.Range(1,3);
        if ((NumberofPlayerTanks > 2) || (NumberofPlayerHumvees > 3)) ProtectionLevel = ProtectionLevel + Random.Range(0, 2); 
        if (NumberAIProtectingHarvesters < ProtectionLevel)
        {
            // Search for a First Idle Unit to Protect Harvester
            foreach (GameObject PossibleRTSObject in GameObject.FindGameObjectsWithTag("RTSGameObject"))
            {
                RTSComponent PossibleRTSItem = PossibleRTSObject.GetComponent<RTSComponent>();
                if (PossibleRTSItem.TheFaction == GameManager.Faction.Enemy)
                {
                    if ((PossibleRTSItem.RTSType == GameManager.RTSType.Tank) || (PossibleRTSItem.RTSType == GameManager.RTSType.Humvee))
                    {
                        // Enemy (AI) Tank or Humvee, So check if just Idle
                        if(PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.IdleNone) 
                        {
                            // Found An Idle Tank or Humvee, so Assign that to Harvest Protection 
                            PossibleRTSItem.SetToProtectAHarvester();
                            return;  // Return out of Loop, if Found One (Others will follow)
                        }
                    } // Review of Tanks and Humvee UInits
                } // Enemy Unist Only 
            } // All RTS Items Search
        } // # Protection level Too Low 
    } // ReviewHarvesterProtections
    // ==================================================================================
    #endregion
    // ==================================================================================
    #region Build Queue Management
    // ==================================================================================
    void PerformNextBuild()
    {
        if (NextBuildIndex < CurrentBuildQueue.Count)
        {
            // NextBuild Item
            GameManager.RTSType NextBuildRequest = CurrentBuildQueue[NextBuildIndex];

            // Need to Skip any Request to Build further Defences  If Defence complete
            if(FindNextMount()<0)
            {
                if((NextBuildRequest == GameManager.RTSType.Gun) || (NextBuildRequest == GameManager.RTSType.Laser))
                {
                    // Skip and Revise to a Tank Request Instead
                    NextBuildRequest = GameManager.RTSType.Tank; 
                }
            } // Build Defnces have been completed 
            // ===============================

            // ================================
            // Humvee Request
            if ((NextBuildRequest == GameManager.RTSType.Humvee) && (OwnEnemyBaseManager.FactoryGameObject!=null))
            {
                if (BuildingEmergencyBudget)
                {
                    if (OwnEnemyBaseManager.CurrentBudget - OwnEnemyBaseManager.TheBaseConfiguration.CostOfHumvee - OwnEnemyBaseManager.TheBaseConfiguration.CostOfRefinary >= 0)
                    {
                        // Can Build a Humvee with Emergency Budget
                        OwnEnemyBaseManager.SpawnHumvee();
                        NextBuildIndex = NextBuildIndex + 1;
                    }
                } // With Emergency Budget
                else
                {
                    if (OwnEnemyBaseManager.CurrentBudget - OwnEnemyBaseManager.TheBaseConfiguration.CostOfHumvee >= 0)
                    {
                        // Can Build a Humvee without any  Emergency Budget
                        OwnEnemyBaseManager.SpawnHumvee();
                        NextBuildIndex = NextBuildIndex + 1;
                    }
                } // Without Emergency Budget
            } // Humvee
              // ==================================
              // Tank Request
            if ((NextBuildRequest == GameManager.RTSType.Tank)  && (OwnEnemyBaseManager.FactoryGameObject != null))
            {
                if (BuildingEmergencyBudget)
                {
                    if (OwnEnemyBaseManager.CurrentBudget - OwnEnemyBaseManager.TheBaseConfiguration.CostOfTank - OwnEnemyBaseManager.TheBaseConfiguration.CostOfRefinary >= 0)
                    {
                        // Can Build a Tank with Emergency Budget
                        OwnEnemyBaseManager.SpawnTank();
                        NextBuildIndex = NextBuildIndex + 1;
                    }
                } // With Emergency Budget
                else
                {
                    if (OwnEnemyBaseManager.CurrentBudget - OwnEnemyBaseManager.TheBaseConfiguration.CostOfTank >= 0)
                    {
                        // Can Build a Tank  without any  Emergency Budget
                        OwnEnemyBaseManager.SpawnTank();
                        NextBuildIndex = NextBuildIndex + 1;
                    }
                } // Without Emergency Budget
            } // Tank
              // ==================================
              // Harvester Request
            if ((NextBuildRequest == GameManager.RTSType.Harvester)  && (OwnEnemyBaseManager.FactoryGameObject != null))
            {
                if (BuildingEmergencyBudget)
                {
                    if (OwnEnemyBaseManager.CurrentBudget - OwnEnemyBaseManager.TheBaseConfiguration.CostOfHavester - OwnEnemyBaseManager.TheBaseConfiguration.CostOfRefinary >= 0)
                    {
                        // Can Build a Harvester with Emergency Budget
                        OwnEnemyBaseManager.SpawnHarvester(false);
                        NextBuildIndex = NextBuildIndex + 1;
                        ReviewCurrentStrategy();   // Prompt a review of Strategy Once successfully built a Harvester 
                    }
                } // With Emergency Budget
                else
                {
                    if (OwnEnemyBaseManager.CurrentBudget - OwnEnemyBaseManager.TheBaseConfiguration.CostOfHavester >= 0)
                    {
                        // Can Build a Harvester without any  Emergency Budget
                        OwnEnemyBaseManager.SpawnHarvester(false);
                        NextBuildIndex = NextBuildIndex + 1;
                        ReviewCurrentStrategy(); // Prompt a review of Strategy Once successfully built a Harvester 
                    }
                } // Without Emergency Budget
            } // Harvester

            // ===================================
            // Gun Request
            int NextAvailableMount = FindNextMount(); 
            if ((NextBuildRequest == GameManager.RTSType.Gun) && (NextAvailableMount >= 0))
            {
                if (BuildingEmergencyBudget)
                {
                    if (OwnEnemyBaseManager.CurrentBudget - OwnEnemyBaseManager.TheBaseConfiguration.CostOfGun - OwnEnemyBaseManager.TheBaseConfiguration.CostOfRefinary >= 0)
                    {
                        // Can Build a Gun with Emergency Budget
                        OwnEnemyBaseManager.SpawnGunSystem(MountSequenceList[NextAvailableMount]);
                        NextBuildIndex = NextBuildIndex + 1;
                    }
                } // With Emergency Budget
                else
                {
                    if (OwnEnemyBaseManager.CurrentBudget - OwnEnemyBaseManager.TheBaseConfiguration.CostOfGun >= 0)
                    {
                        // Can Build a Gin without any  Emergency Budget
                        OwnEnemyBaseManager.SpawnGunSystem(MountSequenceList[NextAvailableMount]);
                        NextBuildIndex = NextBuildIndex + 1;
                    }
                } // Without Emergency Budget
            } // Gun System Build
              // ====================================
              // Laser Request
            if ((NextBuildRequest == GameManager.RTSType.Laser) && (NextAvailableMount >= 0))
            {
                if (BuildingEmergencyBudget)
                {
                    if (OwnEnemyBaseManager.CurrentBudget - OwnEnemyBaseManager.TheBaseConfiguration.CostOfLaser - OwnEnemyBaseManager.TheBaseConfiguration.CostOfRefinary >= 0)
                    {
                        // Can Build a Laser with Emergency Budget
                        OwnEnemyBaseManager.SpawnLaserSystem(MountSequenceList[NextAvailableMount]);
                        NextBuildIndex = NextBuildIndex + 1;
                    }
                } // With Emergency Budget
                else
                {
                    if (OwnEnemyBaseManager.CurrentBudget - OwnEnemyBaseManager.TheBaseConfiguration.CostOfLaser >= 0)
                    {
                        // Can Build a Laser without any  Emergency Budget
                        OwnEnemyBaseManager.SpawnLaserSystem(MountSequenceList[NextAvailableMount]);
                        NextBuildIndex = NextBuildIndex + 1;
                    }
                } // Without Emergency Budget
            } // Laser System Build
            // =========================================================================

        } // Review and Perform Build Item
        // Check If reached End of Build Que
        if (NextBuildIndex >= CurrentBuildQueue.Count)
        {
            // Revise the Build Queue
            UpdateBuildQueue(); 
        }
        // ==========================

    } // ReviewNextBuild
    //===========================================================================
    void InitialiseBuildQueue()
    {
        switch (CurrentAIBuildStrategy)
        {
            case BuildQueStratgyType.Mixed:
                {
                    // Mixed Build Stratgy
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Gun);
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Laser);
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Laser);

                    break;
                }  // Mixed
                // ========================================
            case BuildQueStratgyType.HumveeFest:
                {
                    // HumVee Rush
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);

                    break;
                }  // Humvee Fest
                   // ========================================
 
            case BuildQueStratgyType.HardTank:
                {
                    // Build Tanks !
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Gun);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);

                    break;
                }  // Hard Tank
                   // ========================================
            case BuildQueStratgyType.BuildEconomy:
                {
                    // Eary Harvester
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);
                    CurrentBuildQueue.Add(GameManager.RTSType.Harvester);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);

                    break;
                }  // HarvesterEconomy
                   // ========================================

            case BuildQueStratgyType.BuildDefences:
                {
                    // Early Base Defence
                    CurrentBuildQueue.Add(GameManager.RTSType.Gun);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Laser);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Gun);
                    CurrentBuildQueue.Add(GameManager.RTSType.Laser);

                    break;
                }  // BuildBaseDefence
                // ========================================
        } // Switch On  TheInitialBuildStrategy
        NextBuildIndex = 0;
       
        // ================================
        // Now Build Up the Mount Build Sequence
        // Get Reference to Own Enemy Base
       
        MountSequenceList.Add(OwnEnemyBaseManager.MountPoints[0]);
        MountSequenceList.Add(OwnEnemyBaseManager.MountPoints[2]);
        MountSequenceList.Add(OwnEnemyBaseManager.MountPoints[1]);
        MountSequenceList.Add(OwnEnemyBaseManager.MountPoints[7]);
        MountSequenceList.Add(OwnEnemyBaseManager.MountPoints[10]);
        MountSequenceList.Add(OwnEnemyBaseManager.MountPoints[3]);
        MountSequenceList.Add(OwnEnemyBaseManager.MountPoints[8]);
        MountSequenceList.Add(OwnEnemyBaseManager.MountPoints[5]);
        MountSequenceList.Add(OwnEnemyBaseManager.MountPoints[9]);
        MountSequenceList.Add(OwnEnemyBaseManager.MountPoints[4]);
        MountSequenceList.Add(OwnEnemyBaseManager.MountPoints[6]);
        MountSequenceList.Add(OwnEnemyBaseManager.MountPoints[11]);

    } // InitialiseBuildQueue
    //  ========================================================================
    int FindNextMount()
    {
        for(int i = 0; i < MountSequenceList.Count; i++) 
        { 
            MountPoint MountPointUnderReview = MountSequenceList[i].GetComponent<MountPoint>();
            if(MountPointUnderReview != null)
            {
                // Check if Mount is Free
                if (!MountPointUnderReview.HasTurret) return i; 
            }
        }
        return -1; 
    } // FindNextMount
    // ==========================================================================
    private void UpdateBuildQueue()
    {
        CurrentBuildQueue.Clear();
        switch (CurrentAIBuildStrategy)
        {
            case BuildQueStratgyType.Mixed:
                {
                    // Mixed Build Stratgy
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Gun);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Laser);
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);
                    break;
                }  // Mixed
                   // ========================================
            case BuildQueStratgyType.HumveeFest:
                {
                    // HumVee Rush
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);
                    CurrentBuildQueue.Add(GameManager.RTSType.Humvee);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);

                    break;
                }  // Humvee Fest
                   // ========================================

            case BuildQueStratgyType.HardTank:
                {
                    // Tanks !
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Gun);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Laser);

                    break;
                }  // Hard Tank
                   // ========================================
            case BuildQueStratgyType.BuildEconomy:
                {
                    // Harvester First
                    CurrentBuildQueue.Add(GameManager.RTSType.Harvester);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Laser);

                    break;
                }  // HarvesterEconomy
                   // ========================================

            case BuildQueStratgyType.BuildDefences:
                {
                    // Focus on Base Defences
                    CurrentBuildQueue.Add(GameManager.RTSType.Laser);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Gun);
                    CurrentBuildQueue.Add(GameManager.RTSType.Laser);
                    CurrentBuildQueue.Add(GameManager.RTSType.Tank);
                    CurrentBuildQueue.Add(GameManager.RTSType.Gun);

                    break;
                }  // HarvesterEconomy
                // ========================================
        } // Switch On  TheInitialBuildStrategy
        NextBuildIndex = 0;

    } // UpdateBuildQueue
      // ==========================================================================
    #endregion

    // ================================================================================================
    #region Attack Action Responses
    // ================================================================================================
    bool ReviewPossibleAttackOptions()
    {
        bool AttackActioned = false;
        AttackCommentTB.text = "None";
        TheProposedTargetType = TargetOptionTypes.None;
        int RandomAttackTarget;

        // Check if Any Units on Still on Mission  - if not clear Down Mission Attck target (Just to help debug Display)  
        if (!CheckIfAnyUnitsStillOnMission()) ClearMissionAttackTarget(); 

        // Initialise Minumum Number of Humvees and Tanks necessary for a general Attack
        int RandomHumveeAttackNumber = Random.Range(3, 6);
        int RandomTankAttackNumber = Random.Range(3, 5);

        // First Balance Check between Base or HQ for an Indicative Choice
        RandomAttackTarget = Random.Range(0, 100);
        TargetOptionTypes IndicativeTargetChoice = TargetOptionTypes.None;
        if (RandomAttackTarget < 50) IndicativeTargetChoice = TargetOptionTypes.Harvester;
        else IndicativeTargetChoice = TargetOptionTypes.BaseGeneral;

        // Now revise the Humvee Levels Fn of Defences
        if ((NumberofPlayerBaseDefences > 1) && (IndicativeTargetChoice== TargetOptionTypes.BaseGeneral))  
        {
            // Already Has Base defences, So will need more Units for a Base Attack
            RandomHumveeAttackNumber = RandomHumveeAttackNumber + 2;
            // ===================++;
        }
        if ((NumberPlayerTanksProtectingHarvesters > 1) && (IndicativeTargetChoice == TargetOptionTypes.Harvester))
        {
            // Already Has Harvester Protection, So will need more Units for a Harvester Attack
            RandomHumveeAttackNumber = RandomHumveeAttackNumber + 2;
            RandomTankAttackNumber++;
        }
        Debug.Log("[INFO]: AI Attack Assessment:  Number of AI Tanks required for an Attack: " + RandomTankAttackNumber.ToString() + "  & Number of AI Humvees Required or Attack: " + RandomHumveeAttackNumber.ToString()); 
        // ========================================  
        // Consider Humvee based Attacks
        if (NumberofAIHumvees >= RandomHumveeAttackNumber)
        {
            if (IndicativeTargetChoice == TargetOptionTypes.Harvester)
            {
                // Need to Review Escorts vs a Direct Harvester Attack
                RandomAttackTarget = Random.Range(0, 100);
                GameObject TargetGO = null;
                if (RandomAttackTarget < 50)
                {
                    // Go Direct To Harvester: Direct Attack
                    TargetGO = FindPlayerGameObject(GameManager.RTSType.Harvester);
                    if (TargetGO != null)
                    {
                        AssignAnAllHumveeAttack(TargetGO, RTSComponent.AttackMode.DirectTargetOnly);
                        AttackActioned = true;
                        AttackCommentTB.text = "Humvee Attack on Harvester";
                        TheProposedTargetType = TargetOptionTypes.Harvester;
                        return AttackActioned;
                    } 
                } // Direct Harvester Attack
                else
                {
                    // We are going for an Escort Attack
                    TargetGO = FindPlayerHarvesterEscort();
                    if (TargetGO != null)
                    {
                        AssignAnAllHumveeAttack(TargetGO, RTSComponent.AttackMode.DegradeLocalUnits);
                        AttackActioned = true;
                        AttackCommentTB.text = "Humvee Attack on Harvester Escort";
                        TheProposedTargetType = TargetOptionTypes.HarvesterEscorts;
                        return AttackActioned;
                    }
                } // Escort Attack
            } // Humvee Harvester Attack
            // ======================
            if (IndicativeTargetChoice == TargetOptionTypes.BaseGeneral)
            {
                // We are considering a Base Attack
                // So Choices are Base Defences or Direct HQ,Refinary, Factory, Or Free for All
                RandomAttackTarget = 0;
                Random.Range(0, 100);
                GameObject TargetGO = null;

                if (NumberofPlayerBaseDefences <= 1) RandomAttackTarget = Random.Range(0, 100);
                if ((NumberofPlayerBaseDefences > 1) && (NumberofPlayerBaseDefences < 4)) RandomAttackTarget = Random.Range(0, 150);
                if (NumberofPlayerBaseDefences >= 4) RandomAttackTarget = Random.Range(100, 200);

                if (RandomAttackTarget <= 50)
                {
                    TargetGO = FindPlayerGameObject(GameManager.RTSType.BaseHQ);
                    if (TargetGO != null)
                    {
                        AssignAnAllHumveeAttack(TargetGO, RTSComponent.AttackMode.DirectTargetOnly);
                        AttackActioned = true;
                        AttackCommentTB.text = "Humvee Attack on Base HQ";
                        TheProposedTargetType = TargetOptionTypes.BaseSpecific;
                        return AttackActioned;
                    }
                } // Base HQ Direct
                if ((RandomAttackTarget > 50) && (RandomAttackTarget <= 80))
                {
                    TargetGO = FindPlayerGameObject(GameManager.RTSType.Refinary);
                    if (TargetGO != null)
                    {
                        AssignAnAllHumveeAttack(TargetGO, RTSComponent.AttackMode.DirectTargetOnly);
                        AttackActioned = true;
                        AttackCommentTB.text = "Humvee Attack on Refinary";
                        TheProposedTargetType = TargetOptionTypes.BaseSpecific;
                        return AttackActioned;
                    }
                } // Refinary Direct
                if ((RandomAttackTarget > 80) && (RandomAttackTarget <= 100))
                {
                    TargetGO = FindPlayerGameObject(GameManager.RTSType.Factory);
                    if (TargetGO != null)
                    {
                        AssignAnAllHumveeAttack(TargetGO, RTSComponent.AttackMode.DirectTargetOnly);
                        AttackActioned = true;
                        AttackCommentTB.text = "Humvee Attack on Factory";
                        TheProposedTargetType = TargetOptionTypes.BaseSpecific;
                        return AttackActioned;
                    }
                } // Factory Direct
                if (RandomAttackTarget > 100)
                {
                    // Degrade Base Defences
                    GameObject FindPlayerDefence = FindPlayerGameObject(GameManager.RTSType.Laser);
                    if (FindPlayerDefence == null) FindPlayerDefence = FindPlayerGameObject(GameManager.RTSType.Gun);
                    if (FindPlayerDefence != null)
                    {
                        AssignAnAllHumveeAttack(FindPlayerDefence, RTSComponent.AttackMode.DegradeBaseDefences);
                        AttackActioned = true;
                        AttackCommentTB.text = "Humvee Attack on Base Defences";
                        TheProposedTargetType = TargetOptionTypes.BaseDefences;
                        return AttackActioned;
                    }
                } // Degrade Base Defences Direct
                //=================
                // Should have assigned otherwise jut go for a Free For all Around HQ
                TargetGO = FindPlayerGameObject(GameManager.RTSType.BaseHQ);
                if (TargetGO != null)
                {
                    AssignAnAllHumveeAttack(TargetGO, RTSComponent.AttackMode.FreeForAll);
                    AttackActioned = true;
                    AttackCommentTB.text = "Humvee Attack on Base General";
                    TheProposedTargetType = TargetOptionTypes.BaseGeneral;
                    return AttackActioned;
                }
            }  // Base Attacks
        } // Humvee  Attacks
        // ===============================================

        // Now Consider Tank Based Attacks
        if (NumberofAITanks >= RandomTankAttackNumber)
        {
            if (IndicativeTargetChoice == TargetOptionTypes.Harvester)
            {
                // Need to Review Escorts vs a Direct Harvester Attack
                RandomAttackTarget = Random.Range(0, 100);
                GameObject TargetGO = null;
                if (RandomAttackTarget < 50)
                {
                    // Go Direct To Harvester: Direct Attack
                    TargetGO = FindPlayerGameObject(GameManager.RTSType.Harvester);
                    if (TargetGO != null)
                    {
                        AssignAnAllTankAttack(TargetGO, RTSComponent.AttackMode.DirectTargetOnly);
                        AttackActioned = true;
                        AttackCommentTB.text = "Tank Attack on Harvester";
                        TheProposedTargetType = TargetOptionTypes.Harvester;
                        return AttackActioned;
                    }
                } // Direct Harvester Attack
                else
                {
                    // We are going for an Escort Attack
                    TargetGO = FindPlayerHarvesterEscort();
                    if (TargetGO != null)
                    {
                        AssignAnAllTankAttack(TargetGO, RTSComponent.AttackMode.DegradeLocalUnits);
                        AttackActioned = true;
                        AttackCommentTB.text = "Tank Attack on Harvester Escorts";
                        TheProposedTargetType = TargetOptionTypes.HarvesterEscorts;
                        return AttackActioned;
                    }
                } // Escort Attack
            } // Tanks Harvester Attack
            // ======================
            if (IndicativeTargetChoice == TargetOptionTypes.BaseGeneral)
            {
                // We are considering a Base Attack
                // So Choices are Base Defences or Direct HQ,Refinary, Factory, Or Free for All
                RandomAttackTarget = 0;
                Random.Range(0, 100);
                GameObject TargetGO = null;

                if (NumberofPlayerBaseDefences <= 1) RandomAttackTarget = Random.Range(0, 100);
                if ((NumberofPlayerBaseDefences > 1) && (NumberofPlayerBaseDefences < 4)) RandomAttackTarget = Random.Range(0, 150);
                if (NumberofPlayerBaseDefences >= 4) RandomAttackTarget = Random.Range(100, 200);

                if (RandomAttackTarget <= 50)
                {
                    TargetGO = FindPlayerGameObject(GameManager.RTSType.BaseHQ);
                    if (TargetGO != null)
                    {
                        AssignAnAllTankAttack(TargetGO, RTSComponent.AttackMode.DirectTargetOnly);
                        AttackActioned = true;
                        AttackCommentTB.text = "Tank Attack on Base HQ";
                        TheProposedTargetType = TargetOptionTypes.BaseSpecific;
                        return AttackActioned;
                    }
                } // Base HQ Direct
                if ((RandomAttackTarget > 50) && (RandomAttackTarget <= 80))
                {
                    TargetGO = FindPlayerGameObject(GameManager.RTSType.Refinary);
                    if (TargetGO != null)
                    {
                        AssignAnAllTankAttack(TargetGO, RTSComponent.AttackMode.DirectTargetOnly);
                        AttackActioned = true;
                        AttackCommentTB.text = "Tank Attack on Refinary";
                        TheProposedTargetType = TargetOptionTypes.BaseSpecific;
                        return AttackActioned;
                    }
                } // Refinary Direct
                if ((RandomAttackTarget > 80) && (RandomAttackTarget <= 100))
                {
                    TargetGO = FindPlayerGameObject(GameManager.RTSType.Factory);
                    if (TargetGO != null)
                    {
                        AssignAnAllTankAttack(TargetGO, RTSComponent.AttackMode.DirectTargetOnly);
                        AttackActioned = true;
                        AttackCommentTB.text = "Tank Attack on Factory";
                        TheProposedTargetType = TargetOptionTypes.BaseSpecific;
                        return AttackActioned;
                    }
                } // Factory Direct
                if (RandomAttackTarget > 100)
                {
                    // Degrade Base Defences
                    GameObject FindPlayerDefence = FindPlayerGameObject(GameManager.RTSType.Laser);
                    if (FindPlayerDefence == null) FindPlayerDefence = FindPlayerGameObject(GameManager.RTSType.Gun);
                    if (FindPlayerDefence != null)
                    {
                        AssignAnAllTankAttack(FindPlayerDefence, RTSComponent.AttackMode.DegradeBaseDefences);
                        AttackActioned = true;
                        AttackCommentTB.text = "Tank Attack on Base Defences";
                        TheProposedTargetType = TargetOptionTypes.BaseDefences;
                        return AttackActioned;
                    }
                } // Degrade Base Defences Direct
                  //=================
                  // Should have assigned otherwise just go for a Free For all Around HQ
                TargetGO = FindPlayerGameObject(GameManager.RTSType.BaseHQ);
                if (TargetGO != null)
                {
                    AssignAnAllTankAttack(TargetGO, RTSComponent.AttackMode.FreeForAll);
                    AttackActioned = true;
                    AttackCommentTB.text = "Tank Attack on Base";
                    TheProposedTargetType = TargetOptionTypes.BaseGeneral;
                    return AttackActioned;
                }
            }  // Base Type Attacks
        } // Tank Based Attacks
        // ============================
        // May have fallen through if either Refinary or Harvester already destroyed
        if((FindPlayerGameObject(GameManager.RTSType.Refinary) == null) || (FindPlayerGameObject(GameManager.RTSType.Harvester)==null))
        {
            if ((NumberofAITanks >= RandomTankAttackNumber) || (NumberofAIHumvees >= RandomHumveeAttackNumber))
            {
                // Then Consider a General Base Degrade - To Get Action in Play to kill off their base
                GameObject TargetGO = null;
                TargetGO = FindPlayerGameObject(GameManager.RTSType.BaseHQ);
                if (TargetGO != null)
                {
                    AssignAnAllTankAttack(TargetGO, RTSComponent.AttackMode.FreeForAll);
                    AssignAnAllHumveeAttack(TargetGO, RTSComponent.AttackMode.FreeForAll);
                    AttackActioned = true;
                    AttackCommentTB.text = "Tank & Humvee Attack on Base";
                    TheProposedTargetType = TargetOptionTypes.BaseGeneral;
                    return AttackActioned;
                }
            }
        }  // No Player Refinary or Harvester Left
        // ==============================

        return AttackActioned;
    } // ReviewAttack
    // ============================================================================================================
    public List<int> FindRouteToAttackEnemyBase()
    {
        // Create a Path List to Fill Out and Return
        List<int> PathToBase = new List<int>();
        // Random Choice of Routes
        int RandomRoutesChoice = Random.Range(0, 100);

        if (RandomRoutesChoice < 20)
        {
            // Anti Round Left
            PathToBase.Add(0);
            PathToBase.Add(2);
            PathToBase.Add(9);
            //Debug.Log("[INFO]: Enemy AI Has Chosen Round Left Attack Path");
        }
        if ((RandomRoutesChoice >= 20) && (RandomRoutesChoice < 40))
        {
            // Direct Left
            PathToBase.Add(0);
            PathToBase.Add(4);
            PathToBase.Add(9);
            //Debug.Log("[INFO]: Enemy AI Has Chosen  left Attack Path");
        }
        if ((RandomRoutesChoice >= 40) && (RandomRoutesChoice < 60))
        {
            // Middle
            PathToBase.Add(5);
            PathToBase.Add(4);
            PathToBase.Add(11);
            //Debug.Log("[INFO]: Enemy AI Has Chosen Middle Attack Path");
        }
        if ((RandomRoutesChoice >= 60) && (RandomRoutesChoice < 80))
        {
            // Right
            PathToBase.Add(3);
            PathToBase.Add(6);
            PathToBase.Add(11);
            //Debug.Log("[INFO]: Enemy AI Has Chosen Right Attack Path");
        }
        if (RandomRoutesChoice >= 80)
        {
            // Round Right
            PathToBase.Add(7);
            PathToBase.Add(10);
            PathToBase.Add(12);
            //Debug.Log("[INFO]: Enemy AI Has Chosen Round Right Attack Path");
        }

        return PathToBase;
    }   // FindRouteToAttackEnemyBase
    // ====================================================================================================
    
    void AssignAnAllHumveeAttack(GameObject TargetGameObject, RTSComponent.AttackMode TheAttackMode )
    {
        // First Check No Mission In Play
        if (TargetGameObject == null) return; 
        if (CheckIfAnyUnitsStillOnMission()) return;
        ClearAnyCurrentMissionList();

        // Now Set Up Mission Attack
        SetMissionAttackTarget(TargetGameObject);
        List<int> HQAttackRoute = FindRouteToAttackEnemyBase();
        foreach (GameObject PossibleRTSObject in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            RTSComponent PossibleRTSItem = PossibleRTSObject.GetComponent<RTSComponent>();
            if ((PossibleRTSItem.RTSType == GameManager.RTSType.Humvee) && (PossibleRTSItem.TheFaction == GameManager.Faction.Enemy) && (PossibleRTSItem.TheAttackMode == RTSComponent.AttackMode.None))
            {
                // But Only If NOT already On an Attack Path
                if (!((PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.EngagementPhase) || (PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.AttackMoveToOuter)))
                {
                    PossibleRTSItem.SetUnitOffOnPathToEnemyBase(HQAttackRoute, TargetGameObject);
                    PossibleRTSItem.SetAttackMode(TheAttackMode);
                    UnitsOnCurrentMission.Add(PossibleRTSObject);
                    PossibleRTSItem.OnMissionAttack = true;
                }
            }
        } // Search Across RTS Game Objects

    } // AssignAnAllHumveeAttack
    // =============================================================================
    void AssignAnAllTankAttack(GameObject TargetGameObject,RTSComponent.AttackMode TheAttackMode)
    {
        // First Check No Mission In Play
        if (TargetGameObject == null) return;
        if (CheckIfAnyUnitsStillOnMission()) return;
        ClearAnyCurrentMissionList();

        // Now Set Up Mission Attack
        SetMissionAttackTarget(TargetGameObject);
        List<int> HQAttackRoute = FindRouteToAttackEnemyBase();
        foreach (GameObject PossibleRTSObject in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            RTSComponent PossibleRTSItem = PossibleRTSObject.GetComponent<RTSComponent>();
            if ((PossibleRTSItem.RTSType == GameManager.RTSType.Tank) && (PossibleRTSItem.TheFaction == GameManager.Faction.Enemy) && (PossibleRTSItem.TheAttackMode == RTSComponent.AttackMode.None))
            {
                // But Only If NOT already On an Attack Path
                if (!((PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.EngagementPhase) || (PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.AttackMoveToOuter)))
                {
                    PossibleRTSItem.SetUnitOffOnPathToEnemyBase(HQAttackRoute, TargetGameObject);
                    PossibleRTSItem.SetAttackMode(TheAttackMode);
                    UnitsOnCurrentMission.Add(PossibleRTSObject);
                    PossibleRTSItem.OnMissionAttack = true; 
                }
            }
        } // Search Across RTS Game Objects

    } // AssignAnAllTankAttack
      // ====================================================================================
    bool CheckIfAnyUnitsStillOnMission()
    {
        foreach(GameObject UnitSentOnMission in UnitsOnCurrentMission)
        {
            if (UnitSentOnMission != null)
            {
                if (UnitSentOnMission.GetComponent<RTSComponent>().OnMissionAttack) return true;
            }
        }
        return false;
    } // CheckIfAnyUnitsStillOnMission
    // ======================================================================================
    void ClearAnyCurrentMissionList()
    {
        foreach (GameObject UnitSentOnMission in UnitsOnCurrentMission)
        {
            if (UnitSentOnMission != null)
            {
                UnitSentOnMission.GetComponent<RTSComponent>().OnMissionAttack = false;
            }
        }
        UnitsOnCurrentMission.Clear();
    } // ClearAnyCurrentMissionList
    // ======================================================================================
    public void SetMissionAttackTarget(GameObject NewTarget)
    {
         MissionAttackTarget = NewTarget;
    }
    public void ClearMissionAttackTarget()
    {
        MissionAttackTarget = null;
    }
    public GameObject GetMissionAttackTarget()
    {
        return MissionAttackTarget;
    }
    // =====================================================================================
    #endregion
    // ====================================================================================
    #region Retreat and Defend Action Response Functions
    // ====================================================================================
    void ReturnAvailableUnitsToDefendBase()
    {
        // Set the Group Attack Target
        if(OwnEnemyBaseManager.TheAttackingUnit == null) return;
        SetMissionAttackTarget(OwnEnemyBaseManager.TheAttackingUnit); 

        // Base Under Attack, so send All Tanks Back To Defend
        foreach (GameObject PossibleRTSObject in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            RTSComponent PossibleRTSItem = PossibleRTSObject.GetComponent<RTSComponent>();
            if (((PossibleRTSItem.RTSType == GameManager.RTSType.Tank) || (PossibleRTSItem.RTSType == GameManager.RTSType.Humvee) )&& (PossibleRTSItem.TheFaction == GameManager.Faction.Enemy))
            {
                // A Possible Unit : Check in Viable State to respond 
                if ((PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.IdleNone) || (PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.AttackMoveToInner) || (PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.AttackMoveToMid) || (PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.PatrollingPoints) || (PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.ProtectingHarvester))
                {
                    // Send A Group Attack To the Attacking Units
                    PossibleRTSItem.SetAttackMode(RTSComponent.AttackMode.DegradeLocalUnits);
                    PossibleRTSItem.PartOfGroupDirectAttack(OwnEnemyBaseManager.TheAttackingUnit);
                }
            }
        } // Search Across RTS Game Objects

    } // ReturnAvailableUnitsToDefendBase
    // ====================================================================================
    void ReturnSomeUnitsToDefendUnit()
    {
        // A Unit is Attack, so Consider sending 25% of Units To Support
        if (AssumedNumberUnitsToSendToUnitDefence > 0)
        {
            // OK Send At least On Unit to Help
            foreach (GameObject PossibleRTSObject in GameObject.FindGameObjectsWithTag("RTSGameObject"))
            {
                RTSComponent PossibleRTSItem = PossibleRTSObject.GetComponent<RTSComponent>();
                if (((PossibleRTSItem.RTSType == GameManager.RTSType.Tank) || (PossibleRTSItem.RTSType == GameManager.RTSType.Humvee)) && (PossibleRTSItem.TheFaction == GameManager.Faction.Enemy))
                {
                    // A Possible Unit : Check in Viable State to respond 
                    if ((PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.IdleNone) || (PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.AttackMoveToInner))
                    {
                        if (CountofAssignedUnits < AssumedNumberUnitsToSendToUnitDefence)
                        {
                            // Send Direct Attack To the Attacking Unit
                            PossibleRTSItem.DirectAttackOnly(OwnEnemyBaseManager.TheAttackingUnit);

                            CountofAssignedUnits++;
                        } // Number Of Required Units To Send
                    } // In Availbale Mode
                } // Humvee or Tank To Send
            } // Search Across RTS Game Objects
           
        }  // At Least One Unit to Support
        //Debug.Log("[INFO]:  Enemy AI Has Decided to Send " + CountofAssignedUnits.ToString() + " To Support Unit Being Attacked"); 

    } // ReturnSomeUnitsToDefendUnit
    // ====================================================================================
    void SendToProtectHarvester()
    {
        // Set the Group Attack Target
        if (OwnEnemyBaseManager.TheAttackingUnit == null) return;
        SetMissionAttackTarget(OwnEnemyBaseManager.TheAttackingUnit);

        // Will be sending All Units, not allready Protecting Harvester
        foreach (GameObject PossibleRTSObject in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            RTSComponent PossibleRTSItem = PossibleRTSObject.GetComponent<RTSComponent>();
            if (((PossibleRTSItem.RTSType == GameManager.RTSType.Tank) || (PossibleRTSItem.RTSType == GameManager.RTSType.Humvee)) && (PossibleRTSItem.TheFaction == GameManager.Faction.Enemy))
            {
                // A Possible Unit : Check in Viable State to respond 
                if ((PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.IdleNone) || (PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.AttackMoveToInner) || (PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.AttackMoveToMid) || (PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.PatrollingPoints) || (PossibleRTSItem.CurrentUnitState == RTSComponent.RTSUnitStates.ProtectingHarvester))
                {
                    // Send A Group Attack To the Attacking Units
                    PossibleRTSItem.SetAttackMode(RTSComponent.AttackMode.DegradeLocalUnits);
                    PossibleRTSItem.PartOfGroupDirectAttack(OwnEnemyBaseManager.TheAttackingUnit);

                } // In Availbale Mode
            } // Humvee or Tank To Send
        } // Search Across RTS Game Objects
    } // SendToProtectHarvester
    // ====================================================================================




    // =====================================================================================
    #endregion

    // =================================================================================
    #region Utility Functions
    // =============================================================================
   
    // ==============================================================================
    int NumberPlayerTanksProtectingHarvester()
    {
        // Just count those in resonable proximity to Payer Harvesters
        int PlayerTankCount = 0;
        foreach (GameObject PossibleHarvesterGO in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            RTSComponent PossibleHarvesterRTSItem = PossibleHarvesterGO.GetComponent<RTSComponent>();
            if ((PossibleHarvesterRTSItem.RTSType == GameManager.RTSType.Harvester) && (PossibleHarvesterRTSItem.TheFaction == GameManager.Faction.Player))
            {
                // Now Review which Player Tanks are in Vicinity of Harvester
                foreach (GameObject PlayerTankGO in PlayerTankList)
                {
                    if(PlayerTankGO!= null)
                    {
                        if (Vector3.Distance(PossibleHarvesterGO.transform.position, PlayerTankGO.transform.position) < 75.0f) PlayerTankCount++;
                    }
                }
            }
        } // Possible Harvster GO search
        return PlayerTankCount;
    } // NumberPlayerTanksProtectingHarvester
    // =============================================================================
    GameObject FindPlayerHarvesterEscort()
    {
        GameObject PlayerEscortGO = null; 
        foreach (GameObject PossibleHarvesterGO in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            RTSComponent PossibleHarvesterRTSItem = PossibleHarvesterGO.GetComponent<RTSComponent>();
            if ((PossibleHarvesterRTSItem.RTSType == GameManager.RTSType.Harvester) && (PossibleHarvesterRTSItem.TheFaction == GameManager.Faction.Player))
            {
                // Now Review which Player Tanks are in Vicinity of Harvester
                foreach (GameObject PlayerTankGO in PlayerTankList)
                {
                    if (PlayerTankGO != null)
                    {
                        if (Vector3.Distance(PossibleHarvesterGO.transform.position, PlayerTankGO.transform.position) < 75.0f)
                        {
                            PlayerEscortGO = PlayerTankGO;
                            return PlayerEscortGO;
                        }
                    }
                }
            }
        } // Possible Harvster GO search
        return PlayerEscortGO;
    } // FindPlayerHarvesterEscort
    // ==============================================================================
    GameObject FindPlayerGameObject(GameManager.RTSType TargetType)
    {
        GameObject PlayerTargetGameObject = null;

        foreach (GameObject PossibleRTSObject in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            RTSComponent PossibleRTSItem = PossibleRTSObject.GetComponent<RTSComponent>();
            if ((PossibleRTSItem.RTSType == TargetType) && (PossibleRTSItem.TheFaction == GameManager.Faction.Player))
            {
                // Have Found Oppo (Player) Target
                PlayerTargetGameObject = PossibleRTSObject;
                return PlayerTargetGameObject;
            }
        }
        return PlayerTargetGameObject;

    } // FindPlayerGameObject
    #endregion
    // =========================================================================================
}

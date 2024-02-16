using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class HarvesterManager : MonoBehaviour
{
    // ===========================================================================================
    public enum HarvesterState { IdleWaiting,MovingOut,HarvestingOre,ReturningToRefinaryWP,ReturnToOffloadPoint,OffLoading,Retreating, WaitingInSafePoint}
    // ===========================================================================================
    public HarvesterState TheHarvesterState;
    [SerializeField] public RTSComponentConfiguration TheHarvesterConfigurationParameters;
    Animator TheHarvesterAnimator;
    float OreFieldRadius = 50.0f; 
    List<GameObject> NearestOreFieldsList;
    GameObject CurrentOreField; 
    NavMeshAgent TheNavMeshAgent;
    RTSComponent TheRTSComponent; 
    
    // Home Refinary
    GameObject HomeRefinary;
    Vector3 RefinaryWayPoint;
    Vector3 RefinaryFrontPoint;
    Vector3 RefinarySafePoint; 
    // Home Base
    BaseManager HomeBaseManager; 
    // Dwell Periods
    float HarvestCompleteTime;
    float OffloadCompleteTime;
    float HarvesterSurveillancePeriod = 2.0f;
    float NextHarvesterPlayerUnitsCheckTime;
    int NumberFriendlyAIUnitsNearby;
    float RetreatPeriodCompleteTime;

    int EntrapmentCount = 0; 
    // ===========================================================================================
    private void Awake()
    {
        TheHarvesterState = HarvesterState.IdleWaiting;
        TheHarvesterAnimator = GetComponent<Animator>();
        TheNavMeshAgent = GetComponent<NavMeshAgent>();
        CurrentOreField = null;
        TheRTSComponent = transform.GetComponent<RTSComponent>();
        NumberFriendlyAIUnitsNearby = 0; 

    } // Awake
    // ============================================================================================

    void Start()
    {
        //Debug.Log(" [INFO]: Harvester Start Called");

        NextHarvesterPlayerUnitsCheckTime = Time.time + HarvesterSurveillancePeriod;
        EntrapmentCount = 0;  


    }  // Start
    // ==============================================================================================
    public bool FindHomeRefinaryAndBase(GameManager.Faction ThisUnitFaction)
    {
        bool FoundRefinary = false; 
        // Note we do not Know Faction until AFTER Instantiation Start()
        // Find the corresponding Home Refinary
        foreach (GameObject PossibleRTSObject in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            RTSComponent PossibleRTSItem = PossibleRTSObject.GetComponent<RTSComponent>();
            if ((PossibleRTSItem.RTSType == GameManager.RTSType.Refinary) && (PossibleRTSItem.TheFaction == ThisUnitFaction))
            {
                // Found Home Refinary, set Waypoints
                HomeRefinary = PossibleRTSObject;
                RefinaryFrontPoint = HomeRefinary.transform.position + HomeRefinary.transform.right * 10.0f;
                RefinarySafePoint = HomeRefinary.transform.position - HomeRefinary.transform.right * 25.0f;
                RefinaryWayPoint = HomeRefinary.transform.position - HomeRefinary.transform.forward * 20.0f + HomeRefinary.transform.right * 2.0f;
                 // Reached Refinary Way Point, so Move To Offload Point
                TheHarvesterState = HarvesterState.ReturnToOffloadPoint;
                TheNavMeshAgent.SetDestination(RefinaryFrontPoint);
                FoundRefinary = true; 
            }

        }  // For each Possible RTS Object
        // ================================================
        // Search For Home Base
        foreach (GameObject PossibleBase in GameObject.FindGameObjectsWithTag("Base"))
        {
            if (PossibleBase.GetComponent<BaseManager>().BaseFaction == ThisUnitFaction)
                HomeBaseManager = PossibleBase.GetComponent<BaseManager>();
        }  // Possible Base Search

        // Now Set Up nearest Ore Field List been instantiated (Noting Start() Called some time AFTER Instantiations
        if (NearestOreFieldsList == null)
        {
            // Find All the Ore Fields
            NearestOreFieldsList = new List<GameObject>();
            foreach (GameObject OreFieldGameObjectItem in GameObject.FindGameObjectsWithTag("OreField"))
            {
                NearestOreFieldsList.Add(OreFieldGameObjectItem);
            }
            // Now Order that List According to Current Distance from This.Harvster.Position  : Using Linq Order By
            NearestOreFieldsList = NearestOreFieldsList.OrderBy(OField => Vector3.Distance(OField.transform.position, HomeRefinary.transform.position)).ToList();
            //NearestOreFieldsList.Reverse();
        } // NearestOreFieldsList does not exist
        // ======================

        return FoundRefinary; 
        // =================================================
    } // SetHomeRefinary
    // =================================================================================================
    // Update is called once per frame
    void Update()
    {
      // Local GUI or Debug

    }
    // ==============================================================================================
    // Physics? Manoeuver Updates
    void FixedUpdate()
    {
        // ==============================
        // Harvester Surveillance Check for AI Harvesters 
        if ((Time.time > NextHarvesterPlayerUnitsCheckTime) && (TheRTSComponent.TheFaction == GameManager.Faction.Enemy))
        { 
            if(CheckAnyPlayerUnitsInVicinity() && (NumberFriendlyAIUnitsNearby<2) && ((TheHarvesterState == HarvesterState.MovingOut) || (TheHarvesterState == HarvesterState.HarvestingOre)))
            {
                // Retreat to Safe Point 
                TheHarvesterState = HarvesterState.Retreating;
                TheNavMeshAgent.SetDestination(RefinarySafePoint);
                TheNavMeshAgent.speed = TheHarvesterConfigurationParameters.UnitSpeed;
                TheHarvesterAnimator.SetTrigger("Moving"); 
            }
            NextHarvesterPlayerUnitsCheckTime = Time.time + HarvesterSurveillancePeriod;
        } // Harvester Surevaillance
        // =================================

        // Now Progress the Main Harvester State Management 
        switch (TheHarvesterState) 
        {
            case HarvesterState.IdleWaiting:
            {
                    // Do No Do Anything For Player - Unless Instructed

                    // But AI Seems Somehow to get Caught into an Idle so For Enemy - So Always get going towards an ore field
                    if (TheRTSComponent.TheFaction == GameManager.Faction.Enemy)
                    {
                        GameObject NearestOreField = TheRTSComponent.FindNearestViableOreField();
                        if (NearestOreField != null) SetMovingOutState(NearestOreField.transform.position);
                    } // Enemy Stuck in Idle


                    break;
            }  // HarvesterState.IdleWaiting
            // ==========================================
            case HarvesterState.MovingOut:
            {
                // Check If Reached Destination
                if ((TheNavMeshAgent.remainingDistance < TheNavMeshAgent.stoppingDistance*1.2f)  && (TheNavMeshAgent.hasPath))
                {
                    // Now Check if Within ore Field
                    if(ConfirmWithinAViableField())
                    {
                        TheHarvesterState = HarvesterState.HarvestingOre;
                        TheHarvesterAnimator.SetTrigger("Harvesting");
                        HarvestCompleteTime = Time.time + TheHarvesterConfigurationParameters.HarvestingDwellTime;
                    }
                    else
                    {
                        // Clicked in Open Space, so Simply Set Idle
                        TheHarvesterState = HarvesterState.IdleWaiting;
                        TheHarvesterAnimator.SetTrigger("SetIdle");
                    }

                    TheNavMeshAgent.speed = 0.0f; 
                }  // Reached Destination

                break;
            }  // HarvesterState.MovingOut:
            // ==========================================
            case HarvesterState.HarvestingOre:
            {
                // Check If Completed Harvesting
                if(Time.time> HarvestCompleteTime)
                {
                    // Harvest Time Expired - So Return to Refinary
                    TheHarvesterState = HarvesterState.ReturningToRefinaryWP;
                    TheNavMeshAgent.SetDestination(RefinaryWayPoint);
                    TheNavMeshAgent.speed = TheHarvesterConfigurationParameters.UnitSpeed;
                    TheHarvesterAnimator.SetTrigger("Moving");

                    // And Deplete the Ore Field
                    if(CurrentOreField!=null)
                    {
                        CurrentOreField.GetComponent<OrefieldManager>().DecrementOreField();
                    }

                }  // Harvest Time Expired
                EntrapmentCount = 0;
                break;
            }// HarvesterState.HarvestingOre:
            // ==========================================
            case HarvesterState.ReturningToRefinaryWP:
            {
                // Check If Reached Refinary Way Point
                if ((TheNavMeshAgent.remainingDistance < TheNavMeshAgent.stoppingDistance * 1.2f) && (TheNavMeshAgent.hasPath))
                {
                    if(HomeRefinary != null)
                    {
                        // Reached Refinary Way Point, so Move To Offload Point
                        TheHarvesterState = HarvesterState.ReturnToOffloadPoint;
                        TheNavMeshAgent.SetDestination(RefinaryFrontPoint);
                    }
                    else    
                    {
                        // Lost Home Refinary, so need to attempt search for it again
                        if(FindHomeRefinaryAndBase(TheRTSComponent.TheFaction))
                        {
                            TheHarvesterState = HarvesterState.ReturnToOffloadPoint;
                            TheNavMeshAgent.SetDestination(RefinaryFrontPoint);
                        }
                        else
                        {
                            // Unable to Find a Refinary so Can Only Set Idle
                            TheHarvesterState = HarvesterState.IdleWaiting;
                            TheHarvesterAnimator.SetTrigger("SetIdle");
                        }
                    } // Home Refinary Lost
                }  // Reached Refinary Way Point
                break;
            }  // HarvesterState.ReturningToRefinaryWP:
            // ==========================================
            case HarvesterState.ReturnToOffloadPoint:
            {
                // Check If Reached Refinary Way Point
                if ((TheNavMeshAgent.remainingDistance <  TheNavMeshAgent.stoppingDistance * 1.2f) && (TheNavMeshAgent.hasPath))
                    {
                    // Reached Refinary Offload Point, So begin Off loading
                    TheHarvesterState = HarvesterState.OffLoading;
                    TheHarvesterAnimator.SetTrigger("SetIdle");
                    TheNavMeshAgent.speed = 0.0f;
                    OffloadCompleteTime = Time.time + TheHarvesterConfigurationParameters.RefiningDwellTime;

                }  // Reached Refinary Offload Point

                break;
            }  // HarvesterState.ReturningToRefinaryWP:
            // ==========================================
            case HarvesterState.OffLoading:
                {
                    // Check If Completed Offloading
                    if (Time.time > OffloadCompleteTime)
                    {
                        // Unload the Harvester Into base
                        HomeBaseManager.HarvesterUnload();
                        GameObject NextOreField = ReviewNextOreField();
                        if (NextOreField != null)
                        {
                            // Found Next Ore Field, so Move Out to it
                            SetMovingOutState(NextOreField.transform.position);
                        }
                        else
                        {
                            // No Other Ore Field so Will just Have to Stay Put
                            TheHarvesterState = HarvesterState.IdleWaiting;
                            TheHarvesterAnimator.SetTrigger("SetIdle");
                            TheNavMeshAgent.speed = 0.0f;
                        }
                    }  // Completed Offloading
                    break;
                }  // HarvesterState.OffLoading:
                   // ==========================================
            case HarvesterState.Retreating:
                {
                    // Check If Reached Safe Point
                    if ((TheNavMeshAgent.remainingDistance < TheNavMeshAgent.stoppingDistance * 1.2f) && (TheNavMeshAgent.hasPath))
                    {
                        // Assume Reached Safe point
                        TheHarvesterState = HarvesterState.WaitingInSafePoint;   
                        TheNavMeshAgent.speed = 0.0f;
                        RetreatPeriodCompleteTime = Time.time + HarvesterSurveillancePeriod*5.0f;  // so assume that will resume after 12.5 seconds

                        // Now Check Entrapment Status
                        EntrapmentCount++;
                        HomeBaseManager.HarvesterHalfUnload();

                        if((EntrapmentCount==4)|| (EntrapmentCount == 7)|| (EntrapmentCount == 10) || (EntrapmentCount == 12)) HomeBaseManager.ExcessEntrapmentOffLoad();

                    }  // Reached Destination

                    break;
                }  // HarvesterState.Retreating:
                   // ==========================================
            case HarvesterState.WaitingInSafePoint:
                {
                    // Check If Completed Offloading
                    if (Time.time > RetreatPeriodCompleteTime)
                    {
                        GameObject NextOreField = ReviewNextOreField();
                        if (NextOreField != null)
                        {
                            // Found Next Ore Field, so Move Out to it
                            SetMovingOutState(NextOreField.transform.position);
                        }
                        else
                        {
                            // No Other Ore Field so Will just Have to Stay Put
                            TheHarvesterState = HarvesterState.IdleWaiting;
                            TheHarvesterAnimator.SetTrigger("SetIdle");
                            TheNavMeshAgent.speed = 0.0f;
                        }
                    }  // Completed Retreat 
                    break;
                }  // HarvesterState.WaitingInSafePoint:
                // ============================================

        }  // TheHarvester State switch  
        // ================================

    } // FixedUpdate
    // ==============================================================================================
    bool ConfirmWithinAViableField()
    {
        bool LocationWithinOreField = false;
        CurrentOreField = null;

        // Review each Ore Field, as to the Destination
        foreach (GameObject OreField in NearestOreFieldsList)
        {
            if (OreField.GetComponent<OrefieldManager>().CurrentFill > 0)
            { 
                if ((Vector3.Distance(OreField.transform.position, this.transform.position) < OreFieldRadius))
                {
                    LocationWithinOreField = true;
                    CurrentOreField = OreField;
                }
            }
        }
        return LocationWithinOreField;
    } // ConfirmWithinAnOreField

    // ===============================================================================================
    public GameObject ReviewNextOreField()
    {
        GameObject RtnOreField = null;
        // =============================
        // Review Through List of Ore Fields
        foreach (GameObject OreField in NearestOreFieldsList)
        {
            // Now Check if there is Ore within that Ore Field
            if (OreField.GetComponent<OrefieldManager>().CurrentFill > 0)
            {
                // Also Check that No Player Objects in Vicinity - 
                if (TheRTSComponent.TheFaction == GameManager.Faction.Enemy)
                {
                    // For Enemy AI: Need to check No Player Units at Ore field
                    if (!CheckNoPlayerUnitsNearOreField(OreField)) return OreField;
                }
                else
                {
                    // Player 
                    return OreField;
                }
            }
        } // Fo all Orefields
        return RtnOreField;
    } // ReviewNextOreField
    // ================================================================================================
    
    public void SetMovingOutState(Vector3 Destination)
    {
        //Debug.Log("[INFO]: Harveter Move Instucuted: ");
        TheHarvesterState = HarvesterState.MovingOut;
        TheNavMeshAgent.SetDestination(Destination);
        TheNavMeshAgent.speed = TheHarvesterConfigurationParameters.UnitSpeed;
        TheHarvesterAnimator.SetTrigger("Moving");
    } // SetOreFieldDestination
   // ==============================================================================================
   bool CheckNoPlayerUnitsNearOreField(GameObject OreFieldbeingReviewed)
   {
        bool PlayerUnitsAtOrefield = false;

        foreach (GameObject PossiblePlayerUnit in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            // Now Check if Hostile Player)  And either Tank pr Humvee 
            RTSComponent PossiblePlayerUnitRTS = PossiblePlayerUnit.GetComponent<RTSComponent>();
            if ((PossiblePlayerUnitRTS.TheFaction == GameManager.Faction.Player) && ((PossiblePlayerUnitRTS.RTSType == GameManager.RTSType.Humvee) || (PossiblePlayerUnitRTS.RTSType == GameManager.RTSType.Tank)))
            {
                // Distance From Ore Field
                float RangeFromOreField = Vector3.Distance(PossiblePlayerUnitRTS.transform.position, OreFieldbeingReviewed.transform.position);
                // Check None within 150.0f
                if (RangeFromOreField < 150.0f)
                {
                    PlayerUnitsAtOrefield = true;
                    return true;
                } // Range Check
            } // Check Enemy 
        }  // for all RTS Objects in Game
        return PlayerUnitsAtOrefield;
    }  // CheckNoPlayerUnitsNearOreField
    // ================================================================================================
    bool CheckAnyPlayerUnitsInVicinity()
    {
        bool PlayerUnitsNearby = false;
        NumberFriendlyAIUnitsNearby = 0;
        foreach (GameObject PossiblePlayerUnit in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            // Now Check if Hostile Player)  And either Tank pr Humvee 
            RTSComponent PossiblePlayerUnitRTS = PossiblePlayerUnit.GetComponent<RTSComponent>();
            if ((PossiblePlayerUnitRTS.TheFaction == GameManager.Faction.Player) && ((PossiblePlayerUnitRTS.RTSType == GameManager.RTSType.Humvee) || (PossiblePlayerUnitRTS.RTSType == GameManager.RTSType.Tank)))
            {
                // Distance From Own 
                float RangeFromMe = Vector3.Distance(PossiblePlayerUnitRTS.transform.position, this.transform.position);
                // Check None within 100.0f
                if (RangeFromMe < 100.0f)
                {
                    PlayerUnitsNearby = true;
                } // Range Check
            } // Check Enemy 
            if ((PossiblePlayerUnitRTS.TheFaction == GameManager.Faction.Enemy) && ((PossiblePlayerUnitRTS.RTSType == GameManager.RTSType.Humvee) || (PossiblePlayerUnitRTS.RTSType == GameManager.RTSType.Tank)))
            {
                // Local AI Units
                float RangeFromMe = Vector3.Distance(PossiblePlayerUnitRTS.transform.position, this.transform.position);
                // Check None within 125.0f
                if (RangeFromMe < 125.0f)
                {
                    NumberFriendlyAIUnitsNearby++;
                } // Range Check
            } // Local AI Units 
        }  // for all RTS Objects in Game
        return PlayerUnitsNearby;
    }  // CheckNoPlayerUnitsInVicinity
    // ================================================================================================
}

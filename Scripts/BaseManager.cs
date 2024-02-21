using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class BaseManager : MonoBehaviour
{
    public enum UnderAttackModes { None, BaseBuildingUnderAttack, BaseDefenceUnderAttack,HarvesterUnderAttack, UnitUnderAttack };

    // Manages a Base, HQ, Factory Refinary and the Allocation of Defences to Slot Points
    [Header("Main References")]

    [SerializeField]
    public GameManager.Faction BaseFaction;

    [SerializeField]
    public BaseConfiguration TheBaseConfiguration;

    [SerializeField]
    public List<GameObject> TacticalPoints;

    [Header("Prefabs")]
    [SerializeField]
    private GameObject HQPrefab;
    public GameObject HQGameObject;
    private RTSComponent HQRTSComponent; 

    [SerializeField]
    private GameObject FactoryPrefab;
    public GameObject FactoryGameObject;
    private RTSComponent FactoryRTSComponent;

    [SerializeField]
    private GameObject RefinaryPrefab;
    public GameObject RefinaryGameObject;
    private RTSComponent RefinaryRTSComponent;
    [SerializeField]
    private GameObject MountPointPrefab;
   
    [SerializeField]
    private GameObject GunSystemPrefab;
    [SerializeField]
    private GameObject LaserSystemPrefab;
    [SerializeField]
    private GameObject HumveePrefab;
    [SerializeField]
    private GameObject TankPrefab;
    [SerializeField]
    private GameObject HarvesterPrefab;

    // =============================================
    [Header("Current Variables")]
    public int CurrentBudget;
    public UnderAttackModes CurrentAttacks;
    public GameObject TheAttackingUnit;
    public GameObject[] MountPoints;

    private Vector3 UnitSpawnPoint;
    private Vector3 HarvesterSpawnPoint;
    public Vector3 UnitMeetPoint;

    // ==================================================
    float NextBaseSurveillanceTime = 0.0f;
    float BaseSurveillancePeriod = 10.0f;
    // ================================================

    private EnemyAIManager TheEnemyAIManager;
    // ============================================================================================
    private void Awake()
    {
        MountPoints = new GameObject[12];
        
    } // Awake

    // ============================================================================================
    // Start is called before the first frame update
    void Start()
    {
        //InstantiateBase(); 
        CurrentBudget = TheBaseConfiguration.InitialBudget;
        CurrentAttacks = UnderAttackModes.None;
        TheAttackingUnit = null;
        NextBaseSurveillanceTime = Time.time + BaseSurveillancePeriod;

        // Get Hold of the TheEnemyAIManager
        TheEnemyAIManager = FindFirstObjectByType<EnemyAIManager>(); 
    }
    // ============================================================================================


    // Update is called once per frame
    void Update()
    {
        // No UI

    }
    // ====================================================================================================
    private void FixedUpdate()
    {
        // Perform Periodic Base Surveillance
        if ((Time.time > NextBaseSurveillanceTime) && (BaseFaction == GameManager.Faction.Enemy))
        {
            PerformBaseSurveillance();
            NextBaseSurveillanceTime = Time.time + BaseSurveillancePeriod;
        }
    } // FixedUpdate
    // ===========================================================================================================
    public void InstantiateBase(GameManager.Faction BaseFaction)
    {

        // Instnatiate the Three Core Buildings HQ, Factory and Refinary at Player and Enemy Locations
        if (BaseFaction == GameManager.Faction.Player)
        {
            Vector3 HQLocation = new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z-20.0f);
            HQGameObject = Instantiate(HQPrefab, HQLocation, Quaternion.Euler(0.0f, 0.0f, 0.0f));
           
            Vector3 FactoryLocation = new Vector3(this.transform.position.x-30.0f, this.transform.position.y, this.transform.position.z + 20.0f);
            FactoryGameObject = Instantiate(FactoryPrefab, FactoryLocation, Quaternion.Euler(0.0f, 90.0f, 0.0f));
           
            Vector3 RefinaryLocation = new Vector3(this.transform.position.x + 30.0f, this.transform.position.y, this.transform.position.z + 20.0f);
            RefinaryGameObject = Instantiate(RefinaryPrefab, RefinaryLocation, Quaternion.Euler(0.0f, -90.0f, 0.0f));
            
            // Create the Mount Points for player base
            Vector3 MountPointPosition = new Vector3(this.transform.position.x - 47.5f, this.transform.position.y+0.5f, this.transform.position.z + 45.0f);
            MountPoints[0] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 00.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x, this.transform.position.y + 0.5f, this.transform.position.z + 0.0f);
            MountPoints[1] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 0.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x, this.transform.position.y + 0.5f, this.transform.position.z + 45.0f);
            MountPoints[2] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 0.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x - 35.5f, this.transform.position.y + 0.5f, this.transform.position.z + 0.0f);
            MountPoints[3] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 00.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x + 47.5f, this.transform.position.y + 0.5f, this.transform.position.z + 45.0f);
            MountPoints[4] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 00.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x - 25.0f, this.transform.position.y + 0.5f, this.transform.position.z -20.0f);
            MountPoints[5] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 00.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x + 25.0f, this.transform.position.y + 0.5f, this.transform.position.z - 20.0f);
            MountPoints[6] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 00.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x - 47.5f, this.transform.position.y + 0.5f, this.transform.position.z + 15.0f);
            MountPoints[7] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 00.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x, this.transform.position.y + 0.5f, this.transform.position.z + 20.0f);
            MountPoints[8] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 0.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x - 20.0f, this.transform.position.y + 0.5f, this.transform.position.z + 45.0f);
            MountPoints[9] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 00.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x + 20.0f, this.transform.position.y + 0.5f, this.transform.position.z + 45.0f);
            MountPoints[10] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 00.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x, this.transform.position.y + 0.5f, this.transform.position.z - 40.0f);
            MountPoints[11] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 0.0f, 0.0f));

            UnitSpawnPoint = new Vector3(this.transform.position.x - 30.0f, this.transform.position.y, this.transform.position.z + 25.0f);
            UnitMeetPoint = new Vector3(this.transform.position.x - 40.0f, this.transform.position.y, this.transform.position.z + 70.0f);
            HarvesterSpawnPoint = new Vector3(this.transform.position.x + 40.0f, this.transform.position.y, this.transform.position.z + 25.0f);
        } // Player Base

        if (BaseFaction == GameManager.Faction.Enemy)
        {
            Vector3 HQLocation = new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z + 20.0f);
            HQGameObject = Instantiate(HQPrefab, HQLocation, Quaternion.Euler(0.0f, 180.0f, 0.0f));
            
            Vector3 FactoryLocation = new Vector3(this.transform.position.x + 30.0f, this.transform.position.y, this.transform.position.z - 20.0f);
            FactoryGameObject = Instantiate(FactoryPrefab, FactoryLocation, Quaternion.Euler(0.0f, -90.0f, 0.0f));
            
            Vector3 RefinaryLocation = new Vector3(this.transform.position.x- 30.0f, this.transform.position.y, this.transform.position.z - 20.0f);
            RefinaryGameObject = Instantiate(RefinaryPrefab, RefinaryLocation, Quaternion.Euler(0.0f, 90.0f, 0.0f));    

            // Create the Mount Points for Enemy base
            Vector3 MountPointPosition = new Vector3(this.transform.position.x + 47.5f, this.transform.position.y + 0.5f, this.transform.position.z - 45.0f);
            MountPoints[0] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 00.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x, this.transform.position.y + 0.5f, this.transform.position.z + 0.0f);
            MountPoints[1] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 0.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x, this.transform.position.y + 0.5f, this.transform.position.z - 45.0f);
            MountPoints[2] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 0.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x + 35.5f, this.transform.position.y + 0.5f, this.transform.position.z - 0.0f);
            MountPoints[3] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 00.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x - 47.5f, this.transform.position.y + 0.5f, this.transform.position.z - 45.0f);
            MountPoints[4] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 00.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x + 25.0f, this.transform.position.y + 0.5f, this.transform.position.z + 20.0f);
            MountPoints[5] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 00.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x - 25.0f, this.transform.position.y + 0.5f, this.transform.position.z + 20.0f);
            MountPoints[6] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 00.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x + 47.5f, this.transform.position.y + 0.5f, this.transform.position.z - 15.0f);
            MountPoints[7] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 00.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x, this.transform.position.y + 0.5f, this.transform.position.z - 20.0f);
            MountPoints[8] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 0.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x + 20.0f, this.transform.position.y + 0.5f, this.transform.position.z - 45.0f);
            MountPoints[9] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 00.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x - 20.0f, this.transform.position.y + 0.5f, this.transform.position.z - 45.0f);
            MountPoints[10] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 00.0f, 0.0f));

            MountPointPosition = new Vector3(this.transform.position.x, this.transform.position.y + 0.5f, this.transform.position.z + 40.0f);
            MountPoints[11] = Instantiate(MountPointPrefab, MountPointPosition, Quaternion.Euler(-90.0f, 0.0f, 0.0f));

            UnitSpawnPoint = new Vector3(this.transform.position.x + 30.0f, this.transform.position.y, this.transform.position.z - 25.0f);
            UnitMeetPoint = new Vector3(this.transform.position.x + 40.0f, this.transform.position.y, this.transform.position.z - 70.0f);
            HarvesterSpawnPoint = new Vector3(this.transform.position.x - 40.0f, this.transform.position.y, this.transform.position.z - 25.0f);

        }  // Enemy Base

        HQRTSComponent = HQGameObject.GetComponent<RTSComponent>();
        HQRTSComponent.SetAffinity(BaseFaction);

        FactoryRTSComponent = FactoryGameObject.GetComponent<RTSComponent>();
        FactoryRTSComponent.SetAffinity(BaseFaction);

        RefinaryRTSComponent = RefinaryGameObject.GetComponent<RTSComponent>();
        RefinaryRTSComponent.SetAffinity(BaseFaction);

        // Spawn an Initial Harvester
        SpawnHarvester(true);

        // Ensure each mount Has an Index and Affinity to Avoid Selections
        for (int i = 0; i < MountPoints.Length; i++)
        {
            MountPoint TheMountPoint = MountPoints[i].GetComponent<MountPoint>();
            TheMountPoint.MountIndex = i;
            TheMountPoint.SetAffinity(BaseFaction);
        } // each Mount Point

    } // InstantiateBase
    // ============================================================================================
    public void RebuildFactory()
    {
        // Check If Already Exists
        if(FactoryGameObject != null )
        {
            Debug.Log("[PLAYER_ERROR]: Factory Already Exists");
            return;
        }
        if (BaseFaction == GameManager.Faction.Player)
        {
            Vector3 FactoryLocation = new Vector3(this.transform.position.x - 30.0f, this.transform.position.y, this.transform.position.z + 20.0f);
            FactoryGameObject = Instantiate(FactoryPrefab, FactoryLocation, Quaternion.Euler(0.0f, 90.0f, 0.0f));
            FactoryGameObject.GetComponent<RTSComponent>().SetAffinity(BaseFaction);
            CurrentBudget = CurrentBudget - TheBaseConfiguration.CostOfFactory;
        }
        else
        {
            Vector3 FactoryLocation = new Vector3(this.transform.position.x + 30.0f, this.transform.position.y, this.transform.position.z - 20.0f);
            FactoryGameObject = Instantiate(FactoryPrefab, FactoryLocation, Quaternion.Euler(0.0f, -90.0f, 0.0f));
            FactoryGameObject.GetComponent<RTSComponent>().SetAffinity(BaseFaction);
            CurrentBudget = CurrentBudget - TheBaseConfiguration.CostOfFactory;

        } // Enemy Factory
        return;
    } // RebuildFactory
    // ============================================================================================
    public void RebuildRefinary()
    {
        // Check If Already Exists
        if (RefinaryGameObject != null)
        {
            Debug.Log("[PLAYER_ERROR]: Refinary Already Exists");
            return;
        }
        
        if (BaseFaction == GameManager.Faction.Player)
        {
            Vector3 RefinaryLocation = new Vector3(this.transform.position.x + 30.0f, this.transform.position.y, this.transform.position.z + 20.0f);
            RefinaryGameObject = Instantiate(RefinaryPrefab, RefinaryLocation, Quaternion.Euler(0.0f, -90.0f, 0.0f));
            RefinaryGameObject.GetComponent<RTSComponent>().SetAffinity(BaseFaction);
            CurrentBudget = CurrentBudget - TheBaseConfiguration.CostOfRefinary;
        }
        else
        {
            Vector3 RefinaryLocation = new Vector3(this.transform.position.x - 30.0f, this.transform.position.y, this.transform.position.z - 20.0f);
            RefinaryGameObject = Instantiate(RefinaryPrefab, RefinaryLocation, Quaternion.Euler(0.0f, 90.0f, 0.0f));
            RefinaryGameObject.GetComponent<RTSComponent>().SetAffinity(BaseFaction);
            CurrentBudget = CurrentBudget - TheBaseConfiguration.CostOfRefinary;
        }  // Enemy Refinary

        // Always Spawn a Harvester as Well
        SpawnHarvester(true);

        return;
    } // RebuildRefinary
    // ============================================================================================
    public void SpawnHumvee()
    {
        GameObject HumveeGO = null; 
        if (BaseFaction == GameManager.Faction.Player)
        {
            HumveeGO = Instantiate(HumveePrefab, UnitSpawnPoint, Quaternion.Euler(0.0f, 0.0f, 0.0f));
        }
        else
        {
            HumveeGO = Instantiate(HumveePrefab, UnitSpawnPoint, Quaternion.Euler(0.0f, 180.0f, 0.0f));
        }
        // Now Set the Unit Affinity 
        if(HumveeGO != null) 
        {
            HumveeGO.GetComponent<RTSComponent>().SetAffinity(BaseFaction);
            CurrentBudget = CurrentBudget - TheBaseConfiguration.CostOfHumvee;

            // And set the Unit Off towards Meet Point
            Vector3 randomPoint = UnitMeetPoint + Random.insideUnitSphere * 10.0f;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 12.5f, NavMesh.AllAreas))
            {
                HumveeGO.GetComponent<RTSComponent>().SetNewDestination(hit.position);
            }
            else HumveeGO.GetComponent<RTSComponent>().SetNewDestination(UnitMeetPoint);

        }
    } // SpawnHumvee
    // ============================================================================================
    public void SpawnTank()
    {   
        GameObject TankGO = null;
        
        if (BaseFaction == GameManager.Faction.Player)
        {
            TankGO = Instantiate(TankPrefab, UnitSpawnPoint, Quaternion.Euler(0.0f, 0.0f, 0.0f));
        }
        else
        {
            TankGO = Instantiate(TankPrefab, UnitSpawnPoint, Quaternion.Euler(0.0f, 180.0f, 0.0f));  
        }
        // Now Set the Unit Affinity 
        if (TankGO != null)
        {
            TankGO.GetComponent<RTSComponent>().SetAffinity(BaseFaction);
            CurrentBudget = CurrentBudget - TheBaseConfiguration.CostOfTank;

            // And set the Unit Off towards Meet Point
            Vector3 randomPoint = UnitMeetPoint + Random.insideUnitSphere * 10.0f;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 12.5f, NavMesh.AllAreas))
            {
                TankGO.GetComponent<RTSComponent>().SetNewDestination(hit.position);
            }
            else TankGO.GetComponent<RTSComponent>().SetNewDestination(UnitMeetPoint);

        }
    } // SpawnTank
    // ============================================================================================
    public void SpawnHarvester(bool InitialHarvester)
    {
        GameObject HarvesterGO = null;
        if (BaseFaction == GameManager.Faction.Player)
        {
            HarvesterGO = Instantiate(HarvesterPrefab, HarvesterSpawnPoint, Quaternion.Euler(0.0f, 0.0f, 0.0f));
        }
        else
        {
            HarvesterGO = Instantiate(HarvesterPrefab, HarvesterSpawnPoint, Quaternion.Euler(0.0f, 180.0f, 0.0f));
            //Debug.Log("[INFO]: Enemy Harvester Created");
        }
        // Now Set the Unit Affinity 
        if (HarvesterGO != null)
        {
            HarvesterGO.GetComponent<RTSComponent>().SetAffinity(BaseFaction);
            if(!InitialHarvester) CurrentBudget = CurrentBudget - TheBaseConfiguration.CostOfHavester;

            //Find and set the Harvester Off to Preferred Ore Field loaction if exists, or the Unit Meet point otherwise
            GameObject NearestOreField = HarvesterGO.GetComponent<RTSComponent>().FindNearestViableOreField();
            if(NearestOreField != null) HarvesterGO.GetComponent<RTSComponent>().SetNewDestination(NearestOreField.transform.position);
            else HarvesterGO.GetComponent<RTSComponent>().SetNewDestination(UnitMeetPoint);
        }

        return;
    } // SpawnHarvetser
    // ============================================================================================
    public void SpawnGunSystem(GameObject TheMountPoint)
    {
        GameObject GunSystemGO = null;

        if (BaseFaction == GameManager.Faction.Player)
        {
            GunSystemGO = Instantiate(GunSystemPrefab, TheMountPoint.transform.position, Quaternion.Euler(0.0f, 0.0f, 0.0f));
        }
        else
        {
            GunSystemGO = Instantiate(GunSystemPrefab, TheMountPoint.transform.position, Quaternion.Euler(0.0f, 180.0f, 0.0f));
        }
        // Now Set the Unit Affinity 
        if (GunSystemGO != null)
        {
            RTSComponent ThisGunRTS = GunSystemGO.GetComponent<RTSComponent>();
            MountPoint TheSupportingMount = TheMountPoint.GetComponent<MountPoint>(); 

            ThisGunRTS.SetAffinity(BaseFaction);
            CurrentBudget = CurrentBudget - TheBaseConfiguration.CostOfGun;
            TheSupportingMount.SetMounted();
            ThisGunRTS.SetMountIndex(TheSupportingMount.MountIndex);
        }
    } // SpawnGunSystem
    // =============================================================================================
    public void SpawnLaserSystem(GameObject TheMountPoint)
    {
        GameObject LaserSystemGO = null;

        if (BaseFaction == GameManager.Faction.Player)
        {
            LaserSystemGO = Instantiate(LaserSystemPrefab, TheMountPoint.transform.position, Quaternion.Euler(0.0f, 0.0f, 0.0f));
        }
        else
        {
            LaserSystemGO = Instantiate(LaserSystemPrefab, TheMountPoint.transform.position, Quaternion.Euler(0.0f, 180.0f, 0.0f));
        }
        // Now Set the Unit Affinity 
        if (LaserSystemGO != null)
        {
          
            RTSComponent ThisLaserRTS = LaserSystemGO.GetComponent<RTSComponent>();
            MountPoint TheSupportingMount = TheMountPoint.GetComponent<MountPoint>();

            ThisLaserRTS.SetAffinity(BaseFaction);
            CurrentBudget = CurrentBudget - TheBaseConfiguration.CostOfLaser;
            TheSupportingMount.SetMounted();
            ThisLaserRTS.SetMountIndex(TheSupportingMount.MountIndex);
        }
    } // SpawnLaserSystem
    // =============================================================================================
    public void ResetAttackAssessments()
    {
        CurrentAttacks = UnderAttackModes.None;
        TheAttackingUnit = null;
    } // ResetAttackAssessments

    // ============================================================================================
    public void HarvesterUnload()
    {
        if(BaseFaction == GameManager.Faction.Player) CurrentBudget = CurrentBudget + TheBaseConfiguration.HarvesterLoad;
        else
        {
            // Enemy AI Harvester Off Load
            float EnemyHarvesterOffload = (float)TheBaseConfiguration.HarvesterLoad*1.0f;

            // Greater Offloads For AI if Playing Medium or Hard
            if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Easy) EnemyHarvesterOffload = EnemyHarvesterOffload * 1.0f; 
            if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Medium) EnemyHarvesterOffload = EnemyHarvesterOffload *1.1f;
            if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Hard) EnemyHarvesterOffload = EnemyHarvesterOffload *1.2f;

            if ((TheEnemyAIManager.NumberofPlayerTanks+ TheEnemyAIManager.NumberofPlayerHumvees) > ((TheEnemyAIManager.NumberofAITanks+ TheEnemyAIManager.NumberofAIHumvees)* 2.25f)) EnemyHarvesterOffload = EnemyHarvesterOffload * 1.1f;
            if ((TheEnemyAIManager.NumberofPlayerTanks + TheEnemyAIManager.NumberofPlayerHumvees) > ((TheEnemyAIManager.NumberofAITanks + TheEnemyAIManager.NumberofAIHumvees) * 3.5f))
            {
                if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Easy) EnemyHarvesterOffload = EnemyHarvesterOffload * 1.1f;
                if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Medium) EnemyHarvesterOffload = EnemyHarvesterOffload * 1.2f;
                if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Hard) EnemyHarvesterOffload = EnemyHarvesterOffload * 1.3f;
            }
                
            CurrentBudget = CurrentBudget + (int)EnemyHarvesterOffload; 
        } // Enemy AI Harvester Offload

    } // HarvesterUnload
    // ============================================================================================
    public void HarvesterHalfUnload()
    {
        // Called when Harvester Has Been Scared Return 
        if (BaseFaction == GameManager.Faction.Enemy)
        {
            if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Easy) CurrentBudget = CurrentBudget + (int)(TheBaseConfiguration.HarvesterLoad*0.4f);
            if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Medium) CurrentBudget = CurrentBudget + (int)(TheBaseConfiguration.HarvesterLoad * 0.6f);
            if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Hard) CurrentBudget = CurrentBudget + (int)(TheBaseConfiguration.HarvesterLoad * 1.0f);
        }
    } // HarvesterHalfUnload
    // =============================================================================================
    public void ExcessEntrapmentOffLoad()
    {
        // Called when Harvester Excess Entrapped 
        if (BaseFaction == GameManager.Faction.Enemy)
        {
            if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Easy) CurrentBudget = CurrentBudget + (int)(TheBaseConfiguration.HarvesterLoad * 3.0f);
            if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Medium) CurrentBudget = CurrentBudget + (int)(TheBaseConfiguration.HarvesterLoad * 5.0f);
            if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Hard) CurrentBudget = CurrentBudget + (int)(TheBaseConfiguration.HarvesterLoad * 8.0f);
        }

    } // ExcessEntrapmentOffLoad
      // =============================================================================================
    void PerformBaseSurveillance()
    {
        // Only Apply Surevaillanced on Enemy AI base
        if (BaseFaction == GameManager.Faction.Player) return;

        //  A Base Centric View of Any Units in the Vicinity
        foreach (GameObject PossibleAttacker in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            // Now Check if Hostile Player)  And either Tank pr Humvee 
            RTSComponent PossibleRTSTarget = PossibleAttacker.GetComponent<RTSComponent>();
            if ((PossibleRTSTarget.TheFaction == GameManager.Faction.Player) && ((PossibleRTSTarget.RTSType == GameManager.RTSType.Humvee) || (PossibleRTSTarget.RTSType == GameManager.RTSType.Tank)))
            {
                float RangeFromAIBase = Vector3.Distance(PossibleAttacker.transform.position, this.transform.position);

                // Check within the Base Surveillance Range of 200m
                if (RangeFromAIBase < 200.0f)
                {
                    CurrentAttacks = UnderAttackModes.BaseBuildingUnderAttack;
                    TheAttackingUnit = PossibleAttacker;

                } // Range Check
            } // Check Enemy 
        }  // for all RTS Objects in Game
    } // PerformBaseSurveillance
    // ======================================================================================================


    // ======================================================================================
}

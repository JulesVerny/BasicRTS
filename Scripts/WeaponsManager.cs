using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

public class WeaponsManager : MonoBehaviour
{
    // ====================================================================
    public enum WeaponsStates {Off,SurveillanceMode,TrackingTarget,EngagingTarget}
    // ====================================================================
    [SerializeField]
    GameObject WeaponFireEffectComponent;
    [SerializeField]
    GameObject WeaponSmokeEffectComponent;
    [SerializeField]
    GameObject WeaponsTurret;
    [SerializeField]
    public WeaponConfiguration TheWeaponWeaponConfiguration;

    // Weapons Fire Effects
    private ParticleSystem WeaponFireEffect;
    private VisualEffect WeaponsSmokeEffect;

    private RTSComponent ParentRTSComponent;

    // Sound Effects
    [Header("Sound Effects")]
    [SerializeField] private AudioSource TheAudioPlayer;
    [SerializeField] private AudioClip WeaponsFireClip;

    [Header("Main Variables")]
    // Weapons Management
    private float NextFireTime;
    private float NextSearchTime;
    public WeaponsStates TheWeaponState;
    public GameObject CurrentTargetGO; 
  
    private LineRenderer TheLaserLineRenderer;
    private float LaserOffTime;
    // =====================================================================
    private void Awake()
    {
        // Grab the Particle and Visual Effects Objects
        if(WeaponFireEffectComponent!=null) WeaponFireEffect = WeaponFireEffectComponent.GetComponent<ParticleSystem>();
        if(WeaponSmokeEffectComponent!=null) WeaponsSmokeEffect = WeaponSmokeEffectComponent.GetComponent<VisualEffect>();
        ParentRTSComponent = transform.GetComponent<RTSComponent>();

        if (ParentRTSComponent.RTSType == GameManager.RTSType.Laser)
        {
            TheLaserLineRenderer = GetComponentInChildren<LineRenderer>();
            LaserOffTime = Time.time;

            TheLaserLineRenderer.SetPosition(0, WeaponsTurret.transform.position);
        }

        TheWeaponState = WeaponsStates.Off;
       
    } // Awake
    // ======================================================================
    void Start()
    {
        if(WeaponFireEffect!=null) WeaponFireEffect.Stop();
        if(WeaponsSmokeEffect!=null) WeaponsSmokeEffect.Stop();

        NextFireTime = Time.time + TheWeaponWeaponConfiguration.WeaponsFirePeriod;
        NextSearchTime = Time.time + 1.0f; // Assume that te Search is done every 1 second Only

        TheWeaponState = WeaponsStates.SurveillanceMode;

    } // Start
    // ==========================================================================
    // Update is called once per frame fort UI methods (e.g. Debug) 
    void Update()
    {
        // No UI Debug Here


    } // Update
    // ============================================================================================
    private void FixedUpdate()
    {
        // If  Base Defence Type in Surveillance, Search for more Local Targets
        if((ParentRTSComponent.RTSType == GameManager.RTSType.Laser) || (ParentRTSComponent.RTSType == GameManager.RTSType.Gun))
        {
            if ((TheWeaponState == WeaponsStates.SurveillanceMode) && (Time.time > NextSearchTime))
            {
                CurrentTargetGO = FindAnyTargets();
            }
        }  // Base Defence Surveillance Review
        // =====================================================
        if (CurrentTargetGO == null)
        {
            // No Target so return to Surveillance Mode
            TheWeaponState = WeaponsStates.SurveillanceMode;
            RealignTurretForward();
        }
        else
        {
            // We have a Current Target So Now Check if Tracking or Engaging
            float RangeFromThisWeapon = Vector3.Distance(CurrentTargetGO.transform.position, this.transform.position);
            if(RangeFromThisWeapon <= TheWeaponWeaponConfiguration.WeaponsRange)
            {
                // So ensure Turret is still pointing Towards Target. 
                if (RotateTurretToTarget())
                {
                    TheWeaponState = WeaponsStates.EngagingTarget;
                }
                else TheWeaponState = WeaponsStates.TrackingTarget;
            } // Within Engagement Range
            if ((RangeFromThisWeapon > TheWeaponWeaponConfiguration.WeaponsRange) && (RangeFromThisWeapon <= TheWeaponWeaponConfiguration.WeaponsRange*1.333))
            {
                TheWeaponState = WeaponsStates.TrackingTarget;
                RotateTurretToTarget();
            }
            if (RangeFromThisWeapon > TheWeaponWeaponConfiguration.WeaponsRange * 1.333)
            {
                // Out Of Weapons Surveillance Range
                TheWeaponState = WeaponsStates.SurveillanceMode;
                RealignTurretForward();
            }
        } // We have a Current Target Object
        // ==================================

        // Check Switch off Laser
        if ((TheLaserLineRenderer != null) && (ParentRTSComponent.RTSType == GameManager.RTSType.Laser))
        {
            if(Time.time> LaserOffTime) TheLaserLineRenderer.SetPosition(1, WeaponsTurret.transform.position);
        }
        
        // Now Finally Set a Fire If Currently Engaging a Target 
        if ((CurrentTargetGO != null) && (TheWeaponState == WeaponsStates.EngagingTarget))
        {
            FireWeapon();
        }
        // ===============================

    }  // FixedUpdate
    // ========================================================================
    public void AssignWeaponToTarget(GameObject DesignatedTargetGO)
    {
        TheWeaponState = WeaponsStates.SurveillanceMode;
        RealignTurretForward();
        if (DesignatedTargetGO!=null)
        {
            CurrentTargetGO = DesignatedTargetGO;
        } // Still Valid Target

    } // AssignWeaponToNominatedTarget
    // ==============================================================================
    public void ReturnWeaponsToSurveillanceMode()
    {
        CurrentTargetGO = null;
        TheWeaponState = WeaponsStates.SurveillanceMode;
        RealignTurretForward();
    } // ReturnWeaponsToSurveillanceMode

    // ==============================================================================
    private GameObject FindAnyTargets() 
    {
        GameObject NextTarget = null;
        // Search through ALL RTS Game Objects
        foreach (GameObject GameObjectItem in GameObject.FindGameObjectsWithTag("RTSGameObject"))
        {
            // Now Check if Hostile and Not a Mount
            RTSComponent GORTSComponent = GameObjectItem.GetComponent<RTSComponent>();
            if(GORTSComponent.TheFaction!= ParentRTSComponent.TheFaction)
            {
                float RangeFromThisWeapon = Vector3.Distance(GameObjectItem.transform.position, this.transform.position);

                // Check within the Surveillance Region, which is 1.333 X Engagement Range
                if(RangeFromThisWeapon<1.5* TheWeaponWeaponConfiguration.WeaponsRange)
                {
                    NextTarget = GameObjectItem;
                    return NextTarget;
                } // Range Check
            } // Check Enemy 
        }  // for all RTS Objects in Game

        return NextTarget;
    } // FindAnyTargets
    // ============================================================================
    bool RotateTurretToTarget()
    {
        bool TurretFacingTarget = false; 

        // Rotate the Turret Towards the Target
        Vector3 DirectionToTarget = (CurrentTargetGO.transform.position - this.transform.position).normalized;
        DirectionToTarget.y = 0.0f;
        Quaternion RequiredRotation = Quaternion.LookRotation(DirectionToTarget);
        WeaponsTurret.transform.rotation = Quaternion.Lerp(WeaponsTurret.transform.rotation, RequiredRotation, TheWeaponWeaponConfiguration.TurretTurnSpeed * Time.deltaTime);

        // Now Check if the WeaponsTurret is facing Z, towards the direction of the Target
        if (Vector3.Dot(WeaponsTurret.transform.forward, DirectionToTarget) > 0.4f)
        {
            TurretFacingTarget = true;
        }
        return TurretFacingTarget;

    } // RotateTurretToTarget
    // ============================================================================
    void RealignTurretForward()
    {
        // Rotate the Turret Towards this.forward Target
        Quaternion RequiredRotation = Quaternion.LookRotation(this.transform.forward);
        WeaponsTurret.transform.rotation = Quaternion.Lerp(WeaponsTurret.transform.rotation, RequiredRotation, TheWeaponWeaponConfiguration.TurretTurnSpeed * Time.deltaTime);

    } // RealignTurretForward
    // ================================================================================
    

    // =======================================================================
    public void FireWeapon()
    {
        if(Time.time > NextFireTime) 
        {
            // Then Both Fire and Smoke Effects
            if (WeaponFireEffect != null) WeaponFireEffect.Play();
            if (WeaponsSmokeEffect != null) WeaponsSmokeEffect.Play();
            TheAudioPlayer.PlayOneShot(WeaponsFireClip); 

            // Set Up laser On
            if ((TheLaserLineRenderer !=null) && (ParentRTSComponent.RTSType == GameManager.RTSType.Laser))
            {
                TheLaserLineRenderer.SetPosition(1, CurrentTargetGO.transform.position);
                LaserOffTime = Time.time + 1.0f; ;
            } // Laser type

            // ==========================
            // Need to Modify the Weaspons Damage as a Function of Game Difficulty
            int WeaponsFireDamage = TheWeaponWeaponConfiguration.WeaponsDamage;

            if(ParentRTSComponent.TheFaction == GameManager.Faction.Enemy)
            {
                if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Medium) WeaponsFireDamage = (int)(WeaponsFireDamage * 1.1f);
                if (GameManager.TheGameDifficulty == GameManager.GameDifficultyType.Hard) WeaponsFireDamage =  (int)(WeaponsFireDamage * 1.25f);
            }

            // ===================================
            // Get the Traget RTS Component To Take the Hit Damage to the Traget Game Object RTS Component
            RTSComponent TargetRTSComponent = CurrentTargetGO.GetComponent< RTSComponent>();
            if (TargetRTSComponent != null)
            {
                if ((ParentRTSComponent.RTSType == GameManager.RTSType.Tank) || (ParentRTSComponent.RTSType == GameManager.RTSType.Humvee))
                {
                    // The Defender will be interested in Defending Against the Attacker
                    TargetRTSComponent.TakeHit(WeaponsFireDamage, this.transform.gameObject);
                }
                else
                {
                    // This is a Base defence Weapons, so the Attacked Unit is not interested in How/ Who being Attacked
                    TargetRTSComponent.TakeHit(WeaponsFireDamage, null);
                }
            } // Target RTS Compoent - To Take Hit

            NextFireTime = Time.time + TheWeaponWeaponConfiguration.WeaponsFirePeriod;
        }  // Fire Time Elapsed

    } // EngageWeapon
    // =============================================================================================


    // ==============================================================================================

}

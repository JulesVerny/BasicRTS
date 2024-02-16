using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogWarManager2 : MonoBehaviour
{
    [SerializeField] GameObject FogCloudSegmentPrefab;

    [SerializeField] GameObject FOGProjectionPlane;
    [SerializeField] CustomRenderTexture TheFOGMaskRenderTexture;

    GameObject[,] FullFogCloud;

    float NextFOGTimeout;
    float FOGupdatePeriod = 1.0f;
    // =================================================================================================
    public static Vector2Int LocationToGridCoordinates(Vector3 UnitLocation)
    {
        Vector2Int RtnCoordinates = new Vector2Int(-1, -1);
        // Return the Grid Coordinates of a Unit Location
        // [-550:550, -550:550]  => [0:22,0:22]

        int XIndex = Mathf.FloorToInt((UnitLocation.x + 550.0f) / 50.0f);
        int ZIndex = Mathf.FloorToInt((UnitLocation.z + 550.0f) / 50.0f);
        RtnCoordinates = new Vector2Int(XIndex, ZIndex);

        return RtnCoordinates;
    } // LocationToGridCoordinates
    // =================================================================================================
    void Start()
    {
        FullFogCloud = new GameObject[22, 22];
        NextFOGTimeout = Time.time; 

        // Instantiate Fill the FOG Render Surfacewith Fog Elements
        for (int i = 0; i < 22; i++)
        {
            for (int j = 0; j < 22; j++)
            {
                // Fog Segement 3D coordinate
                float xpos = this.transform.position.x -55.0f + i * 5.0f;
                float zpos = this.transform.position.z -55.0f  + j * 5.0f;
                Vector3 FlogCloudPosition = new Vector3(xpos, 2.0f, zpos);
                GameObject FogSegment = Instantiate(FogCloudSegmentPrefab, FlogCloudPosition, Quaternion.Euler(0.0f, 0.0f, 0.0f));
                FullFogCloud[i, j] = FogSegment;
            }
        } // ============================================


    } // Start
    // =================================================================================================
    // Update is called once per frame
    void Update()
    {
        // Ensure that the FOG Shader has the FOG Mask Render Texture assigned
        if (Time.time > NextFOGTimeout)
        {
            var material = FOGProjectionPlane.GetComponent<Renderer>().material;
            material.SetTexture("_FOGMask", TheFOGMaskRenderTexture);
            NextFOGTimeout = Time.time + FOGupdatePeriod; 
        }
    }
    // =================================================================================================
    public bool QueryFogEnabledAt(Vector3 CheckPosition)
    {

        Vector2Int TheFogSegmentIndex = LocationToGridCoordinates(CheckPosition);
        FogWarSegment TheFogWarSegement = FullFogCloud[TheFogSegmentIndex.x, TheFogSegmentIndex.y].GetComponent<FogWarSegment>();

        return TheFogWarSegement.FogEnabled;
    } // FogEnabledAt
    // =================================================================================================
    public void FriendlyCurrentlyAt(Vector3 CheckPosition)
    {
        Vector2Int TheFogSegmentIndex = LocationToGridCoordinates(CheckPosition);
        FullFogCloud[TheFogSegmentIndex.x, TheFogSegmentIndex.y].GetComponent<FogWarSegment>().SwitchFogSegmentOff(); ;

        // To the Right and Left
        if (TheFogSegmentIndex.y > 0) FullFogCloud[TheFogSegmentIndex.x, TheFogSegmentIndex.y - 1].GetComponent<FogWarSegment>().SwitchFogSegmentOff();
        if (TheFogSegmentIndex.y < 21) FullFogCloud[TheFogSegmentIndex.x, TheFogSegmentIndex.y + 1].GetComponent<FogWarSegment>().SwitchFogSegmentOff();
        
        // The Layer "Above"
        if (TheFogSegmentIndex.x > 0)
        {
            FullFogCloud[TheFogSegmentIndex.x - 1, TheFogSegmentIndex.y].GetComponent<FogWarSegment>().SwitchFogSegmentOff();
            if (TheFogSegmentIndex.y > 0) FullFogCloud[TheFogSegmentIndex.x - 1, TheFogSegmentIndex.y - 1].GetComponent<FogWarSegment>().SwitchFogSegmentOff();
            if (TheFogSegmentIndex.y < 21) FullFogCloud[TheFogSegmentIndex.x - 1, TheFogSegmentIndex.y + 1].GetComponent<FogWarSegment>().SwitchFogSegmentOff();
        }

        // The Layer "Below"  - Where the Camera View Intercepts Unit
        if (TheFogSegmentIndex.x < 21)
        {
            FullFogCloud[TheFogSegmentIndex.x + 1, TheFogSegmentIndex.y].GetComponent<FogWarSegment>().SwitchFogSegmentOff();
            if (TheFogSegmentIndex.y > 0) FullFogCloud[TheFogSegmentIndex.x + 1, TheFogSegmentIndex.y - 1].GetComponent<FogWarSegment>().SwitchFogSegmentOff();
            if (TheFogSegmentIndex.y < 21) FullFogCloud[TheFogSegmentIndex.x + 1, TheFogSegmentIndex.y + 1].GetComponent<FogWarSegment>().SwitchFogSegmentOff();
            // Inc the Right edge
            if (TheFogSegmentIndex.y < 20) FullFogCloud[TheFogSegmentIndex.x+1, TheFogSegmentIndex.y + 2].GetComponent<FogWarSegment>().SwitchFogSegmentOff();

        }
        // The Second Layer "Below"
        if (TheFogSegmentIndex.x < 20)
        {
            FullFogCloud[TheFogSegmentIndex.x + 2, TheFogSegmentIndex.y].GetComponent<FogWarSegment>().SwitchFogSegmentOff();
            if (TheFogSegmentIndex.y > 0) FullFogCloud[TheFogSegmentIndex.x + 2, TheFogSegmentIndex.y - 1].GetComponent<FogWarSegment>().SwitchFogSegmentOff();
            if (TheFogSegmentIndex.y < 21) FullFogCloud[TheFogSegmentIndex.x + 2, TheFogSegmentIndex.y + 1].GetComponent<FogWarSegment>().SwitchFogSegmentOff();
        }
  
    } // FriendlyCurrentlyAt
    // =================================================================================================
    public void DebugClearAllFog(bool DebugState)
    {
        for (int i = 0; i < 22; i++)
        {
            for (int j = 0; j < 22; j++)
            {
                if (DebugState) FullFogCloud[i, j].GetComponent<FogWarSegment>().DebugClearFogSegment();
                else FullFogCloud[i, j].GetComponent<FogWarSegment>().SwitchFogSegmentOn();
            }
        } // ============================================
    } // DebugClearAllFog
}

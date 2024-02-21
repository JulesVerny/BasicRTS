using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogWarSegment : MonoBehaviour
{
    
    [SerializeField] 
    GameObject TheFogCloud;

    float NextSwitchOffTime;
    public bool FogEnabled; 
    // ===============================================================================
    private void Awake()
    {
        

    } // Awake
    // ===============================================================================
    void Start()
    {
        TheFogCloud.SetActive(true);
        FogEnabled = true;

        // Put Some Randomize onto the cloud segment

        var material = this.transform.GetComponentInChildren<Renderer>().material;

        //Call SetColor using the shader property name "_Color" and "_Color2" and setting their color to red
        Vector4 RandomOffset = new Vector4(Random.Range(0.0f, 25.0f), Random.Range(0.0f, 25.0f),0.0f,0.0f); 

        material.SetVector("_RandomOffset", RandomOffset);

    }
    // ===============================================================================
    // Update is called once per frame
    void Update()
    {
        
    }
    // ===============================================================================
    private void FixedUpdate()
    {
        // Time Out the Fog Cloud
        if(Time.time > NextSwitchOffTime) 
        {
            SwitchFogSegmentOn(); 
        }
    } // FixedUpdate
    // ===============================================================================
    public void SwitchFogSegmentOn()
    {
        TheFogCloud.SetActive(true);
        FogEnabled = true;
    } // SwitchFogSegmentOn
    // ===============================================================================
    public void SwitchFogSegmentOff()
    {
        TheFogCloud.SetActive(false);
        NextSwitchOffTime = Time.time + 30.0f;
        FogEnabled = false;
    }  // SwitchFogSegmentOff
       // =============================================================================
    public void DebugClearFogSegment()
    {
        TheFogCloud.SetActive(false);
        NextSwitchOffTime = Time.time + 3600.0f;  // Just set to a very long time
        FogEnabled = false;
    }  // SwitchFogSegmentOff

    // ===============================================================================
}

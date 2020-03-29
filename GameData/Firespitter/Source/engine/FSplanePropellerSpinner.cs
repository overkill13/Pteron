﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using UnityEngine;

public class FSplanePropellerSpinner : PartModule
{
    [KSPField]
    public string propellerName = "propeller";
    [KSPField]
    public float rotationSpeed = -300f; // in RPM
    [KSPField]
    public int useRotorDiscSwap = 0;
    [KSPField]
    public float rotorDiscSpeed = -30f;
    [KSPField]
    public string rotorDiscName = "rotorDisc";
    [KSPField]
    public float rotorDiscFadeInStart = 0.5f;
    [KSPField]
    public float rotorDiscFadeInEnd = 0.95f; // not currently used
    [KSPField]
    public float windmillMinAirspeed = 30.0f;
    [KSPField]
    public float windmillRPM = 0.1f;
    [KSPField]
    public float spinUpTime = 10f; // divide engineResponseSpeed by this amount for dramatic effect
    [KSPField]
    public float thrustRPM = 0f; // added to rotationSpeed
    [KSPField]
    public string blade1 = "";
    [KSPField]
    public string blade2 = "";
    [KSPField]
    public string blade3 = "";
    [KSPField]
    public string blade4 = "";
    [KSPField]
    public string blade5 = "";
    [KSPField]
    public string blade6 = "";
    [KSPField]
    public bool usesDeployAnimation = false;
    [KSPField]
    public bool deployAnimationStartsDeployed = true;

    private ModuleEngines engine;
    private Transform propeller;
    private Transform rotorDisc;
    private float targetRPM = 0f;
    private float currentRPM = 0f;
    private float maxThrust = 0f;
    private float smoothedThrustRPM = 0f;
    private List<GameObject> bladeObjects = new List<GameObject>();
    private List<String> bladeNames = new List<string>();
    private FSanimateGeneric deployAnimation = new FSanimateGeneric();

    private void setBladeRendererState(bool newState)
    {
        for (int i = 0; i < bladeObjects.Count; i++)
        {
            bladeObjects[i].renderer.enabled = newState;
            //Debug.Log("FSplanePropellerSpinner: blade " + i + " renderer set to " + newState);
        }
    }

    public override void OnStart(PartModule.StartState state)
    {
        base.OnStart(state);
        engine = part.Modules.OfType<ModuleEngines>().FirstOrDefault();
        propeller = part.FindModelTransform(propellerName);        
            
        if (engine != null)
        {
            maxThrust = engine.maxThrust;
        }
        if (maxThrust <= 0f) // to avoid division by zero
        {
            maxThrust = 50f;
        }

        //assign blade meshes so they gan be hidden
        if (useRotorDiscSwap == 1)
        {
            rotorDisc = part.FindModelTransform(rotorDiscName);
            if (rotorDisc != null)
            {
                rotorDisc.gameObject.renderer.enabled = false;

                if (blade1 != "")
                    bladeNames.Add(blade1);
                if (blade2 != "")
                    bladeNames.Add(blade2);
                if (blade3 != "")
                    bladeNames.Add(blade3);
                if (blade4 != "")
                    bladeNames.Add(blade4);
                if (blade5 != "")
                    bladeNames.Add(blade5);
                if (blade6 != "")
                    bladeNames.Add(blade6);

                for (int i = 0; i < bladeNames.Count; i++)
                {
                    try
                    {
                        bladeObjects.Add(part.FindModelTransform(bladeNames[i]).gameObject);
                    }
                    catch
                    {
                        Debug.Log("FSplanePropellerSpinner: Unable to find blade called " + bladeNames[i] + ", disabling swap");
                        useRotorDiscSwap = 0;
                    }
                }
            }
            else
            {
                Debug.Log("FSplanePropellerSpinner: Unable to find rotor disc " + rotorDiscName + ", disabling swap");
                useRotorDiscSwap = 0;
            }
        }

        if (usesDeployAnimation)
        {
            deployAnimation = part.Modules.OfType<FSanimateGeneric>().FirstOrDefault();
        }
    }

    //public override void OnFixedUpdate()    
    public override void OnUpdate()    
    {
        if (!HighLogic.LoadedSceneIsFlight) return; // || !vessel.isActiveVessel

        if (engine != null)
        {

            if (usesDeployAnimation)
            {
                if (deployAnimationStartsDeployed && deployAnimation.animTime > 0)
                    engine.EngineIgnited = false;
                else if (!deployAnimationStartsDeployed && deployAnimation.animTime < 1)
                {
                    engine.EngineIgnited = false;
                }
            }

            //check if the engine is running, or the airplane is moving through the air
            if (!engine.getIgnitionState || engine.getFlameoutState)
            {
                if (FlightGlobals.ship_srfSpeed > windmillMinAirspeed && vessel.atmDensity > 0.1f)
                    targetRPM = windmillRPM + (windmillRPM * FlightInputHandler.state.mainThrottle); //spins depending on the blade angle
                else
                    targetRPM = 0f;
            }
            else
                targetRPM = 1f;            
            
            currentRPM = Mathf.Lerp(currentRPM, targetRPM, engine.engineAccelerationSpeed/spinUpTime * TimeWarp.deltaTime);

            if (currentRPM != 0f)
            {
                float finalRotationSpeed = rotationSpeed;
                if (thrustRPM != 0f)
                {
                    float normalizedThrustRPM = (engine.finalThrust / maxThrust);
                    smoothedThrustRPM = Mathf.Lerp(smoothedThrustRPM, normalizedThrustRPM, 0.1f);
                    finalRotationSpeed += (thrustRPM * normalizedThrustRPM);
                }
                propeller.transform.Rotate(Vector3.forward * ((finalRotationSpeed * 6) * TimeWarp.deltaTime * currentRPM));
            }

            if (useRotorDiscSwap == 1)
            {
                if (rotorDisc != null)
                {

                    if (currentRPM > rotorDiscFadeInStart)
                    {
                        rotorDisc.renderer.enabled = true;
                        setBladeRendererState(false);
                        rotorDisc.Rotate(Vector3.forward * ((rotorDiscSpeed * 6) * TimeWarp.deltaTime));
                    }
                    else
                    {
                        rotorDisc.renderer.enabled = false;
                        setBladeRendererState(true);
                    }

                    //if (currentRPM > rotorDiscFadeInStart)
                    //{
                    //    rotorDisc.renderer.enabled = true;
                    //    if (currentRPM > rotorDiscFadeInEnd)
                    //    {
                    //        setBladeRendererState(false);
                    //        rotorDisc.Rotate(Vector3.forward * ((rotorDiscSpeed * 6) * TimeWarp.deltaTime));
                    //    }
                    //    else
                    //    {
                    //        setBladeRendererState(true);
                    //    }
                    //}
                    //else
                    //{
                    //    rotorDisc.renderer.enabled = false;
                    //    setBladeRendererState(true);
                    //}                     
                }
            }
        }
    }
}



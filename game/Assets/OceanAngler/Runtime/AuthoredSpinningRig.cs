using System;
using System.Collections.Generic;
using UnityEngine;
namespace OceanAngler
{
    public sealed class AuthoredSpinningRig : MonoBehaviour
    {
        public Transform[] rodBones,guides;
        public Transform rotor,rotorAxis,spool,crank,crankAxis,bail,bailAxis,lineExit,lure,lureNose;
        public Transform roller,rollerAxis,rollerSurface,spoolLineRadius,spoolLipCenter,spoolLipSurface;
        public LineRenderer fishingLine;
        public Vector3 inspectionCenter;
        public float CrankDegrees {get;private set;}
        public float RotorDegrees {get;private set;}
        public float SpoolStroke {get;private set;}
        public float BailOpenDegrees {get;private set;}
        public float RollerDegrees {get;private set;}
        public bool LinePassesRoller {get;private set;}
        Quaternion[] boneRest;Vector3[] bendAxes;
        Quaternion rotorRest,crankRest,bailRest,spoolRest,lureRootRotation,rollerRest;
        Vector3 rotorLocalAxis,crankLocalAxis,bailLocalAxis,spoolRestRoot,lureRestRoot,lureAxisRoot,rollerLocalAxis;
        float tension,dragAngle;bool reeling,paused,reduced,casting,preview,hideLure;Vector3 endpoint;
        FishVisual attachedFish;
        sealed class MaterialBinding {public Renderer renderer;public int index;public Color color;public float smoothness;public readonly MaterialPropertyBlock block=new MaterialPropertyBlock();}
        readonly List<MaterialBinding> materialBindings=new List<MaterialBinding>();
        public Vector3 CastOrigin=>guides[guides.Length-1].position;
        void Awake(){InitializeMechanisms();}
        public void InitializeMechanisms()
        {
            InitializePresentation();
            if(boneRest!=null)return;
            if(roller==null||rollerAxis==null||rollerSurface==null||spoolLineRadius==null||spoolLipCenter==null||spoolLipSurface==null)throw new InvalidOperationException("Rebuild the complete spinning rig prefab with its roller and payout markers");
            boneRest=new Quaternion[rodBones.Length];bendAxes=new Vector3[rodBones.Length];
            for(int i=0;i<rodBones.Length;i++){boneRest[i]=rodBones[i].localRotation;bendAxes[i]=rodBones[i].InverseTransformDirection(transform.right);}
            rotorRest=rotor.localRotation;crankRest=crank.localRotation;bailRest=bail.localRotation;spoolRest=spool.localRotation;
            rotorLocalAxis=rotor.InverseTransformDirection((rotorAxis.position-rotor.position).normalized);
            crankLocalAxis=crank.InverseTransformDirection((crankAxis.position-crank.position).normalized);
            bailLocalAxis=bail.InverseTransformDirection((bailAxis.position-bail.position).normalized);
            rollerRest=roller.localRotation;rollerLocalAxis=roller.InverseTransformDirection((rollerAxis.position-roller.position).normalized);
            spoolRestRoot=transform.InverseTransformPoint(spool.position);lureRestRoot=transform.InverseTransformPoint(lure.position);
            lureRootRotation=Quaternion.Inverse(transform.rotation)*lure.rotation;lureAxisRoot=transform.InverseTransformDirection((lureNose.position-lure.position).normalized);
        }
        public void InitializePresentation()
        {
            if(materialBindings.Count!=0)return;
            // Match the authored fish's explicit per-slot initialization. This
            // prevents first-draw slot state bleeding without cloning assets.
            foreach(var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var material=materials[i];var binding=new MaterialBinding{renderer=renderer,index=i,color=material.color,smoothness=material.GetFloat("_Smoothness")};renderer.GetPropertyBlock(binding.block,i);
                    binding.block.SetColor("_BaseColor",binding.color);binding.block.SetFloat("_Smoothness",binding.smoothness);renderer.SetPropertyBlock(binding.block,i);materialBindings.Add(binding);
                }
            }
        }
        public void ReleasePresentation(){foreach(var binding in materialBindings)if(binding.renderer!=null)binding.renderer.SetPropertyBlock(null,binding.index);materialBindings.Clear();}
        void OnDestroy(){ReleasePresentation();}
        public void Drive(float load,bool wind,bool stop,bool reduce,bool openBail,Vector3 target,bool inspection=false,bool hooked=false,FishVisual attachment=null)
        {tension=Mathf.Clamp01(load);reeling=wind;paused=stop;reduced=reduce;casting=openBail;endpoint=target;preview=inspection;hideLure=hooked;attachedFish=attachment;}
        void LateUpdate(){Present(Time.deltaTime);}
        public void Present(float deltaTime)
        {
            if(boneRest==null)return;
            float dt=float.IsNaN(deltaTime)||float.IsInfinity(deltaTime)?0:Mathf.Clamp(deltaTime,0,.1f);
            if(!paused&&!reduced)
            {
                if(reeling)CrankDegrees+=dt*220;
                if(!reeling&&tension>.82f)dragAngle+=dt*(tension-.82f)*180;
            }
            if(!paused)BailOpenDegrees=reduced?(casting?82:0):Mathf.MoveTowards(BailOpenDegrees,casting?82:0,dt*360);
            LinePassesRoller=BailOpenDegrees<1;
            if(!paused&&!reduced&&reeling&&LinePassesRoller)
            {
                // Visible kinematic approximation: pickup circumference drives
                // roller travel. No line inventory, friction or slip simulation.
                float pickup=Vector3.Distance(spool.position,spoolLineRadius.position),radius=Vector3.Distance(roller.position,rollerSurface.position);
                RollerDegrees=Mathf.Repeat(RollerDegrees+dt*220*5.2f*pickup/radius,360);
            }
            RotorDegrees=CrankDegrees*5.2f;SpoolStroke=Mathf.Sin(CrankDegrees*Mathf.Deg2Rad*.75f)*.006f;
            rotor.localRotation=rotorRest*Quaternion.AngleAxis(RotorDegrees,rotorLocalAxis);
            crank.localRotation=crankRest*Quaternion.AngleAxis(CrankDegrees,crankLocalAxis);
            bail.localRotation=bailRest*Quaternion.AngleAxis(BailOpenDegrees,bailLocalAxis);
            roller.localRotation=rollerRest*Quaternion.AngleAxis(RollerDegrees,rollerLocalAxis);
            spool.position=transform.TransformPoint(spoolRestRoot+Vector3.up*SpoolStroke);
            var dragAxis=spool.InverseTransformDirection(transform.up);spool.localRotation=spoolRest*Quaternion.AngleAxis(dragAngle,dragAxis);
            for(int i=0;i<rodBones.Length;i++)rodBones[i].localRotation=boneRest[i]*Quaternion.AngleAxis(-tension*2.8f*Mathf.Pow(i/(float)(rodBones.Length-1),1.3f),bendAxes[i]);
            lure.gameObject.SetActive(preview||!hideLure);
            if(preview){lure.position=transform.TransformPoint(lureRestRoot);lure.rotation=transform.rotation*lureRootRotation;}
            else
            {
                // Read the animated mouth after Animator updates, including a landed fish.
                if(attachedFish!=null)endpoint=attachedFish.HookPosition;
                lure.rotation=Quaternion.FromToRotation(transform.TransformDirection(lureAxisRoot),Vector3.left)*transform.rotation*lureRootRotation;
                lure.position+=endpoint-lureNose.position;
            }
            Vector3 target=preview||!hideLure?lureNose.position:endpoint;int span=16;
            float startWidth=preview?.0007f:.002f,endWidth=preview?.0007f:.0013f;
            if(fishingLine.startWidth!=startWidth)fishingLine.startWidth=startWidth;if(fishingLine.endWidth!=endWidth)fishingLine.endWidth=endWidth;
            Vector3 axis=(rollerAxis.position-roller.position).normalized;
            float surfaceRadius=Vector3.Distance(roller.position,rollerSurface.position),lineRadius=fishingLine.startWidth*.5f;
            var projected=Vector3.ProjectOnPlane(guides[0].position-roller.position,axis);float distance=projected.magnitude;
            if(distance<=surfaceRadius+lineRadius)throw new InvalidOperationException("First guide must remain outside the line roller");
            var along=projected/distance;float ratio=(surfaceRadius+lineRadius)/distance;
            var normal=along*ratio+Vector3.Cross(axis,along)*Mathf.Sqrt(1-ratio*ratio);
            lineExit.position=roller.position+normal*(surfaceRadius+lineRadius);
            var spoolAxis=(rotorAxis.position-rotor.position).normalized;
            var outward=Vector3.ProjectOnPlane(guides[0].position-spoolLipCenter.position,spoolAxis).normalized;
            float lipRadius=Vector3.Distance(spoolLipSurface.position,spoolLipCenter.position);
            Vector3 start=LinePassesRoller?lineExit.position:spoolLipCenter.position+outward*(lipRadius+lineRadius);
            fishingLine.positionCount=1+guides.Length+span;fishingLine.SetPosition(0,start);
            for(int i=0;i<guides.Length;i++)fishingLine.SetPosition(i+1,guides[i].position);
            Vector3 tip=guides[guides.Length-1].position;
            for(int i=1;i<=span;i++){float u=i/(float)span;fishingLine.SetPosition(guides.Length+i,Vector3.Lerp(tip,target,u)+Vector3.down*Mathf.Sin(u*Mathf.PI)*(1-tension)*.10f);}
        }
        public float MaximumGuideLineError()
        {
            if(fishingLine.positionCount<guides.Length+1)return float.PositiveInfinity;
            float error=0;for(int i=0;i<guides.Length;i++)error=Mathf.Max(error,Vector3.Distance(fishingLine.GetPosition(i+1),guides[i].position));return error;
        }
    }
}

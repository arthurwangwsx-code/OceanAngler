using System;
using UnityEngine;
namespace OceanAngler
{
    public sealed class AuthoredSpinningRig : MonoBehaviour
    {
        public Transform[] rodBones,guides;
        public Transform rotor,rotorAxis,spool,crank,crankAxis,bail,bailAxis,lineExit,lure,lureNose;
        public LineRenderer fishingLine;
        public Vector3 inspectionCenter;
        public float CrankDegrees {get;private set;}
        public float RotorDegrees {get;private set;}
        public float SpoolStroke {get;private set;}
        public float BailOpenDegrees {get;private set;}
        Quaternion[] boneRest;Vector3[] bendAxes;
        Quaternion rotorRest,crankRest,bailRest,spoolRest,lureRootRotation;
        Vector3 rotorLocalAxis,crankLocalAxis,bailLocalAxis,spoolRestRoot,lureRestRoot,lureAxisRoot;
        float tension,dragAngle;bool reeling,paused,reduced,casting,preview,hideLure;Vector3 endpoint;
        FishVisual attachedFish;
        public Vector3 CastOrigin=>guides[guides.Length-1].position;
        void Awake(){InitializeMechanisms();}
        public void InitializeMechanisms()
        {
            if(boneRest!=null)return;
            boneRest=new Quaternion[rodBones.Length];bendAxes=new Vector3[rodBones.Length];
            for(int i=0;i<rodBones.Length;i++){boneRest[i]=rodBones[i].localRotation;bendAxes[i]=rodBones[i].InverseTransformDirection(transform.right);}
            rotorRest=rotor.localRotation;crankRest=crank.localRotation;bailRest=bail.localRotation;spoolRest=spool.localRotation;
            rotorLocalAxis=rotor.InverseTransformDirection((rotorAxis.position-rotor.position).normalized);
            crankLocalAxis=crank.InverseTransformDirection((crankAxis.position-crank.position).normalized);
            bailLocalAxis=bail.InverseTransformDirection((bailAxis.position-bail.position).normalized);
            spoolRestRoot=transform.InverseTransformPoint(spool.position);lureRestRoot=transform.InverseTransformPoint(lure.position);
            lureRootRotation=Quaternion.Inverse(transform.rotation)*lure.rotation;lureAxisRoot=transform.InverseTransformDirection((lureNose.position-lure.position).normalized);
        }
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
            RotorDegrees=CrankDegrees*5.2f;SpoolStroke=Mathf.Sin(CrankDegrees*Mathf.Deg2Rad*.75f)*.006f;
            rotor.localRotation=rotorRest*Quaternion.AngleAxis(RotorDegrees,rotorLocalAxis);
            crank.localRotation=crankRest*Quaternion.AngleAxis(CrankDegrees,crankLocalAxis);
            bail.localRotation=bailRest*Quaternion.AngleAxis(BailOpenDegrees,bailLocalAxis);
            spool.position=transform.TransformPoint(spoolRestRoot+Vector3.up*SpoolStroke);
            var axis=spool.InverseTransformDirection(transform.up);spool.localRotation=spoolRest*Quaternion.AngleAxis(dragAngle,axis);
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
            fishingLine.positionCount=1+guides.Length+span;fishingLine.SetPosition(0,lineExit.position);
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

using UnityEngine;
namespace OceanAngler
{
    public sealed partial class OceanWorld
    {
        FishingPhase previousWaterlinePhase;
        bool cameraInitialized,transitioning,transitionTarget;
        float transitionTime,splashBegan=float.NegativeInfinity;
        const float HalfTransition=.22f;
        public bool FishingSurfaceView {get;private set;}
        public float CameraFade {get;private set;}
        public static void PoseGearAtWaterline(AuthoredSpinningRig rig,float seaLevel,bool leftHanded)
        {
            float hand=leftHanded?-1:1;var start=new Vector3(.72f*hand,seaLevel+.25f,-4.9f);
            var direction=(new Vector3(.15f*hand,seaLevel+2.4f,-2.3f)-start).normalized;
            rig.transform.localScale=Vector3.one;
            rig.transform.SetPositionAndRotation(start,Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.up,direction).normalized,direction));
        }
        void UpdateWaterlineCamera(Vector3 lurePosition)
        {
            var phase=game.Session.Phase;
            if(previousWaterlinePhase==FishingPhase.Casting&&phase==FishingPhase.Waiting)splashBegan=visualTime;
            previousWaterlinePhase=phase;
            activeHabitat.SetFishingSplash(new Vector3(lurePosition.x,activeHabitat.SeaLevel,lurePosition.z),game.Progress.Data.settings.reducedMotion?-1:visualTime-splashBegan);
            if(!ui.Playing)
            {
                cameraInitialized=transitioning=FishingSurfaceView=false;CameraFade=0;return;
            }
            bool allowed=!game.Progress.Data.settings.underwaterOnly&&!game.Progress.Data.settings.reducedMotion;
            bool surface=allowed&&(phase==FishingPhase.Casting||phase==FishingPhase.Ready&&releasing==null||phase==FishingPhase.Waiting&&visualTime-splashBegan<.22f);
            if(!cameraInitialized){cameraInitialized=true;FishingSurfaceView=surface;}
            if(!transitioning&&surface!=FishingSurfaceView){transitioning=true;transitionTarget=surface;transitionTime=0;}
            if(transitioning)
            {
                transitionTarget=surface;
                transitionTime+=Mathf.Clamp(Time.deltaTime,0,.1f);
                if(transitionTime>=HalfTransition)FishingSurfaceView=transitionTarget;
                float triangle=transitionTime<HalfTransition?transitionTime/HalfTransition:2-transitionTime/HalfTransition;
                CameraFade=Mathf.SmoothStep(0,1,Mathf.Clamp01(triangle));
                if(transitionTime>=2*HalfTransition){transitioning=false;CameraFade=0;}
            }
        }
        void OnGUI()
        {
            if(CameraFade<=0||ui==null||!ui.Playing||Event.current.type!=EventType.Repaint)return;
            Color prior=GUI.color;
            try{GUI.color=new Color(.075f,.32f,.38f,CameraFade);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture,ScaleMode.StretchToFill,true);}
            finally{GUI.color=prior;}
        }
    }
}

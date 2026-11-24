using System;
using UnityEngine;
namespace OceanAngler
{
    public sealed class OceanWorld : MonoBehaviour
    {
        GameController game;GameUI ui;Transform fishRoot;FishVisual fish;AuthoredSpinningRig gear;
        string fishId,habitat;
        MarineHabitat activeHabitat;
        Camera view;
        public FishVisual TargetFish=>fish;
        public MarineHabitat ActiveHabitat=>activeHabitat;
        public ObservationEncounter Observations {get;private set;}
        float visualTime;
        Vector3 fishHookLocal;
        CatchRecord releasing;
        float releaseTime;
        Vector3 releaseStart,releaseHeading;
        bool awaitingNextFish;
        const float ReleaseDuration=2.4f;
        void Start()
        {
            game=GetComponent<GameController>();ui=GetComponent<GameUI>();view=Camera.main;
            RenderSettings.ambientLight=new Color(.2f,.32f,.38f);
            Observations=gameObject.AddComponent<ObservationEncounter>();
            SwitchHabitat(game.Progress.Data.habitatId);MakeGear();SelectFish(game.Content.fish[0].id);game.Released+=BeginRelease;
        }
        void SwitchHabitat(string id)
        {
            if(activeHabitat!=null){if(activeHabitat.lighting!=null)activeHabitat.lighting.Restore();activeHabitat.gameObject.SetActive(false);Destroy(activeHabitat.gameObject);activeHabitat=null;}
            habitat=id;var resource=MarineHabitat.ResourceFor(id);
            if(resource!=null)
            {
                var prefab=Resources.Load<GameObject>(resource);if(prefab==null)throw new InvalidOperationException("Missing authored habitat "+id);
                activeHabitat=Instantiate(prefab).GetComponent<MarineHabitat>();if(activeHabitat==null)throw new InvalidOperationException("Habitat presentation missing "+id);
            }
            else throw new InvalidOperationException("No authored habitat registered for "+id);
        }
        void MakeGear()
        {
            var prefab=Resources.Load<GameObject>("Models/SpinningRig");if(prefab==null)throw new InvalidOperationException("Missing authored spinning rig prefab");
            gear=Instantiate(prefab).GetComponent<AuthoredSpinningRig>();
        }
        void SelectFish(string id)
        {
            if(fishRoot!=null)Destroy(fishRoot.gameObject);
            var authored=id=="trevally"?Resources.Load<GameObject>("Models/Bluefin"):null;
            if(authored!=null){fishRoot=Instantiate(authored).transform;fish=fishRoot.GetComponent<AuthoredFishVisual>();}
            else{fishRoot=new GameObject("Fish-"+id).transform;var prototype=fishRoot.gameObject.AddComponent<FishModel>();prototype.Initialize(game.Content.Fish(id));fish=prototype;}
            fishId=id;
            fishHookLocal=fishRoot.InverseTransformPoint(fish.HookPosition);
        }
        void BeginRelease(CatchRecord caught)
        {
            releasing=caught;releaseTime=0;awaitingNextFish=true;
            releaseStart=fishRoot.position;releaseHeading=fishRoot.TransformDirection(Vector3.left);
        }
        void PresentRelease(bool paused)
        {
            if(!paused)releaseTime+=Time.deltaTime;
            float u=Mathf.SmoothStep(0,1,Mathf.Clamp01(releaseTime/ReleaseDuration)),v=1-u;
            var p1=releaseStart+releaseHeading*.8f;var p2=releaseStart+new Vector3(-2,.12f,4);var p3=releaseStart+new Vector3(-2,.15f,12);
            fishRoot.position=v*v*v*releaseStart+3*v*v*u*p1+3*v*u*u*p2+u*u*u*p3;
            var direction=3*v*v*(p1-releaseStart)+6*v*u*(p2-p1)+3*u*u*(p3-p2);
            fishRoot.rotation=Quaternion.Euler(0,Mathf.Atan2(direction.z,-direction.x)*Mathf.Rad2Deg,-3);
            fishRoot.localScale=Vector3.one;fish.semantic=releaseTime<.4f?"turn":"release";fish.speed=1;
            if(releaseTime>=ReleaseDuration){releasing=null;fishRoot.gameObject.SetActive(false);}
        }
        void PoseFishingGear(bool leftHanded)
        {
            float hand=leftHanded?-1:1;var start=new Vector3(1.75f*hand,-.30f,-2.0f);var direction=(new Vector3(.70f*hand,2.6f,1.3f)-start).normalized;
            gear.transform.localScale=Vector3.one;gear.transform.position=start;gear.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.up,direction).normalized,direction);
        }
        void Update()
        {
            if(game==null||game.Progress.RequiresRecovery)return;
            var data=game.Progress.Data;bool reduced=data.settings.reducedMotion;
            if(game.Session.Phase!=FishingPhase.Ready||habitat!=null&&habitat!=data.habitatId){releasing=null;awaitingNextFish=false;}
            if(ui.FocusedFish!=null||ui.InspectingGear||ui.InspectingHabitat||ui.InspectingObservation||reduced)releasing=null;
            string desired=ui.FocusedFish??releasing?.fishId??game.Session.Catch?.fishId??(awaitingNextFish?fishId:System.Array.Find(game.Content.fish,f=>f.habitatId==data.habitatId).id);
            if(desired!=fishId)SelectFish(desired);
            fishRoot.gameObject.SetActive(!ui.InspectingGear&&!ui.InspectingHabitat&&!ui.InspectingObservation&&(!awaitingNextFish||releasing!=null||ui.FocusedFish!=null));gear.gameObject.SetActive(!ui.InspectingHabitat&&!ui.InspectingObservation);
            if(!reduced&&!game.Session.Paused)visualTime+=Time.deltaTime;float time=reduced?0:visualTime;
            fish.reducedMotion=reduced;fish.paused=ui.FocusedFish!=null?ui.ModelPaused:game.Session.Paused;fish.speed=game.Session.Phase==FishingPhase.Fighting?1.6f:1;
            bool journal=ui.FocusedFish!=null;
            fish.semantic=journal?"swim":game.Session.Phase==FishingPhase.Fighting?"hooked":game.Session.Phase==FishingPhase.Bite?"bite":game.Session.Phase==FishingPhase.Casting?"burst":game.Session.Phase==FishingPhase.Landed?"idle":"swim";
            if(!ui.InspectingGear)PoseFishingGear(data.settings.leftHanded);
            var pose=FishingEncounter.Evaluate(game.Session.Phase,game.Session.PhaseProgress,game.Session.CastPower,fishHookLocal,gear.CastOrigin,ui.Compact?(data.settings.leftHanded?1.3f:-1.3f):0,time,reduced);
            fishRoot.position=journal?new Vector3(ui.CompactViewer?-1.3f:2.35f,ui.CompactViewer?.7f:1.35f,1.2f):pose.fishPosition;
            fishRoot.localScale=Vector3.one*(journal?ui.ModelZoom:1);
            fishRoot.rotation=journal?Quaternion.Euler(0,-25+ui.ModelYaw,-3):pose.fishRotation;
            if(releasing!=null)PresentRelease(game.Session.Paused);
            bool discovered=game.Progress.Data.catches.Exists(c=>c.fishId==fishId);fish.SetSilhouette(journal&&!discovered);
            float t=game.Session.Tension;
            if(ui.InspectingGear)
            {
                float scale=ui.GearCloseup?6.5f:1.15f;gear.transform.localScale=Vector3.one*scale;gear.transform.rotation=Quaternion.Euler(ui.GearCloseup?-28:-6,ui.GearYaw,6);
                var focus=ui.GearCloseup?gear.inspectionCenter:new Vector3(0,1.16f,0);gear.transform.position=new Vector3(-1.3f,.45f,1.2f)-gear.transform.TransformVector(focus);
                gear.Drive(.45f,ui.GearReelingDemo,false,reduced,false,Vector3.zero,true);
            }
            else
            {
                bool hooked=game.Session.Phase==FishingPhase.Fighting||game.Session.Phase==FishingPhase.Landed;
                bool attached=hooked||game.Session.Phase==FishingPhase.Bite;
                gear.Drive(t,game.Reeling,game.Session.Paused,reduced,game.Session.Phase==FishingPhase.Casting,pose.lurePosition,false,hooked,attached?fish:null);
            }
            if(habitat!=data.habitatId)SwitchHabitat(data.habitatId);
            if(activeHabitat!=null)activeHabitat.Present(view,ui.InspectingHabitat,ui.InspectingHabitat&&ui.HabitatSurfaceView,ui.HabitatAngle,game.Session.Paused,reduced,ui.FocusedFish==null&&!ui.InspectingGear&&!ui.InspectingObservation,ui.InspectingHabitat?ui.HabitatCompositionOffset:0);
            Observations.Present(ui.ObservingSpecies,view,ui.Compact,reduced,Time.unscaledDeltaTime);
        }
        void OnDestroy(){if(game!=null)game.Released-=BeginRelease;if(activeHabitat!=null)Destroy(activeHabitat.gameObject);}
    }
}

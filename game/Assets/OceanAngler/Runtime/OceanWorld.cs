using System;
using UnityEngine;
namespace OceanAngler
{
    public sealed class OceanWorld : MonoBehaviour
    {
        GameController game;GameUI ui;Transform fishRoot,rodTip;FishVisual fish;LineRenderer line;
        string fishId,habitat;
        Transform[] rodSegments;
        Material wood,metal,water;
        Camera view;
        float visualTime;
        void Start()
        {
            game=GetComponent<GameController>();ui=GetComponent<GameUI>();view=Camera.main;
            wood=Mat(new Color(.47f,.28f,.12f),.25f);metal=Mat(new Color(.17f,.22f,.23f),.7f,.7f);water=Mat(new Color(.025f,.4f,.53f),.85f);
            MakeEnvironment();MakeGear();SelectFish(game.Content.fish[0].id);
        }
        Material Mat(Color color,float smooth,float metallic=0) {var m=SurfaceMaterial.Create();m.color=color;m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metallic);return m;}
        void MakeEnvironment()
        {
            view.transform.position=new Vector3(0,1.25f,-6.5f);view.transform.LookAt(new Vector3(0,.45f,1));view.fieldOfView=42;
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.04f,.31f,.41f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.028f;RenderSettings.ambientLight=new Color(.2f,.32f,.38f);
            var sand=Mat(new Color(.36f,.34f,.25f),.15f);Box("Sand",new Vector3(0,-1.1f,4),new Vector3(80,.12f,80),sand);
            Box("Surface",new Vector3(0,4.5f,4),new Vector3(80,.05f,80),water);
            var random=new System.Random(41);var rock=Mat(new Color(.19f,.32f,.30f),.2f);var coral=Mat(new Color(.55f,.33f,.25f),.15f);
            for(int i=0;i<26;i++)
            {
                float x=(float)random.NextDouble()*32-16,z=5+(float)random.NextDouble()*20;float size=.4f+(float)random.NextDouble()*2;
                var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name="ReefRock";obj.transform.position=new Vector3(x,-.8f,z);obj.transform.localScale=new Vector3(size,size*.65f,size);obj.GetComponent<Renderer>().sharedMaterial=rock;Destroy(obj.GetComponent<Collider>());
                if(i%3==0)for(int j=0;j<5;j++)Cylinder("CoralBranch",new Vector3(x,-.8f,z),new Vector3(x+Mathf.Sin(j*1.2f)*.3f,-.1f+j*.1f,z+Mathf.Cos(j)*.3f),.05f,coral);
            }
            for(int i=0;i<6;i++)Box("PierPlank",new Vector3(-4.2f+i*.4f,1.7f,2),new Vector3(.38f,.15f,9),wood);
            for(int i=0;i<3;i++)Cylinder("PierPost",new Vector3(-4.1f,-1,1+i*3),new Vector3(-4.1f,2,1+i*3),.12f,wood);
            var light=new GameObject("WaterFill").AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.37f,.79f,.86f);light.intensity=2.5f;light.range=15;light.transform.position=new Vector3(0,3,0);
        }
        void MakeGear()
        {
            rodSegments=new Transform[12];var carbon=Mat(new Color(.06f,.11f,.12f),.6f,.3f);
            for(int i=0;i<12;i++)rodSegments[i]=Cylinder("RodSection"+i,Vector3.zero,Vector3.up,.02f,carbon).transform;
            var grip=Mat(new Color(.20f,.15f,.10f),.2f);
            Cylinder("CorkGrip",new Vector3(1.8f,-.1f,-1.9f),new Vector3(1.6f,.22f,-1.55f),.065f,grip);
            Cylinder("ReelSeat",new Vector3(1.68f,.02f,-1.75f),new Vector3(1.65f,-.25f,-1.7f),.025f,metal);
            Cylinder("Spool",new Vector3(1.57f,-.22f,-1.72f),new Vector3(1.75f,-.22f,-1.72f),.095f,metal);
            Cylinder("ReelHandle",new Vector3(1.78f,-.22f,-1.72f),new Vector3(1.89f,-.4f,-1.72f),.022f,metal);
            Cylinder("HandleGrip",new Vector3(1.89f,-.4f,-1.72f),new Vector3(1.98f,-.4f,-1.72f),.03f,grip);
            var obj=new GameObject("FishingLine");line=obj.AddComponent<LineRenderer>();line.sharedMaterial=Mat(new Color(.8f,.92f,.91f),.3f);line.positionCount=24;line.startWidth=.009f;line.endWidth=.003f;
            rodTip=new GameObject("RodTip").transform;
        }
        GameObject Box(string name,Vector3 pos,Vector3 scale,Material material)
        {var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.position=pos;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=material;Destroy(o.GetComponent<Collider>());return o;}
        GameObject Cylinder(string name,Vector3 from,Vector3 to,float radius,Material material)
        {var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.name=name;Between(o.transform,from,to,radius);o.GetComponent<Renderer>().sharedMaterial=material;Destroy(o.GetComponent<Collider>());return o;}
        static void Between(Transform t,Vector3 a,Vector3 b,float radius) {t.position=(a+b)*.5f;t.up=(b-a).normalized;t.localScale=new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2);}
        void SelectFish(string id)
        {
            if(fishRoot!=null)Destroy(fishRoot.gameObject);
            var authored=id=="trevally"?Resources.Load<GameObject>("Models/Bluefin"):null;
            if(authored!=null){fishRoot=Instantiate(authored).transform;fish=fishRoot.GetComponent<AuthoredFishVisual>();}
            else{fishRoot=new GameObject("Fish-"+id).transform;var prototype=fishRoot.gameObject.AddComponent<FishModel>();prototype.Initialize(game.Content.Fish(id));fish=prototype;}
            fishId=id;
        }
        void Update()
        {
            if(game==null||game.Progress.RequiresRecovery)return;
            var data=game.Progress.Data;string desired=ui.FocusedFish??game.Session.Catch?.fishId??System.Array.Find(game.Content.fish,f=>f.habitatId==data.habitatId).id;
            if(desired!=fishId)SelectFish(desired);
            bool reduced=data.settings.reducedMotion;if(!reduced&&!game.Session.Paused)visualTime+=Time.deltaTime;float time=reduced?0:visualTime;
            fish.reducedMotion=reduced;fish.paused=ui.FocusedFish!=null?ui.ModelPaused:game.Session.Paused;fish.speed=game.Session.Phase==FishingPhase.Fighting?1.6f:1;
            bool journal=ui.FocusedFish!=null;
            fish.semantic=journal?"swim":game.Session.Phase==FishingPhase.Fighting?"hooked":game.Session.Phase==FishingPhase.Bite?"bite":game.Session.Phase==FishingPhase.Casting?"burst":game.Session.Phase==FishingPhase.Landed?"idle":"swim";
            fishRoot.position=journal?new Vector3(ui.CompactViewer?-1.3f:2.35f,ui.CompactViewer?.7f:1.35f,1.2f):new Vector3((ui.Compact?(data.settings.leftHanded?1.3f:-1.3f):0)+Mathf.Sin(time*.3f)*.18f,.30f+Mathf.Sin(time*.5f)*.05f,1.2f);
            fishRoot.localScale=Vector3.one*(journal?ui.ModelZoom:1);
            fishRoot.rotation=Quaternion.Euler(0,journal?-25+ui.ModelYaw:-25+Mathf.Sin(time*.3f)*7,-3);
            bool discovered=game.Progress.Data.catches.Exists(c=>c.fishId==fishId);fish.SetSilhouette(journal&&!discovered);
            float t=game.Session.Tension;Vector3 start=new Vector3(1.6f,.22f,-1.55f),tip=new Vector3(.7f,2.6f,1.3f);
            Vector3 prev=start;
            for(int i=0;i<12;i++)
            {
                float u=(i+1)/12f;Vector3 point=Vector3.Lerp(start,tip,u)+new Vector3(-.35f,-t*.75f,.18f)*u*u;Between(rodSegments[i],prev,point,Mathf.Lerp(.026f,.006f,u));prev=point;
            }
            rodTip.position=prev;Vector3 endpoint=game.Session.Phase==FishingPhase.Fighting?fish.HookPosition:new Vector3(-.1f,.2f,2);
            for(int i=0;i<24;i++){float u=i/23f;line.SetPosition(i,Vector3.Lerp(prev,endpoint,u)+Vector3.down*Mathf.Sin(u*Mathf.PI)*(1-t)*.2f);}
            if(habitat!=data.habitatId)
            {
                habitat=data.habitatId;Color color=habitat=="bay"?new Color(.04f,.34f,.44f):habitat=="channel"?new Color(.035f,.20f,.35f):new Color(.025f,.10f,.22f);RenderSettings.fogColor=color;view.backgroundColor=color;
            }
        }
        void OnDestroy(){if(wood!=null)Destroy(wood);if(metal!=null)Destroy(metal);if(water!=null)Destroy(water);}
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
namespace OceanAngler
{
    // 原创解剖样板：连续拓扑、鳍/鳃/口/眼、物种花纹与分段骨骼；仍待生物和美术审校。
    public sealed class FishModel : FishVisual
    {
        readonly List<Material> ownedMaterials = new List<Material>();
        readonly List<Mesh> ownedMeshes = new List<Mesh>();
        Texture2D texture;
        readonly Dictionary<Material,Color> colors = new Dictionary<Material,Color>();
        Transform[] bones;
        float animationTime;
        public void Initialize(FishDefinition fish)
        {
            string shape=fish.shape; bool deep=shape=="deep",flat=shape=="flat",mahi=shape=="mahi",grouper=shape=="grouper";
            float height=deep?.48f:mahi?.40f:grouper?.34f:flat?.52f:.28f, thickness=flat?.065f:deep?.17f:grouper?.27f:.20f;
            var mesh=new Mesh { name="AuthoredFish-"+fish.id };
            const int rings=42, sides=32;var vertices=new Vector3[(rings+1)*(sides+1)];var uv=new Vector2[vertices.Length];var weights=new BoneWeight[vertices.Length];var tris=new List<int>();
            for(int i=0;i<=rings;i++)
            {
                float u=i/(float)rings;float radius=Mathf.Pow(Mathf.Sin(Mathf.PI*Mathf.Clamp01(u*.94f+.025f)),.65f)*(1-u*.55f);
                if(u>.85f)radius=Mathf.Lerp(radius,.075f,(u-.85f)/.15f);
                for(int j=0;j<=sides;j++)
                {
                    float angle=j/(float)sides*Mathf.PI*2;int index=i*(sides+1)+j;
                    float top=mahi&&u<.25f?1.15f:1;
                    vertices[index]=new Vector3(Mathf.Lerp(-1,1,u),Mathf.Cos(angle)*height*radius*(Mathf.Cos(angle)>0?top:1),Mathf.Sin(angle)*thickness*radius);
                    uv[index]=new Vector2(u,j/(float)sides);
                    float bone=Mathf.Clamp01((u-.22f)/.78f)*4;int lower=Math.Min(3,(int)bone);float blend=bone-lower;
                    weights[index]=new BoneWeight {boneIndex0=lower,boneIndex1=lower+1,weight0=1-blend,weight1=blend};
                    if(i<rings&&j<sides){int a=index,b=index+sides+1;tris.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
                }
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=tris.ToArray();mesh.boneWeights=weights;mesh.RecalculateNormals();mesh.RecalculateBounds();ownedMeshes.Add(mesh);
            bones=new Transform[5];var bind=new Matrix4x4[5];
            for(int i=0;i<5;i++)
            {
                bones[i]=new GameObject("SwimBone"+i).transform;bones[i].SetParent(i==0?transform:bones[i-1],false);bones[i].localPosition=new Vector3(i==0?-.55f:.38f,0,0);bind[i]=bones[i].worldToLocalMatrix*transform.localToWorldMatrix;
            }
            mesh.bindposes=bind;
            var renderer=gameObject.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=mesh;renderer.bones=bones;renderer.rootBone=bones[0];renderer.localBounds=new Bounds(Vector3.zero,new Vector3(4,3,2));renderer.updateWhenOffscreen=false;
            texture=Skin(fish.id);var body=Material("Skin",Color.white,.45f,.18f);body.SetTexture("_BaseMap",texture);renderer.sharedMaterial=body;
            Color finColor=fish.id=="trevally"?new Color(.06f,.32f,.58f):fish.id=="yellowfin"?new Color(.85f,.68f,.15f):fish.id=="alfonsino"||fish.id=="redsnapper"?new Color(.7f,.18f,.13f):new Color(.22f,.38f,.44f);
            var fin=Material("Fin",finColor,.35f,.05f); fin.SetFloat("_Cull",0);
            Fin("Tail",new[]{new Vector3(.96f,0,0),new Vector3(1.48f,.46f,0),new Vector3(1.28f,.10f,0),new Vector3(1.25f,-.1f,0),new Vector3(1.48f,-.46f,0)},fin,bones[4]);
            float dorsal=height*.85f;
            Fin("Dorsal",new[]{new Vector3(-.45f,dorsal,0),new Vector3(-.24f,dorsal+(grouper?.22f:deep?.28f:.18f),0),new Vector3(.13f,dorsal+.16f,0),new Vector3(.66f,.12f,0)},fin,bones[1]);
            Fin("Anal",new[]{new Vector3(.03f,-height*.8f,0),new Vector3(.20f,-height-.17f,0),new Vector3(.70f,-.12f,0)},fin,bones[2]);
            for(int sign=-1;sign<=1;sign+=2)
            {
                Fin("Pectoral",new[]{new Vector3(-.52f,-.04f,sign*thickness*.84f),new Vector3(.10f,-.25f,sign*(thickness+.35f)),new Vector3(-.04f,-.08f,sign*(thickness+.05f))},fin,bones[0]);
                Fin("Pelvic",new[]{new Vector3(-.26f,-height*.75f,sign*.09f),new Vector3(.03f,-height-.12f,sign*.17f),new Vector3(.16f,-height*.65f,sign*.06f)},fin,bones[1]);
                Eye(new Vector3(-.77f,height*.2f,sign*thickness*.69f),sign);
                var gill=new List<Vector3>();for(int i=0;i<=16;i++){float t=i/16f*Mathf.PI;gill.Add(new Vector3(-.53f+Mathf.Sin(t)*.055f,Mathf.Cos(t)*height*.68f,sign*thickness*.85f));}
                Curve("Gill",gill.ToArray(),.009f,Material("GillShade",new Color(.12f,.16f,.16f),.1f,0),bones[0]);
            }
            Curve("Mouth",new[]{new Vector3(-.965f,.013f,-thickness*.22f),new Vector3(-1.005f,0,0),new Vector3(-.965f,.013f,thickness*.22f)},.012f,fin,bones[0]);
            if(shape=="tuna")for(int i=0;i<6;i++)
            {
                float x=.45f+i*.075f;Fin("Finlet",new[]{new Vector3(x,.17f,0),new Vector3(x+.025f,.24f-i*.012f,0),new Vector3(x+.07f,.13f,0)},fin,bones[3]);
            }
        }
        public override void SetSilhouette(bool hidden) { foreach(var pair in colors)pair.Key.color=hidden?new Color(.015f,.035f,.045f):pair.Value; }
        void Update()
        {
            if(bones==null)return;if(!paused)animationTime+=Time.deltaTime*speed;float time=animationTime;
            for(int i=0;i<bones.Length;i++)bones[i].localRotation=Quaternion.Euler(0,reducedMotion?0:Mathf.Sin(time*3.5f-i*.7f)*i*3.8f,0);
        }
        void Eye(Vector3 pos,int sign)
        {
            var white=Material("Iris",new Color(.83f,.72f,.38f),.8f,.1f);var pupil=Material("Pupil",new Color(.008f,.015f,.02f),.85f,0);
            Sphere("Eye",pos,new Vector3(.075f,.075f,.035f),white,bones[0]);Sphere("Pupil",pos+new Vector3(0,0,sign*.024f),new Vector3(.045f,.045f,.012f),pupil,bones[0]);
        }
        void Sphere(string name,Vector3 pos,Vector3 scale,Material material,Transform parent)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.name=name;obj.transform.SetParent(transform,false);obj.transform.localPosition=pos;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=material;Destroy(obj.GetComponent<Collider>());obj.transform.SetParent(parent,true);
        }
        void Fin(string name,Vector3[] points,Material material,Transform parent)
        {
            var mesh=new Mesh { name=name };var triangles=new List<int>();for(int i=1;i<points.Length-1;i++)triangles.AddRange(new[]{0,i,i+1});mesh.vertices=points;mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();ownedMeshes.Add(mesh);
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(transform,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=material;obj.transform.SetParent(parent,true);
            for(int i=1;i<points.Length;i++)Curve(name+"Ray",new[]{points[0],points[i]},.0035f,material,parent);
        }
        void Curve(string name,Vector3[] points,float width,Material material,Transform parent)
        {
            var obj=new GameObject(name);obj.transform.SetParent(transform,false);var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=points.Length;line.SetPositions(points);line.startWidth=line.endWidth=width;line.sharedMaterial=material;line.numCapVertices=3;obj.transform.SetParent(parent,true);
        }
        Material Material(string name,Color color,float smoothness,float metallic)
        {
            var mat=SurfaceMaterial.Create();mat.color=color;colors[mat]=color;mat.SetFloat("_Smoothness",smoothness);mat.SetFloat("_Metallic",metallic);ownedMaterials.Add(mat);return mat;
        }
        Texture2D Skin(string id)
        {
            var tex=new Texture2D(512,256,TextureFormat.RGBA32,false){name="OriginalScales-"+id,wrapMode=TextureWrapMode.Repeat};
            Color back=id=="grouper"?new Color(.36f,.25f,.16f):id=="redsnapper"||id=="alfonsino"?new Color(.66f,.16f,.12f):id=="mahi"?new Color(.04f,.42f,.28f):new Color(.07f,.25f,.36f);
            Color side=id=="grouper"?new Color(.63f,.48f,.28f):id=="redsnapper"||id=="alfonsino"?new Color(.9f,.45f,.35f):id=="mahi"?new Color(.68f,.77f,.24f):new Color(.5f,.68f,.72f);
            for(int y=0;y<256;y++)for(int x=0;x<512;x++)
            {
                float u=x/511f,v=y/255f,top=Mathf.Cos(v*Mathf.PI*2);Color c=Color.Lerp(side,top>0?back:new Color(.88f,.86f,.72f),Mathf.Pow(Mathf.Abs(top),1.2f));
                float row=v*35,col=u*65+(Mathf.Floor(row)%2)*.5f;float dx=Mathf.Repeat(col,1)-.5f,dy=Mathf.Repeat(row,1)-.5f;float scale=Mathf.Sqrt(dx*dx+dy*dy);
                c*=Mathf.Lerp(.89f,1.08f,Mathf.Clamp01((scale-.2f)*3));
                if(id=="grouper"&&Mathf.Sin(u*123+Mathf.Sin(v*41)*3)*Mathf.Cos(v*73+u*11)>.68f)c*=.48f;
                if(id=="mackerel"&&top>.1f&&Mathf.Sin(u*90+Mathf.Sin(v*33)*2)>.5f)c*=.48f;
                if(id=="skipjack"&&top<.3f&&Mathf.Sin(v*110)>.78f)c*=.55f;
                if(id=="trevally"&&Mathf.Sin(u*143)*Mathf.Cos(v*129)>.91f)c=Color.Lerp(c,new Color(.04f,.38f,.55f),.7f);
                if(id=="mahi"&&Mathf.Sin(u*160)*Mathf.Cos(v*117)>.9f)c*=.45f;
                tex.SetPixel(x,y,c);
            }
            tex.Apply();return tex;
        }
        void OnDestroy() { foreach(var m in ownedMaterials)Destroy(m);foreach(var m in ownedMeshes)Destroy(m);if(texture!=null)Destroy(texture); }
    }
}

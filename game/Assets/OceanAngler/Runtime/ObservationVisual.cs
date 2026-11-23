using System;
using System.Collections.Generic;
using UnityEngine;
namespace OceanAngler
{
    // A0 interaction models only. These are not the authored A1–A3 assets.
    public sealed class ObservationVisual : MonoBehaviour
    {
        readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
        readonly List<Transform> paddles=new List<Transform>();
        Transform tail;string species;
        public void Initialize(string id)
        {
            if(species!=null)throw new InvalidOperationException("Visual already initialized");
            species=id;var dark=Material(new Color(.018f,.03f,.035f));
            if(id=="green-turtle")
            {
                var shell=Material(new Color(.29f,.31f,.14f));var skin=Material(new Color(.38f,.39f,.22f));var belly=Material(new Color(.68f,.64f,.39f));
                Ellipsoid("Carapace",Vector3.zero,new Vector3(.72f,.23f,.47f),shell);
                Ellipsoid("Plastron",new Vector3(0,-.12f,0),new Vector3(.67f,.1f,.43f),belly);
                Ellipsoid("Neck",new Vector3(-.69f,-.005f,0),new Vector3(.21f,.09f,.12f),skin);
                Ellipsoid("Head",new Vector3(-.93f,.01f,0),new Vector3(.21f,.105f,.14f),skin);
                for(int side=-1;side<=1;side+=2)
                {
                    Ellipsoid("Eye",new Vector3(-1.015f,.055f,side*.12f),new Vector3(.025f,.025f,.012f),dark);
                    for(int rear=0;rear<2;rear++)
                    {
                        var fin=Fin("Flipper",new Vector3(rear==0?-.43f:.49f,-.05f,side*.34f),new[]{Vector3.zero,new Vector3(-.13f,0,side*.22f),new Vector3(.21f,-.02f,side*(rear==0?.68f:.38f)),new Vector3(.4f,0,side*.2f),new Vector3(.12f,0,0)},Vector3.up,skin);
                        paddles.Add(fin);
                    }
                }
                // Coarse scute seams on the shell, not final anatomical topology.
                for(int i=0;i<5;i++)
                {
                    float x=-.48f+i*.24f;float y=.23f*Mathf.Sqrt(1-x*x/(.72f*.72f));
                    Ellipsoid("Scute",new Vector3(x,y*.98f,0),new Vector3(.105f,.012f,.13f),skin);
                }
                Ellipsoid("Tail",new Vector3(.73f,-.08f,0),new Vector3(.16f,.035f,.045f),skin);
            }
            else if(id=="bottlenose-dolphin"||id=="whale-shark")
            {
                bool dolphin=id=="bottlenose-dolphin";var body=Material(Color.white);body.SetTexture("_BaseMap",Pattern(dolphin));
                var finMat=Material(dolphin?new Color(.29f,.36f,.4f):new Color(.09f,.22f,.29f));
                Body(dolphin?new[]{(-1.02f,.035f,.04f),(-.88f,.22f,.24f),(-.6f,.34f,.3f),(0f,.33f,.31f),(.65f,.19f,.17f),(1.14f,.055f,.065f),(1.29f,.015f,.02f)}:new[]{(-1.35f,.16f,.38f),(-1.2f,.26f,.47f),(-.65f,.32f,.48f),(0f,.3f,.35f),(.7f,.19f,.19f),(1.36f,.065f,.075f),(1.5f,.012f,.015f)},body);
                if(dolphin)Ellipsoid("Rostrum",new Vector3(-1.06f,-.085f,0),new Vector3(.3f,.065f,.095f),finMat);
                else Ellipsoid("Mouth",new Vector3(-1.353f,-.07f,0),new Vector3(.012f,.018f,.29f),dark);
                Fin("Dorsal",new Vector3(-.05f,.27f,0),new[]{Vector3.zero,new Vector3(.18f,.48f,0),new Vector3(.36f,.15f,0),new Vector3(.55f,0,0)},Vector3.forward,finMat);
                if(!dolphin)Fin("SecondDorsal",new Vector3(.9f,.12f,0),new[]{Vector3.zero,new Vector3(.1f,.22f,0),new Vector3(.32f,0,0)},Vector3.forward,finMat);
                for(int side=-1;side<=1;side+=2)
                {
                    Ellipsoid("Eye",new Vector3(dolphin?-.88f:-1.23f,.035f,side*(dolphin?.22f:.39f)),new Vector3(.028f,.028f,.015f),dark);
                    Fin("Pectoral",new Vector3(-.48f,-.12f,side*(dolphin?.24f:.39f)),new[]{Vector3.zero,new Vector3(.1f,-.04f,side*.13f),new Vector3(.53f,-.1f,side*(dolphin?.48f:.73f)),new Vector3(.34f,0,side*.1f)},Vector3.up,finMat);
                    if(!dolphin)for(int i=0;i<5;i++)Ellipsoid("GillSlit",new Vector3(-.95f+i*.1f,-.035f,side*.447f),new Vector3(.008f,.13f,.012f),dark);
                }
                tail=new GameObject("TailPivot").transform;tail.SetParent(transform,false);tail.localPosition=new Vector3(dolphin?1.11f:1.3f,0,0);
                if(dolphin)
                    for(int side=-1;side<=1;side+=2)Fin("Fluke",tail.localPosition,new[]{Vector3.zero,new Vector3(.01f,0,side*.6f),new Vector3(.31f,0,side*.37f),new Vector3(.19f,0,side*.03f)},Vector3.up,finMat).SetParent(tail,true);
                else Fin("Caudal",tail.localPosition,new[]{Vector3.zero,new Vector3(.2f,.73f,0),new Vector3(.5f,.39f,0),new Vector3(.18f,0,0),new Vector3(.4f,-.38f,0),new Vector3(.1f,-.2f,0)},Vector3.forward,finMat).SetParent(tail,true);
            }
            else throw new ArgumentException("Unknown observation visual");
        }
        Material Material(Color color)
        {var m=SurfaceMaterial.Create();m.color=color;m.SetFloat("_Smoothness",.28f);owned.Add(m);return m;}
        void Ellipsoid(string name,Vector3 position,Vector3 scale,Material material)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();const int rings=12,sides=20;
            for(int i=0;i<=rings;i++)for(int j=0;j<sides;j++)
            {
                float theta=Mathf.PI*i/rings,phi=2*Mathf.PI*j/sides;
                vertices.Add(new Vector3(Mathf.Cos(theta),Mathf.Sin(theta)*Mathf.Cos(phi),Mathf.Sin(theta)*Mathf.Sin(phi)));uv.Add(new Vector2(i/(float)rings,j/(float)sides));
            }
            for(int i=0;i<rings;i++)for(int j=0;j<sides;j++){int a=i*sides+j,b=i*sides+(j+1)%sides,c=a+sides,d=b+sides;tris.AddRange(new[]{a,c,b,b,c,d});}
            var t=MeshPart(name,vertices,uv,tris,material);t.localPosition=position;t.localScale=scale;
        }
        void Body((float x,float y,float z)[] profile,Material material)
        {
            const int sides=32,segments=40;var vertices=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(int i=0;i<=segments;i++)
            {
                float u=i/(float)segments,x=Mathf.Lerp(profile[0].x,profile[profile.Length-1].x,u);int k=0;while(k<profile.Length-2&&x>profile[k+1].x)k++;
                float t=Mathf.InverseLerp(profile[k].x,profile[k+1].x,x),height=Mathf.Lerp(profile[k].y,profile[k+1].y,t),width=Mathf.Lerp(profile[k].z,profile[k+1].z,t);
                for(int j=0;j<=sides;j++){float v=j/(float)sides,phi=v*Mathf.PI*2;vertices.Add(new Vector3(x,Mathf.Cos(phi)*height,Mathf.Sin(phi)*width));uv.Add(new Vector2(u,v));}
            }
            for(int i=0;i<segments;i++)for(int j=0;j<sides;j++){int a=i*(sides+1)+j,b=a+1,c=a+sides+1,d=c+1;tris.AddRange(new[]{a,b,c,b,d,c});}
            for(int end=0;end<2;end++)
            {
                int start=end==0?0:segments*(sides+1),center=vertices.Count;vertices.Add(new Vector3(profile[end==0?0:profile.Length-1].x,0,0));uv.Add(new Vector2(end,.5f));
                for(int j=0;j<sides;j++)tris.AddRange(end==0?new[]{center,start+j+1,start+j}:new[]{center,start+j,start+j+1});
            }
            var body=MeshPart("Body",vertices,uv,tris,material).GetComponent<MeshFilter>().sharedMesh;
            var normals=body.normals;
            for(int i=0;i<=segments;i++){int first=i*(sides+1),last=first+sides;var normal=(normals[first]+normals[last]).normalized;normals[first]=normals[last]=normal;}
            body.normals=normals;
        }
        Transform Fin(string name,Vector3 position,Vector3[] polygon,Vector3 normal,Material material)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();int n=polygon.Length;
            for(int side=0;side<2;side++)for(int i=0;i<n;i++){v.Add(polygon[i]+normal*(side==0?.008f:-.008f));uv.Add(new Vector2(i/(float)n,side));}
            for(int i=1;i<n-1;i++){t.AddRange(new[]{0,i,i+1,n,n+i+1,n+i});}
            for(int i=0;i<n;i++){int j=(i+1)%n;t.AddRange(new[]{i,j,n+i,j,n+j,n+i});}
            // Ensure the closed prism faces point away from its center.
            Vector3 center=Vector3.zero;foreach(var p in v)center+=p;center/=v.Count;
            for(int i=0;i<t.Count;i+=3)if(Vector3.Dot(Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]),(v[t[i]]+v[t[i+1]]+v[t[i+2]])/3-center)<0){int swap=t[i+1];t[i+1]=t[i+2];t[i+2]=swap;}
            var part=MeshPart(name,v,uv,t,material);part.localPosition=position;return part;
        }
        Transform MeshPart(string name,List<Vector3> vertices,List<Vector2> uv,List<int> triangles,Material material)
        {
            var mesh=new Mesh{name="A0-"+name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);
            var part=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(transform,false);part.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=part.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            // Pin each renderer's constants on first draw; newly cloned runtime
            // materials otherwise showed another part's color in the initial sample.
            var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",material.color);block.SetFloat("_Smoothness",material.GetFloat("_Smoothness"));renderer.SetPropertyBlock(block,0);
            return part.transform;
        }
        Texture2D Pattern(bool dolphin)
        {
            const int width=256,height=128;var tex=new Texture2D(width,height,TextureFormat.RGB24,false){name="A0-ObservationSkin",wrapMode=TextureWrapMode.Repeat};var pixels=new Color[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                float side=Mathf.Cos(y/(float)height*Mathf.PI*2);var top=dolphin?new Color(.24f,.31f,.36f):new Color(.08f,.20f,.28f);var belly=dolphin?new Color(.65f,.70f,.71f):new Color(.65f,.69f,.67f);
                var color=Color.Lerp(top,belly,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,-.65f,side)));
                if(!dolphin&&side>-.1f){float a=Mathf.Repeat(x/11f+(y/9%2)*.5f,1)-.5f,b=Mathf.Repeat(y/9f,1)-.5f;if(a*a+b*b<.035f)color=new Color(.68f,.75f,.73f);}
                pixels[y*width+x]=color;
            }
            tex.SetPixels(pixels);tex.Apply(false,true);owned.Add(tex);return tex;
        }
        public void Pose(float time,bool reduced)
        {
            float phase=reduced?0:time*2.2f;
            if(tail!=null)tail.localRotation=species=="bottlenose-dolphin"?Quaternion.Euler(0,0,Mathf.Sin(phase)*12):Quaternion.Euler(0,Mathf.Sin(phase)*13,0);
            for(int i=0;i<paddles.Count;i++)paddles[i].localRotation=Quaternion.Euler((i<2?-1:1)*Mathf.Sin(phase+i%2*.4f)*18,0,0);
        }
        void OnDestroy(){foreach(var o in owned)if(o!=null){if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}}
    }
}

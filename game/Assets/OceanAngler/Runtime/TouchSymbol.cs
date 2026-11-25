using UnityEngine;
using UnityEngine.UI;
namespace OceanAngler
{
    // Original, resolution-independent UI marks. No emoji or font glyph dependency.
    public sealed class TouchSymbol : MaskableGraphic
    {
        public enum Kind { Pause, Play, Back, Home, Rod, Reel, Fish, Book, Map, Settings, Tasks, Release, Rotate, ZoomIn, ZoomOut, Reset, Sound }
        public Kind symbol;
        public bool background=true;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;float scale=Mathf.Min(r.width,r.height);Vector2 origin=r.center;
            Vector2 P(float x,float y)=>origin+new Vector2(x,y)*scale;
            Color ink=color;
            void Quad(Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint)
            {
                int n=mesh.currentVertCount;mesh.AddVert(a,tint,Vector2.zero);mesh.AddVert(b,tint,Vector2.zero);mesh.AddVert(c,tint,Vector2.zero);mesh.AddVert(d,tint,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
            }
            void Line(float x,float y,float a,float b,float width=.035f)
            {
                var start=P(x,y);var end=P(a,b);var normal=new Vector2(-(end-start).y,(end-start).x).normalized*scale*width*.5f;
                Quad(start-normal,start+normal,end+normal,end-normal,ink);
            }
            void Ring(float x,float y,float radius)
            {
                for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Line(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius);}
            }
            if(background)
            {
                Color fill=new Color(.025f,.12f,.19f,.86f)*color;
                for(int i=0;i<40;i++)
                {
                    int n=mesh.currentVertCount;float a=i*Mathf.PI/20,b=(i+1)*Mathf.PI/20;
                    mesh.AddVert(origin,fill,Vector2.zero);mesh.AddVert(P(Mathf.Cos(a)*.49f,Mathf.Sin(a)*.49f),fill,Vector2.zero);mesh.AddVert(P(Mathf.Cos(b)*.49f,Mathf.Sin(b)*.49f),fill,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);
                }
            }
            switch(symbol)
            {
                case Kind.Pause:Line(-.1f,-.2f,-.1f,.2f,.075f);Line(.1f,-.2f,.1f,.2f,.075f);break;
                case Kind.Play:Line(-.13f,.22f,.22f,0);Line(.22f,0,-.13f,-.22f);Line(-.13f,-.22f,-.13f,.22f);break;
                case Kind.Back:Line(.18f,.22f,-.14f,0);Line(-.14f,0,.18f,-.22f);break;
                case Kind.Home:Line(-.27f,0,0,.25f);Line(0,.25f,.27f,0);Line(-.2f,.03f,-.2f,-.24f);Line(-.2f,-.24f,.2f,-.24f);Line(.2f,-.24f,.2f,.03f);Line(-.05f,-.24f,-.05f,-.08f);Line(-.05f,-.08f,.06f,-.08f);Line(.06f,-.08f,.06f,-.24f);break;
                case Kind.Rod:Line(-.25f,-.26f,.1f,.27f,.05f);Line(.1f,.27f,.28f,.12f);Line(.28f,.12f,.28f,-.18f);Line(.28f,-.18f,.22f,-.25f);Line(.22f,-.25f,.16f,-.18f);Ring(-.12f,-.12f,.065f);break;
                case Kind.Reel:Ring(0,0,.18f);Ring(0,0,.06f);Line(0,0,.23f,-.22f,.055f);Ring(.25f,-.24f,.045f);Line(-.22f,.23f,.15f,.23f);Line(-.13f,.2f,-.13f,.09f);break;
                case Kind.Fish:case Kind.Release:
                    Line(-.28f,0,-.05f,.16f);Line(-.05f,.16f,.16f,.12f);Line(.16f,.12f,.3f,0);Line(.3f,0,.16f,-.12f);Line(.16f,-.12f,-.05f,-.16f);Line(-.05f,-.16f,-.28f,0);Line(-.24f,0,-.34f,.13f);Line(-.34f,.13f,-.34f,-.13f);Line(-.34f,-.13f,-.24f,0);Ring(.17f,.035f,.02f);if(symbol==Kind.Release){Line(.03f,-.25f,.23f,-.25f);Line(.23f,-.25f,.16f,-.2f);}break;
                case Kind.Book:Line(-.27f,.22f,0,.16f);Line(0,.16f,.27f,.22f);Line(-.27f,.22f,-.27f,-.2f);Line(.27f,.22f,.27f,-.2f);Line(-.27f,-.2f,0,-.25f);Line(0,-.25f,.27f,-.2f);Line(0,.16f,0,-.25f);break;
                case Kind.Map:Line(-.28f,.2f,-.1f,.27f);Line(-.1f,.27f,.1f,.17f);Line(.1f,.17f,.28f,.24f);Line(-.28f,.2f,-.28f,-.22f);Line(-.28f,-.22f,-.1f,-.15f);Line(-.1f,-.15f,.1f,-.25f);Line(.1f,-.25f,.28f,-.18f);Line(.28f,-.18f,.28f,.24f);Line(-.1f,.27f,-.1f,-.15f);Line(.1f,.17f,.1f,-.25f);break;
                case Kind.Settings:Ring(0,0,.17f);Ring(0,0,.06f);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(Mathf.Cos(a)*.18f,Mathf.Sin(a)*.18f,Mathf.Cos(a)*.28f,Mathf.Sin(a)*.28f,.06f);}break;
                case Kind.Rotate:case Kind.Reset:
                    for(int i=0;i<19;i++){float a=(i+2)*Mathf.PI/12,b=(i+3)*Mathf.PI/12;Line(Mathf.Cos(a)*.24f,Mathf.Sin(a)*.24f,Mathf.Cos(b)*.24f,Mathf.Sin(b)*.24f);}
                    Line(.21f,.12f,.27f,.22f);Line(.27f,.22f,.13f,.23f);
                    if(symbol==Kind.Reset){Line(-.1f,0,.1f,0);Line(0,-.1f,0,.1f);}break;
                case Kind.ZoomIn:case Kind.ZoomOut:Ring(-.06f,.06f,.18f);Line(.07f,-.08f,.25f,-.26f,.055f);Line(-.16f,.06f,.04f,.06f);if(symbol==Kind.ZoomIn)Line(-.06f,-.04f,-.06f,.16f);break;
                case Kind.Sound:Line(-.25f,.1f,-.13f,.1f);Line(-.13f,.1f,.04f,.24f);Line(.04f,.24f,.04f,-.24f);Line(.04f,-.24f,-.13f,-.1f);Line(-.13f,-.1f,-.25f,-.1f);Line(-.25f,-.1f,-.25f,.1f);Line(.14f,.13f,.22f,.06f);Line(.22f,.06f,.22f,-.06f);Line(.22f,-.06f,.14f,-.13f);break;
                case Kind.Tasks:Line(-.18f,.24f,.23f,.24f);Line(.23f,.24f,.23f,-.24f);Line(.23f,-.24f,-.23f,-.24f);Line(-.23f,-.24f,-.23f,.18f);for(int i=0;i<3;i++){float y=.12f-i*.13f;Line(-.13f,y,-.09f,y-.035f);Line(-.09f,y-.035f,-.02f,y+.025f);Line(.045f,y,.15f,y);}break;
            }
        }
    }
    public sealed class HudFootprint : MonoBehaviour { }
}

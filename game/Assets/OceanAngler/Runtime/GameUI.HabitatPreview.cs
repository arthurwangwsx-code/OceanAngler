using UnityEngine;
namespace OceanAngler
{
    public sealed partial class GameUI
    {
        public bool InspectingHabitat=>page=="海湾观察";
        public bool HabitatSurfaceView {get;private set;}=true;
        public int HabitatAngle {get;private set;}
        public float HabitatCompositionOffset=>0;
        void BuildHabitatPreview()
        {
            float width=Mathf.Min(520,Width-24),cell=(width-12)/3,left=(Width-width)/2,top=Height-78;
            IconButton(pageRoot,HabitatSurfaceView?"潜入水下":"回到水面",TouchSymbol.Kind.Fish,left,top,cell,66,()=>{HabitatSurfaceView=!HabitatSurfaceView;dirty=true;},32);
            IconButton(pageRoot,"换个角度",TouchSymbol.Kind.Rotate,left+cell+6,top,cell,66,()=>HabitatAngle++,32);
            IconButton(pageRoot,"返回钓鱼",TouchSymbol.Kind.Back,left+2*(cell+6),top,cell,66,()=>Navigate("出海"),32);
        }
    }
}

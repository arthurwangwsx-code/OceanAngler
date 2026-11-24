using UnityEngine;
namespace OceanAngler
{
    public sealed partial class GameUI
    {
        public bool InspectingHabitat=>page=="海湾观察";
        public bool HabitatSurfaceView {get;private set;}=true;
        public int HabitatAngle {get;private set;}
        void BuildHabitatPreview()
        {
            float width=Mathf.Min(300,Width*.35f);
            var panel=Box(pageRoot,"HabitatDetails",Panel,new Vector2(1,0),Vector2.one,new Vector2(-width-12,Compact?36:72),new Vector2(-12,Compact?-102:-156));
            Label(panel,game.Content.Habitat(game.Progress.Data.habitatId).name,Compact?23:28,Color.white,14,8,-28,44);
            Label(panel,game.Progress.Data.habitatId=="bay"?"看看沙地和礁石，\n让珊瑚安静生长。":"看看岩岛与海床，\n安静欣赏开阔蓝海。",Compact?17:21,Muted,14,52,-28,Compact?54:64);
            Button(panel,HabitatSurfaceView?"潜入水下":"回到水面",14,Compact?110:122,-28,Compact?44:48,()=>{HabitatSurfaceView=!HabitatSurfaceView;dirty=true;},Navy).GetComponentInChildren<UnityEngine.UI.Text>().fontSize=Compact?18:22;
            Button(panel,"换个角度",14,Compact?162:182,-28,Compact?44:48,()=>HabitatAngle++,Teal).GetComponentInChildren<UnityEngine.UI.Text>().fontSize=Compact?18:22;
            Button(pageRoot,"返回钓鱼",12,Height-(Compact?86:124),160,44,()=>Navigate("出海"),Navy).GetComponentInChildren<UnityEngine.UI.Text>().fontSize=18;
        }
    }
}

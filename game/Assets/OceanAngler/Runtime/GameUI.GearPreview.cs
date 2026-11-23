using UnityEngine;
namespace OceanAngler
{
    public sealed partial class GameUI
    {
        void BuildGearPreview()
        {
            float width=Mathf.Min(300,Width*.35f);
            var panel=Box(pageRoot,"GearDetails",Panel,new Vector2(1,0),Vector2.one,new Vector2(-width-12,Compact?36:72),new Vector2(-12,Compact?-102:-156));
            Label(panel,"入门钓组",Compact?23:28,Color.white,14,8,-28,40);
            Label(panel,"鱼线穿过导环，\n摇柄带动收线。",Compact?17:21,Muted,14,52,-28,54);
            Button(panel,GearCloseup?"查看全竿":"近看卷线器",14,112,-28,48,()=>{GearCloseup=!GearCloseup;dirty=true;},Navy).GetComponentInChildren<UnityEngine.UI.Text>().fontSize=Compact?18:22;
            Button(panel,GearReelingDemo?"停止收线演示":"试摇卷线器",14,172,-28,48,()=>{GearReelingDemo=!GearReelingDemo;dirty=true;},Teal).GetComponentInChildren<UnityEngine.UI.Text>().fontSize=Compact?18:22;
            Button(pageRoot,"返回钓具",12,Height-(Compact?86:124),130,44,()=>Navigate("钓具"),Navy).GetComponentInChildren<UnityEngine.UI.Text>().fontSize=18;
            Button(pageRoot,"旋转钓组",152,Height-(Compact?86:124),130,44,()=>GearYaw+=30,Navy).GetComponentInChildren<UnityEngine.UI.Text>().fontSize=18;
        }
    }
}

using UnityEngine;
namespace OceanAngler
{
    public sealed partial class GameUI
    {
        void BuildGearPreview()
        {
            float width=Mathf.Min(600,Width-24),cell=(width-18)/4,left=(Width-width)/2,top=Height-78;
            IconButton(pageRoot,"旋转钓组",TouchSymbol.Kind.Rotate,left,top,cell,66,()=>GearYaw+=30,32);
            IconButton(pageRoot,GearCloseup?"查看全竿":"近看卷线器",TouchSymbol.Kind.ZoomIn,left+cell+6,top,cell,66,()=>{GearCloseup=!GearCloseup;dirty=true;},32);
            IconButton(pageRoot,GearReelingDemo?"停止演示":"试摇卷线器",GearReelingDemo?TouchSymbol.Kind.Pause:TouchSymbol.Kind.Reel,left+2*(cell+6),top,cell,66,()=>{GearReelingDemo=!GearReelingDemo;dirty=true;},32);
            IconButton(pageRoot,"返回钓具",TouchSymbol.Kind.Back,left+3*(cell+6),top,cell,66,GoBack,32);
            var hint=Label(pageRoot,"摇动卷线器，看看鱼线。",17,Muted,112,46,-224,28);
            var outline=hint.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=Navy;outline.effectDistance=new Vector2(1,-1);
        }
    }
}

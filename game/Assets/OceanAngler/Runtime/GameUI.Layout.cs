using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace OceanAngler
{
    public sealed partial class GameUI
    {
        void BuildChrome()
        {
            var header=At(pageRoot,"Header",Navy,0,0,0,Compact?44:80);
            IconButton(header,"上一级",TouchSymbol.Kind.Back,8,0,92,Compact?44:70,GoBack,32);
            Label(header,page=="准备"?"出海准备":page,Compact?20:30,Color.white,100,Compact?6:14,260,Compact?32:46);
            pointsText=Label(header,game.Progress.RequiresRecovery?"存档需要恢复":"研究积分  "+game.Progress.Data.totalPoints,Compact?17:22,Gold,Compact?-260:-310,Compact?9:22,Compact?145:280,Compact?28:36,true);
            if(!game.Progress.RequiresRecovery)IconButton(header,"菜单",TouchSymbol.Kind.Home,-78,0,72,Compact?44:70,()=>Navigate("首页"),32);
            var nav=At(pageRoot,"Navigation",Navy,0,Compact?44:82,0,Compact?52:60);
            string[] names={"出海","图鉴","钓具","任务","设置"};float navWidth=Compact?(Width-24)/5:145;
            for(int i=0;i<names.Length;i++)
            {
                string target=names[i];var b=Button(nav,target,(Compact?12:16)+i*navWidth,Compact?4:7,navWidth-8,Compact?44:46,()=>Navigate(target),page==target||(page=="鱼类详情"||ObservationJournal)&&target=="图鉴"||InspectingGear&&target=="钓具"||InspectingHabitat&&target=="出海"?Teal:Panel);b.interactable=!game.Progress.RequiresRecovery;
                if(Compact)b.GetComponentInChildren<Text>().fontSize=18;
            }
            var footer=At(pageRoot,"Footer",Navy,0,0,0,Compact?30:58,true);
            noticeText=Label(footer,"",Compact?17:19,Muted,Compact?12:24,Compact?2:12,-(Compact?24:48),Compact?28:35);
        }
        RectTransform BodyPanel(string name)
        {
            return Box(pageRoot,name,Panel,Vector2.zero,Vector2.one,new Vector2(12,36),new Vector2(-12,-102));
        }
        RectTransform PagePanel(string name,Vector2 min,Vector2 max,float contentHeight)
        {
            var viewport=Compact?BodyPanel(name):Box(pageRoot,name,Panel,min,max,Vector2.zero,Vector2.zero);
            float available=Compact?Height-138:Height*(max.y-min.y);if(!Compact&&contentHeight<=available)return viewport;viewport.gameObject.AddComponent<RectMask2D>();
            var content=new GameObject("ScrollableContent",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(viewport,false);content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,contentHeight);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            return content;
        }
        void BuildFishCards(RectTransform parent,float width,int columns,float top)
        {
            float cell=(width-44)/columns;
            for(int i=0;i<game.Content.fish.Length;i++)
            {
                var f=game.Content.fish[i];bool caught=game.Progress.Data.catches.Any(c=>c.fishId==f.id);
                Button(parent,(caught?f.name:"未发现 · "+f.name),22+i%columns*cell,top+i/columns*62,cell-12,54,()=>{selectedFish=focusedFish=f.id;if(Compact)page="鱼类详情";dirty=true;},selectedFish==f.id?Teal:Navy).GetComponentInChildren<Text>().fontSize=Compact?18:23;
            }
        }
        string CompactHint()
        {
            if(game.Session.Paused)return "点继续探索后，游戏才会恢复。";
            switch(game.Session.Phase)
            {
                case FishingPhase.Ready:return "按住蓄力，松手抛竿。";
                case FishingPhase.Waiting:return "等咬钩，再点提竿。";
                case FishingPhase.Bite:return "鱼儿咬钩了，点提竿！";
                case FishingPhase.Fighting:return "按住收线，松手降张力。";
                case FishingPhase.Landed:return "记录后，放回海中。";
                case FishingPhase.Escaped:return "已有积分不减少，再试一次吧。";
                default:return "观察落水，寻找鱼群。";
            }
        }
        void BuildCompactJournal()
        {
            var content=PagePanel("Journal",Vector2.zero,Vector2.one,550);Label(content,"鱼类图鉴  "+game.Progress.Data.catches.Select(c=>c.fishId).Distinct().Count()+" / 12",22,Color.white,22,12,-44,34);
            ObservationEntry(content,56);
            BuildFishCards(content,Width-24,2,110);
        }
        void BuildCompactInspect()
        {
            var target=game.Content.Fish(selectedFish??game.Content.fish[0].id);
            Button(pageRoot,"返回图鉴",12,102,140,44,()=>{page="图鉴";dirty=true;},Navy).GetComponentInChildren<Text>().fontSize=18;
            float width=Mathf.Min(310,Width*.44f);
            var detail=Box(pageRoot,"Details",Panel,new Vector2(1,0),Vector2.one,new Vector2(-width-12,36),new Vector2(-12,-102));
            BuildFishDetails(detail,target);
            BuildJournalControls(12,Height-86,Width-width-42);
        }
    }
}

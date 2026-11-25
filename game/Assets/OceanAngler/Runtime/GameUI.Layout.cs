using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace OceanAngler
{
    public sealed partial class GameUI
    {
        float PageWidth=>Mathf.Min(960,Width-24);
        float PageTop=>Compact?56:80;
        bool ModelPage=>page=="鱼类详情"||InspectingGear||InspectingHabitat||InspectingObservation;
        void BuildChrome()
        {
            // Destinations live in the menu, not in a second global tab bar.
            // A model visit keeps its ocean backdrop and one local back route.
            if(!ModelPage)Box(pageRoot,"PageShade",new Color(.01f,.06f,.1f,.7f),Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var header=At(pageRoot,"Header",ModelPage?Color.clear:Navy,0,0,0,Compact?48:70);
            IconButton(header,"上一级",TouchSymbol.Kind.Back,12,0,92,44,GoBack,28);
            string title=page=="准备"?"出海准备":page=="海湾观察"?"海域观察":page=="鱼类详情"?game.Content.Fish(selectedFish??game.Content.fish[0].id).name:page=="观察生物"?game.Content.Observation(observationId).name:page;
            var heading=Label(header,title,Compact?21:26,Color.white,112,Compact?8:ModelPage?6:14,-224,Compact?36:44);
            if(ModelPage){var outline=heading.gameObject.AddComponent<Outline>();outline.effectColor=Navy;outline.effectDistance=new Vector2(1,-1);}
            if(!game.Progress.RequiresRecovery&&!ModelPage)IconButton(header,"菜单",TouchSymbol.Kind.Home,-84,0,72,44,()=>Navigate("首页"),28);
            // Gear feedback belongs to its own page. Other pages do not inherit
            // stale equipment notices or a permanent banner over the world.
            if(page=="钓具")
            {
                var feedback=At(pageRoot,"GearFeedback",Navy,12,0,12,38,true);
                noticeText=Label(feedback,"",17,Muted,12,6,-24,28);
            }
        }
        RectTransform BodyPanel(string name)
        {
            float bottom=page=="钓具"?44:12;
            return FixedBox(pageRoot,name,Panel,(Width-PageWidth)/2,PageTop,PageWidth,Height-PageTop-bottom);
        }
        RectTransform PagePanel(string name,Vector2 min,Vector2 max,float contentHeight)
        {
            var viewport=BodyPanel(name);
            float available=Height-PageTop-(page=="钓具"?44:12);
            if(contentHeight<=available)return viewport;
            viewport.gameObject.AddComponent<RectMask2D>();
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
                Button(parent,(caught?f.name:"未发现 · "+f.name),22+i%columns*cell,top+i/columns*62,cell-12,54,()=>{selectedFish=focusedFish=f.id;Navigate("鱼类详情");},selectedFish==f.id?Teal:Navy).GetComponentInChildren<Text>().fontSize=Compact?18:23;
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
            int columns=PageWidth>=760?3:2;
            var content=PagePanel("Journal",Vector2.zero,Vector2.one,110+Mathf.Ceil(game.Content.fish.Length/(float)columns)*62);
            Label(content,"鱼类图鉴  "+game.Progress.Data.catches.Select(c=>c.fishId).Distinct().Count()+" / 12",24,Color.white,22,12,-44,36);
            ObservationEntry(content,56);
            BuildFishCards(content,PageWidth,columns,110);
        }
        void BuildCompactInspect()
        {
            var status=Label(pageRoot,"开发版模型 · 还在制作",17,Muted,112,46,-224,28);status.name="ModelStatus";
            var outline=status.gameObject.AddComponent<Outline>();outline.effectColor=Navy;outline.effectDistance=new Vector2(1,-1);
            float controls=Mathf.Min(680,Width-24);
            BuildJournalControls((Width-controls)/2,Height-78,controls);
            BuildJournalModelViewport(12,80,Width-24,Height-88);
        }
        void BuildFishReading()
        {
            var panel=BodyPanel("SpeciesReading");BuildFishDetails(panel,game.Content.Fish(selectedFish??game.Content.fish[0].id));
        }
    }
}

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace OceanAngler
{
    public sealed partial class GameUI
    {
        bool preparation;
        void BuildChrome()
        {
            var header=At(pageRoot,"Header",Navy,0,0,0,Compact?44:80);
            Label(header,"海洋小钓手",Compact?20:30,Color.white,Compact?12:24,Compact?6:14,260,Compact?32:46);
            pointsText=Label(header,game.Progress.RequiresRecovery?"存档需要恢复":"研究积分  "+game.Progress.Data.totalPoints,Compact?17:22,Gold,Compact?-260:-310,Compact?9:22,Compact?145:280,Compact?28:36,true);
            if(Compact&&!game.Progress.RequiresRecovery)Button(header,(InspectingGear||InspectingHabitat)?"返回":game.Session.Paused?"继续":"暂停",-104,0,100,44,()=>{if(InspectingGear)Navigate("钓具");else if(InspectingHabitat)Navigate("出海");else game.Pause(!game.Session.Paused);},Panel);
            var nav=At(pageRoot,"Navigation",Navy,0,Compact?44:82,0,Compact?52:60);
            string[] names={"出海","图鉴","钓具","任务","设置"};float navWidth=Compact?(Width-24)/5:145;
            for(int i=0;i<names.Length;i++)
            {
                string target=names[i];var b=Button(nav,target,(Compact?12:16)+i*navWidth,Compact?4:7,navWidth-8,Compact?44:46,()=>Navigate(target),page==target||page=="鱼类详情"&&target=="图鉴"||InspectingGear&&target=="钓具"||InspectingHabitat&&target=="出海"?Teal:Panel);b.interactable=!game.Progress.RequiresRecovery;
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
        void BuildCompactFishing()
        {
            bool left=game.Progress.Data.settings.leftHanded;
            float cardWidth=Mathf.Min(330,Width*.47f),height=Height-140;
            var controls=Box(pageRoot,"Controls",Panel,new Vector2(left?0:1,0),new Vector2(left?0:1,1),new Vector2(left?12:-cardWidth-12,36),new Vector2(left?cardWidth+12:-12,-102));
            phaseText=Label(controls,"",20,Color.white,12,5,-24,32);
            hintText=Label(controls,"",17,Muted,12,39,-24,52);
            tensionText=Label(controls,"",17,Color.white,12,96,-24,28);tensionFill=CompactBar(controls,124,Teal);
            progressText=Label(controls,"",17,Color.white,12,137,-24,28);progressFill=CompactBar(controls,165,Gold);
            var action=Button(controls,ActionName(),12,178,-24,56,MainAction,Teal);actionText=action.GetComponentInChildren<Text>();actionText.fontSize=18;BindFishingInput(action);
            if(height<244)
            {
                bool veryShort=height<214;
                hintText.gameObject.SetActive(!veryShort);hintText.rectTransform.sizeDelta=new Vector2(-24,28);
                tensionText.rectTransform.anchoredPosition=new Vector2(12,veryShort?-39:-72);tensionFill.transform.parent.GetComponent<RectTransform>().anchoredPosition=new Vector2(12,veryShort?-67:-100);
                progressText.rectTransform.anchoredPosition=new Vector2(12,veryShort?-79:-114);progressFill.transform.parent.GetComponent<RectTransform>().anchoredPosition=new Vector2(12,veryShort?-107:-142);
                action.GetComponent<RectTransform>().anchoredPosition=new Vector2(12,veryShort?-121:-156);
            }
            float infoX=left?Width-190:12;
            Button(pageRoot,preparation?"收起准备":"海域与装备",infoX,102,178,44,()=>{preparation=!preparation;dirty=true;},Navy).GetComponentInChildren<Text>().fontSize=18;
            if(preparation)
            {
                float w=Width-cardWidth-38;
                var prep=Box(pageRoot,"Preparation",Panel,new Vector2(left?1:0,0),new Vector2(left?1:0,1),new Vector2(left?-w-12:12,36),new Vector2(left?-12:w+12,-152));
                prep.gameObject.AddComponent<RectMask2D>();var content=new GameObject("PreparationContent",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(prep,false);content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,366);
                var scroll=prep.gameObject.AddComponent<ScrollRect>();scroll.viewport=prep;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
                Label(content,game.Content.Habitat(game.Progress.Data.habitatId).name,22,Color.white,12,6,-24,34);
                for(int i=0;i<game.Content.habitats.Length;i++)
                {
                    var h=game.Content.habitats[i];bool unlocked=game.Progress.Data.totalPoints>=h.unlockPoints;
                    var b=Button(content,h.name+(unlocked?"":" · "+h.unlockPoints+"分"),12,48+i*52,-24,46,()=>game.SelectHabitat(h.id),h.id==game.Progress.Data.habitatId?Teal:Navy);b.GetComponentInChildren<Text>().fontSize=17;b.interactable=unlocked&&game.Session.Phase==FishingPhase.Ready;
                }
                Label(content,"已装备\n"+game.Content.Gear(game.Progress.Data.rodId).name+" · "+game.Content.Gear(game.Progress.Data.reelId).name+"\n"+game.Content.Gear(game.Progress.Data.lineId).name+" · "+game.Content.Gear(game.Progress.Data.lureId).name,17,Muted,12,212,-24,86);
                if(game.Progress.Data.habitatId=="bay")Button(content,"看看海湾",12,306,-24,44,()=>Navigate("海湾观察"),Navy).GetComponentInChildren<Text>().fontSize=18;
            }
            if(game.Session.Paused)Button(pageRoot,"结束本次 · 返回准备",left?Width-252:12,Height-90,240,48,game.ReturnToPreparation,Navy).GetComponentInChildren<Text>().fontSize=17;
            if(game.Session.Phase==FishingPhase.Landed&&game.Session.Catch!=null)
            {
                var c=game.Session.Catch;Label(pageRoot,game.Content.Fish(c.fishId).name+"\n"+c.length.ToString("F1")+"厘米 · "+c.weight.ToString("F2")+"千克\n"+GentleResult(c),19,Color.white,left?Width*.50f:12,150,Width-cardWidth-38,84);
            }
        }
        Image CompactBar(RectTransform parent,float y,Color color)
        {
            var bg=FixedBox(parent,"Bar",Navy,12,y,-24,7);return Box(bg,"Fill",color,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).GetComponent<Image>();
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
                case FishingPhase.Ready:return Height<384?"按住蓄力，松手抛竿。":"按住蓄力，松手抛竿。轻点也能抛出。";
                case FishingPhase.Waiting:return "等咬钩提示出现，再点提竿。";
                case FishingPhase.Bite:return "鱼儿咬钩了，点提竿！";
                case FishingPhase.Fighting:return "按住收线，松手降张力。";
                case FishingPhase.Landed:return "记录尺寸，再把它放回海里。";
                case FishingPhase.Escaped:return "已有积分不减少，再试一次吧。";
                default:return "观察拟饵落水，寻找鱼群。";
            }
        }
        void BuildCompactJournal()
        {
            var content=PagePanel("Journal",Vector2.zero,Vector2.one,490);Label(content,"鱼类图鉴  "+game.Progress.Data.catches.Select(c=>c.fishId).Distinct().Count()+" / 12",22,Color.white,22,12,-44,34);
            BuildFishCards(content,Width-24,2,56);
        }
        void BuildCompactInspect()
        {
            var target=game.Content.Fish(selectedFish??game.Content.fish[0].id);var records=game.Progress.Data.catches.Where(c=>c.fishId==target.id).ToArray();
            Button(pageRoot,"返回图鉴",12,102,140,44,()=>{page="图鉴";dirty=true;},Navy).GetComponentInChildren<Text>().fontSize=18;
            float width=Mathf.Min(310,Width*.44f);
            var detail=Box(pageRoot,"Details",Panel,new Vector2(1,0),Vector2.one,new Vector2(-width-12,36),new Vector2(-12,-102));
            Label(detail,target.name,23,Color.white,12,8,-24,36);
            Label(detail,records.Length==0?"尚未发现，出海寻找它吧。":"已记录 "+records.Length+" 次\n最长 "+records.Max(c=>c.length).ToString("F1")+"厘米\n最重 "+records.Max(c=>c.weight).ToString("F2")+"千克",18,Gold,12,54,-24,90);
            Label(detail,"先观察鱼鳍和体型，再认识它的生活环境。",17,Muted,12,154,-24,76);
            string[] names={"旋转","放大","缩小",ModelPaused?"游动":"静止"};Action[] actions={()=>ModelYaw+=30,()=>ModelZoom=Mathf.Min(1.5f,ModelZoom+.15f),()=>ModelZoom=Mathf.Max(.7f,ModelZoom-.15f),()=>{ModelPaused=!ModelPaused;dirty=true;}};
            float buttonWidth=Mathf.Min(76,(Width-width-48)/4);
            for(int i=0;i<4;i++)Button(pageRoot,names[i],12+i*(buttonWidth+5),Height-86,buttonWidth,44,actions[i],Navy).GetComponentInChildren<Text>().fontSize=17;
        }
    }
}

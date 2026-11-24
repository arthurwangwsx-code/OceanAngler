using UnityEngine;
using UnityEngine.UI;
namespace OceanAngler
{
    public sealed partial class GameUI
    {
        float hintBegan;
        FishingPhase hintPhase;
        GameObject hintGroup;
        void GoBack()
        {
            string parent=page=="鱼类详情"||page=="保护观察"?"图鉴":page=="观察生物"?"保护观察":page=="钓组预览"?"钓具":page=="海湾观察"?"准备":page=="隐私许可"?"家长区":page=="家长区"||page=="家长验证"?"设置":"出海";
            Navigate(parent);
        }
        Button IconButton(Transform parent,string title,TouchSymbol.Kind symbol,float x,float y,float w,float h,System.Action click,float iconSize=52)
        {
            var button=Button(parent,title,x,y,w,h,click,Color.clear);button.name="Touch-"+symbol;button.GetComponent<Image>().raycastTarget=true;
            var mark=new GameObject("Symbol",typeof(RectTransform),typeof(TouchSymbol));var rt=mark.GetComponent<RectTransform>();rt.SetParent(button.transform,false);rt.anchorMin=rt.anchorMax=new Vector2(.5f,1);rt.pivot=new Vector2(.5f,1);rt.anchoredPosition=new Vector2(0,-2);rt.sizeDelta=new Vector2(iconSize,iconSize);
            var graphic=mark.GetComponent<TouchSymbol>();graphic.symbol=symbol;graphic.raycastTarget=false;button.targetGraphic=graphic;
            var text=button.GetComponentInChildren<Text>();text.fontSize=17;text.rectTransform.anchoredPosition=new Vector2(0,-iconSize-4);text.rectTransform.sizeDelta=new Vector2(0,h-iconSize-4);text.alignment=TextAnchor.UpperCenter;
            if(h<60)
            {
                rt.anchorMin=rt.anchorMax=new Vector2(0,.5f);rt.pivot=new Vector2(0,.5f);rt.anchoredPosition=new Vector2(0,0);rt.sizeDelta=new Vector2(28,28);
                text.rectTransform.anchoredPosition=new Vector2(29,0);text.rectTransform.sizeDelta=new Vector2(-29,h);text.alignment=TextAnchor.MiddleLeft;
            }
            var outline=text.gameObject.AddComponent<Outline>();outline.effectColor=new Color(0,.04f,.07f,.95f);outline.effectDistance=new Vector2(1,-1);return button;
        }
        RectTransform HudRegion(string name,float x,float y,float w,float h)
        {
            var region=FixedBox(pageRoot,name,Color.clear,x,y,w,h);region.gameObject.AddComponent<HudFootprint>();return region;
        }
        void BuildFishing()
        {
            bool left=game.Progress.Data.settings.leftHanded;bool fighting=game.Session.Phase==FishingPhase.Fighting;
            var pause=HudRegion("PauseHUD",left?Width-78:14,12,64,70);
            IconButton(pause,"暂停",TouchSymbol.Kind.Pause,0,0,64,70,()=>{game.Pause(true);Navigate("出海");},40);
            // Navigate("出海") normally retains pause. Opening the pause control
            // must freeze the simulation before replacing a held action.
            float actionWidth=112,actionX=left?14:Width-actionWidth-14;
            var actionRegion=HudRegion("ActionHUD",actionX,Height-128,actionWidth,114);
            var phase=game.Session.Phase;
            var symbol=phase==FishingPhase.Ready||phase==FishingPhase.Bite?TouchSymbol.Kind.Rod:phase==FishingPhase.Fighting?TouchSymbol.Kind.Reel:phase==FishingPhase.Landed?TouchSymbol.Kind.Release:phase==FishingPhase.Escaped?TouchSymbol.Kind.Play:TouchSymbol.Kind.Fish;
            var action=IconButton(actionRegion,ActionName(),symbol,0,0,actionWidth,114,MainAction,58);actionText=action.GetComponentInChildren<Text>();BindFishingInput(action);
            action.interactable=phase!=FishingPhase.Casting&&phase!=FishingPhase.Waiting;
            float hintWidth=Mathf.Min(210,Width-actionWidth-44);
            var hint=HudRegion("PhaseHUD",left?Width-hintWidth-14:14,Height-(fighting?132:78),hintWidth,66);hintGroup=hint.gameObject;
            phaseText=Label(hint,"",20,Color.white,0,0,-1,34);hintText=Label(hint,"",17,Color.white,0,38,-1,28);
            foreach(var t in new[]{phaseText,hintText}){var shadow=t.gameObject.AddComponent<Outline>();shadow.effectColor=new Color(.01f,.05f,.08f,.95f);shadow.effectDistance=new Vector2(1,-1);}
            if(fighting)
            {
                var meters=HudRegion("FightHUD",left?Width-242:14,Height-62,228,48);
                tensionText=Label(meters,"",17,Color.white,0,0,146,26);progressText=Label(meters,"",17,Color.white,154,0,74,26);
                var tension=FixedBox(meters,"TensionTrack",Navy,0,28,146,8);tensionFill=Box(tension,"Fill",Teal,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).GetComponent<Image>();
                // A stationary notch and the !/松手 label encode danger without
                // relying on red/green or a color-only boundary.
                FixedBox(tension,"WarningNotch",Color.white,115,-4,3,16);
                var progress=FixedBox(meters,"ProgressTrack",Navy,154,28,74,8);progressFill=Box(progress,"Fill",Gold,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).GetComponent<Image>();
            }
            if(phase==FishingPhase.Landed&&game.Session.Catch!=null)
            {
                var c=game.Session.Catch;var result=FixedBox(pageRoot,"CatchResult",new Color(.025f,.12f,.19f,.78f),left?Width-310:14,102,296,112);
                Label(result,game.Content.Fish(c.fishId).name+"\n"+c.length.ToString("F1")+"厘米 · "+c.weight.ToString("F2")+"千克\n"+GentleResult(c),19,Color.white,12,8,-24,98);
            }
            if(hintPhase!=phase){hintPhase=phase;hintBegan=Time.unscaledTime;}
        }
        void UpdateImmersiveHint()
        {
            if(hintGroup==null||!Playing)return;
            if(game.Session.Phase==FishingPhase.Ready&&game.Notice!=null&&game.Notice.StartsWith("已记录并放流")&&game.Progress.Data.catches.Count>0)
            {
                phaseText.text="已记录并放流";
                hintText.text="获得 "+game.Progress.Data.catches[game.Progress.Data.catches.Count-1].awardedPoints+" 研究积分";
            }
            if(game.RestReminder&&game.Session.Phase==FishingPhase.Ready)hintText.text="和家人休息一下吧。";
            bool quiet=game.Progress.Data.tutorialComplete&&(game.Session.Phase==FishingPhase.Ready||game.Session.Phase==FishingPhase.Waiting)&&Time.unscaledTime-hintBegan>6;
            hintGroup.SetActive(!quiet||game.RestReminder);
        }
        void BuildPauseMenu(bool home)
        {
            Box(pageRoot,"MenuShade",new Color(.01f,.06f,.1f,.56f),Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            float w=Mathf.Min(660,Width-24),h=Mathf.Min(334,Height-24),x=(Width-w)/2,y=(Height-h)/2;
            var panel=FixedBox(pageRoot,home?"HomeMenu":"PauseMenu",Panel,x,y,w,h);
            Label(panel,home?"海洋小钓手":"探索已暂停",26,Color.white,20,12,-40,40);
            Label(panel,game.Content.Habitat(game.Progress.Data.habitatId).name+" · 研究积分 "+game.Progress.Data.totalPoints,17,Muted,20,53,-40,28);
            float half=(w-52)/2;
            Button(panel,"继续探索",20,88,half,46,()=>{page="出海";focusedFish=null;game.Pause(false);dirty=false;Rebuild();},Teal);
            Button(panel,"海域与装备",32+half,88,half,46,()=>Navigate("准备"),Navy);
            string[] names={"出海","图鉴","钓具","任务","设置"};TouchSymbol.Kind[] icons={TouchSymbol.Kind.Rod,TouchSymbol.Kind.Book,TouchSymbol.Kind.Reel,TouchSymbol.Kind.Tasks,TouchSymbol.Kind.Settings};float cell=(w-32)/5;
            for(int i=0;i<names.Length;i++){string target=names[i];IconButton(panel,target,icons[i],16+i*cell,146,cell,82,()=>Navigate(target),48);}
            Button(panel,"结束本次 · 返回准备",20,240,half,44,()=>Navigate("退出确认"),Navy).GetComponentInChildren<Text>().fontSize=17;
            Button(panel,home?"返回暂停":"返回首页",32+half,240,half,44,()=>Navigate(home?"出海":"首页"),Navy).GetComponentInChildren<Text>().fontSize=17;
            Label(panel,"当前探索会保留，点继续才恢复。",17,Muted,20,294,-40,28);
        }
        void BuildExitConfirmation()
        {
            Box(pageRoot,"ExitShade",new Color(.01f,.06f,.1f,.68f),Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            float w=Mathf.Min(520,Width-24),h=276;var p=FixedBox(pageRoot,"ExitConfirmation",Panel,(Width-w)/2,(Height-h)/2,w,h);
            Label(p,"结束本次探索？",26,Color.white,22,18,-44,40);
            Label(p,game.Session.Catch!=null?"本次还没记录的鱼会放回海中。\n已有图鉴、记录和研究积分都会保留。":"已有图鉴、记录和研究积分都会保留。\n你可以在准备页再次出海。",18,Muted,22,72,-44,82);
            Button(p,"取消，继续留在这里",22,166,-44,44,()=>Navigate("出海"),Teal).GetComponentInChildren<Text>().fontSize=18;
            Button(p,"确认结束并返回准备",22,222,-44,44,()=>{game.ReturnToPreparation();Navigate("准备");},Navy).GetComponentInChildren<Text>().fontSize=18;
        }
        void BuildPreparation()
        {
            var p=PagePanel("Preparation",new Vector2(.1f,.12f),new Vector2(.9f,.81f),488);
            Label(p,"选择海域，准备好再出发",24,Color.white,22,12,-44,38);
            bool ready=game.Session.Phase==FishingPhase.Ready;
            for(int i=0;i<game.Content.habitats.Length;i++)
            {
                var habitat=game.Content.habitats[i];bool unlocked=game.Progress.Data.totalPoints>=habitat.unlockPoints;
                var b=Button(p,habitat.name+(unlocked?"":" · "+habitat.unlockPoints+"分"),22,62+i*52,-44,46,()=>game.SelectHabitat(habitat.id),habitat.id==game.Progress.Data.habitatId?Teal:Navy);b.GetComponentInChildren<Text>().fontSize=19;b.interactable=ready&&unlocked;
            }
            Label(p,"已装备\n"+game.Content.Gear(game.Progress.Data.rodId).name+" · "+game.Content.Gear(game.Progress.Data.reelId).name+"\n"+game.Content.Gear(game.Progress.Data.lineId).name+" · "+game.Content.Gear(game.Progress.Data.lureId).name,18,Muted,22,230,-44,82);
            Button(p,"更换钓具",22,320,-44,44,()=>Navigate("钓具"),Navy).GetComponentInChildren<Text>().fontSize=18;
            if(MarineHabitat.ResourceFor(game.Progress.Data.habitatId)!=null)Button(p,game.Progress.Data.habitatId=="bay"?"看看海湾":"看看海域",22,376,-44,44,()=>Navigate("海湾观察"),Navy).GetComponentInChildren<Text>().fontSize=18;
            Button(p,ready?"开始钓鱼":"返回当前探索",22,432,-44,44,()=>{page="出海";focusedFish=null;game.Pause(false);dirty=false;Rebuild();},Teal).GetComponentInChildren<Text>().fontSize=18;
        }
    }
}

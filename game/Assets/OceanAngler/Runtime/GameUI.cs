using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace OceanAngler
{
    public sealed class HoldInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Action<bool> changed;
        bool down;
        public void OnPointerDown(PointerEventData e) { down = true; changed?.Invoke(true); }
        public void OnPointerUp(PointerEventData e) { if (down) { down = false; changed?.Invoke(false); } }
        void OnDisable() { if (down) { down = false; changed?.Invoke(false); } }
    }
    public sealed class GameUI : MonoBehaviour
    {
        GameController game;
        RectTransform safe, pageRoot;
        Font font;
        Text phaseText, hintText, tensionText, progressText, actionText, noticeText, pointsText;
        Image tensionFill, progressFill;
        RectTransform actionRect;
        string page = "出海", gearTab = "rod", focusedFish;
        string selectedFish;
        bool dirty = true, charging, resetConfirm;
        float chargeStart;
        FishingPhase lastPhase;
        bool lastPause;
        int parentAnswer;
        public string FocusedFish => focusedFish;
        public float ModelYaw { get; private set; }
        public float ModelZoom { get; private set; } = 1;
        public bool ModelPaused { get; private set; }
        static readonly Color Navy = new Color(.025f,.12f,.19f,.94f), Panel = new Color(.035f,.18f,.25f,.94f), Muted = new Color(.65f,.81f,.82f), Gold = new Color(1,.78f,.35f), Teal = new Color(.17f,.77f,.71f);
        void Start()
        {
            game = GetComponent<GameController>(); font = Resources.Load<Font>("UIFont");
            if (font == null) throw new InvalidOperationException("UI font missing; run tools/prepare_font.py");
            var canvasObject = new GameObject("OceanUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1440,900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            safe = Box(canvasObject.transform, "SafeArea", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            game.Changed += Dirty;
            Rebuild();
        }
        void Dirty() { dirty = true; }
        void Update()
        {
            if (safe == null) return;
            var area = Screen.safeArea; safe.anchorMin = new Vector2(area.xMin/Screen.width,area.yMin/Screen.height); safe.anchorMax = new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);
            if (game.Session.Phase != lastPhase || game.Session.Paused != lastPause) dirty = true;
            if (dirty) { dirty = false; Rebuild(); }
            if (phaseText != null)
            {
                phaseText.text = PhaseName();
                hintText.text = Hint();
                tensionFill.rectTransform.anchorMax = new Vector2(game.Session.Tension,1); progressFill.rectTransform.anchorMax = new Vector2(game.Session.Progress,1);
                tensionFill.color = game.Session.Tension > .8f ? Gold : Teal;
                tensionText.text = "鱼线张力  " + Mathf.RoundToInt(game.Session.Tension*100) + "%" + (game.Session.Tension > .8f ? "  松手降张力" : "  安全");
                progressText.text = "收鱼进度  " + Mathf.RoundToInt(game.Session.Progress*100) + "%";
                if (charging && actionText != null) actionText.text = "松手抛竿  " + Mathf.RoundToInt(Mathf.Clamp01((Time.unscaledTime-chargeStart)/1.5f)*100) + "%";
            }
            if (noticeText != null) noticeText.text = game.Notice ?? (game.RestReminder ? "已经探索一会儿了，和家人休息一下吧。进度已保存。" : "鱼获记录后放流 · 不消耗鱼饵 · 随时可以暂停");
        }
        void Rebuild()
        {
            if (pageRoot != null) { pageRoot.gameObject.SetActive(false); Destroy(pageRoot.gameObject); }
            pageRoot = Box(safe,"Page",Color.clear,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            phaseText = hintText = tensionText = progressText = actionText = null; charging = false;
            lastPhase = game.Session.Phase; lastPause = game.Session.Paused;
            var header = At(pageRoot,"Header",Navy,0,0,0,80);
            Label(header,"海洋小钓手",30,Color.white,24,14,260,46);
            pointsText = Label(header,game.Progress.RequiresRecovery ? "存档需要恢复" : "研究积分  " + game.Progress.Data.totalPoints,22,Gold,-310,22,280,36,true);
            var nav = At(pageRoot,"Navigation",Navy,0,82,0,60);
            string[] names = { "出海", "图鉴", "钓具", "任务", "设置" };
            for (int i=0;i<names.Length;i++)
            {
                string target=names[i]; var b=Button(nav,target,16+i*145,7,134,46,()=>Navigate(target),page==target?Teal:Panel);
                b.interactable = !game.Progress.RequiresRecovery;
            }
            var footer = At(pageRoot,"Footer",Navy,0,-58,0,58,true);
            noticeText=Label(footer,"",19,Muted,24,12,-48,35);
            if (game.Progress.RequiresRecovery) { BuildRecovery(); return; }
            switch(page)
            {
                case "出海": BuildFishing(); break;
                case "图鉴": BuildJournal(); break;
                case "钓具": BuildGear(); break;
                case "任务": BuildTasks(); break;
                case "设置": BuildSettings(); break;
                case "家长验证": BuildParentGate(); break;
                case "家长区": BuildParent(); break;
                case "隐私许可": BuildLegal(); break;
            }
        }
        void Navigate(string target)
        {
            game.HoldReel(false); charging=false;
            if (target != "出海") game.Pause(true);
            // 返回钓鱼页仍显示继续界面，避免导航触发自动输入。
            page=target; focusedFish=target=="图鉴"?(selectedFish??game.Content.fish[0].id):null; dirty=true;
        }
        void BuildFishing()
        {
            bool left = game.Progress.Data.settings.leftHanded;
            var info=Box(pageRoot,"Info",Panel,new Vector2(left?.69f:.02f,.12f),new Vector2(left?.98f:.31f,.81f),Vector2.zero,Vector2.zero);
            Label(info,"今日出海",19,Muted,20,16,-40,28);
            Label(info,game.Content.Habitat(game.Progress.Data.habitatId).name,30,Color.white,20,50,-40,50);
            Label(info,game.Content.Habitat(game.Progress.Data.habitatId).description,20,Muted,20,110,-40,65);
            Label(info,"选择研究海域",20,Gold,20,170,-40,30);
            for(int i=0;i<game.Content.habitats.Length;i++)
            {
                var h=game.Content.habitats[i];bool unlocked=game.Progress.Data.totalPoints>=h.unlockPoints;
                var b=Button(info,h.name+(unlocked?"":" · "+h.unlockPoints+"分"),20,208+i*52,-40,44,()=>game.SelectHabitat(h.id),h.id==game.Progress.Data.habitatId?Teal:Navy);
                b.interactable=unlocked && game.Session.Phase==FishingPhase.Ready;
            }
            Label(info,"已装备\n"+game.Content.Gear(game.Progress.Data.rodId).name+" · "+game.Content.Gear(game.Progress.Data.reelId).name+"\n"+game.Content.Gear(game.Progress.Data.lineId).name+" · "+game.Content.Gear(game.Progress.Data.lureId).name,19,Muted,20,380,-40,100);
            var controls=Box(pageRoot,"Controls",Panel,new Vector2(left?.02f:.69f,.12f),new Vector2(left?.31f:.98f,.81f),Vector2.zero,Vector2.zero);
            phaseText=Label(controls,"",32,Color.white,24,22,-48,48);
            hintText=Label(controls,"",21,Muted,24,86,-48,116);
            tensionText=Label(controls,"",20,Color.white,24,217,-48,32);tensionFill=Bar(controls,258,Teal);
            progressText=Label(controls,"",20,Color.white,24,288,-48,32);progressFill=Bar(controls,329,Gold);
            var action=Button(controls,ActionName(),24,385,-48,88,MainAction,Teal);actionText=action.GetComponentInChildren<Text>();actionRect=action.GetComponent<RectTransform>();
            if (!game.Session.Paused && game.Session.Phase==FishingPhase.Fighting)
            {
                action.onClick.RemoveAllListeners();action.gameObject.AddComponent<HoldInput>().changed=game.HoldReel;
            }
            if (!game.Session.Paused && game.Session.Phase==FishingPhase.Ready)
            {
                action.onClick.RemoveAllListeners();action.gameObject.AddComponent<HoldInput>().changed=down=>
                {
                    if (down) { charging=true;chargeStart=Time.unscaledTime; }
                    else if (charging) { charging=false;game.Cast(Mathf.Clamp((Time.unscaledTime-chargeStart)/1.5f,.3f,1)); }
                };
            }
            Button(controls,game.Session.Paused?"结束本次 · 返回准备":"暂停",24,487,-48,44,()=>{if(game.Session.Paused)game.ReturnToPreparation();else game.Pause(true);},Navy);
            if (game.Session.Phase==FishingPhase.Landed && game.Session.Catch!=null)
            {
                var c=game.Session.Catch;
                Label(pageRoot,game.Content.Fish(c.fishId).name+"\n"+c.length.ToString("F1")+"厘米 · "+c.weight.ToString("F2")+"千克",30,Color.white,0,190,0,95,false,TextAnchor.UpperCenter);
            }
        }
        void MainAction()
        {
            if(game.Session.Paused) { game.Pause(false);return; }
            switch(game.Session.Phase)
            {
                case FishingPhase.Bite:game.Strike();break;
                case FishingPhase.Landed:game.Release();break;
                case FishingPhase.Escaped:game.Retry();break;
            }
        }
        string PhaseName()
        {
            if(game.Session.Paused)return "已暂停";
            switch(game.Session.Phase) {case FishingPhase.Ready:return "准备好出海了吗";case FishingPhase.Casting:return "拟饵飞向海面";case FishingPhase.Waiting:return "耐心等待咬钩";case FishingPhase.Bite:return "鱼儿咬钩了！";case FishingPhase.Fighting:return "和鱼儿轻柔较量";case FishingPhase.Landed:return "发现海洋新朋友";default:return "鱼儿游走了";}
        }
        string Hint()
        {
            if(game.Session.Paused)return "进度已保留。准备好后点继续，游戏才会恢复。";
            switch(game.Session.Phase) {case FishingPhase.Ready:return "按住蓄力，松手抛竿。轻点也能抛出。首次探索有辅助陪伴。";case FishingPhase.Casting:return "观察拟饵落水，寻找鱼群。";case FishingPhase.Waiting:return "观察海面，等咬钩提示再提竿。";case FishingPhase.Bite:return "点一下提竿，让鱼线轻轻绷紧。";case FishingPhase.Fighting:return game.Session.Assisted?"按住收线，松手降张力。张力过高时，辅助会帮你松线。":"按住收线，松手降张力。看到警戒提示就及时松手。";case FishingPhase.Landed:return "记录体长和重量，再把它放回海里。积分会和记录一起保存。";default:return "已有积分不会减少，随时可以再试一次。";}
        }
        string ActionName() => game.Session.Paused?"继续探索":game.Session.Phase==FishingPhase.Ready?"按住蓄力 · 松手抛竿":game.Session.Phase==FishingPhase.Fighting?"按住收线":game.Session.Phase==FishingPhase.Bite?"提竿":game.Session.Phase==FishingPhase.Landed?"记录并放流":game.Session.Phase==FishingPhase.Escaped?"再试一次":"观察海面";
        void BuildJournal()
        {
            var panel=Box(pageRoot,"Journal",Panel,new Vector2(.02f,.12f),new Vector2(.60f,.81f),Vector2.zero,Vector2.zero);
            Label(panel,"鱼类图鉴  "+game.Progress.Data.catches.Select(c=>c.fishId).Distinct().Count()+" / 12",28,Color.white,22,16,-44,46);
            for(int i=0;i<game.Content.fish.Length;i++)
            {
                var f=game.Content.fish[i];var caught=game.Progress.Data.catches.Where(c=>c.fishId==f.id).ToArray();int index=i;
                Button(panel,(caught.Length>0?f.name:"未发现 · "+f.name),22+(i%2)*385,78+(i/2)*70,370,60,()=>{selectedFish=f.id;focusedFish=f.id;dirty=true;},selectedFish==f.id?Teal:Navy);
            }
            Button(pageRoot,"旋转",-490,410,100,44,()=>ModelYaw+=30,Navy);
            Button(pageRoot,"放大",-380,410,100,44,()=>ModelZoom=Mathf.Min(1.5f,ModelZoom+.15f),Navy);
            Button(pageRoot,"缩小",-270,410,100,44,()=>ModelZoom=Mathf.Max(.7f,ModelZoom-.15f),Navy);
            Button(pageRoot,ModelPaused?"游动":"静止",-160,410,100,44,()=>{ModelPaused=!ModelPaused;dirty=true;},Navy);
            var detail=Box(pageRoot,"FishDetails",Panel,new Vector2(.63f,.12f),new Vector2(.98f,.48f),Vector2.zero,Vector2.zero);
            var target=game.Content.Fish(selectedFish??game.Content.fish[0].id);var records=game.Progress.Data.catches.Where(c=>c.fishId==target.id).ToArray();
            Label(detail,target.name,30,Color.white,24,16,-48,46);
            Label(detail,records.Length==0?"尚未发现，出海寻找它吧。":"已记录 "+records.Length+" 次\n最长 "+records.Max(c=>c.length).ToString("F1")+"厘米 · 最重 "+records.Max(c=>c.weight).ToString("F2")+"千克",21,Gold,24,74,-48,80);
            Label(detail,target.note,19,Muted,24,165,-48,120);
        }
        void BuildGear()
        {
            var panel=Box(pageRoot,"Gear",Panel,new Vector2(.02f,.12f),new Vector2(.75f,.81f),Vector2.zero,Vector2.zero);
            string[] categories={"rod","reel","line","lure"};string[] labels={"鱼竿","卷线器","鱼线","拟饵"};
            for(int i=0;i<4;i++){string tab=categories[i];Button(panel,labels[i],22+i*180,18,166,48,()=>{gearTab=tab;dirty=true;},gearTab==tab?Teal:Navy);}
            var items=game.Content.gear.Where(g=>g.category==gearTab).ToArray();
            for(int i=0;i<items.Length;i++)
            {
                var g=items[i];bool unlocked=game.Progress.Data.totalPoints>=g.unlockPoints;
                bool equipped=new[]{game.Progress.Data.rodId,game.Progress.Data.reelId,game.Progress.Data.lineId,game.Progress.Data.lureId}.Contains(g.id);
                var b=Button(panel,g.name+(equipped?"  已装备":unlocked?"  装备":"  "+g.unlockPoints+"分解锁")+"\n"+g.description,22,90+i*72,-44,64,()=>game.SelectGear(g.id),equipped?Teal:Navy);
                b.interactable=unlocked&&game.Session.Phase==FishingPhase.Ready;b.GetComponentInChildren<Text>().fontSize=19;
            }
            Label(pageRoot,game.Session.Phase==FishingPhase.Ready?"解锁后永久可用\n不扣研究积分\n无损坏与消耗":"请先完成当前鱼获，\n或返回出海继续。\n钓鱼中不能更换装备。",24,Color.white,-315,245,290,160,true);
        }
        void BuildTasks()
        {
            var panel=Box(pageRoot,"Tasks",Panel,new Vector2(.02f,.12f),new Vector2(.98f,.81f),Vector2.zero,Vector2.zero);
            Label(panel,"探索手册  ·  "+game.Content.tasks.Count(t=>TaskProgress.Complete(t,game.Progress.Data))+" / 18",28,Color.white,22,14,-44,46);
            for(int i=0;i<game.Content.tasks.Length;i++)
            {
                var t=game.Content.tasks[i];bool done=TaskProgress.Complete(t,game.Progress.Data);int value=Math.Min(t.target,TaskProgress.Value(t,game.Progress.Data));
                var row=FixedBox(panel,"Task",done?new Color(.06f,.32f,.32f):Navy,22+(i%2)*670,75+(i/2)*51,652,44);
                Label(row,(done?"已完成  ":"探索中  ")+t.name+"    "+value+" / "+t.target,21,done?Teal:Color.white,14,5,-28,35);
            }
        }
        void BuildSettings()
        {
            var panel=Box(pageRoot,"Settings",Panel,new Vector2(.02f,.12f),new Vector2(.78f,.81f),Vector2.zero,Vector2.zero);
            Label(panel,"按自己的节奏探索",30,Color.white,24,18,-48,46);
            var s=game.Progress.Data.settings;
            Setting(panel,90,"初学辅助",s.assisted,()=>game.Configure(x=>x.assisted=!x.assisted));
            Setting(panel,158,"左手操作",s.leftHanded,()=>game.Configure(x=>x.leftHanded=!x.leftHanded));
            Setting(panel,226,"减弱动态效果",s.reducedMotion,()=>game.Configure(x=>x.reducedMotion=!x.reducedMotion));
            Setting(panel,294,"声音",s.sound,()=>game.Configure(x=>x.sound=!x.sound));
            Setting(panel,362,"咬钩触觉提示",s.haptics,()=>game.Configure(x=>x.haptics=!x.haptics));
            Label(panel,"离线保存 · 无广告 · 无内购\n设置即时保存。首次教学始终提供辅助。",22,Muted,24,430,-48,80);
            Button(panel,"家长区与隐私",24,520,330,52,()=>{parentAnswer=UnityEngine.Random.Range(4,9)+9;page="家长验证";dirty=true;},Navy);
        }
        void Setting(RectTransform parent,float y,string title,bool enabled,Action action) => Button(parent,title+"    "+(enabled?"已开启":"已关闭"),24,y,-48,54,action,enabled?new Color(.07f,.32f,.34f):Navy);
        void BuildParentGate()
        {
            var p=Box(pageRoot,"ParentGate",Panel,new Vector2(.2f,.22f),new Vector2(.8f,.75f),Vector2.zero,Vector2.zero);
            Label(p,"请让家长协助",30,Color.white,28,24,-56,46);
            Label(p,"9 + "+(parentAnswer-9)+" = ?",32,Gold,28,94,-56,52);
            int[] answers={parentAnswer-1,parentAnswer,parentAnswer+2};
            for(int i=0;i<3;i++){int answer=answers[i];Button(p,answer.ToString(),28+i*200,180,182,60,()=>{if(answer==parentAnswer){page="家长区";resetConfirm=false;}else{page="设置";}dirty=true;},Navy);}
            Button(p,"返回游戏",28,285,250,52,()=>Navigate("出海"),Navy);
        }
        void BuildParent()
        {
            var p=Box(pageRoot,"Parents",Panel,new Vector2(.06f,.16f),new Vector2(.94f,.81f),Vector2.zero,Vector2.zero);
            Label(p,"家长区",30,Color.white,28,18,-56,46);
            Label(p,"游戏进度仅保存在此设备，无账号、广告、内购或第三方分析。\n技术支持、反馈与隐私请求：arthur.wang.wsx@gmail.com\n模型和科普内容仍在制作审核，当前为开发测试版本。\n字体：Noto Sans SC（SIL Open Font License 1.1）。",23,Muted,28,90,-56,160);
            Button(p,"查看隐私与字体许可",28,270,420,54,()=>{page="隐私许可";dirty=true;},Navy);
            Button(p,"联系技术支持",470,270,360,54,()=>Application.OpenURL("mailto:arthur.wang.wsx@gmail.com?subject=OceanAngler%20Feedback"),Navy);
            Button(p,resetConfirm?"再次确认：建立新进度（保留原文件）":"清除游戏进度",28,350,660,56,()=>{if(resetConfirm){game.ResetProgress();resetConfirm=false;page="出海";}else resetConfirm=true;dirty=true;},resetConfirm?new Color(.45f,.21f,.16f):Navy);
            Button(p,"取消并返回设置",28,430,420,52,()=>{resetConfirm=false;Navigate("设置");},Navy);
        }
        void BuildLegal()
        {
            var p=Box(pageRoot,"Legal",Panel,new Vector2(.04f,.12f),new Vector2(.96f,.81f),Vector2.zero,Vector2.zero);
            Button(p,"返回家长区",22,16,260,48,()=>{page="家长区";dirty=true;},Navy);
            var viewport=Box(p,"Scroll",Navy,new Vector2(.02f,.04f),new Vector2(.98f,.84f),Vector2.zero,Vector2.zero);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content=new GameObject("Content",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(viewport,false);content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,2200);
            string license=Resources.Load<TextAsset>("Legal/NotoSansSC-OFL")?.text??"字体许可暂时无法读取，请联系支持。";
            var text=Label(content,"隐私说明\n本游戏离线运行，游戏进度只保存在本设备。无账号、广告、内购或第三方分析。不请求位置、相机或麦克风权限。清除进度须在家长区二次确认。\n支持与隐私请求：arthur.wang.wsx@gmail.com\n\n字体许可\n"+license,21,Muted,20,12,-40,2150);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
        }
        void BuildRecovery()
        {
            page="家长验证";
            var p=Box(pageRoot,"Recovery",Panel,new Vector2(.15f,.2f),new Vector2(.85f,.75f),Vector2.zero,Vector2.zero);
            Label(p,"需要家长帮助恢复存档",30,Color.white,28,24,-56,46);
            Label(p,game.Progress.LoadMessage,23,Muted,28,95,-56,100);
            Label(p,"9 + 7 = ?",28,Gold,28,210,-56,46);
            Button(p,"15",28,280,180,58,()=>{},Navy);
            Button(p,resetConfirm?"16 · 再次确认恢复":"16",228,280,470,58,()=>{if(resetConfirm){game.ResetProgress();page="出海";resetConfirm=false;}else resetConfirm=true;dirty=true;},Teal);
        }
        public void SelectJournalFish(string id) { selectedFish=focusedFish=id;dirty=true; }
        Image Bar(RectTransform parent,float y,Color color)
        {
            var bg=FixedBox(parent,"Bar",Navy,24,y,-48,12);
            var fill=Box(bg,"Fill",color,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).GetComponent<Image>();fill.type=Image.Type.Simple;return fill;
        }
        RectTransform At(Transform parent,string name,Color color,float left,float top,float right,float height,bool bottom=false)
        {
            var rt=Box(parent,name,color,new Vector2(0,bottom?0:1),new Vector2(1,bottom?0:1),new Vector2(left,bottom?0:-top-height),new Vector2(-right,bottom?height:-top));return rt;
        }
        RectTransform FixedBox(Transform parent,string name,Color color,float x,float y,float w,float h)
        {
            var rt=Box(parent,name,color,new Vector2(x<0?1:0,1),new Vector2(w<=0?1:x<0?1:0,1),Vector2.zero,Vector2.zero);
            rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);return rt;
        }
        RectTransform Box(Transform parent,string name,Color color,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image));var rt=obj.GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=offsetMin;rt.offsetMax=offsetMax;var image=obj.GetComponent<Image>();image.color=color;image.raycastTarget=color.a>.01f;return rt;
        }
        Text Label(Transform parent,string text,int size,Color color,float x,float y,float w,float h,bool right=false,TextAnchor align=TextAnchor.UpperLeft)
        {
            var obj=new GameObject("Label",typeof(RectTransform),typeof(Text));var rt=obj.GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=new Vector2(right?1:0,1);rt.anchorMax=new Vector2(w<=0?1:right?1:0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);
            var t=obj.GetComponent<Text>();t.font=font;t.fontSize=size;t.color=color;t.text=text;t.alignment=align;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;return t;
        }
        Button Button(Transform parent,string text,float x,float y,float w,float h,Action click,Color color)
        {
            var rt=FixedBox(parent,"Button",color,x,y,w,h);var b=rt.gameObject.AddComponent<Button>();b.targetGraphic=rt.GetComponent<Image>();b.onClick.AddListener(()=>click());
            var t=Label(rt,text,23,Color.white,12,4,-24,h-8,false,TextAnchor.MiddleCenter);var colors=b.colors;colors.disabledColor=new Color(.35f,.43f,.46f,.8f);b.colors=colors;return b;
        }
        void OnDestroy() { if(game!=null)game.Changed-=Dirty; }
    }
}

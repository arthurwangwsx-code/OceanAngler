using System;
using System.Collections.Generic;
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
    public sealed partial class GameUI : MonoBehaviour
    {
        GameController game;
        RectTransform safe, pageRoot;
        Font font;
        Text phaseText, hintText, tensionText, progressText, actionText, noticeText, pointsText;
        Image tensionFill, progressFill;
        Canvas canvas;
        public bool Compact { get; private set; }
        public string PageName => page;
        public bool Playing => page=="出海"&&!game.Session.Paused;
        public bool CompactViewer => page=="鱼类详情";
        float Width => DisplayMetrics.SafeArea.width/DisplayMetrics.PixelsPerPoint;
        float Height => DisplayMetrics.SafeArea.height/DisplayMetrics.PixelsPerPoint;
        string page = "首页", gearTab = "rod", focusedFish;
        string selectedFish;
        readonly Stack<string> pageTrail=new Stack<string>();
        ScrollRect gearScroll;
        string renderedGearTab;
        readonly Dictionary<string,float> gearScrollPositions=new Dictionary<string,float>();
        bool dirty = true, charging, resetConfirm;
        float chargeStart;
        Vector2 lastSafeSize;
        FishingPhase lastPhase;
        bool lastPause;
        int parentAnswer;
        public string FocusedFish => focusedFish;
        public float ModelYaw { get; private set; }
        public float ModelZoom { get; private set; } = 1;
        public bool ModelPaused { get; private set; }
        public bool InspectingGear=>page=="钓组预览";
        public bool GearCloseup {get;private set;}
        public bool GearReelingDemo {get;private set;}
        public float GearYaw {get;private set;}
        static readonly Color Navy = new Color(.025f,.12f,.19f,.94f), Panel = new Color(.035f,.18f,.25f,.94f), Muted = new Color(.65f,.81f,.82f), Gold = new Color(1,.78f,.35f), Teal = new Color(.17f,.77f,.71f);
        void Start()
        {
            game = GetComponent<GameController>(); font = Resources.Load<Font>("UIFont");
            if (font == null) throw new InvalidOperationException("UI font missing; run tools/prepare_font.py");
            var canvasObject = new GameObject("OceanUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor=DisplayMetrics.PixelsPerPoint;
            Compact=Height<600;
            safe = Box(canvasObject.transform, "SafeArea", Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            game.Changed += Dirty;
            game.Pause(true);
            Rebuild();
        }
        void Dirty() { dirty = true; }
        void Update()
        {
            if (safe == null) return;
            canvas.scaleFactor=DisplayMetrics.PixelsPerPoint;
            bool compact=Height<600;if(compact!=Compact){Compact=compact;dirty=true;}
            var size=new Vector2(Width,Height);if(Vector2.Distance(size,lastSafeSize)>.5f){lastSafeSize=size;dirty=true;}
            var area = DisplayMetrics.SafeArea; safe.anchorMin = new Vector2(area.xMin/Screen.width,area.yMin/Screen.height); safe.anchorMax = new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);
            if (game.Session.Phase != lastPhase || game.Session.Paused != lastPause) dirty = true;
            if (dirty) { dirty = false; Rebuild(); }
            if (phaseText != null)
            {
                phaseText.text = PhaseName();
                hintText.text = game.Session.Phase==FishingPhase.Fighting&&game.Session.Tension>.8f?"！张力过高，请松手。":CompactHint();
                if(tensionFill!=null)
                {
                    tensionFill.rectTransform.anchorMax = new Vector2(game.Session.Tension,1); progressFill.rectTransform.anchorMax = new Vector2(game.Session.Progress,1);
                    tensionFill.color = game.Session.Tension > .8f ? Gold : Teal;
                    tensionText.text = "张力  " + Mathf.RoundToInt(game.Session.Tension*100) + "%" + (game.Session.Tension > .8f ? "  ！" : "  安全");
                    progressText.text = "收鱼  " + Mathf.RoundToInt(game.Session.Progress*100) + "%";
                }
                if (charging && actionText != null) actionText.text = "松手抛竿  " + Mathf.RoundToInt(Mathf.Clamp01((Time.unscaledTime-chargeStart)/1.5f)*100) + "%";
                UpdateImmersiveHint();
            }
            UpdateObservationUI();
            if (noticeText != null) noticeText.text = InspectingObservation?game.Notice??"安静观察，不投饵、不挂钩。":InspectingGear?"转动钓组，看看收线过程。":InspectingHabitat?"观察时暂停钓鱼，返回后点继续。":game.Notice ?? (page=="钓具"?"选择已解锁的钓具，研究积分不会扣除。":game.RestReminder ? (Compact?"探索一会儿了，和家人休息一下吧。":"已经探索一会儿了，和家人休息一下吧。进度已保存。") : (Compact?"记录后放流 · 随时暂停":"鱼获记录后放流 · 不消耗鱼饵 · 随时可以暂停"));
        }
        void Rebuild()
        {
            charging=false;
            if(gearScroll!=null)gearScrollPositions[renderedGearTab]=gearScroll.verticalNormalizedPosition;
            gearScroll=null;
            if (pageRoot != null) { pageRoot.gameObject.SetActive(false); Destroy(pageRoot.gameObject); }
            pageRoot = Box(safe,"Page",Color.clear,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            phaseText = hintText = tensionText = progressText = actionText = noticeText = pointsText = null; tensionFill=progressFill=null;charging = false;
            observationRecord=null;observationStatus=observationPlayback=null;
            lastPhase = game.Session.Phase; lastPause = game.Session.Paused;
            if(!game.Progress.RequiresRecovery&&page=="出海")
            {
                if(game.Session.Paused)BuildPauseMenu(false);else BuildFishing();return;
            }
            if(!game.Progress.RequiresRecovery&&page=="首页"){BuildPauseMenu(true);return;}
            if(!game.Progress.RequiresRecovery&&page=="退出确认"){BuildExitConfirmation();return;}
            BuildChrome();
            if (game.Progress.RequiresRecovery) { BuildRecovery(); return; }
            switch(page)
            {
                case "准备": BuildPreparation(); break;
                case "图鉴": BuildJournal(); break;
                case "保护观察": BuildObservations(); break;
                case "观察生物": BuildObservationVisit(); break;
                case "鱼类详情": BuildCompactInspect(); break;
                case "鱼类资料": BuildFishReading(); break;
                case "钓具": BuildGear(); break;
                case "钓组预览": BuildGearPreview(); break;
                case "海湾观察": BuildHabitatPreview(); break;
                case "任务": BuildTasks(); break;
                case "设置": BuildSettings(); break;
                case "操作与辅助": BuildSettingGroup("操作"); break;
                case "声音与触觉": BuildSettingGroup("声音"); break;
                case "画面与动态": BuildSettingGroup("画面"); break;
                case "观察资料": BuildObservationReading(); break;
                case "家长验证": BuildParentGate(); break;
                case "家长区": BuildParent(); break;
                case "开发者模式": BuildDeveloperMode(); break;
                case "切换模式确认": BuildDeveloperConfirmation(false); break;
                case "重建测试确认": BuildDeveloperConfirmation(true); break;
                case "隐私许可": BuildLegal(); break;
            }
            if(gearScroll!=null)
            {
                Canvas.ForceUpdateCanvases();gearScroll.StopMovement();
                gearScroll.verticalNormalizedPosition=gearScrollPositions.TryGetValue(gearTab,out float position)?position:1;
            }
        }
        void Navigate(string target)=>OpenPage(target,true);
        void OpenPage(string target,bool remember)
        {
            game.HoldReel(false);charging=false;
            if(target=="首页"||target=="出海")pageTrail.Clear();
            else if(remember&&page!=target)pageTrail.Push(page);
            if(target=="钓具")game.ClearNotice();
            if(target!="出海")game.Pause(true);
            page=target;focusedFish=target=="鱼类详情"?(selectedFish??game.Content.fish[0].id):null;
            if(target!="钓组预览")GearReelingDemo=false;
            // Disable the old tree in the event frame before a release can
            // propagate into a hidden hold control.
            dirty=false;Rebuild();
        }
        void BindFishingInput(Button action)
        {
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
            switch(game.Session.Phase) {case FishingPhase.Ready:return "按住蓄力，松手抛竿。轻点也能抛出。首次探索有辅助陪伴。";case FishingPhase.Casting:return "观察拟饵落水，寻找鱼群。";case FishingPhase.Waiting:return "观察拟饵，等咬钩提示再提竿。";case FishingPhase.Bite:return "点一下提竿，让鱼线轻轻绷紧。";case FishingPhase.Fighting:return game.Session.Assisted?"按住收线，松手降张力。张力过高时，辅助会帮你松线。":"按住收线，松手降张力。看到警戒提示就及时松手。";case FishingPhase.Landed:return "记录体长和重量，再把它放回海里。积分会和记录一起保存。";default:return "已有积分不会减少，随时可以再试一次。";}
        }
        string ActionName() => game.Session.Paused?"继续探索":game.Session.Phase==FishingPhase.Ready?"按住蓄力 · 松手抛竿":game.Session.Phase==FishingPhase.Fighting?"按住收线":game.Session.Phase==FishingPhase.Bite?"提竿":game.Session.Phase==FishingPhase.Landed?"记录并放流":game.Session.Phase==FishingPhase.Escaped?"再试一次":game.Session.Phase==FishingPhase.Casting?"等待落水":"等待咬钩";
        void BuildJournal()=>BuildCompactJournal();
        void BuildGear()
        {
            var panel=PagePanel("Gear",new Vector2(.02f,.12f),new Vector2(.75f,.81f),600);
            gearScroll=panel.GetComponentInParent<ScrollRect>();renderedGearTab=gearTab;
            string[] categories={"rod","reel","line","lure"};string[] labels={"鱼竿","卷线器","鱼线","拟饵"};
            float tabWidth=(PageWidth-44)/4;
            for(int i=0;i<4;i++){string tab=categories[i];Button(panel,labels[i],22+i*tabWidth,18,tabWidth-10,48,()=>{gearTab=tab;dirty=true;},gearTab==tab?Teal:Navy);}
            Button(panel,"观察入门钓组",22,78,-44,48,()=>Navigate("钓组预览"),Navy);
            var items=game.Content.gear.Where(g=>g.category==gearTab).ToArray();
            for(int i=0;i<items.Length;i++)
            {
                var g=items[i];bool unlocked=game.Progress.IsUnlocked(g.unlockPoints);
                bool equipped=new[]{game.Progress.Data.rodId,game.Progress.Data.reelId,game.Progress.Data.lineId,game.Progress.Data.lureId}.Contains(g.id);
                var b=Button(panel,g.name+(equipped?"  已装备":unlocked?"  装备":"  "+g.unlockPoints+"分解锁")+"\n"+g.description,22,142+i*72,-44,64,()=>game.SelectGear(g.id),equipped?Teal:Navy);
                b.interactable=unlocked&&game.Session.Phase==FishingPhase.Ready;b.GetComponentInChildren<Text>().fontSize=19;
            }
        }
        void BuildTasks()
        {
            float available=PageWidth-44;int columns=available<600?1:2;float cellWidth=available/columns;
            var panel=PagePanel("Tasks",new Vector2(.02f,.12f),new Vector2(.98f,.81f),Mathf.Max(600,88+Mathf.Ceil(game.Content.tasks.Length/(float)columns)*136));
            Label(panel,"探索手册  ·  "+game.Content.tasks.Count(t=>TaskProgress.Complete(t,game.Progress.Data))+" / 18",28,Color.white,22,14,-44,46);
            for(int i=0;i<game.Content.tasks.Length;i++)
            {
                var t=game.Content.tasks[i];bool done=TaskProgress.Complete(t,game.Progress.Data);int value=Math.Min(t.target,TaskProgress.Value(t,game.Progress.Data));
                var row=FixedBox(panel,"Task",done?new Color(.06f,.32f,.32f):Navy,22+(i%columns)*cellWidth,75+(i/columns)*136,cellWidth-18,128);
                Label(row,(done?"已完成  ":"探索中  ")+t.name,cellWidth<340?18:21,done?Teal:Color.white,14,5,-28,32);
                Label(row,TaskProgress.GoalText(t,game.Content),17,Muted,14,40,-28,52);
                Label(row,"进度  "+value+" / "+t.target,18,done?Teal:Gold,14,96,-28,28);
            }
        }
        static string GentleResult(CatchRecord record)=>TaskProgress.Gentle(record)?"轻柔控线已达标":"轻柔控线，下次再试";
        void BuildSettings()
        {
            int columns=PageWidth>=500?2:1;
            var panel=PagePanel("Settings",Vector2.zero,Vector2.one,columns==2?278:408);
            Label(panel,"按自己的节奏探索",24,Color.white,22,12,-44,36);
            Label(panel,"选择一项，再调整对应设置。",17,Muted,22,52,-44,28);
            string[] labels={"操作与辅助","声音与触觉","画面与动态","家长区与隐私"};
            TouchSymbol.Kind[] icons={TouchSymbol.Kind.Rod,TouchSymbol.Kind.Sound,TouchSymbol.Kind.Map,TouchSymbol.Kind.Settings};
            float cell=(PageWidth-44)/columns;
            for(int i=0;i<labels.Length;i++)
            {
                string target=labels[i];
                IconButton(panel,target,icons[i],22+i%columns*cell,92+i/columns*66,cell-12,54,()=>
                {
                    if(target=="家长区与隐私"){parentAnswer=UnityEngine.Random.Range(4,9)+9;Navigate("家长验证");}
                    else Navigate(target);
                },28);
            }
            Label(panel,"无广告 · 无内购 · 设置即时保存",17,Muted,22,columns==2?238:370,-44,30);
        }
        void BuildSettingGroup(string group)
        {
            var panel=PagePanel("SettingGroup",Vector2.zero,Vector2.one,group=="声音"?272:278);
            Label(panel,group=="操作"?"找到顺手的玩法":group=="声音"?"听见海洋的声音":"用舒服的画面探索",24,Color.white,24,14,-48,36);
            var s=game.Progress.Data.settings;
            if(group=="操作")
            {
                Setting(panel,70,"初学辅助",s.assisted,()=>game.Configure(x=>x.assisted=!x.assisted));
                Setting(panel,138,"左手操作",s.leftHanded,()=>game.Configure(x=>x.leftHanded=!x.leftHanded));
                Label(panel,"辅助会在张力过高时帮忙松线。",18,Muted,24,212,-48,54);
            }
            else if(group=="声音")
            {
                Setting(panel,70,"环境与提示音",s.sound,()=>game.Configure(x=>x.sound=!x.sound));
                Setting(panel,138,"探索音乐",s.music,()=>game.Configure(x=>x.music=!x.music));
                Setting(panel,206,"咬钩触觉提示",s.haptics,()=>game.Configure(x=>x.haptics=!x.haptics));
            }
            else
            {
                Setting(panel,70,"减弱动态效果",s.reducedMotion,()=>game.Configure(x=>x.reducedMotion=!x.reducedMotion));
                Setting(panel,138,"固定水下镜头",s.underwaterOnly,()=>game.Configure(x=>x.underwaterOnly=!x.underwaterOnly));
                Label(panel,"减弱动态时镜头也固定在水下。",18,Muted,24,212,-48,54);
            }
        }
        void Setting(RectTransform parent,float y,string title,bool enabled,Action action) => Button(parent,title+"    "+(enabled?"已开启":"已关闭"),24,y,-48,54,action,enabled?new Color(.07f,.32f,.34f):Navy);
        void BuildParentGate()
        {
            var p=PagePanel("ParentGate",new Vector2(.2f,.22f),new Vector2(.8f,.75f),380);
            Label(p,"请让家长协助",30,Color.white,28,24,-56,46);
            Label(p,"9 + "+(parentAnswer-9)+" = ?",32,Gold,28,94,-56,52);
            int[] answers={parentAnswer-1,parentAnswer,parentAnswer+2};
            for(int i=0;i<3;i++){int answer=answers[i];Button(p,answer.ToString(),28+i*((PageWidth)-56)/3,180,((PageWidth)-76)/3,60,()=>{if(answer==parentAnswer){resetConfirm=false;OpenPage("家长区",false);}else GoBack();},Navy);}
            Button(p,"返回游戏",28,285,-56,52,()=>Navigate("出海"),Navy);
        }
        void BuildParent()
        {
            var p=PagePanel("Parents",new Vector2(.06f,.16f),new Vector2(.94f,.81f),640);
            Label(p,"家长区",30,Color.white,28,18,-56,46);
            Label(p,"游戏进度仅保存在此设备，无账号、广告、内购或第三方分析。\n技术支持、反馈与隐私请求：arthur.wang.wsx@gmail.com\n模型和科普内容仍在制作审核，当前为开发测试版本。\n字体：Noto Sans SC（SIL Open Font License 1.1）。",Compact?17:23,Muted,28,90,-56,160);
            Button(p,game.DeveloperMode?"开发者模式 · 已开启":"开发者模式",28,270,-56,54,()=>{game.ClearNotice();Navigate("开发者模式");},game.DeveloperMode?Teal:Navy);
            Button(p,"查看隐私与字体许可",28,334,-56,54,()=>Navigate("隐私许可"),Navy);
            Button(p,"联系技术支持",28,398,-56,54,()=>Application.OpenURL("mailto:arthur.wang.wsx@gmail.com?subject=OceanAngler%20Feedback"),Navy);
            Button(p,resetConfirm?"再次确认：建立新进度（保留原文件）":game.DeveloperMode?"清除测试进度":"清除游戏进度",28,474,-56,56,()=>{if(resetConfirm){game.ResetProgress();resetConfirm=false;page="出海";}else resetConfirm=true;dirty=true;},resetConfirm?new Color(.45f,.21f,.16f):Navy);
            Button(p,"取消并返回设置",28,554,-56,52,()=>{resetConfirm=false;GoBack();},Navy);
        }
        void BuildLegal()
        {
            var p=BodyPanel("Legal");
            var viewport=Box(p,"Scroll",Navy,Vector2.zero,Vector2.one,new Vector2(12,12),new Vector2(-12,-12));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content=new GameObject("Content",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(viewport,false);content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.sizeDelta=new Vector2(0,2200);
            string license=Resources.Load<TextAsset>("Legal/NotoSansSC-OFL")?.text??"字体许可暂时无法读取，请联系支持。";
            var text=Label(content,"隐私说明\n本游戏离线运行，游戏进度只保存在本设备。无账号、广告、内购或第三方分析。不请求位置、相机或麦克风权限。清除进度须在家长区二次确认。\n支持与隐私请求：arthur.wang.wsx@gmail.com\n\n字体许可\n"+license,21,Muted,20,12,-40,2150);
            Canvas.ForceUpdateCanvases();float needed=text.preferredHeight;content.sizeDelta=new Vector2(0,needed+24);text.rectTransform.sizeDelta=new Vector2(-40,needed+4);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
        }
        void BuildRecovery()
        {
            page="家长验证";
            var p=PagePanel("Recovery",new Vector2(.15f,.2f),new Vector2(.85f,.75f),400);
            Label(p,"需要家长帮助恢复存档",30,Color.white,28,24,-56,46);
            Label(p,game.Progress.LoadMessage,23,Muted,28,95,-56,100);
            Label(p,"9 + 7 = ?",28,Gold,28,210,-56,46);
            Button(p,"15",28,280,180,58,()=>{},Navy);
            Button(p,resetConfirm?"16 · 再次确认恢复":"16",228,280,-256,58,()=>{if(resetConfirm){game.ResetProgress();page="出海";resetConfirm=false;}else resetConfirm=true;dirty=true;},Teal);
        }
        public void SelectJournalFish(string id) { selectedFish=id;if(page!="鱼类详情")Navigate("鱼类详情");else{focusedFish=id;dirty=true;} }
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

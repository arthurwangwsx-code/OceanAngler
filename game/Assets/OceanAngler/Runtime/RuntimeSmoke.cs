using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace OceanAngler
{
    public sealed class RuntimeSmoke : MonoBehaviour
    {
        [Serializable] sealed class LabelRender { public string page,text;public float requiredHeight,availableHeight;public int visibleCharacters,vertices; }
        [Serializable] sealed class JourneyRound { public int number,pointsBefore,pointsAfter;public string fishId,habitatId,mode;public float elapsedSeconds; }
        [Serializable] sealed class HitFailure { public string target;public string[] hitTargets;public Vector2 point,screen;public Rect button,viewport;public float scrollPosition,contentHeight;public bool culled,insideViewport; }
        [Serializable] sealed class Receipt { public string status="RUNNING",scope;public List<string> checks=new List<string>();public List<string> failures=new List<string>();public List<LabelRender> inspectedLabels=new List<LabelRender>();public List<JourneyRound> rounds=new List<JourneyRound>();public List<HitFailure> hitFailures=new List<HitFailure>();public int catches,points,reflectionPasses,reflectionWidth,reflectionHeight,requestedRounds,completedRounds,capturedFrames;public bool captureEnabled;public float pixelsPerPoint, safeWidthPoints, safeHeightPoints,reflectionVariance,elapsedSeconds; }
        GameController game;string folder;Receipt receipt;bool finished,journeyMode,journeyQuit;float began;
        IEnumerator Start()
        {
            var args=LaunchArguments.Get();int journey=Array.IndexOf(args,"--ocean-journey20");
            if(journey>=0&&journey+1<args.Length){yield return RunJourney20(args[journey+1],true,true);yield break;}
            int index=Array.IndexOf(args,"--ocean-smoke");if(index<0||index+1>=args.Length){if(Debug.isDebugBuild)Debug.Log("OCEAN_SMOKE_MODE_OFF");yield break;}
            folder=args[index+1];Directory.CreateDirectory(folder);receipt=new Receipt();began=Time.realtimeSinceStartup;game=GetComponent<GameController>();Application.runInBackground=true;
            Debug.Log("OCEAN_SMOKE_BEGIN");
            yield return new WaitForSecondsRealtime(1);
            if(game.Session.Paused){Click("继续探索");yield return null;yield return null;}
            var spinning=FindFirstObjectByType<AuthoredSpinningRig>();Require(spinning!=null,"authored-spinning-rig-loaded");
            if(spinning!=null){Require(spinning.guides.Length==9&&spinning.rodBones.Length==10,"rig-guides-and-bend-bones");Require(spinning.GetComponentsInChildren<LODGroup>(true).Length==1,"rig-single-lod-owner");Require(spinning.MaximumGuideLineError()<.0001f,"rig-line-through-guides");}
            var authored=GetComponent<OceanWorld>().TargetFish as AuthoredFishVisual;Require(authored!=null,"authored-bluefin-loaded");
            if(authored!=null)
            {
                var animator=authored.GetComponentInChildren<Animator>();var group=authored.GetComponent<LODGroup>();
                Require(group!=null&&group.lodCount==3&&authored.GetComponentsInChildren<LODGroup>(true).Length==1,"authored-three-lods-single-owner");
                Require(animator!=null&&new[]{"idle","swim","turn","burst","bite","hooked","release"}.All(s=>animator.HasState(0,Animator.StringToHash(s))),"authored-seven-motion-states");
                Require(authored.transform.InverseTransformPoint(authored.HookPosition).x<-.9f,"authored-hook-at-mouth");
                var tail=authored.GetComponentsInChildren<Transform>().First(t=>t.name=="tail");var prior=tail.localRotation;
                yield return new WaitForSecondsRealtime(.37f);
                Require(Quaternion.Angle(prior,tail.localRotation)>.1f,"authored-swim-actually-deforms-tail");
                Require(authored.GetComponentsInChildren<SkinnedMeshRenderer>().All(r=>r.sharedMesh!=null&&r.sharedMaterials.All(m=>m!=null&&m.shader.name=="Universal Render Pipeline/Lit")),"authored-renderer-materials");
            }
            CheckLayout("fishing");yield return Snapshot("01-fishing");
            var bay=FindFirstObjectByType<BayHabitat>();Require(bay!=null,"authored-bay-loaded");
            var ui=GetComponent<GameUI>();
            if(ui.Compact)
            {
                Click("海域与装备",true);yield return null;yield return null;
                var preparation=FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).First(s=>s.name=="Preparation");preparation.verticalNormalizedPosition=0;yield return null;yield return null;
            }
            Click("看看海湾",true);yield return null;yield return null;CheckLayout("bay-surface");
            Require(ui.InspectingHabitat&&game.Session.Paused,"observation-pauses-fishing");Require(Camera.main.transform.position.y>bay.SeaLevel,"surface-camera-above-water");yield return Snapshot("07-bay-surface");
            Require(bay.reflection.Ready,"real-planar-reflection-ready");CaptureReflection(bay.reflection);
            int captures=bay.reflection.RenderCount;yield return new WaitForSecondsRealtime(.15f);Require(bay.reflection.RenderCount==captures,"static-view-reuses-reflection");
            Click("换个角度",true);yield return null;yield return null;CheckLayout("bay-pier");Require(bay.reflection.RenderCount>captures,"new-view-refreshes-reflection");yield return Snapshot("12-bay-pier");
            Click("换个角度",true);yield return null;yield return null;captures=bay.reflection.RenderCount;
            Click("潜入水下",true);yield return null;yield return null;CheckLayout("bay-underwater");Require(Camera.main.transform.position.y<bay.SeaLevel,"underwater-camera-below-water");yield return Snapshot("08-bay-underwater");
            Require(!bay.reflection.Ready&&bay.reflection.RenderCount==captures,"underwater-skips-reflection-render");
            Click("换个角度",true);yield return null;yield return null;Require(Vector3.Distance(Camera.main.transform.position,bay.reefCamera.position)<.01f,"reef-camera-actually-changes");yield return Snapshot("11-bay-reef");
            Click("返回钓鱼",true);yield return null;yield return null;Require(game.Session.Paused,"observation-return-keeps-pause");
            if(ui.Compact){Click("收起准备",true);yield return null;yield return null;}
            Click("继续探索",true);yield return null;yield return null;Require(Vector3.Distance(Camera.main.transform.position,bay.underwaterCamera.position)<.01f,"fishing-camera-restored");
            int before=game.Progress.Data.catches.Count;
            var cast=Find("按住蓄力");Pointer(cast,true);yield return new WaitForSecondsRealtime(.4f);Pointer(cast,false);
            float timeout=Time.realtimeSinceStartup+18;
            while(game.Session.Phase!=FishingPhase.Bite&&Time.realtimeSinceStartup<timeout)yield return null;
            Require(game.Session.Phase==FishingPhase.Bite,"cast-to-bite");
            yield return null;yield return null;
            Click("提竿");yield return null;yield return null;
            Require(game.Session.Phase==FishingPhase.Fighting,"strike-to-fighting");
            bool held=false;Button reel=Find("按住收线");timeout=Time.realtimeSinceStartup+60;
            while(game.Session.Phase==FishingPhase.Fighting&&Time.realtimeSinceStartup<timeout)
            {
                bool desired=game.Session.Tension<.65f;if(desired!=held){held=desired;Pointer(reel,held);}yield return null;
            }
            if(held)Pointer(reel,false);yield return null;yield return null;
            Require(game.Session.Phase==FishingPhase.Landed,"controlled-catch");CheckLayout("landed");yield return Snapshot("02-catch");
            if(spinning!=null){Require(spinning.CrankDegrees>0&&Mathf.Abs(spinning.RotorDegrees-spinning.CrankDegrees*5.2f)<.01f,"rig-crank-rotor-drive");Require(Mathf.Abs(spinning.SpoolStroke)<=.0061f,"rig-spool-stroke-bounded");Require(spinning.MaximumGuideLineError()<.0001f,"rig-loaded-line-through-guides");}
            Click("记录并放流");yield return null;yield return null;
            Require(game.Progress.Data.catches.Count==before+1&&game.Progress.Data.totalPoints>0,"release-and-persist");
            yield return new WaitForSecondsRealtime(.65f);yield return Snapshot("09-release");
            Click("图鉴",true);yield return null;yield return null;CheckLayout("journal");yield return Snapshot("03-journal");
            int journalPoints=game.Progress.Data.totalPoints;
            Click("未发现 · 大西洋庸鲽",true);yield return null;yield return null;CheckLayout("species-details");
            var details=FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Single(s=>s.content.name=="SpeciesContent");
            Require(details.content.GetComponentsInChildren<Text>().Any(t=>t.name=="ScientificName"&&t.text=="Hippoglossus hippoglossus"),"journal-full-scientific-name");
            yield return Snapshot("13-species-details");
            details.verticalNormalizedPosition=0;yield return null;yield return null;CheckLayout("species-habitat");
            var habitatText=details.content.GetComponentsInChildren<Text>().Single(t=>t.name=="NaturalHabitat");Require(habitatText.cachedTextGenerator.vertexCount>0,"journal-habitat-rendered-after-scroll");
            Require(game.Progress.Data.totalPoints==journalPoints&&game.Progress.Data.catches.Count==before+1,"journal-does-not-award-or-discover");
            yield return Snapshot("14-species-habitat");
            Click("钓具",true);yield return null;yield return null;CheckLayout("gear");
            Click("观察入门钓组",true);yield return null;yield return null;CheckLayout("gear-preview");Require(GetComponent<GameUI>().InspectingGear&&game.Session.Paused,"gear-view-pauses-fishing");
            Click("近看卷线器",true);yield return null;yield return null;CheckLayout("gear-closeup");
            var priorCrankRotation=spinning.crank.localRotation;var priorRotorRotation=spinning.rotor.localRotation;
            float spoolMin=float.PositiveInfinity,spoolMax=float.NegativeInfinity;Click("试摇卷线器",true);float demoEnd=Time.realtimeSinceStartup+.65f;
            while(Time.realtimeSinceStartup<demoEnd){float y=spinning.transform.InverseTransformPoint(spinning.spool.position).y;spoolMin=Mathf.Min(spoolMin,y);spoolMax=Mathf.Max(spoolMax,y);yield return null;}
            Require(Quaternion.Angle(priorCrankRotation,spinning.crank.localRotation)>5&&Quaternion.Angle(priorRotorRotation,spinning.rotor.localRotation)>5,"gear-preview-real-winding");Require(spoolMax-spoolMin>.0002f,"gear-preview-spool-actually-oscillates");yield return Snapshot("04-gear");
            Click("停止收线演示",true);yield return null;yield return null;var stopped=spinning.crank.localRotation;yield return new WaitForSecondsRealtime(.15f);Require(Quaternion.Angle(spinning.crank.localRotation,stopped)<.01f,"gear-demo-stops-on-release");
            Click("旋转钓组",true);yield return null;yield return null;Click("查看全竿",true);yield return null;yield return null;yield return Snapshot("07-gear-whole");
            Click("返回钓具",true);yield return null;yield return null;
            Click("任务",true);yield return null;yield return null;CheckLayout("tasks");yield return Snapshot("05-tasks");
            Click("设置",true);yield return null;yield return null;CheckLayout("settings");Click("左手操作");yield return null;yield return null;
            Require(game.Progress.Data.settings.leftHanded,"left-hand-setting");
            yield return Snapshot("10-settings");
            Click("家长区与隐私",true);yield return null;yield return null;CheckLayout("parent-gate");
            var question=FindObjectsByType<Text>(FindObjectsSortMode.None).First(t=>t.isActiveAndEnabled&&t.text.StartsWith("9 + ")).text;
            var match=System.Text.RegularExpressions.Regex.Match(question,@"9 \+ (\d+)");int answer=9+int.Parse(match.Groups[1].Value);
            Click(answer.ToString(),true);yield return null;yield return null;CheckLayout("parents");
            Click("查看隐私与字体许可",true);yield return null;yield return null;CheckLayout("legal");
            Click("返回家长区",true);yield return null;yield return null;
            Click("清除游戏进度",true);yield return null;yield return null;
            Require(game.Progress.Data.catches.Count==before+1,"reset-first-confirm-does-not-delete");
            Click("取消并返回设置",true);yield return null;yield return null;
            Require(game.Progress.Data.catches.Count==before+1,"cancel-reset-keeps-catches");
            Click("出海",true);yield return null;yield return null;Require(game.Session.Paused&&!game.Reeling,"navigation-clears-held-input");CheckLayout("left-handed");yield return Snapshot("06-left-handed");
            receipt.pixelsPerPoint=DisplayMetrics.PixelsPerPoint;receipt.safeWidthPoints=DisplayMetrics.SafeArea.width/DisplayMetrics.PixelsPerPoint;receipt.safeHeightPoints=DisplayMetrics.SafeArea.height/DisplayMetrics.PixelsPerPoint;
            receipt.reflectionPasses=bay.reflection.RenderCount;bay.reflection.enabled=false;yield return null;yield return null;Require(bay.reflection.Texture==null,"reflection-dispose-releases-target");
            receipt.catches=game.Progress.Data.catches.Count;receipt.points=game.Progress.Data.totalPoints;receipt.status=receipt.failures.Count==0?"PASS":"FAIL";finished=true;
            File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));Debug.Log("OCEAN_SMOKE_"+receipt.status);Application.Quit(receipt.failures.Count==0?0:1);
        }
        // The same EventSystem journey can run headlessly in PlayMode and with
        // real frames in a native player. Headless results never claim rendering.
        public IEnumerator RunJourney20(string outputFolder,bool captureFrames,bool quitProcess=false)
        {
            folder=Path.GetFullPath(outputFolder);Directory.CreateDirectory(folder);
            if(File.Exists(Path.Combine(folder,"receipt.json")))throw new InvalidOperationException("Use a new journey output folder");
            game=GetComponent<GameController>();began=Time.realtimeSinceStartup;finished=false;journeyMode=true;journeyQuit=quitProcess;
            receipt=new Receipt{requestedRounds=20,captureEnabled=captureFrames,scope=captureFrames?"native Player EventSystem journey; not OS touch or human play":"headless PlayMode EventSystem journey; no rendered-frame/device acceptance"};
            Application.runInBackground=true;
            var stack=new Stack<IEnumerator>();stack.Push(Journey20());
            while(stack.Count>0&&!finished)
            {
                object next=null;bool moved=false;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}
                catch(Exception e){failure=e;}
                if(failure!=null){receipt.failures.Add("journey-exception:"+failure.GetType().Name+":"+failure.Message);break;}
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(next is IEnumerator nested)stack.Push(nested);else yield return next;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
            if(!finished)FinishJourney();
        }
        void JourneyRequire(bool condition,string name)
        {
            Require(condition,name);if(!condition)throw new InvalidOperationException(name);
        }
        IEnumerator JourneyFrame(string name)
        {
            CheckLayout(name);CheckTouchTargets(name);
            if(receipt.captureEnabled){yield return Snapshot(name);receipt.capturedFrames++;}
            else {yield return null;yield return null;}
        }
        IEnumerator JourneyClick(string text,bool exact=false)
        {
            var button=Find(text,exact);
            // Scrolling changes clipping/culling in the next canvas update.
            // A callback before that update is not a valid visible UI tap.
            if(button.GetComponentInParent<ScrollRect>()!=null){yield return null;yield return null;}
            Click(text,exact);
        }
        IEnumerator Journey20()
        {
            var args=LaunchArguments.Get();int saveIndex=Array.IndexOf(args,"--ocean-save-root");
            JourneyRequire(saveIndex>=0&&saveIndex+1<args.Length,"journey-requires-explicit-isolated-save-root");
            string saveFile=Path.Combine(Path.GetFullPath(args[saveIndex+1]),"ocean-progress.json");
            JourneyRequire(!game.Progress.RequiresRecovery&&game.Progress.Data.catches.Count==0&&game.Progress.Data.totalPoints==0&&game.Progress.Data.observations.Count==0&&!File.Exists(saveFile)&&!File.Exists(saveFile+".bak")&&!File.Exists(saveFile+".tmp"),"journey-requires-fresh-save");
            if(receipt.captureEnabled)JourneyRequire(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null,"journey-capture-needs-graphics-device");
            yield return null;yield return null;
            var ui=GetComponent<GameUI>();
            for(int round=1;round<=20;round++)
            {
                float roundStart=Time.realtimeSinceStartup;
                if(round%4==1)
                {
                    yield return JourneyClick("钓具",true);yield return null;yield return null;
                    string[] categories={"rod","reel","line","lure"},labels={"鱼竿","卷线器","鱼线","拟饵"};
                    for(int i=0;i<categories.Length;i++)
                    {
                        yield return JourneyClick(labels[i],true);yield return null;yield return null;
                        var gear=game.Content.gear.Where(g=>g.category==categories[i]&&g.unlockPoints<=game.Progress.Data.totalPoints).OrderByDescending(g=>g.power).First();
                        yield return JourneyClick(gear.name);yield return null;yield return null;
                        JourneyRequire(new[]{game.Progress.Data.rodId,game.Progress.Data.reelId,game.Progress.Data.lineId,game.Progress.Data.lureId}.Contains(gear.id),"round-"+round+"-equip-"+categories[i]);
                    }
                }
                if(round==11){yield return JourneyClick("设置",true);yield return null;yield return null;yield return JourneyClick("初学辅助");yield return null;yield return null;JourneyRequire(!game.Progress.Data.settings.assisted,"journey-standard-mode-enabled");}
                yield return JourneyClick("出海",true);yield return null;yield return null;
                JourneyRequire(game.Session.Phase==FishingPhase.Ready&&!game.Reeling,"round-"+round+"-ready-with-no-held-input");
                if(ui.Compact){yield return JourneyClick("海域与装备",true);yield return null;yield return null;}
                var available=game.Content.habitats.Where(h=>h.unlockPoints<=game.Progress.Data.totalPoints).ToArray();
                var habitat=available.FirstOrDefault(h=>!receipt.rounds.Any(r=>r.habitatId==h.id))??available[(round-1)%available.Length];
                yield return JourneyClick(habitat.name,true);yield return null;yield return null;
                JourneyRequire(game.Progress.Data.habitatId==habitat.id,"round-"+round+"-selected-habitat");
                if(!receipt.rounds.Any(r=>r.habitatId==habitat.id)&&MarineHabitat.ResourceFor(habitat.id)!=null)
                {
                    yield return JourneyClick(habitat.id=="bay"?"看看海湾":"看看海域",true);yield return null;yield return null;
                    if(!ui.HabitatSurfaceView){yield return JourneyClick("回到水面",true);yield return null;yield return null;}
                    var active=GetComponent<OceanWorld>().ActiveHabitat;
                    JourneyRequire(active!=null&&game.Session.Paused&&Camera.main.transform.position.y>active.SeaLevel,"journey-"+habitat.id+"-surface");
                    yield return JourneyFrame("habitat-"+habitat.id+"-surface");
                    yield return JourneyClick("潜入水下",true);yield return null;yield return null;
                    JourneyRequire(Camera.main.transform.position.y<active.SeaLevel,"journey-"+habitat.id+"-underwater");
                    yield return JourneyFrame("habitat-"+habitat.id+"-underwater");
                    yield return JourneyClick("返回钓鱼",true);yield return null;yield return null;
                }
                if(ui.Compact){yield return JourneyClick("收起准备",true);yield return null;yield return null;}
                if(game.Session.Paused){yield return JourneyClick("继续探索",true);yield return null;yield return null;}
                int beforePoints=game.Progress.Data.totalPoints,beforeCount=game.Progress.Data.catches.Count;
                var cast=Find("按住蓄力");Pointer(cast,true);yield return new WaitForSecondsRealtime(.35f);Pointer(cast,false);
                float deadline=Time.realtimeSinceStartup+20;
                while(game.Session.Phase!=FishingPhase.Bite&&Time.realtimeSinceStartup<deadline)yield return null;
                JourneyRequire(game.Session.Phase==FishingPhase.Bite,"round-"+round+"-cast-to-bite");
                yield return null;yield return null;yield return JourneyClick("提竿",true);yield return null;yield return null;
                JourneyRequire(game.Session.Phase==FishingPhase.Fighting,"round-"+round+"-strike");
                var reel=Find("按住收线");bool held=false;
                if(round%5==0)
                {
                    Pointer(reel,true);yield return new WaitForSecondsRealtime(.2f);yield return JourneyClick("图鉴",true);yield return null;yield return null;
                    JourneyRequire(game.Session.Paused&&!game.Reeling,"round-"+round+"-navigation-cancels-held-reel");
                    float tension=game.Session.Tension,progress=game.Session.Progress;yield return new WaitForSecondsRealtime(.2f);
                    JourneyRequire(game.Session.Tension==tension&&game.Session.Progress==progress,"round-"+round+"-pause-freezes-fight");
                    yield return JourneyClick("出海",true);yield return null;yield return null;yield return JourneyClick("继续探索",true);yield return null;yield return null;reel=Find("按住收线");
                }
                deadline=Time.realtimeSinceStartup+75;
                while(game.Session.Phase==FishingPhase.Fighting&&Time.realtimeSinceStartup<deadline)
                {
                    bool desired=game.Session.Tension<.65f;
                    if(desired!=held){held=desired;Pointer(reel,held);}yield return null;
                }
                if(held)Pointer(reel,false);yield return null;yield return null;
                JourneyRequire(game.Session.Phase==FishingPhase.Landed,"round-"+round+"-landed");
                var caught=game.Session.Catch;
                if(round==1||round==10||round==20)yield return JourneyFrame("round-"+round.ToString("D2")+"-landed");
                yield return JourneyClick("记录并放流",true);yield return null;yield return null;
                JourneyRequire(game.Session.Phase==FishingPhase.Ready&&!game.Reeling&&game.Progress.Data.catches.Count==beforeCount+1,"round-"+round+"-released-and-ready");
                var loaded=new SaveStore(saveFile,game.Content).Load();
                JourneyRequire(!loaded.requiresRecovery&&loaded.data.catches.Count==round&&loaded.data.totalPoints==game.Progress.Data.totalPoints&&loaded.data.catches.Sum(c=>c.awardedPoints)==loaded.data.totalPoints&&loaded.data.catches.Select(c=>c.catchId).Distinct().Count()==round&&loaded.data.catches.All(c=>c.released)&&loaded.data.habitatId==habitat.id,"round-"+round+"-disk-ledger-matches");
                receipt.rounds.Add(new JourneyRound{number=round,fishId=caught.fishId,habitatId=habitat.id,mode=caught.mode,pointsBefore=beforePoints,pointsAfter=loaded.data.totalPoints,elapsedSeconds=Time.realtimeSinceStartup-roundStart});receipt.completedRounds=round;
                WriteJourney();
            }
            int points=game.Progress.Data.totalPoints;
            foreach(var species in game.Content.observations)
            {
                JourneyRequire(game.Content.Habitat(species.habitatId).unlockPoints<=points,"journey-natural-progression-unlocked-observation-"+species.id);
                yield return JourneyClick("图鉴",true);yield return null;yield return null;yield return JourneyClick("保护观察");yield return null;yield return null;yield return JourneyClick("观察"+species.name,true);yield return null;yield return null;
                yield return JourneyClick("安静观察",true);float deadline=Time.realtimeSinceStartup+8;
                while(!GetComponent<OceanWorld>().Observations.CanRecord&&Time.realtimeSinceStartup<deadline)yield return null;
                JourneyRequire(GetComponent<OceanWorld>().Observations.CanRecord,"journey-observation-ready-"+species.id);
                yield return JourneyFrame("observation-"+species.id);yield return JourneyClick("记入观察图鉴",true);yield return null;yield return null;
                var loaded=new SaveStore(saveFile,game.Content).Load();
                JourneyRequire(!loaded.requiresRecovery&&loaded.data.observations.Contains(species.id)&&loaded.data.totalPoints==points&&loaded.data.catches.Count==20,"journey-observation-persists-without-catch-points-"+species.id);
            }
            yield return JourneyClick("返回观察图鉴",true);yield return null;yield return null;
            JourneyRequire(ui.ObservationJournal&&!ui.InspectingObservation,"journey-observation-return-to-journal");
            yield return JourneyClick("观察鲸鲨",true);yield return null;yield return null;
            yield return JourneyClick("出海",true);yield return null;yield return null;
            JourneyRequire(receipt.completedRounds==20&&receipt.rounds.Any(r=>r.mode=="assisted")&&receipt.rounds.Any(r=>r.mode=="standard"),"journey-20-rounds-both-modes");
            JourneyRequire(game.Progress.Data.observations.Count==3,"journey-three-protected-observations");
            receipt.pixelsPerPoint=DisplayMetrics.PixelsPerPoint;receipt.safeWidthPoints=DisplayMetrics.SafeArea.width/DisplayMetrics.PixelsPerPoint;receipt.safeHeightPoints=DisplayMetrics.SafeArea.height/DisplayMetrics.PixelsPerPoint;
        }
        void WriteJourney()
        {
            receipt.catches=game.Progress.Data.catches.Count;receipt.points=game.Progress.Data.totalPoints;receipt.elapsedSeconds=Time.realtimeSinceStartup-began;
            File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));
        }
        void FinishJourney()
        {
            receipt.status=receipt.failures.Count==0?"PASS":"FAIL";finished=true;WriteJourney();Debug.Log("OCEAN_JOURNEY_"+receipt.status);
            if(journeyQuit)Application.Quit(receipt.failures.Count==0?0:1);
        }
        Button Find(string text,bool exact=false)
        {
            var button=FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.isActiveAndEnabled&&b.interactable&&(exact?b.GetComponentInChildren<Text>()?.text==text:b.GetComponentInChildren<Text>()?.text.StartsWith(text)==true));
            if(button==null)throw new InvalidOperationException("Missing button "+text);
            var scroll=button.GetComponentInParent<ScrollRect>();
            if(scroll!=null&&scroll.content.rect.height>scroll.viewport.rect.height)
            {
                Canvas.ForceUpdateCanvases();var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.content,button.GetComponent<RectTransform>());
                float max=scroll.content.rect.height-scroll.viewport.rect.height;float offset=Mathf.Clamp(scroll.content.rect.yMax-bounds.center.y-scroll.viewport.rect.height/2,0,max);scroll.verticalNormalizedPosition=1-offset/max;Canvas.ForceUpdateCanvases();
            }
            return button;
        }
        void Click(string text,bool exact=false)
        {
            var b=Find(text,exact);Pointer(b,true);Pointer(b,false);
            ExecuteEvents.Execute(b.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);
        }
        void Pointer(Button b,bool down)
        {
            if(b==null)return;
            Canvas.ForceUpdateCanvases();var rect=b.GetComponent<RectTransform>();var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
            if(down)
            {
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
                bool hit=hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b;
                string check="visible-hit-target:"+b.GetComponentInChildren<Text>().text;
                if(!hit)
                {
                    Rect ScreenRect(RectTransform target){var corners=new Vector3[4];target.GetWorldCorners(corners);return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);}
                    var scroll=b.GetComponentInParent<ScrollRect>();
                    receipt.hitFailures.Add(new HitFailure{target=b.GetComponentInChildren<Text>().text,point=point,screen=new Vector2(Screen.width,Screen.height),button=ScreenRect(rect),viewport=scroll!=null?ScreenRect(scroll.viewport):default,scrollPosition=scroll!=null?scroll.verticalNormalizedPosition:0,contentHeight=scroll!=null?scroll.content.rect.height:0,insideViewport=scroll==null||RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport,point),culled=b.GetComponent<Image>().canvasRenderer.cull,hitTargets=hits.Take(5).Select(h=>h.gameObject.GetComponentInParent<Button>()?.GetComponentInChildren<Text>()?.text??h.gameObject.name).ToArray()});
                }
                if(journeyMode)JourneyRequire(hit,check);else Require(hit,check);
                ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerDownHandler);
            }
            else ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerUpHandler);
        }
        void Require(bool condition,string name){if(condition){if(!receipt.checks.Contains(name))receipt.checks.Add(name);}else if(!receipt.failures.Contains(name))receipt.failures.Add(name);}
        void CaptureReflection(BayPlanarReflection reflection)
        {
            if(reflection.Texture==null)return;
            var rt=reflection.Texture;receipt.reflectionWidth=rt.width;receipt.reflectionHeight=rt.height;
            Require(rt.width<=768&&rt.height<=768,"reflection-texture-resolution-capped");
            var prior=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();var pixels=image.GetPixels();float mean=0,squared=0;
                foreach(var p in pixels){float value=(p.r+p.g+p.b)/3;mean+=value;squared+=value*value;}mean/=pixels.Length;receipt.reflectionVariance=squared/pixels.Length-mean*mean;
                Require(receipt.reflectionVariance>.0005f,"reflection-contains-rendered-scene-detail");File.WriteAllBytes(Path.Combine(folder,"00-water-reflection.png"),image.EncodeToPNG());
            }
            finally{RenderTexture.active=prior;Destroy(image);}
        }
        void CheckLayout(string page)
        {
            Canvas.ForceUpdateCanvases();
            foreach(var t in FindObjectsByType<Text>(FindObjectsSortMode.None))
                if(t.isActiveAndEnabled&&t.text.Length>0)
                {
                    if(t.fontSize<17)receipt.failures.Add("small-text:"+page+":"+t.text);
                    else if(t.preferredHeight>t.rectTransform.rect.height+.01f)receipt.failures.Add("clipped-text:"+page+":"+t.text.Replace("\n"," "));
                    if((t.transform.parent.name=="GearDetails"||t.transform.parent.name=="HabitatDetails")&&Mathf.Abs(t.rectTransform.anchoredPosition.y+8)<.01f)
                    {
                        receipt.inspectedLabels.Add(new LabelRender{page=page,text=t.text,requiredHeight=t.preferredHeight,availableHeight=t.rectTransform.rect.height,visibleCharacters=t.cachedTextGenerator.characterCountVisible,vertices=t.cachedTextGenerator.vertexCount});
                        Require(t.cachedTextGenerator.characterCountVisible>=t.text.Length&&t.cachedTextGenerator.vertexCount>0,"rendered-inspection-title:"+page);
                    }
                }
        }
        void CheckTouchTargets(string page)
        {
            foreach(var b in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if(!b.isActiveAndEnabled)continue;var r=b.GetComponent<RectTransform>();var points=new Vector3[4];r.GetWorldCorners(points);
                float w=Vector3.Distance(points[0],points[3])/DisplayMetrics.PixelsPerPoint,h=Vector3.Distance(points[0],points[1])/DisplayMetrics.PixelsPerPoint;
                if(w<43.5f||h<43.5f)receipt.failures.Add("small-target:"+page+":"+b.GetComponentInChildren<Text>()?.text);
            }
        }
        IEnumerator Snapshot(string name)
        {
            CheckTouchTargets(name);yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(folder,name+".png"),tex.EncodeToPNG());Destroy(tex);
        }
        void Update()
        {
            if(receipt!=null&&!finished&&Time.realtimeSinceStartup-began>(journeyMode?1800:100))
            {
                receipt.failures.Add("Runtime smoke timed out");
                if(journeyMode)FinishJourney();
                else {receipt.status="FAIL";finished=true;File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));Application.Quit(1);}
            }
        }
        void OnDestroy()
        {
            if(receipt!=null&&!finished){receipt.status="INTERRUPTED";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));}
        }
    }
}

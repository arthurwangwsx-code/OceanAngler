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
        [Serializable] sealed class Receipt { public string status="RUNNING";public List<string> checks=new List<string>();public List<string> failures=new List<string>();public List<LabelRender> inspectedLabels=new List<LabelRender>();public int catches,points;public float pixelsPerPoint, safeWidthPoints, safeHeightPoints; }
        GameController game;string folder;Receipt receipt;bool finished;float began;
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--ocean-smoke");if(index<0||index+1>=args.Length){if(Debug.isDebugBuild)Debug.Log("OCEAN_SMOKE_MODE_OFF");yield break;}
            folder=args[index+1];Directory.CreateDirectory(folder);receipt=new Receipt();began=Time.realtimeSinceStartup;game=GetComponent<GameController>();Application.runInBackground=true;
            Debug.Log("OCEAN_SMOKE_BEGIN");
            yield return new WaitForSecondsRealtime(1);
            if(game.Session.Paused){Click("继续探索");yield return null;yield return null;}
            var spinning=FindFirstObjectByType<AuthoredSpinningRig>();Require(spinning!=null,"authored-spinning-rig-loaded");
            if(spinning!=null){Require(spinning.guides.Length==9&&spinning.rodBones.Length==10,"rig-guides-and-bend-bones");Require(spinning.GetComponentsInChildren<LODGroup>(true).Length==1,"rig-single-lod-owner");Require(spinning.MaximumGuideLineError()<.0001f,"rig-line-through-guides");}
            var authored=FindFirstObjectByType<AuthoredFishVisual>();Require(authored!=null,"authored-bluefin-loaded");
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
            Click("潜入水下",true);yield return null;yield return null;CheckLayout("bay-underwater");Require(Camera.main.transform.position.y<bay.SeaLevel,"underwater-camera-below-water");yield return Snapshot("08-bay-underwater");
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
            receipt.catches=game.Progress.Data.catches.Count;receipt.points=game.Progress.Data.totalPoints;receipt.status=receipt.failures.Count==0?"PASS":"FAIL";finished=true;
            File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));Debug.Log("OCEAN_SMOKE_"+receipt.status);Application.Quit(receipt.failures.Count==0?0:1);
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
                Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==b,"visible-hit-target:"+b.GetComponentInChildren<Text>().text);
                ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerDownHandler);
            }
            else ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerUpHandler);
        }
        void Require(bool condition,string name){if(condition){if(!receipt.checks.Contains(name))receipt.checks.Add(name);}else if(!receipt.failures.Contains(name))receipt.failures.Add(name);}
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
            if(receipt!=null&&!finished&&Time.realtimeSinceStartup-began>100)
            {
                receipt.status="FAIL";receipt.failures.Add("Runtime smoke timed out");finished=true;
                File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));Application.Quit(1);
            }
        }
        void OnDestroy()
        {
            if(receipt!=null&&!finished){receipt.status="INTERRUPTED";File.WriteAllText(Path.Combine(folder,"receipt.json"),JsonUtility.ToJson(receipt,true));}
        }
    }
}

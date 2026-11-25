using System;
using System.IO;
using UnityEngine;
namespace OceanAngler
{
    public sealed class GameController : MonoBehaviour
    {
        public ContentCatalog Content { get; private set; }
        public ProgressService Progress { get; private set; }
        public FishingSession Session { get; private set; }
        public bool Reeling { get; private set; }
        public string Notice { get; private set; }
        public bool RestReminder { get; private set; }
        public bool DeveloperMode => Progress!=null&&Progress.DeveloperMode;
        public bool DeveloperProfileNeedsAttention { get; private set; }
        public event Action Changed;
        public event Action<CatchRecord> Released;
        readonly System.Random random = new System.Random();
        float accumulator, activeTime;
        SaveStore normalStore,developerStore;
        void Awake()
        {
            Application.targetFrameRate = 60;
            Content = ContentCatalog.Load(); Session = new FishingSession();
            var args=LaunchArguments.Get();int preview=Array.IndexOf(args,"--ocean-save-root");
            string saveRoot=preview>=0&&preview+1<args.Length?args[preview+1]:Application.persistentDataPath;
            normalStore=new SaveStore(Path.Combine(saveRoot,"ocean-progress.json"),Content);
            developerStore=new SaveStore(Path.Combine(saveRoot,"ocean-developer-progress.json"),Content,true);
            var developer=developerStore.HasExisting?new ProgressService(Content,developerStore):null;
            DeveloperProfileNeedsAttention=developer!=null&&(developer.RequiresRecovery||developer.RecoveredBackup);
            bool selected=developer!=null&&!developer.RequiresRecovery&&!developer.RecoveredBackup&&developer.DeveloperSelected;
            Progress=selected?developer:new ProgressService(Content,normalStore);
            Notice=DeveloperProfileNeedsAttention?"开发者测试存档需要确认，正常进度保留。请在家长区重新开启或重建测试进度。":Progress.LoadMessage;
        }
        void Update()
        {
            if (Progress.RequiresRecovery || Session.Paused) return;
            accumulator += Mathf.Min(Time.unscaledDeltaTime, 0.25f);
            while (accumulator >= 1f/30) { Session.Tick(1f/30, Reeling); accumulator -= 1f/30; }
            activeTime += Time.unscaledDeltaTime;
            if (activeTime >= 900) RestReminder = true;
        }
        public void Cast(float power)
        {
            if (Progress.RequiresRecovery || Session.Paused || Session.Phase!=FishingPhase.Ready) return;
            var target = EncounterSelection.Choose(Content,Progress.Data,random);
            Session.Cast(target, Progress.Data, Content, random, power); Notice = null; Changed?.Invoke();
        }
        public void Strike() { Session.Strike(); Changed?.Invoke(); }
        public void HoldReel(bool value) { Reeling = value && !Session.Paused && Session.Phase == FishingPhase.Fighting; }
        public void Pause(bool paused) { Reeling = false; accumulator = 0; Session.Paused = paused; Changed?.Invoke(); }
        public void Release()
        {
            if (Session.Paused || Session.Phase != FishingPhase.Landed) return;
            var caught=Session.Catch;
            if (Try(() => { int points = Progress.Settle(caught); Notice = "已记录并放流，获得 " + points + " 研究积分"; }))
            {
                Reeling=false;Session.FinishRelease();Released?.Invoke(caught);
            }
            Changed?.Invoke();
        }
        public void RecordObservation()
        {
            var encounter=GetComponent<OceanWorld>()?.Observations;
            if(Progress.RequiresRecovery||encounter==null||!encounter.CanRecord||!GetComponent<GameUI>().InspectingObservation)return;
            Try(()=>{bool first=Progress.Observe(encounter.SpeciesId);Notice=first?"已记入观察图鉴，安静地和它告别吧。":"这位朋友已在观察图鉴中。";},"暂时无法保存观察记录，请重试。");
        }
        public void ReturnToPreparation() { Session.Cancel(); Pause(false); Notice="本次鱼获已放回海中，已有记录和积分保留。"; Changed?.Invoke(); }
        public void Retry() { Session.Retry(); Reeling = false; Changed?.Invoke(); }
        public void SelectHabitat(string id) { if (Session.Phase == FishingPhase.Ready) Try(() => Progress.SelectHabitat(id)); }
        public void SelectGear(string id)
        {
            if(Session.Phase==FishingPhase.Ready)
                Try(()=>{Progress.SelectGear(id);Notice="已装备"+Content.Gear(id).name+"，研究积分未扣除。";},"暂时无法保存装备，请重试。");
        }
        public void Configure(Action<GameSettings> action) { Try(() => Progress.Configure(action)); }
        public bool SwitchDeveloperMode(bool enabled) => ChangeDeveloperProfile(enabled,false);
        public bool RebuildDeveloperProgress() => ChangeDeveloperProfile(true,true);
        bool ChangeDeveloperProfile(bool enabled,bool rebuild)
        {
            if(!Session.Paused){Notice="请先暂停，再切换测试模式。";Changed?.Invoke();return false;}
            if(!rebuild&&DeveloperMode==enabled)return true;
            return Try(()=>
            {
                bool fresh=!developerStore.HasExisting;
                var next=new ProgressService(Content,enabled?developerStore:normalStore);
                if(enabled)
                {
                    if(rebuild)next.Reset(true);
                    else next.SelectDeveloperProfile(true,fresh?Progress.Data?.settings:null);
                }
                else Progress.SelectDeveloperProfile(false);
                // Only publish the selected profile and cancel the old hold
                // after its mode choice has reached the isolated test file.
                Progress=next;Session.Cancel();Pause(true);activeTime=0;RestReminder=false;
                DeveloperProfileNeedsAttention=false;
                GetComponent<OceanWorld>()?.ResetProfilePresentation();
                Notice=next.RequiresRecovery?next.LoadMessage:enabled?"开发者模式已开启，全部内容可体验。":"已回到正常进度，开发者测试记录单独保留。";
            },"暂时无法切换。当前模式和正常进度保留；可重试或重建测试进度。");
        }
        public void ResetProgress() { if (Try(() => Progress.Reset())) { Session.Cancel(); Reeling = false; Notice = "已建立新存档，原始文件已保留。"; } }
        public void DismissRest() { RestReminder = false; activeTime = 0; }
        public void ClearNotice() { Notice = null; }
        bool Try(Action action,string failureNotice=null)
        {
            try { action(); Changed?.Invoke(); return true; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is InvalidOperationException || e is ArgumentException)
            { Notice = failureNotice ?? "暂时无法保存，请重试；本次鱼获还没有丢失。"; Debug.LogWarning("Progress operation failed: " + e.GetType().Name); Changed?.Invoke(); return false; }
        }
        void OnApplicationFocus(bool focus) { if (!focus) Pause(true); }
        void OnApplicationPause(bool paused) { if (paused) Pause(true); }
        void OnDisable() { Reeling = false; }
    }
}

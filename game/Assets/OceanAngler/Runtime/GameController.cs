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
        public event Action Changed;
        public event Action<CatchRecord> Released;
        readonly System.Random random = new System.Random();
        float accumulator, activeTime;
        void Awake()
        {
            Application.targetFrameRate = 60;
            Content = ContentCatalog.Load(); Session = new FishingSession();
            var args=Environment.GetCommandLineArgs();int preview=Array.IndexOf(args,"--ocean-save-root");
            string saveRoot=preview>=0&&preview+1<args.Length?args[preview+1]:Application.persistentDataPath;
            Progress = new ProgressService(Content, new SaveStore(Path.Combine(saveRoot, "ocean-progress.json"), Content));
            Notice = Progress.LoadMessage;
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
        public void ReturnToPreparation() { Session.Cancel(); Pause(false); Notice="本次鱼获已放回海中，已有记录和积分保留。"; Changed?.Invoke(); }
        public void Retry() { Session.Retry(); Reeling = false; Changed?.Invoke(); }
        public void SelectHabitat(string id) { if (Session.Phase == FishingPhase.Ready) Try(() => Progress.SelectHabitat(id)); }
        public void SelectGear(string id) { if (Session.Phase == FishingPhase.Ready) Try(() => Progress.SelectGear(id)); }
        public void Configure(Action<GameSettings> action) { Try(() => Progress.Configure(action)); }
        public void ResetProgress() { if (Try(() => Progress.Reset())) { Session.Cancel(); Reeling = false; Notice = "已建立新存档，原始文件已保留。"; } }
        public void DismissRest() { RestReminder = false; activeTime = 0; }
        public void ClearNotice() { Notice = null; }
        bool Try(Action action)
        {
            try { action(); Changed?.Invoke(); return true; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is InvalidOperationException || e is ArgumentException)
            { Notice = "暂时无法保存，请重试；本次鱼获还没有丢失。"; Debug.LogWarning("Progress operation failed: " + e.GetType().Name); Changed?.Invoke(); return false; }
        }
        void OnApplicationFocus(bool focus) { if (!focus) Pause(true); }
        void OnApplicationPause(bool paused) { if (paused) Pause(true); }
        void OnDisable() { Reeling = false; }
    }
}

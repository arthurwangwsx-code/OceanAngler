using UnityEngine;
namespace OceanAngler
{
    // 功能原型，所有几何鱼均为占位，不代表成品美术。
    public sealed class PrototypeView : MonoBehaviour
    {
        private readonly FishingSession session = new FishingSession();
        private bool held;
        private float clock;
        private GUIStyle label, button;
        private GameObject fish;
        void Start()
        {
            Application.targetFrameRate = 60;
            fish = GameObject.Find("PrototypeFish");
        }
        void Update()
        {
            session.Tick(Time.deltaTime, held);
            if (!session.Paused) clock += Time.deltaTime;
            if (fish != null) fish.transform.position = new Vector3(Mathf.Sin(clock * 0.4f) * 2, -0.4f, 2 + Mathf.Cos(clock * 0.4f));
        }
        void OnApplicationFocus(bool focus) { if (!focus) { held = false; session.Paused = true; } }
        void OnApplicationPause(bool paused) { if (paused) { held = false; session.Paused = true; } }
        void OnGUI()
        {
            float scale = Mathf.Min(Screen.width / 1100f, Screen.height / 650f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            label = new GUIStyle(GUI.skin.label) { fontSize = 24, wordWrap = true };
            button = new GUIStyle(GUI.skin.button) { fontSize = 24 };
            GUILayout.BeginArea(new Rect(28, 25, 1000, 600));
            GUILayout.Label("OceanAngler / Early 3D prototype", label);
            GUILayout.Label("Prototype geometry - production fish and gear are not included.", label);
            GUILayout.Label("State: " + session.Phase + "    Research: " + session.Points, label);
            GUILayout.Label("Tension " + Mathf.RoundToInt(session.Tension * 100) + "%    Catch " + Mathf.RoundToInt(session.Progress * 100) + "%", label);
            if (GUILayout.Button(session.Paused ? "Resume" : "Pause", button, GUILayout.Width(180))) { held = false; session.Paused = !session.Paused; }
            if (!session.Paused)
            {
                if (session.Phase == FishingPhase.Ready && GUILayout.Button("Cast", button, GUILayout.Width(240))) session.Cast();
                if (session.Phase == FishingPhase.Waiting) GUILayout.Label("Watch the lure. Wait for a bite...", label);
                if (session.Phase == FishingPhase.Bite && GUILayout.Button("Bite! Lift rod", button, GUILayout.Width(300))) session.Strike();
                if (session.Phase == FishingPhase.Fighting)
                {
                    GUILayout.Label("Hold to reel. Release to reduce tension.", label);
                    held = GUILayout.RepeatButton("HOLD TO REEL", button, GUILayout.Width(320), GUILayout.Height(90));
                }
                if (session.Phase == FishingPhase.Landed && GUILayout.Button("Record and release (+120)", button, GUILayout.Width(420))) session.Release();
                if (session.Phase == FishingPhase.Escaped && GUILayout.Button("Fish escaped - try again", button, GUILayout.Width(420))) session.Retry();
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label("Support: arthur.wang.wsx@gmail.com", label);
            GUILayout.EndArea();
        }
    }
}

using System;
using UnityEngine;
namespace OceanAngler
{
    [Serializable] public sealed class JournalEnvelopeInput {public string path,sha256;}
    [Serializable] public sealed class JournalEnvelopeShape
    {public string fishId;public int poses,sourceVertices;public Bounds bounds;public Bounds[] sections;}
    [Serializable] public sealed class JournalEnvelopeData
    {
        public int version=1,sections=32;public float padding=.012f;public string scope,unityVersion;
        public JournalEnvelopeInput[] inputs;public JournalEnvelopeShape[] shapes;
        static JournalEnvelopeData cached;
        public static JournalEnvelopeShape Find(string id)
        {
            if(cached==null){var file=Resources.Load<TextAsset>("JournalEnvelopes");if(file==null)return null;cached=JsonUtility.FromJson<JournalEnvelopeData>(file.text);}
            if(cached.version!=1||cached.unityVersion!=Application.unityVersion||cached.shapes==null)return null;
            return Array.Find(cached.shapes,s=>s.fishId==id);
        }
    }
}

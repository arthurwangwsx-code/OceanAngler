using System;
using UnityEditor;
using UnityEngine;

namespace OceanAngler.Editor
{
    public static class AudioAssetBuilder
    {
        static readonly string[] Names={"OceanBed","ExploreTheme"};
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            foreach(string name in Names)
            {
                var importer=AssetImporter.GetAtPath("Assets/OceanAngler/Resources/Audio/"+name+".wav") as AudioImporter;
                if(importer==null)throw new InvalidOperationException("Missing authored audio "+name);
                var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.CompressedInMemory;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.55f;
                settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.SaveAndReimport();
            }
            Validate();
        }
        public static void Validate()
        {
            foreach(string name in Names)
            {
                var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/OceanAngler/Resources/Audio/"+name+".wav");
                if(clip==null||clip.channels!=2||clip.frequency!=22050||clip.length<10||clip.loadType!=AudioClipLoadType.CompressedInMemory)
                    throw new InvalidOperationException("Invalid authored audio import "+name);
            }
        }
    }
}

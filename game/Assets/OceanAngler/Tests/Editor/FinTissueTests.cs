using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace OceanAngler.Tests
{
    public sealed class FinTissueTests
    {
        [Test] public void ImportedMembranesUseTransparentStateWhileSolidRaysAndAnatomyRetainOpaqueDepthAndShadows()
        {
            var prefab=Resources.Load<GameObject>("Models/Bluefin");var renderers=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);Assert.That(renderers.Length,Is.EqualTo(3));
            var materials=renderers.SelectMany(r=>r.sharedMaterials).Distinct().ToArray();Assert.That(materials.Length,Is.EqualTo(7));
            var membrane=materials.Single(m=>m.name=="Bluefin_Fins");var rays=materials.Single(m=>m.name=="Bluefin_Rays");
            Assert.That(membrane.renderQueue,Is.EqualTo((int)RenderQueue.Transparent));Assert.That(membrane.GetFloat("_Surface"),Is.EqualTo(1));Assert.That(membrane.GetFloat("_ZWrite"),Is.Zero);Assert.That(membrane.GetFloat("_Cull"),Is.EqualTo((float)CullMode.Back));Assert.That(membrane.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"),Is.True);Assert.That(membrane.GetShaderPassEnabled("ShadowCaster"),Is.False);Assert.That(membrane.GetShaderPassEnabled("DepthOnly"),Is.False);
            foreach(var renderer in renderers){Assert.That(renderer.sharedMaterials.Contains(membrane),Is.True);Assert.That(renderer.sharedMaterials.Contains(rays),Is.True);}
            foreach(var material in materials.Where(m=>m!=membrane)){Assert.That(material.GetFloat("_Surface"),Is.Zero);Assert.That(material.GetFloat("_ZWrite"),Is.EqualTo(1));Assert.That(material.color.a,Is.EqualTo(1));Assert.That(material.renderQueue,Is.LessThan((int)RenderQueue.Transparent));}
            Assert.That(rays.GetShaderPassEnabled("ShadowCaster"),Is.True);Assert.That(rays.GetShaderPassEnabled("DepthOnly"),Is.True);
            foreach(var material in materials){Assert.That(material.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(ShaderUtil.ShaderHasError(material.shader),Is.False);}
        }
        [Test] public void ActualImportedAlphaAndTechnicalMapsSurviveTextureImport()
        {
            const string folder="Assets/OceanAngler/Art/Bluefin/";var importer=(TextureImporter)AssetImporter.GetAtPath(folder+"Bluefin_Fin.png");Assert.That(importer.DoesSourceTextureHaveAlpha(),Is.True);Assert.That(importer.alphaSource,Is.EqualTo(TextureImporterAlphaSource.FromInput));Assert.That(importer.sRGBTexture,Is.True);Assert.That(importer.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
            var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"Bluefin_Fins.mat");foreach(string property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap"}){var texture=(Texture2D)material.GetTexture(property);Assert.That(texture,Is.Not.Null);Assert.That(texture.width,Is.EqualTo(1024));Assert.That(texture.height,Is.EqualTo(512));}
            Assert.That(((TextureImporter)AssetImporter.GetAtPath(folder+"Bluefin_Fin_Normal.png")).textureType,Is.EqualTo(TextureImporterType.NormalMap));Assert.That(((TextureImporter)AssetImporter.GetAtPath(folder+"Bluefin_Fin_MetallicSmoothness.png")).sRGBTexture,Is.False);
        }
    }
}

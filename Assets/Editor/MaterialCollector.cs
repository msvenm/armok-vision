﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MaterialStore
{
    public class MaterialCollector
    {
        private static List<Texture2D> albedoList;
        private static List<Texture2D> alphaList;
        private static Texture2D defaultAlbedo;
        private static Texture2D defaultAlpha;
        private static Texture2D defaultNormal;
        private static Texture2D defaultOcclusion;
        private static Texture2D defaultSpecular;
        private static List<Texture2D> normalList;
        private static List<Texture2D> occlusionList;
        private static Dictionary<string, int> patternIndex;
        private static Dictionary<string, int> shapeIndex;
        private static List<Texture2D> specularList;

        [MenuItem("Mytools/Build Material Collection")]
        public static void BuildMaterialCollection()
        {
            var materialGUIDs = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials/MaterialTypes" });

            int colorID = Shader.PropertyToID("_Color");
            int albedoID = Shader.PropertyToID("_MainTex");
            int specularID = Shader.PropertyToID("_Specular");
            int normalID = Shader.PropertyToID("_Normal");
            int occlusionID = Shader.PropertyToID("_Occlusion");
            int heightID = Shader.PropertyToID("_Height");

            MaterialCollection matCollection = ScriptableObject.CreateInstance<MaterialCollection>();

            patternIndex = new Dictionary<string, int>();
            albedoList = new List<Texture2D>();
            specularList = new List<Texture2D>();

            shapeIndex = new Dictionary<string, int>();
            normalList = new List<Texture2D>();
            occlusionList = new List<Texture2D>();
            alphaList = new List<Texture2D>();

            defaultAlbedo = Resources.Load<Texture2D>("Grey");
            defaultSpecular = Resources.Load<Texture2D>("Low_S");

            defaultNormal = Resources.Load<Texture2D>("Flat_N");
            defaultOcclusion = Resources.Load<Texture2D>("Flat_O");
            defaultAlpha = Resources.Load<Texture2D>("Opaque_A");

            matCollection.textures.Add(CreateTextureSet(new MaterialTag(), new Color(0.5f, 0.5f, 0.5f, 0.5f), null, null, null, null, null));

            foreach (var item in materialGUIDs)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(item));

                matCollection.textures.Add(
                    CreateTextureSet(MaterialTag.Parse(mat.name),
                    mat.GetColor(colorID),
                    (Texture2D)mat.GetTexture(albedoID),
                    (Texture2D)mat.GetTexture(specularID),
                    (Texture2D)mat.GetTexture(normalID),
                    (Texture2D)mat.GetTexture(occlusionID),
                    (Texture2D)mat.GetTexture(heightID)));
            }

            Texture2DArray patternArray = new Texture2DArray(256, 256, albedoList.Count, TextureFormat.ARGB32, true, true);

            for (int i = 0; i < albedoList.Count; i++)
            {
                Debug.Log(i + ": " + albedoList[i].name + "," + specularList[i].name);
                MakeUsable(albedoList[i]);
                MakeUsable(specularList[i]);

                if (albedoList[i].width != 256 || albedoList[i].height != 256)
                    TextureScale.Bilinear(albedoList[i], 256, 256);
                if (specularList[i].width != 256 || specularList[i].height != 256)
                    TextureScale.Bilinear(specularList[i], 256, 256);

                var albedoArray = albedoList[i].GetPixels();
                var specularArray = specularList[i].GetPixels();

                for (int j = 0; j < albedoArray.Length; j++)
                {
                    albedoArray[j].a = specularArray[j].r;
                }
                
                //No idea why this happens, but it does.
                if(patternArray == null)
                    patternArray = new Texture2DArray(256, 256, albedoList.Count, TextureFormat.ARGB32, true, true);
                patternArray.SetPixels(albedoArray, i);
            }

            patternArray.Apply(true);

            Texture2DArray shapeArray = new Texture2DArray(256, 256, normalList.Count, TextureFormat.ARGB32, true, true);

            for (int i = 0; i < normalList.Count; i++)
            {
                Debug.Log(i + ": " + normalList[i].name + "," + occlusionList[i].name + "," + alphaList[i].name);
                MakeUsable(normalList[i]);
                MakeUsable(occlusionList[i]);
                MakeUsable(alphaList[i]);

                if (normalList[i].width != 256 || normalList[i].height != 256)
                    TextureScale.Bilinear(normalList[i], 256, 256);
                if (occlusionList[i].width != 256 || occlusionList[i].height != 256)
                    TextureScale.Bilinear(occlusionList[i], 256, 256);
                if (alphaList[i].width != 256 || alphaList[i].height != 256)
                    TextureScale.Bilinear(alphaList[i], 256, 256);

                var normalArray = normalList[i].GetPixels();
                var occlusionArray = occlusionList[i].GetPixels();
                var alphaArray = alphaList[i].GetPixels();


                for (int j = 0; j < normalArray.Length; j++)
                {
                    normalArray[j] = new Color(occlusionArray[j].r, normalArray[j].b, alphaArray[j].r, normalArray[j].a);
                }

                shapeArray.SetPixels(normalArray, i);
            }

            shapeArray.Apply(true);

            AssetDatabase.CreateAsset(patternArray, "Assets/Resources/patternTextures.asset");
            AssetDatabase.CreateAsset(shapeArray, "Assets/Resources/shapeTextures.asset");
            AssetDatabase.CreateAsset(matCollection, "Assets/Resources/materialDefinitions.asset");
            AssetDatabase.SaveAssets();
        }

        private static MaterialTextureSet CreateTextureSet(MaterialTag materialTag, Color color, Texture2D albedo = null, Texture2D specular = null, Texture2D normal = null, Texture2D occlusion = null, Texture2D alpha = null)
        {
            if (albedo == null)
                albedo = defaultAlbedo;
            if (specular == null)
                specular = defaultSpecular;
            if (normal == null)
                normal = defaultNormal;
            if (occlusion == null)
                occlusion = defaultOcclusion;
            if (alpha == null)
                alpha = defaultAlpha;

            MaterialTextureSet set = new MaterialTextureSet();

            set.tag = materialTag;
            set.color = color;

            string patternID = albedo.GetInstanceID().ToString() + "," + specular.GetInstanceID().ToString();

            if (patternIndex.ContainsKey(patternID))
            {
                set.patternIndex = patternIndex[patternID];
            }
            else
            {
                set.patternIndex = albedoList.Count;
                patternIndex[patternID] = albedoList.Count;
                albedoList.Add(albedo);
                specularList.Add(specular);
            }

            string shapeID = normal.GetInstanceID().ToString() + "," + occlusion.GetInstanceID().ToString() + "," + alpha.GetInstanceID().ToString();

            if(shapeIndex.ContainsKey(shapeID))
            {
                set.shapeIndex = shapeIndex[shapeID];
            }
            else
            {
                set.shapeIndex = normalList.Count;
                shapeIndex[shapeID] = normalList.Count;
                normalList.Add(normal);
                occlusionList.Add(occlusion);
                alphaList.Add(alpha);
            }

            return set;
        }

        public static void MakeUsable(Texture texture)
        {
            var path = AssetDatabase.GetAssetPath(texture);

            if (string.IsNullOrEmpty(path))
                return; //Not a valid file.

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);

            importer.isReadable = true;
            importer.maxTextureSize = 256;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            importer.SaveAndReimport();
        }
    }
}

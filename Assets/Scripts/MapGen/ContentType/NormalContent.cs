﻿using System;
using System.IO;
using System.Xml.Linq;
using UnityEngine;

public class NormalContent : IContent
{
    static int num_created = 0;
    public static int NumCreated { get { return num_created; } }
    public int UniqueIndex { get; private set; }

    TextureStorage store;
    public int StorageIndex { get; private set; }
    
    public Texture2D Texture { get; private set; }

    public Matrix4x4 UVTransform
    {
        get
        {
            return store.getUVTransform(StorageIndex);
        }
    }

    public float ArrayIndex
    {
        get
        {
            return (float)StorageIndex / store.Count;
        }
    }


    public bool AddTypeElement(XElement elemtype)
    {
        XAttribute normalAtt = elemtype.Attribute("normal");
        Texture2D normalMap = ContentLoader.LoadTexture(normalAtt, elemtype, new Color(0.5f, 0.5f, 1f), true);

        XAttribute alphaAtt = elemtype.Attribute("alpha");
        Texture2D alphaMap = ContentLoader.LoadTexture(alphaAtt, elemtype, Color.white, true);

        XAttribute occlusionAtt = elemtype.Attribute("occlusion");
        Texture2D occlusionMap = ContentLoader.LoadTexture(occlusionAtt, elemtype, Color.white, true);

        XAttribute patternAtt = elemtype.Attribute("pattern");
        Texture2D patternTex = ContentLoader.LoadTexture(patternAtt, elemtype, Color.gray);

        GameSettings.MatchSizes(new Texture2D[] { normalMap, alphaMap, occlusionMap, patternTex });

        Texture2D combinedMap = new Texture2D(normalMap.width, normalMap.height, TextureFormat.ARGB32, true, true);
        combinedMap.filterMode = FilterMode.Trilinear;
        combinedMap.name = normalMap.name + occlusionMap.name + alphaMap.name + patternTex.name;


        Color[] normalColors = normalMap.GetPixels();
        Color[] occlusionColors = occlusionMap.GetPixels();
        Color[] alphaColors = alphaMap.GetPixels();
        Color[] patternColors = patternTex.GetPixels();

        for (int i = 0; i < normalColors.Length; i++)
        {
            normalColors[i] = new Color(occlusionColors[i].r, normalColors[i].g, alphaColors[i].r * patternColors[i].a, normalColors[i].r);
        }

        combinedMap.SetPixels(normalColors);
        combinedMap.Apply();

        if (store != null)
            StorageIndex = store.AddTexture(combinedMap);
        else
            StorageIndex = -1;
        Texture = combinedMap;
        UniqueIndex = num_created;
        num_created++;
        return true;
    }

    public object ExternalStorage
    {
        set
        {
            store = value as TextureStorage;
        }
    }
}

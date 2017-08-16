﻿using System;
using System.Collections;
using System.Collections.Generic;
using RemoteFortressReader;
using UnityEngine;

public class LayeredSprite : MonoBehaviour
{
    private static Material _spriteMat;

    public static Material SpriteMat
    {
        get
        {
            if (_spriteMat == null)
                _spriteMat = Resources.Load<Material>("SpriteMat");
            return _spriteMat;
        }
    }

    public float gap = 0.0001f;
    public CreatureSpriteCollection spriteCollection;
    private List<SpriteRenderer> spriteList;

    private void Start()
    {
        if (spriteCollection == null)
        {
            enabled = false;
            return;
        }
    }

    public void UpdateLayers(List<CreatureSpriteLayer> spriteLayers)
    {
        for(int i = 0; i < spriteList.Count; i++)
        {
            spriteList[i].gameObject.SetActive(spriteLayers[i].preview);
            if(spriteLayers[i].colorSource != CreatureSpriteLayer.ColorSource.None)
                spriteList[i].color = spriteLayers[i].color;
        }
    }

    private void BuildSpriteLayers(List<CreatureSpriteLayer> spriteLayers)
    {
        spriteList = new List<SpriteRenderer>();
        float depth = 0;
        foreach (var layer in spriteLayers)
        {
            GameObject go = new GameObject(layer.spriteTexture.name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = layer.color;
            sr.sprite = layer.spriteTexture;
            sr.sharedMaterial = SpriteMat;
            var pos = go.transform.localPosition;
            pos.z = depth;
            pos.x += layer.positionOffset.x;
            pos.y += layer.positionOffset.y;
            go.transform.localPosition = pos;
            spriteList.Add(sr);
            depth -= gap;
        }
    }

    internal void UpdateLayers(UnitDefinition unit, CreatureRaw creatureRaw, CasteRaw casteRaw)
    {
        if (spriteList == null)
            BuildSpriteLayers(spriteCollection.spriteLayers);
        for (int i = 0; i < spriteList.Count; i++)
        {
            var spriteLayerDef = spriteCollection.spriteLayers[i];
            var sprite = spriteList[i];
            switch (spriteLayerDef.spriteSource)
            {
                case CreatureSpriteLayer.SpriteSource.Static:
                    sprite.enabled = true;
                    switch (spriteLayerDef.colorSource)
                    {
                        case CreatureSpriteLayer.ColorSource.Fixed:
                            sprite.color = spriteLayerDef.color;
                            break;
                        case CreatureSpriteLayer.ColorSource.Job:
                            sprite.color = new Color(unit.profession_color.red / 255.0f, unit.profession_color.green / 255.0f, unit.profession_color.blue / 255.0f, 0.5f);
                            break;
                        default:
                            sprite.color = new Color32(128, 128, 128, 128);
                            break;
                    }
                    break;
                case CreatureSpriteLayer.SpriteSource.Bodypart:
                    sprite.enabled = true;
                    switch (spriteLayerDef.colorSource)
                    {
                        case CreatureSpriteLayer.ColorSource.Fixed:
                            sprite.color = spriteLayerDef.color;
                            break;
                        case CreatureSpriteLayer.ColorSource.Material:
                            int colorModIndex = -1;
                            for (int j = 0; j < casteRaw.color_modifiers.Count; j++)
                            {
                                if(casteRaw.color_modifiers[j].part == spriteLayerDef.token && casteRaw.color_modifiers[j].start_date == 0)
                                {
                                    colorModIndex = j;
                                    break;
                                }
                            }
                            if(colorModIndex == -1)
                            {
                                sprite.enabled = false;
                                continue;
                            }
                            var unitColor = casteRaw.color_modifiers[colorModIndex].patterns[unit.appearance.colors[colorModIndex]].colors[spriteLayerDef.patternIndex];
                            sprite.color = new Color32((byte)unitColor.red, (byte)unitColor.green, (byte)unitColor.blue, 128);
                            break;
                        case CreatureSpriteLayer.ColorSource.Job:
                            sprite.color = new Color(unit.profession_color.red / 255.0f, unit.profession_color.green / 255.0f, unit.profession_color.blue / 255.0f, 0.5f);
                            break;
                        default:
                            sprite.color = new Color32(128, 128, 128, 128);
                            break;
                    }
                    switch (spriteLayerDef.hairType)
                    {
                        case CreatureSpriteLayer.HairType.Hair:
                        case CreatureSpriteLayer.HairType.Beard:
                        case CreatureSpriteLayer.HairType.Moustache:
                        case CreatureSpriteLayer.HairType.Sideburns:
                            switch (spriteLayerDef.hairStyle)
                            {
                                case HairStyle.UNKEMPT:
                                    sprite.enabled = true;
                                    break;
                                case HairStyle.NEATLY_COMBED:
                                case HairStyle.BRAIDED:
                                case HairStyle.DOUBLE_BRAID:
                                case HairStyle.PONY_TAILS:
                                case HairStyle.CLEAN_SHAVEN:
                                default:
                                    sprite.enabled = false;
                                    continue;
                            }
                            break;
                        default:
                            break;
                    }
                    sprite.enabled = true;
                    break;
                case CreatureSpriteLayer.SpriteSource.Equipment:
                    sprite.enabled = spriteLayerDef.preview;
                    switch (spriteLayerDef.colorSource)
                    {
                        case CreatureSpriteLayer.ColorSource.Fixed:
                            sprite.color = spriteLayerDef.color;
                            break;
                        case CreatureSpriteLayer.ColorSource.Material:
                            sprite.color = new Color(unit.profession_color.red / 255.0f, unit.profession_color.green / 255.0f, unit.profession_color.blue / 255.0f, 0.5f);
                            break;
                        case CreatureSpriteLayer.ColorSource.Job:
                            sprite.color = new Color(unit.profession_color.red / 255.0f, unit.profession_color.green / 255.0f, unit.profession_color.blue / 255.0f, 0.5f);
                            break;
                        default:
                            sprite.color = new Color32(128, 128, 128, 128);
                            break;
                    }
                    break;
                default:
                    sprite.enabled = false;
                    break;
            }
        }
    }
}
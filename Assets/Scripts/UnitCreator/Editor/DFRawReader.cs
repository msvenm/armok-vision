﻿using System;
using System.Collections.Generic;
using System.IO;
using DFHack;
using dfproto;
using MaterialStore;
using RemoteFortressReader;
using TokenLists;
using UnityEditor;
using UnityEngine;

public class DFRawReader : EditorWindow
{
    private Vector2 raceScroll;
    private Vector2 unitScroll;
    [SerializeField]
    private List<CreatureRaw> creatureRaws;
    [SerializeField]
    private List<CreatureRaw> filteredRaws;

    [MenuItem("Window/DF Raw Reader")]
    public static void ShowWindow()
    {
        GetWindow<DFRawReader>();
    }

    [SerializeField]
    string filter;

    [SerializeField]
    CreatureBody.BodyCategory bodyCategoryFilter;

    [SerializeField]
    bool filterName = true;
    [SerializeField]
    bool filterToken = true;
    [SerializeField]
    bool filterDescription = true;
    [SerializeField]
    bool filterParts = true;
    private bool showRaces;
    private bool showUnits;
    [SerializeField]
    private List<UnitDefinition> units;

    class ChildCount
    {
        public int min = int.MaxValue;
        public int max = int.MinValue;
    }

    bool FitsFilter(CreatureRaw creature)
    {
        if (!string.IsNullOrEmpty(filter) && (filterName || filterDescription || filterToken || filterParts))
        {
            bool matched = false;
            if (filterToken && creature.creature_id.ToUpper().Contains(filter.ToUpper()))
                matched = true;
            if (filterName && creature.name[0].ToUpper().Contains(filter.ToUpper()))
                matched = true;
            if(!matched)
                foreach (var caste in creature.caste)
                {
                    if (filterName && caste.caste_name[0].ToUpper().Contains(filter.ToUpper()))
                        matched = true;
                    if (filterDescription && caste.description.ToUpper().Contains(filter.ToUpper()))
                        matched = true;
                    if(filterParts)
                    {
                        foreach (var part in caste.body_parts)
                        {
                            if (part.category.ToUpper().Contains(filter.ToUpper()))
                            {
                                matched = true;
                                break;
                            }
                        }
                    }
                }
            if (!matched)
                return false;
        }
        if (bodyCategoryFilter != CreatureBody.BodyCategory.None)
        {
            foreach (var caste in creature.caste)
            {
                if (bodyCategoryFilter == CreatureBody.FindBodyCategory(caste))
                    return true;
            }
            return false;
        }
        return true;
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Read Raws"))
        {
            var client = new RemoteClient();
            if (!client.Connect())
                return;
            client.SuspendGame();
            var getCreatureRaws = new RemoteFunction<EmptyMessage, CreatureRawList>(client, "GetCreatureRaws", "RemoteFortressReader");
            var materialListCall = new RemoteFunction<EmptyMessage, MaterialList>(client, "GetMaterialList", "RemoteFortressReader");
            var itemListCall = new RemoteFunction<EmptyMessage, MaterialList>(client, "GetItemList", "RemoteFortressReader");
            var unitListCall = new RemoteFunction<EmptyMessage, UnitList>(client, "GetUnitList", "RemoteFortressReader");
            client.ResumeGame();
            creatureRaws = getCreatureRaws.Execute().creature_raws;
            var ExistingMatList = AssetDatabase.LoadAssetAtPath<MaterialRaws>("Assets/Resources/MaterialRaws.asset");
            var ExistingItemList = AssetDatabase.LoadAssetAtPath<ItemRaws>("Assets/Resources/ItemRaws.asset");
            MaterialRaws.Instance.MaterialList = materialListCall.Execute().material_list;
            ItemRaws.Instance.ItemList = itemListCall.Execute().material_list;
            units = unitListCall.Execute().creature_list;
            if (ExistingMatList == null)
                AssetDatabase.CreateAsset(MaterialRaws.Instance, "Assets/Resources/MaterialRaws.asset");
            if (ExistingItemList == null)
                AssetDatabase.CreateAsset(ItemRaws.Instance, "Assets/Resources/ItemRaws.asset");
            AssetDatabase.SaveAssets();
            Debug.Log(string.Format("Pulled {0} creature raws from DF.", creatureRaws.Count));
            if (MaterialCollection.Instance == null)
                MaterialCollector.BuildMaterialCollection();
            MaterialCollection.Instance.PopulateMatTextures();
            client.Disconnect();
            //foreach (var raw in creatureRaws)
            //{
            //    raw.creature_id = BodyDefinition.GetCorrectedCreatureID(raw);
            //}
            RefilterList();
        }
        if (creatureRaws != null)
        {
            EditorGUI.BeginChangeCheck();
            filter = EditorGUILayout.TextField(filter);
            filterToken = EditorGUILayout.Toggle("Token", filterToken);
            filterName = EditorGUILayout.Toggle("Name", filterName);
            filterDescription = EditorGUILayout.Toggle("Description", filterDescription);
            filterParts = EditorGUILayout.Toggle("Parts", filterParts);

            bodyCategoryFilter = (CreatureBody.BodyCategory)EditorGUILayout.EnumPopup(bodyCategoryFilter);

            if (EditorGUI.EndChangeCheck())
            {
                RefilterList();
            }
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Sort by name"))
            {
                creatureRaws.Sort((x, y) => x.creature_id.CompareTo(y.creature_id));
                RefilterList();
            }
            if (GUILayout.Button("Sort by size"))
            {
                creatureRaws.Sort((x, y) => x.adultsize.CompareTo(y.adultsize));
                RefilterList();
            }
            if (GUILayout.Button("Sort by index"))
            {
                creatureRaws.Sort((x, y) => x.index.CompareTo(y.index));
                RefilterList();
            }
            GUILayout.EndHorizontal();

            showRaces = EditorGUILayout.Foldout(showRaces, "Races");
            if (showRaces)
            {
                raceScroll = EditorGUILayout.BeginScrollView(raceScroll);
                foreach (var creature in filteredRaws)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(string.Format("{0} ({1})", creature.creature_id, creature.name[0]));
                    EditorGUILayout.BeginVertical();
                    foreach (var caste in creature.caste)
                    {
                        if (GUILayout.Button(string.Format("{0} ({1})", caste.caste_id, caste.caste_name[0])))
                        {
                            var creatureBase = new GameObject().AddComponent<CreatureBody>();
                            creatureBase.name = caste.caste_name[0];
                            creatureBase.race = creature;
                            creatureBase.caste = caste;
                            creatureBase.MakeBody();
                            Selection.SetActiveObjectWithContext(creatureBase, null);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
                if (GUILayout.Button("Dump Part Categories"))
                {
                    var path = EditorUtility.SaveFilePanel("Save bodypart list", "", "Bodyparts.csv", "csv");
                    if (!string.IsNullOrEmpty(path))
                    {
                        Dictionary<string, Dictionary<string, ChildCount>> parts = new Dictionary<string, Dictionary<string, ChildCount>>();
                        foreach (var creature in filteredRaws)
                        {
                            foreach (var caste in creature.caste)
                            {
                                if (bodyCategoryFilter != CreatureBody.BodyCategory.None && bodyCategoryFilter != CreatureBody.FindBodyCategory(caste))
                                    continue;

                                for (int i = 0; i < caste.body_parts.Count; i++)
                                {
                                    var part = caste.body_parts[i];
                                    //this is an internal part, and doesn't need modeling.
                                    if (part.flags[(int)BodyPartFlags.BodyPartRawFlags.INTERNAL])
                                        continue;
                                    if (!parts.ContainsKey(part.category))
                                        parts[part.category] = new Dictionary<string, ChildCount>();

                                    Dictionary<string, int> childCounts = new Dictionary<string, int>();

                                    foreach (var sub in caste.body_parts)
                                    {
                                        if (sub.parent != i)
                                            continue;
                                        if (sub.flags[(int)BodyPartFlags.BodyPartRawFlags.INTERNAL])
                                            continue;
                                        if (!childCounts.ContainsKey(sub.category))
                                            childCounts[sub.category] = 1;
                                        else
                                            childCounts[sub.category]++;
                                    }

                                    foreach (var item in childCounts)
                                    {
                                        if (!parts[part.category].ContainsKey(item.Key))
                                            parts[part.category][item.Key] = new ChildCount();
                                        if (parts[part.category][item.Key].min > item.Value)
                                            parts[part.category][item.Key].min = item.Value;
                                        if (parts[part.category][item.Key].max < item.Value)
                                            parts[part.category][item.Key].max = item.Value;
                                    }
                                }
                            }
                        }
                        using (var writer = new StreamWriter(path))
                        {
                            foreach (var parent in parts)
                            {
                                writer.Write("\"" + parent.Key + "\",");
                                foreach (var child in parent.Value)
                                {
                                    writer.Write(string.Format("\"{0}\",{1},{2},", child.Key, child.Value.min, child.Value.max));
                                }
                                writer.WriteLine();
                            }
                        }
                    }
                }
                if (GUILayout.Button("Place all races"))
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    CreatureBody prevCreature = null;
                    foreach (var creature in filteredRaws)
                    {
                        var creatureBase = new GameObject().AddComponent<CreatureBody>();
                        creatureBase.name = creature.caste[0].caste_name[0];
                        creatureBase.race = creature;
                        creatureBase.caste = creature.caste[0];
                        creatureBase.MakeBody();
                        if (prevCreature != null)
                        {
                            creatureBase.transform.position = new Vector3(prevCreature.transform.position.x + prevCreature.bounds.max.x - creatureBase.bounds.min.x, 0, 0);
                        }
                        prevCreature = creatureBase;
                    }
                    watch.Stop();
                    Debug.Log(string.Format("Took {0}ms to create {1} creatures, averaging {2}ms per creature.", watch.ElapsedMilliseconds, filteredRaws.Count, (float)watch.ElapsedMilliseconds / filteredRaws.Count));
                }
            }
            showUnits = EditorGUILayout.Foldout(showUnits, "Units");
            if(showUnits)
            {
                unitScroll = EditorGUILayout.BeginScrollView(unitScroll);
                foreach (var unit in units)
                {
                    string name = unit.name;
                    if (string.IsNullOrEmpty(name))
                        name = creatureRaws[unit.race.mat_type].caste[unit.race.mat_index].caste_name[0];
                    if(!string.IsNullOrEmpty(filter) && (filterParts|| filterName))
                    {
                        bool matched = false;
                        if (filterName)
                            matched = name.ToUpper().Contains(filter.ToUpper());
                        if (filterParts)
                        {
                            foreach (var item in unit.inventory)
                            {
                                if (!ItemRaws.Instance.ContainsKey(item.item.type))
                                    continue;
                                matched = ItemRaws.Instance[item.item.type].id.ToUpper().Contains(filter.ToUpper());
                                if (matched)
                                    break;
                            }
                        }
                        if (!matched)
                            continue;
                    }
                    if(GUILayout.Button(name))
                    {
                        var creatureBase = new GameObject().AddComponent<CreatureBody>();
                        creatureBase.name = name;
                        creatureBase.race = creatureRaws[unit.race.mat_type];
                        creatureBase.caste = creatureRaws[unit.race.mat_type].caste[unit.race.mat_index];
                        creatureBase.unit = unit;
                        creatureBase.MakeBody();
                        creatureBase.UpdateUnit(unit);
                        Selection.SetActiveObjectWithContext(creatureBase, null);
                    }
                }
                EditorGUILayout.EndScrollView();
                if (GUILayout.Button("Place all units"))
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    CreatureBody prevCreature = null;
                    foreach (var unit in units)
                    {
                        string name = unit.name;
                        if (string.IsNullOrEmpty(name))
                            name = creatureRaws[unit.race.mat_type].caste[unit.race.mat_index].caste_name[0];
                        if (!string.IsNullOrEmpty(filter) && (filterParts || filterName))
                        {
                            bool matched = false;
                            if(filterToken)
                                matched = creatureRaws[unit.race.mat_type].creature_id.ToUpper().Contains(filter.ToUpper());
                            if (filterName)
                                matched = name.ToUpper().Contains(filter.ToUpper());
                            if (filterParts)
                            {
                                foreach (var item in unit.inventory)
                                {
                                    if (!ItemRaws.Instance.ContainsKey(item.item.type))
                                        continue;
                                    matched = ItemRaws.Instance[item.item.type].id.ToUpper().Contains(filter.ToUpper());
                                    if (matched)
                                        break;
                                }
                            }
                            if (!matched)
                                continue;
                        }
                        var creatureBase = new GameObject().AddComponent<CreatureBody>();
                        creatureBase.name = name;
                        creatureBase.race = creatureRaws[unit.race.mat_type];
                        creatureBase.caste = creatureRaws[unit.race.mat_type].caste[unit.race.mat_index];
                        creatureBase.unit = unit;
                        creatureBase.MakeBody();
                        creatureBase.UpdateUnit(unit);
                        creatureBase.transform.localRotation = Quaternion.identity;
                        if (prevCreature != null)
                        {
                            creatureBase.transform.position = new Vector3(prevCreature.transform.position.x + prevCreature.bounds.max.x - creatureBase.bounds.min.x, 0, 0);
                        }
                        prevCreature = creatureBase;
                    }
                    watch.Stop();
                    Debug.Log(string.Format("Took {0}ms to create {1} creatures, averaging {2}ms per creature.", watch.ElapsedMilliseconds, filteredRaws.Count, (float)watch.ElapsedMilliseconds / filteredRaws.Count));
                }
            }
        }
    }

    private void RefilterList()
    {
        if (creatureRaws == null)
        {
            filteredRaws = null;
            return;
        }
        if (filteredRaws == null)
            filteredRaws = new List<CreatureRaw>();
        filteredRaws.Clear();
        foreach (var creature in creatureRaws)
        {
            if (FitsFilter(creature))
                filteredRaws.Add(creature);
        }
    }
}
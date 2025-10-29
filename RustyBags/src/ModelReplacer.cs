using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using Managers;
using UnityEngine;

namespace RustyBags;

public class ModelReplacer
{
    public static ZNetScene? _scene;
    public static List<ModelReplacer> replacers = new();
    public GameObject Prefab;
    public Dictionary<string, ReplacementInfo> replacements = new();

    public ModelReplacer(GameObject prefab)
    {
        Prefab = prefab;
        replacers.Add(this);
    }

    public void Add(string child, ReplacementInfo info)
    {
        replacements.Add(child, info);
    }

    public void Replace()
    {
        if (_scene == null) return;
        foreach (var replacement in replacements)
        {
            var target = Prefab.transform.Find(replacement.Key);
            if (target == null) continue;
            var source = _scene.m_prefabs.Find(x => x.name == replacement.Value.source);
            if (source == null) continue;
            var model = source.transform.Find(replacement.Value.target);
            if (model == null) continue;
            var renderer = model.GetComponent<MeshRenderer>();
            var filter = model.GetComponent<MeshFilter>();
            if (renderer == null || filter == null) continue;
            var targetFilter = target.GetComponent<MeshFilter>();
            var targetRenderer = target.GetComponent<MeshRenderer>();
            if (targetFilter == null || targetRenderer == null) continue;

            targetFilter.mesh = filter.mesh;
            targetFilter.sharedMesh = filter.sharedMesh;
            targetRenderer.sharedMaterials = renderer.sharedMaterials;
            targetRenderer.materials = renderer.materials;
            Debug.LogWarning($"Successfully replaced {Prefab.name}/{replacement.Key}");
        }
    }

    public class ReplacementInfo
    {
        public string source;
        public string target;

        public ReplacementInfo(string source, string target)
        {
            this.source = source;
            this.target = target;
        }
    }
    
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    private static class FejdStartup_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(FejdStartup __instance)
        {
            MaterialReplacer.ReplaceAllMaterialsWithOriginal();
            _scene = __instance.m_objectDBPrefab.GetComponent<ZNetScene>();
            foreach(var replacer in replacers) replacer.Replace();
        }
    }
}
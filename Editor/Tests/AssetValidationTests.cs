// MIT License

// Copyright (c) 2026 Milton Candelero

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Tests
{
    public class AssetValidationTests
    {
        static readonly string[] FoldersToScan = new[]
        {
            "Assets"
        };

        #region Scenes

        [Test]
        public void Scenes_MissingScripts_Fail()
        {
            List<string> failures = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Scene", FoldersToScan);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                if (scene.IsValid())
                {
                    GameObject[] roots = scene.GetRootGameObjects();

                    try
                    {
                        foreach (GameObject root in roots)
                        {
                            FindMissingScripts(root, path, failures);
                        }
                    }
                    finally
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            Assert.Zero(failures.Count(), $"Scenes with missing scripts:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }

        [Test]
        public void Scenes_RequiredReferences_NotNull()
        {
            List<string> failures = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Scene", FoldersToScan);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                if (scene.IsValid())
                {
                    GameObject[] roots = scene.GetRootGameObjects();
                    try
                    {
                        foreach (GameObject root in roots)
                        {
                            Component[] comps = root.GetComponentsInChildren<Component>(true);
                            foreach (Component comp in comps)
                            {
                                ScanRequiredReferences(comp, path, failures);
                            }
                        }
                    }
                    finally
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            Assert.Zero(failures.Count(), $"Scenes with missing [Required] references:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }

        [Test]
        public void Scenes_RequireComponent_Satisfied()
        {
            List<string> failures = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Scene", FoldersToScan);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                if (scene.IsValid())
                {
                    GameObject[] roots = scene.GetRootGameObjects();
                    try
                    {
                        foreach (GameObject root in roots)
                        {
                            ScanRequireComponent(root, path, failures);
                        }
                    }
                    finally

                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            Assert.Zero(failures.Count(),
                $"Scenes with missing [RequireComponent] components:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }

        [Test]
        public void Scenes_ReferencesDeletedAssets()
        {
            List<string> failures = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Scene", FoldersToScan);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                if (scene.IsValid())
                {
                    GameObject[] roots = scene.GetRootGameObjects();
                    try
                    {
                        foreach (GameObject root in roots)
                        {
                            foreach (Component comp in root.GetComponentsInChildren<Component>(true))
                            {
                                FindDeletedAssets(comp, path, failures);
                            }
                        }
                    }
                    finally
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            Assert.Zero(failures.Count(), $"Scenes contain missing references:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }

#if ENABLE_ADDRESSABLES
        [Test]
        public void Scenes_ReferencesDeletedAddressableAssets()
        {
            List<string> failures = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Scene", FoldersToScan);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                if (scene.IsValid())
                {
                    GameObject[] roots = scene.GetRootGameObjects();
                    try
                    {
                        foreach (GameObject root in roots)
                        {
                            foreach (Component comp in root.GetComponentsInChildren<Component>(true))
                            {
                                FindDeletedAddressableAssets(comp, path, failures);
                            }
                        }
                    }
                    finally
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            Assert.Zero(failures.Count(),
                $"Scenes contain missing addressable references:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }
#endif

        [Test]
        public void Scene_PrefabInstanceWithoutParent_Fail()
        {
            List<string> failures = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Scene", FoldersToScan);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                if (scene.IsValid())
                {
                    GameObject[] roots = scene.GetRootGameObjects();

                    try
                    {
                        foreach (GameObject root in roots)
                        {
                            FindPrefabInstanceWithoutParent(root, path, failures);
                        }
                    }
                    finally
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }

            Assert.Zero(failures.Count(), $"Scenes with orphaned prefab instances:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }

        #endregion

        #region Prefabs

        [Test]
        public void Prefabs_MissingScripts_Fail()
        {
            List<string> failures = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", FoldersToScan);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    FindMissingScripts(root, path, failures);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            Assert.Zero(failures.Count(), $"Prefabs with missing scripts:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }

        [Test]
        public void Prefabs_RequiredReferences_NotNull()
        {
            List<string> failures = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", FoldersToScan);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Component[] comps = root.GetComponentsInChildren<Component>(true);
                    foreach (Component comp in comps)
                    {
                        ScanRequiredReferences(comp, path, failures);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            Assert.Zero(failures.Count(),
                $"Prefabs with missing [Required] references:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }

        [Test]
        public void Prefabs_RequireComponent_Satisfied()
        {
            List<string> failures = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", FoldersToScan);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ScanRequireComponent(root, path, failures);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            Assert.Zero(failures.Count(),
                $"Prefabs with missing [RequireComponent] components:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }

        [Test]
        public void Prefabs_ReferencesDeletedAssets()
        {
            List<string> failures = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", FoldersToScan);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (Component comp in root.GetComponentsInChildren<Component>(true))
                    {
                        FindDeletedAssets(comp, path, failures);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            Assert.Zero(failures.Count(), $"Prefabs contain missing references:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }

#if ENABLE_ADDRESSABLES
        [Test]
        public void Prefabs_ReferencesDeletedAddressableAssets()
        {
            List<string> failures = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", FoldersToScan);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (Component comp in root.GetComponentsInChildren<Component>(true))
                    {
                        FindDeletedAddressableAssets(comp, path, failures);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            Assert.Zero(failures.Count(),
                $"Prefabs contain missing addressable references:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }
#endif

        [Test]
        public void Prefabs_VariantMissingParent_Fail()
        {
            List<string> failures = new List<string>();

            string[] assetPaths = AssetDatabase.GetAllAssetPaths()
                .Where(p => FoldersToScan.Any(p.StartsWith) && p.EndsWith(".prefab")).ToArray();

            foreach (string path in assetPaths)
            {
                Object asset = AssetDatabase.LoadMainAssetAtPath(path);

                if (!asset)
                {
                    failures.Add($"{path} : Prefab asset is unreadable");
                    continue;
                }

                PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(asset);

                if (prefabType == PrefabAssetType.MissingAsset || prefabType == PrefabAssetType.NotAPrefab)
                {
                    failures.Add($"{path} : Prefab Variant missing parent");
                }
            }

            Assert.Zero(failures.Count(), $"Prefab variants with missing parents:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }

        [Test]
        public void Prefabs_PrefabInstanceWithoutParent_Fail()
        {
            List<string> failures = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", FoldersToScan);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                GameObject root = PrefabUtility.LoadPrefabContents(path);

                try
                {
                    FindPrefabInstanceWithoutParent(root, path, failures);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            Assert.Zero(failures.Count(), $"Prefabs with orphaned prefab instances:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }

        #endregion

        #region Scriptables

        [Test]
        public void ScriptableObjects_MissingType_Fail()
        {
            List<string> failures = new List<string>();

            string[] assetPaths = AssetDatabase.GetAllAssetPaths()
                .Where(p => FoldersToScan.Any(p.StartsWith) && p.EndsWith(".asset")).ToArray();

            foreach (string path in assetPaths)
            {
                Object so = AssetDatabase.LoadMainAssetAtPath(path);
                if (!so)
                {
                    failures.Add($"{path} : ScriptableObject missing parent script or type not found");
                }
            }

            Assert.Zero(failures.Count(), $"ScriptableObjects with missing scripts:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }

        [Test]
        public void ScriptableObjects_RequiredReferences_NotNull()
        {
            List<string> failures = new List<string>();
            string[] soGuids = AssetDatabase.FindAssets("t:ScriptableObject", FoldersToScan);

            foreach (string guid in soGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (!so) continue; // missing script already handled in other test
                ScanRequiredReferences(so, path, failures);
            }

            Assert.Zero(failures.Count(),
                $"ScriptableObjects with missing [Required] references:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }

        [Test]
        public void ScriptableObjects_ReferencesDeletedAssets()
        {
            List<string> failures = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", FoldersToScan);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject soAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                FindDeletedAssets(soAsset, path, failures);
            }

            Assert.Zero(failures.Count(),
                $"ScriptableObjects contain missing references:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }

#if ENABLE_ADDRESSABLES
        [Test]
        public void ScriptableObjects_ReferencesDeletedAddressableAssets()
        {
            List<string> failures = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", FoldersToScan);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject soAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (!soAsset) continue; // missing parent script handled elsewhere

                FindDeletedAddressableAssets(soAsset, path, failures);
            }

            Assert.Zero(failures.Count(),
                $"ScriptableObjects contain missing addressable references:\n\t{string.Join("\n\t", CollapseFails(failures))}");
        }
#endif

        #endregion

        #region Helpers

        static void FindMissingScripts(GameObject root, string assetPath, List<string> outFailures)
        {
            if (!root) return;

            foreach (GameObject go in root.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject))
            {
                // We could get ALL components in one go from the root, but we couldn't get the hierarchy to know where the object that failed lives.
                Component[] comps = go.GetComponents<Component>();
                foreach (Component comp in comps)
                {
                    if (!comp)
                    {
                        outFailures.Add($"{assetPath} : {GetHierarchy(go)} (missing script)");
                        break;
                    }
                }
            }
        }

        static void ScanRequiredReferences(Object componentOrScriptable, string assetPath, List<string> outFailures)
        {
            if (!componentOrScriptable) return;

            Type type = componentOrScriptable.GetType();

            // Check missing [Required] fields
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo f in fields)
            {
                // Only UnityEngine.Object references
                if (!typeof(Object).IsAssignableFrom(f.FieldType)) continue;

                // Only fields with [Required]
                bool hasRequired = f.GetCustomAttributes(true).Any(a => a.GetType().Name == "RequiredAttribute");
                if (!hasRequired) continue;

                object val;
                try
                {
                    val = f.GetValue(componentOrScriptable);
                }
                catch
                {
                    continue;
                }

                if ((Object)val == null)
                {
                    string location = componentOrScriptable is Component comp ? GetHierarchy(comp.gameObject) : componentOrScriptable.name;
                    outFailures.Add($"{assetPath} : {location} : {type.Name}.{f.Name} (null reference)");
                }
            }
        }

        static void ScanRequireComponent(GameObject root, string assetPath, List<string> outFailures)
        {
            if (!root) return;

            Component[] comps = root.GetComponentsInChildren<Component>(true);
            foreach (Component comp in comps)
            {
                if (!comp) continue;

                Type type = comp.GetType();

                IEnumerable<RequireComponent> attrs = type.GetCustomAttributes(typeof(RequireComponent), true)
                    .Cast<RequireComponent>();

                foreach (RequireComponent req in attrs)
                {
                    if (req.m_Type0 != null && !comp.gameObject.GetComponent(req.m_Type0))
                        outFailures.Add($"{assetPath} : {GetHierarchy(comp.gameObject)} : {type.Name} requires {req.m_Type0.Name} but missing");
                    if (req.m_Type1 != null && !comp.gameObject.GetComponent(req.m_Type1))
                        outFailures.Add($"{assetPath} : {GetHierarchy(comp.gameObject)} : {type.Name} requires {req.m_Type1.Name} but missing");
                    if (req.m_Type2 != null && !comp.gameObject.GetComponent(req.m_Type2))
                        outFailures.Add($"{assetPath} : {GetHierarchy(comp.gameObject)} : {type.Name} requires {req.m_Type2.Name} but missing");
                }
            }
        }
        
        static void FindDeletedAssets(Object componentOrScriptable, string assetPath, List<string> outFailures)
        {
            if (!componentOrScriptable) return;

            SerializedObject so = new SerializedObject(componentOrScriptable);
            SerializedProperty prop = so.GetIterator();

            while (prop.NextVisible(true))
            {
                if (prop.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                if (prop.objectReferenceInstanceIDValue != 0 &&
                    !prop.objectReferenceValue)
                {
                    string location = componentOrScriptable is Component comp ? GetHierarchy(comp.gameObject) : componentOrScriptable.name;

                    outFailures.Add(
                        $"{assetPath} : {location} : {componentOrScriptable.GetType().Name}.{prop.propertyPath} (Missing)"
                    );
                }
            }
        }

#if ENABLE_ADDRESSABLES
        static void FindDeletedAddressableAssets(Object componentOrScriptable, string assetPath, List<string> outFailures)
        {
            if (!componentOrScriptable) return;

            SerializedObject so = new SerializedObject(componentOrScriptable);
            SerializedProperty prop = so.GetIterator();

            while (prop.NextVisible(true))
            {
                if (prop.propertyType != SerializedPropertyType.Generic)
                    continue;

                // Hacky shenanigans start here...
                if (!prop.type.StartsWith("AssetReference"))
                    continue;

                SerializedProperty guidProp = prop.FindPropertyRelative("m_AssetGUID");
                if (guidProp == null)
                    continue;

                var addressableGuid = guidProp.stringValue;

                // "none" is still kinda valid
                if (string.IsNullOrEmpty(addressableGuid))
                    continue;

                var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
                if (settings && settings.FindAssetEntry(addressableGuid) == null)
                {
                    string location = componentOrScriptable is Component comp ? GetHierarchy(comp.gameObject) : componentOrScriptable.name;

                    outFailures.Add(
                        $"{assetPath} : {location} : {componentOrScriptable.GetType().Name}.{prop.propertyPath} (Missing Addressable)"
                    );
                }
            }
        }
#endif

        static void FindPrefabInstanceWithoutParent(GameObject root, string assetPath, List<string> outFailures)
        {
            // Stack approach to avoid recursion
            Stack<GameObject> stack = new Stack<GameObject>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var go = stack.Pop();

                if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
                {
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
                    var parentPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);

                    if (!source || string.IsNullOrEmpty(parentPath))
                    {
                        outFailures.Add(
                            $"{assetPath} : {GetHierarchy(go)} (orphaned prefab instance)"
                        );

                        // The prefab is deemed corrupt/broken/orphaned. Do not push its descendant into the stack
                        continue;
                    }
                }

                var t = go.transform;
                for (int i = 0; i < t.childCount; i++)
                    stack.Push(t.GetChild(i).gameObject);
            }
        }

        static string GetHierarchy(GameObject go)
        {
            List<string> parts = new List<string>();
            Transform t = go.transform;
            while (t != null)
            {
                parts.Add(t.name);
                t = t.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        #endregion

        static List<string> CollapseFails(List<string> fails)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            List<string> ordered = new List<string>();

            foreach (string line in fails)
            {
                if (!counts.TryAdd(line, 1))
                    counts[line]++;
                else
                    ordered.Add(line);
            }

            IEnumerable<string> result = ordered
                .Select(l => counts[l] > 1 ? $"{l} (x{counts[l]})" : l);

            return result.ToList();
        }
    }
}
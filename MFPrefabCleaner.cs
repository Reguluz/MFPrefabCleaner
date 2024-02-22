using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moonflow;
using Moonflow.Core;
using Moonflow.Tools.MFPrefabCleaner;
using Moonflow.Utility;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;



public class MFPrefabCleaner: EditorWindow
{
    public static MFPrefabCleanFunction[] Functions;
    // public string filterType;
    // public Type type;
    public Object folder;
    public bool SearchChild;
    public ResultType Result;

    public string pathFolder;
    // public string[] indicators = new[]
    //     {"multi_compile", "shader_feature"};
    public List<GameObject> objects;
    
    private static MFPrefabCleaner instance;
    private string[] oldKeywords;
    private List<string> shaderKeywords;
    private List<string> newKeywords;
    private bool foldout = false;
    private Object _folder;
    private int index = 0;
    private string errorResult = "";
    private string[] AssetPaths;
    private string changelist;
    private event Func<GameObject, string, bool> realProcess;
    private static readonly int PAGENUMBER = 20;
    private bool cancel;
    
    public enum ResultType
    {
        OutputList = 0,
        PrefabSave = 1
    }
    [MenuItem("Moonflow/Tools/Assets/Prefab清理")]
    public static void ShowWindow()
    {
        if(!instance)instance = GetWindow<MFPrefabCleaner>("Moonflow Prefab Cleaner");
        instance.minSize = new Vector2(905, 605);
        instance.Focus();
        Functions = new MFPrefabCleanFunction[]
        {
            new MFParticleHiddenMatClean(),
            new MFSubEmitterChecker(),
            // new MFMatRefCacheClean()
        };
    }


    public void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope("box",new []{GUILayout.ExpandWidth(false), GUILayout.Width(300), GUILayout.Height(600)}))
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.PrefixLabel("清理模式");
                folder = EditorGUILayout.ObjectField("目标清理文件夹", folder, typeof(Object), false,
                    GUILayout.MaxWidth(300));
                SearchChild = EditorGUILayout.Toggle("需要遍历子对象", SearchChild);
                Result = (ResultType) EditorGUILayout.Popup("结果处理", (int) Result, new[] {"Prefab保存", "输出列表"});
                EditorGUILayout.Space(10);
                if (GUILayout.Button("Clean"))
                {
                    SetFunc();
                    int index = 0;
                    errorResult = "";
                    changelist = "";
                    AssetDatabase.StartAssetEditing();
                    EditorApplication.update = delegate()
                    {
                        cancel = EditorUtility.DisplayCancelableProgressBar("清理中",
                            $"{objects[index].name}({index}/{objects.Count})", (float) index / (float) objects.Count);
                        Process(objects[index]);
                        index++;
                        if (cancel || index >= objects.Count)
                        {
                            EditorUtility.ClearProgressBar();
                            EditorApplication.update = null;
                            index = 0;
                            AssetDatabase.StopAssetEditing();
                            AssetDatabase.Refresh();
                        }
                    };
                    EditorGUILayout.TextArea(changelist);
                }
                EditorGUILayout.Space(10);
                foreach (var funcs in Functions)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        funcs.needProcess = EditorGUILayout.Toggle(funcs.needProcess, new []{GUILayout.Width(20)});
                        EditorGUILayout.LabelField(funcs.showName,new []{GUILayout.Width(280)});
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope("box", new []{GUILayout.ExpandWidth(false), GUILayout.Width(300), GUILayout.Height(600)}))
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.PrefixLabel("Prefab列表");
                DrawPrefabList();
            }

            using (new EditorGUILayout.VerticalScope(new []{GUILayout.ExpandWidth(false), GUILayout.Width(300), GUILayout.Height(600)}))
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.PrefixLabel("清理结果");
                EditorGUILayout.TextArea(errorResult);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                if (_folder != folder)
                {
                    _folder = folder;
                    if (_folder != null)
                    {
                        objects = new List<GameObject>();
                        pathFolder = AssetDatabase.GetAssetPath(_folder);
                        AssetPaths = AssetDatabase.FindAssets($"t:Prefab", new[] {pathFolder});
                        for (int i = 0; i < AssetPaths.Length; i++)
                        {
                            objects.Add(AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(AssetPaths[i]),
                                typeof(GameObject)) as GameObject);
                        }
                    }
                }
            }
        }
    }

    private void DrawPrefabList()
    {
        MFEditorUI.DrawFlipList(objects, ref index, ref foldout, PAGENUMBER);
    }

    public void SetFunc()
    {
        realProcess = default;
        foreach (var funcs in Functions)
        {
            if (funcs.needProcess) realProcess += funcs.Process;
        }
    }
    public bool Process(GameObject obj, bool isRoot = true, string rootname = "")
    {
        if (isRoot) rootname = obj.name;
        GameObject go;
        bool hasChanged = false;
        if (isRoot)
        {
            go = PrefabUtility.InstantiatePrefab(obj) as GameObject;
            // PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        else
        {
            go = obj;
        }
        
        //开始清理*****************************************
        if(realProcess!= default)hasChanged = realProcess(go, rootname);
        //清理结束*************************
        if (SearchChild)
        {
            if (go.transform.childCount != 0)
            {
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    GameObject child = go.transform.GetChild(i).gameObject;
                    hasChanged |= Process(child, false, rootname);
                }
            }
        }
        Debug.Log($"{go} from root{rootname} changed: {hasChanged}");

        if (isRoot)
        {
            if (hasChanged)
            {
                if (Result != ResultType.OutputList)
                {
                    try
                    {
                        PrefabUtility.ApplyPrefabInstance(go, InteractionMode.AutomatedAction);
                        // PrefabUtility.SavePrefabAsset(go);
                   
                        Debug.Log($"处理并保存了Prefab {go.name}");
                    }
                    catch
                    {
                        Debug.Log($"Can't save {go}");
                    }
                }
                else
                {
                    errorResult += $"处理了{go.name}\n";
                }
            }
            
            DestroyImmediate(go);
        }
        return hasChanged;
    }
}

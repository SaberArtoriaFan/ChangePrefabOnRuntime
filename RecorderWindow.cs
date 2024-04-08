using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

public class RecorderWindow : OdinEditorWindow
{
    [MenuItem("Tools/Recorder")]
    public static void Open()
    {
        var rw = EditorWindow.GetWindow<RecorderWindow>();
        rw.Show();
    }
    //[Button("开关监听", Icon = SdfIconType.Sun), HorizontalGroup("L")] void ListenChange()
    //{

    //    if (Application.isPlaying)
    //    {
    //        IsListenChange = !IsListenChange;

    //        if (listener == null)
    //        {
    //            GameObject go = new GameObject("RecorderListener");
    //            listener = go.AddComponent<RecorderMono>();
    //            DontDestroyOnLoad(listener.gameObject);
    //        }
    //        listener.IsListenChange = IsListenChange;
    //        listener.enabled = IsListenChange;
    //    }

    //}

    //[LabelText("***---***"), ReadOnly, HorizontalGroup("L")] public bool IsListenChange = false;

    [OnValueChanged(nameof(SetPrefab))] public GameObject prefab;

    [HorizontalGroup(GroupName ="animation",LabelWidth =75,MaxWidth =150), OnValueChanged(nameof(SetSaveClip))] public bool IsSaveClip;


    [HorizontalGroup(GroupName = "animation",LabelWidth =30),ShowInInspector, NonSerialized] public AnimationClip clip;
    [SerializeField] public List<RecorderData> recorderDatas = new List<RecorderData>();
    RecorderMono listener = null;
    protected override void OnEnable()
    {
        base.OnEnable();
    }
    private void SetSaveClip()
    {
        if (listener != null)
            listener.isSaveAsClip = IsSaveClip;
    }

    void SetPrefab()
    {
        if(prefab == null && listener != null)
        {
            listener.prefabRoot = prefab;
            listener.enabled = false;
            return;
        }
        if (Application.isPlaying)
        {
            if (listener == null)
            {
                GameObject go = new GameObject("RecorderListener");
                listener = go.AddComponent<RecorderMono>();
                listener.isSaveAsClip = this.IsSaveClip;
                DontDestroyOnLoad(listener.gameObject);
            }
            if (listener == null) prefab = null;
            else
            {
                if (AssetDatabase.Contains(prefab))
                {
                    var sceneGO=GameObject.Find(prefab.name);
                    if (sceneGO == null)
                        sceneGO = GameObject.Find($"{prefab.name}(Clone)");
                    if(sceneGO!=null)
                        listener.prefabRoot = sceneGO;
                    else
                    {
                        Debug.LogError($"场景中没有名为{prefab.name}的GameObject！！！");
                        prefab = null;
                    }    
                }
                else
                    listener.prefabRoot = prefab;
            }
            listener.enabled = prefab != null;
        }

    }

    [Button]
    void Apply()
    {
        if (prefab == null) return;
        foreach (var v in recorderDatas)
            v.Apply(prefab,false);
    }
    [Button]
    void Clear()
    {
        recorderDatas.Clear();
    }
}

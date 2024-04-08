using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using Object = UnityEngine.Object;
using Type = System.Type;
using System.IO;
using System.Text;

#if UNITY_EDITOR
using UnityEditor.Animations;
using UnityEditor;
#endif
public class RecorderMono : MonoBehaviour
{
    bool IsListenChange=>prefabRoot!=null;
    public bool isRecord;
    float startTime = 0;
    public bool isSaveAsClip = false;
    private GameObject listenRoot;
    GameObjectRecorder recorder;
     AnimationClip clip;
    public GameObject prefabRoot;
    void Update()
    {

        if (IsListenChange)
        {
            RealListen();
        }
    }
#if UNITY_EDITOR
   public  void RealListen()
    {
        GameObject go = Selection.activeGameObject;
        if (prefabRoot == null) go = null;
        if (go == gameObject) return;
        if (go == null)
        {
            if (isRecord) EndRecorder();
            else return;
        }
        if (go == listenRoot) return;
        //一定要是预制体名称的下部
        if (go.transform.IsChildOf(prefabRoot.transform) == false) return;

        if (listenRoot != null && isRecord)
        {
            EndRecorder();
        }
        if (go.scene.IsValid())
            StartRecorder(go);
    }

    public void StartRecorder(GameObject gameObject)
    {
        Debug.Log("开始录制");
        recorder = new GameObjectRecorder(gameObject);
        recorder.BindAll(gameObject, false);
        recorder.TakeSnapshot(0);
        startTime = Time.time;
        isRecord = true;
        listenRoot = gameObject;
    }
    public void EndRecorder()
    {

        recorder.TakeSnapshot(Time.time - startTime);

        recorder.TakeSnapshot(Time.time - startTime + 1);
        EditorWindow.GetWindow<RecorderWindow>().clip = new AnimationClip();
        clip = EditorWindow.GetWindow<RecorderWindow>().clip;

        recorder.SaveToClip(clip, 60);

        if (isSaveAsClip)
        {
            clip.name = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            var path = $"Assets/Clips/{clip.name}.playable";
            if (Directory.Exists(Path.GetDirectoryName(path)) == false)
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(clip, path);
            AssetDatabase.SaveAssets();
        }

        Calculate(recorder.root);

        recorder.ResetRecording();
        isRecord = false;
        listenRoot = null;
    }

    void Calculate(GameObject root)
    {
        var endTime = clip.length;
        List<(Type, string, float)> floatList = new List<(Type, string, float)>();
        List<(Type, string, Object)> objetsList = new List<(Type, string, Object)>();

        var vs = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        foreach (var v in vs)
        {
            var objs = AnimationUtility.GetObjectReferenceCurve(clip, v);
            if (objs != null && objs.Length > 1)
            {
                if (objs[0].value != objs[objs.Length - 1].value)
                {
                    Debug.LogError($"OBJ: Name:{v.propertyName},Origin:{objs[0].value},Now:{objs[objs.Length - 1].value}");
                    objetsList.Add((v.type, v.propertyName, objs[objs.Length - 1].value));
                }
            }
        }
        var cs = AnimationUtility.GetCurveBindings(clip);
        foreach (var v in cs)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, v);
            if (curve != null && curve.length > 1)
            {
                var origin = curve[0];
                var end = curve[curve.length - 1];
                if (origin.value != end.value)
                {
                    Debug.LogError($"Float: Name:{v.propertyName} Type:{v.type.FullName} ,Origin:{origin.value},Now:{end.value}");
                    floatList.Add((v.type, v.propertyName, end.value));

                }
                else
                    Debug.Log($"Float: Name:{v.propertyName},Type:{v.type.FullName} , Origin:{origin.value},Now:{end.value}");

            }
        }
        if (floatList.Count > 0 || objetsList.Count > 0)
        {
            Dictionary<System.Type, RecorderData.RecorderTypeData> dict = new Dictionary<Type, RecorderData.RecorderTypeData>();
            foreach (var obj in floatList)
            {
                if (dict.TryGetValue(obj.Item1, out var dta) == false)
                {
                    Object o = root.GetComponent(obj.Item1);
                    dta = new RecorderData.RecorderTypeData() { typeFullName = obj.Item1.FullName, AssemblyFullName = obj.Item1.Assembly.FullName };
                    dict.Add(obj.Item1, dta);
                }
                dta.floatList.Add(new RecorderData.FloatS(obj.Item2, obj.Item3));
            }
            foreach (var obj in objetsList)
            {
                if (dict.TryGetValue(obj.Item1, out var dta) == false)
                {

                    Object o = root.GetComponent(obj.Item1);
                    dta = new RecorderData.RecorderTypeData() { typeFullName = obj.Item1.FullName,AssemblyFullName=obj.Item1.Assembly.FullName};
                    dict.Add(obj.Item1, dta);
                }
                dta.objectsList.Add(new RecorderData.ObjectS(obj.Item2, obj.Item3));
            }

            var data = new RecorderData() {path= CalcuPath(), recorderTypeDatas = dict.Values.ToList() };
            //List<RecorderData> recorderDatas = new List<RecorderData>();
            EditorWindow.GetWindow<RecorderWindow>().recorderDatas.Add(data);
        }
    }
    string CalcuPath()
    {
        string s = "";
        var tr = recorder.root.transform;
        while(tr!=prefabRoot.transform)
        {
            if(string.IsNullOrEmpty(s))
                s = $"{tr.gameObject.name}";
            else
                s = $"{tr.gameObject.name}/{s}";
            tr = tr?.parent;
        }
        return s;
    }
#endif
}

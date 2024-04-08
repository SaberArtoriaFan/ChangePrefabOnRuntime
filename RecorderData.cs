using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[System.Serializable]
public class RecorderData 
{
    [Serializable]
    public struct FloatS
    {
        [LabelText("属性名:"),ReadOnly, HorizontalGroup(GroupName ="Base", LabelWidth = 60)]
        public string pName;
        [LabelText("值:"),HorizontalGroup(GroupName = "Base",LabelWidth =30)]
        public float f;

        public FloatS(string pName, float f)
        {
            this.pName = pName;
            this.f = f;
        }
    }
    [Serializable]
    public struct ObjectS
    {
        [LabelText("属性名:"),ReadOnly, HorizontalGroup(GroupName = "Base", LabelWidth = 60)]
        public string pName;
        [LabelText("值:"), HorizontalGroup(GroupName = "Base", LabelWidth = 30)]
        public object obj;

        public ObjectS(string pName, object obj)
        {
            this.pName = pName;
            this.obj = obj;
        }
    }
    [System.Serializable]
    public class RecorderTypeData
    {
        //public Object obj;
        [LabelText("类型全名"),ReadOnly]
        public string typeFullName;
        [LabelText("程序集全名"), ReadOnly]
        public string AssemblyFullName;
        [LabelText("值类型更改")]
        public List<FloatS> floatList = new List<FloatS>();
        [LabelText("引用类型更改")]
        public List<ObjectS> objectsList=new List<ObjectS>();
    }
    [ReadOnly]
    public string path;

    public List<RecorderTypeData> recorderTypeDatas = new List<RecorderTypeData>();
    //public List<(Type,string, float)> floatList = new List<(Type,string, float)>();
    //public List<(Type, string, Object)> objetsList = new List<(Type, string, Object)>();
    internal void SetToValueOfTarget(SerializedProperty serializedProperty,float fv)
    {
        switch (serializedProperty.propertyType)
        {
            case SerializedPropertyType.Integer:
                serializedProperty.intValue = (int)fv;
                break;
            case SerializedPropertyType.Boolean:
                serializedProperty.boolValue = fv==0?false:true;
                break;
            case SerializedPropertyType.Float:
                serializedProperty.floatValue = fv;
                break;
            case SerializedPropertyType.LayerMask:
                serializedProperty.intValue = (int)fv;
                break;
            case SerializedPropertyType.Enum:
                serializedProperty.enumValueIndex = (int)fv;
                break;
            case SerializedPropertyType.ArraySize:
                serializedProperty.intValue = (int)fv;
                break;
            case SerializedPropertyType.Character:
                serializedProperty.intValue = (int)fv;
                break;
            default:
                Debug.LogError($"出现未知的序列化类型:{serializedProperty.propertyType}");
                break;
        }
    }
    public void Apply(GameObject rootGo,bool autoAddMono)
    {

        GameObject root = rootGo;
        if(string.IsNullOrEmpty(path)==false) root = rootGo.transform.Find(path)?.gameObject;
        if (root == null)
        {
            return;
        }
        for(int i=0;i<recorderTypeDatas.Count;i++)           
        {
            var data = recorderTypeDatas[i];
            var assembly = Assembly.Load(data.AssemblyFullName);
            var type =  assembly.GetType(data.typeFullName);
            var obj = root.GetComponent(type);
            if (obj == null)
            {
                if (!autoAddMono)
                {
                    Debug.LogError($"Root身上并无该类型序列化脚本{data.typeFullName},可能是动态生成，如是请忽略本Error");
                    continue;
                }else
                    obj = root.AddComponent(Type.GetType(data.typeFullName));
            }
            var so = new SerializedObject(obj);
            foreach(var tp in data.objectsList)
            {
                var p1 = so.FindProperty(tp.pName);
                if (p1.propertyType == SerializedPropertyType.ObjectReference)
                {
                    p1.objectReferenceValue = tp.obj as Object;
                }
                else if(p1.propertyType == SerializedPropertyType.ManagedReference)
                    p1.managedReferenceValue=tp.pName;
                else
                    Debug.LogError($"出现未知的序列化类型:{p1.propertyType}");
                Debug.Log($"GO:{root.name},Component:{obj},设置字段->{tp.pName},字段类型:{p1?.propertyType}:{tp.obj}");
            }
            foreach(var v in data.floatList)
            {
                var p1 = so.FindProperty(v.pName);
                SetToValueOfTarget(p1, v.f);
                Debug.Log($"GO:{root.name},Component:{obj.GetType()},设置字段->{v.pName},字段类型:{p1?.propertyType} 值:{v.f}");
            }


            so.ApplyModifiedProperties();
        }
        EditorUtility.SetDirty(root.gameObject);
        AssetDatabase.Refresh();
        Debug.Log("应用完成了!!!");
    }
}

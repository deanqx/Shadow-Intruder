using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UpdateData), true)]
public class UpdateDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        UpdateData data = (UpdateData)target;

        if (GUILayout.Button("Update"))
        {
            data.UpdateValues();
        }
    }
}

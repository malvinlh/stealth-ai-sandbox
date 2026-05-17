using UnityEditor;
using UnityEngine;
using StealthGame;

[CustomEditor(typeof(GuardProfile))]
public class GuardProfileEditor : Editor
{
    private bool _movFold = true, _visFold = true, _hearFold = true, _commFold = true, _behFold = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        _movFold = EditorGUILayout.BeginFoldoutHeaderGroup(_movFold, "Movement");
        if (_movFold)
        {
            DrawSlider("patrolSpeed",     0.05f, 2f,  "Patrol Speed");
            DrawSlider("suspiciousSpeed", 0.05f, 2f,  "Suspicious Speed");
            DrawSlider("alertSpeed",      0.05f, 3f,  "Alert Speed");
            DrawSlider("rotationSpeed",   30f,   720f,"Rotation Speed (°/s)");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        _visFold = EditorGUILayout.BeginFoldoutHeaderGroup(_visFold, "Vision");
        if (_visFold)
        {
            DrawSlider("visionRange",     0.05f, 3f,  "Vision Range");
            DrawSlider("fovAngle",        10f,   360f,"FOV Angle (°)");
            DrawSlider("coneResolution",  10,    120, "Cone Resolution (rays)");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("obstacleLayerMask"), new GUIContent("Obstacle Layer Mask"));
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        _hearFold = EditorGUILayout.BeginFoldoutHeaderGroup(_hearFold, "Hearing");
        if (_hearFold)
            DrawSlider("hearingRadius", 0f, 3f, "Hearing Radius");
        EditorGUILayout.EndFoldoutHeaderGroup();

        _commFold = EditorGUILayout.BeginFoldoutHeaderGroup(_commFold, "Communication");
        if (_commFold)
            DrawSlider("communicationRadius", 0f, 5f, "Communication Radius");
        EditorGUILayout.EndFoldoutHeaderGroup();

        _behFold = EditorGUILayout.BeginFoldoutHeaderGroup(_behFold, "Behaviour");
        if (_behFold)
        {
            DrawSlider("suspicionDuration",      0.5f,  10f,  "Suspicion Duration (s)");
            DrawSlider("searchDuration",         1f,    20f,  "Search Duration (s)");
            DrawSlider("caughtRange",            0.02f, 1f,   "Caught Range");
            DrawSlider("searchWanderRadius",     0.05f, 2f,   "Wander Radius");
            DrawSlider("waypointArriveThreshold",0.01f, 0.5f, "Arrive Threshold");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSlider(string propName, float min, float max, string label)
    {
        var prop = serializedObject.FindProperty(propName);
        if (prop == null) return;
        prop.floatValue = EditorGUILayout.Slider(label, prop.floatValue, min, max);
    }

    private void DrawSlider(string propName, int min, int max, string label)
    {
        var prop = serializedObject.FindProperty(propName);
        if (prop == null) return;
        prop.intValue = EditorGUILayout.IntSlider(label, prop.intValue, min, max);
    }
}

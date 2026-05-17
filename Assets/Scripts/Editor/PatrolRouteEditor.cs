using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using StealthGame;

[CustomEditor(typeof(PatrolRoute))]
public class PatrolRouteEditor : Editor
{
    private PatrolRoute _route;
    private List<Vector2> _waypoints;
    private List<float> _waitTimes;

    private void OnEnable()
    {
        _route = (PatrolRoute)target;
        _waypoints = _route.GetWaypointsRaw();
        _waitTimes = _route.GetWaitTimesRaw();
        SyncWaitTimesLength();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Waypoints  (position + optional wait time in seconds)", EditorStyles.boldLabel);

        SyncWaitTimesLength();

        for (int i = 0; i < _waypoints.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            _waypoints[i] = EditorGUILayout.Vector2Field($"[{i}]", _waypoints[i]);
            _waitTimes[i] = Mathf.Max(0f, EditorGUILayout.FloatField("wait", _waitTimes[i], GUILayout.Width(110)));
            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                Undo.RecordObject(_route, "Remove Waypoint");
                _waypoints.RemoveAt(i);
                if (i < _waitTimes.Count) _waitTimes.RemoveAt(i);
                EditorUtility.SetDirty(_route);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Waypoint"))
        {
            Undo.RecordObject(_route, "Add Waypoint");
            Vector2 last = _waypoints.Count > 0 ? _waypoints[^1] : (Vector2)_route.transform.position;
            _waypoints.Add(last + Vector2.right);
            _waitTimes.Add(0f);
            EditorUtility.SetDirty(_route);
        }
        if (GUILayout.Button("Clear All"))
        {
            Undo.RecordObject(_route, "Clear Waypoints");
            _waypoints.Clear();
            _waitTimes.Clear();
            EditorUtility.SetDirty(_route);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("Shift+Click in Scene to append a waypoint at that position. Set wait time > 0 to make the guard pause at that waypoint before advancing.", MessageType.Info);
    }

    private void OnSceneGUI()
    {
        if (_waypoints == null) return;

        // Draw position handles for each waypoint
        for (int i = 0; i < _waypoints.Count; i++)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(_waypoints[i], Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_route, "Move Waypoint");
                _waypoints[i] = newPos;
                EditorUtility.SetDirty(_route);
            }

            // Index label + wait-time annotation when set
            Handles.color = Color.white;
            string label = (i < _waitTimes.Count && _waitTimes[i] > 0f)
                ? $"{i}  ({_waitTimes[i]:0.##}s)"
                : i.ToString();
            Handles.Label((Vector3)(Vector2)_waypoints[i] + Vector3.up * 0.25f, label);
        }

        // Shift+Click to add waypoint
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && e.shift)
        {
            Vector2 worldPos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            Undo.RecordObject(_route, "Add Waypoint via Scene");
            _waypoints.Add(worldPos);
            _waitTimes.Add(0f);
            EditorUtility.SetDirty(_route);
            e.Use();
        }
    }

    // Keep the parallel waitTimes list the same length as waypoints.
    // Handles backward-compatible loads of routes saved before waitTimes existed.
    private void SyncWaitTimesLength()
    {
        if (_waitTimes == null || _waypoints == null) return;
        while (_waitTimes.Count < _waypoints.Count) _waitTimes.Add(0f);
        while (_waitTimes.Count > _waypoints.Count) _waitTimes.RemoveAt(_waitTimes.Count - 1);
    }
}

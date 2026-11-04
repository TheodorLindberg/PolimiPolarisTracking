// Put on a Camera. Press "Fit To Scene" in the Inspector (works in Play mode)
// to move the camera back along its current view direction until every
// visible object in the scene is in view. The camera's rotation is kept.
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    [Tooltip("Extra space around the objects (1 = tight fit)")]
    public float padding = 1.2f;

    [Tooltip("Also adjust near/far clip planes so nothing gets cut off")]
    public bool adjustClipPlanes = true;

    public void FitToScene()
    {
        Camera cam = GetComponent<Camera>();
        if (!TryGetSceneBounds(out Bounds bounds))
        {
            Debug.LogWarning("CameraFit: no visible objects to fit.");
            return;
        }

        // Use a sphere around the bounds so the fit works from any view direction
        float radius = Mathf.Max(bounds.extents.magnitude, 0.01f) * padding;
        float distance;

        if (cam.orthographic)
        {
            cam.orthographicSize = radius / Mathf.Min(1f, cam.aspect);
            distance = radius * 2f;
        }
        else
        {
            float halfV = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float halfH = Mathf.Atan(Mathf.Tan(halfV) * cam.aspect);
            distance = radius / Mathf.Sin(Mathf.Min(halfV, halfH));
        }

        transform.position = bounds.center - transform.forward * distance;

        if (adjustClipPlanes)
        {
            cam.nearClipPlane = Mathf.Max(0.01f, (distance - radius) * 0.5f);
            cam.farClipPlane = (distance + radius) * 2f;
        }
    }

    bool TryGetSceneBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (r.transform.IsChildOf(transform)) continue; // skip things attached to the camera

            if (found) bounds.Encapsulate(r.bounds);
            else { bounds = r.bounds; found = true; }
        }
        return found;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CameraFit))]
public class CameraFitEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();

        if (GUILayout.Button("Fit To Scene", GUILayout.Height(30)))
        {
            var fit = (CameraFit)target;
            Undo.RecordObject(fit.transform, "Fit Camera To Scene");
            Undo.RecordObject(fit.GetComponent<Camera>(), "Fit Camera To Scene");
            fit.FitToScene();
        }
    }
}
#endif

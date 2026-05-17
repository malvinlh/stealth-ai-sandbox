using UnityEngine;

namespace StealthGame
{
    /// <summary>
    /// Real-time triangle-fan mesh that visualises the guard's vision cone, clipped
    /// against obstacles by per-ray Physics2D raycasts. The mesh is regenerated each
    /// LateUpdate; the colour is driven by <see cref="SetStateColor"/> from each state's
    /// Enter callback.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class FieldOfViewMesh : MonoBehaviour
    {
        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private GuardProfile _profile;

        // World-space facing direction. Updated each frame by GuardController so the cone
        // tracks the agent's heading without the parent transform having to rotate.
        public Vector2 FacingDirection { get; set; } = Vector2.up;

        // State colors
        private static readonly Color ColorPatrol     = new(0.65f, 0.85f, 1.00f, 0.45f);
        private static readonly Color ColorSuspicious = new(1.00f, 0.85f, 0.05f, 0.60f);
        private static readonly Color ColorAlert      = new(1.00f, 0.10f, 0.05f, 0.70f);
        private static readonly Color ColorSearching  = new(1.00f, 0.45f, 0.05f, 0.55f);

        /// <summary>Bind the tuning profile and build the mesh/material. Called once from <see cref="GuardController.Awake"/>.</summary>
        public void Initialize(GuardProfile profile)
        {
            _profile = profile;
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            _mesh = new Mesh { name = "FOV Cone" };
            _meshFilter.mesh = _mesh;

            // Sprites/Default is in Always Included Shaders (ProjectSettings/GraphicsSettings.asset)
            // so it survives build-time shader stripping. URP/Unlit gets stripped because no asset
            // Material references it, which made Shader.Find return null in builds and produced
            // Unity's opaque magenta "missing shader" placeholder.
            var mat = new Material(Shader.Find("Sprites/Default")) { color = ColorPatrol };
            _meshRenderer.sortingOrder = 5;
            _meshRenderer.material = mat;
        }

        /// <summary>Switches the cone's render colour to match the FSM state.</summary>
        public void SetStateColor(GuardStateType stateType)
        {
            if (_meshRenderer == null) return;
            _meshRenderer.material.color = stateType switch
            {
                GuardStateType.Suspicious => ColorSuspicious,
                GuardStateType.Alert      => ColorAlert,
                GuardStateType.Searching  => ColorSearching,
                _                         => ColorPatrol,
            };
        }

        private void LateUpdate()
        {
            if (_profile == null) return;
            BuildMesh();
        }

        private void BuildMesh()
        {
            int rayCount = _profile.coneResolution;
            float halfFov = _profile.fovAngle * 0.5f;
            float angleStep = _profile.fovAngle / (rayCount - 1);

            var vertices = new Vector3[rayCount + 1];
            var triangles = new int[(rayCount - 1) * 3];
            // Sprites/Default multiplies vertex color by material color; without vertex colors the
            // result is (0,0,0,0). Fill with white so the material's tinted color shows through.
            var colors = new Color[rayCount + 1];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;

            vertices[0] = Vector3.zero; // local origin = guard pivot

            for (int i = 0; i < rayCount; i++)
            {
                float localAngleDeg = -halfFov + i * angleStep;
                Vector2 worldDir = RotateDir(FacingDirection, localAngleDeg);
                Vector2 origin = transform.position;

                RaycastHit2D hit = Physics2D.Raycast(origin, worldDir, _profile.visionRange, _profile.obstacleLayerMask);
                Vector2 worldEndpoint = hit ? hit.point : origin + worldDir * _profile.visionRange;

                vertices[i + 1] = transform.InverseTransformPoint(worldEndpoint);
            }

            for (int i = 0; i < rayCount - 1; i++)
            {
                triangles[i * 3]     = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            _mesh.Clear();
            _mesh.vertices  = vertices;
            _mesh.triangles = triangles;
            _mesh.colors    = colors;
            _mesh.RecalculateNormals();
        }

        // Rotates a 2D direction vector clockwise by angleDeg degrees
        private static Vector2 RotateDir(Vector2 dir, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);
            return new Vector2(dir.x * cos + dir.y * sin, -dir.x * sin + dir.y * cos);
        }
    }

    /// <summary>State labels used by <see cref="FieldOfViewMesh.SetStateColor"/> to pick a cone tint.</summary>
    public enum GuardStateType { Idle, Suspicious, Alert, Searching }
}

using Sandouq.Player;
using Sandouq.Tools;
using UnityEngine;

namespace Sandouq.Cleaning
{
    public sealed class CleaningToolController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerInteraction interaction;
        [SerializeField] private CleaningTool tool;
        [SerializeField] private LocalPlayerRig localRig;
        private CleaningActionRouter actionRouter;
        public void BindRouter(CleaningActionRouter router) => actionRouter = router;
        [Header("Stroke")]
        [Tooltip("Minimum movement as fraction of brush radius before advancing the stroke anchor. Segments are filled analytically on the GPU.")]
        [SerializeField, Range(0.01f, 0.5f)] private float spacing = 0.05f;
        [Tooltip("Break continuity across large jumps in contact, in metres.")]
        [SerializeField, Min(0.1f)] private float maximumStrokeDistance = 2f;
        [Header("Debug")]
        [SerializeField] private bool showContactGizmo = true;
        [SerializeField] private bool showUV;
        private CleanableSurface previousSurface;
        private Vector2 previousUV, contactUV;
        private Vector3 previousPoint, contactPoint;
        private bool touching;
        public string ToolName => tool && tool.Data ? tool.Data.ToolName : "None";
        private void LateUpdate()
        {
            if (!tool || !tool.Data) return;
            CleaningToolData data = tool.Data;
            touching = interaction.TryContact(data.Reach, out CleanableSurface surface, out RaycastHit hit);
            tool.ShowContact(touching, hit.point, hit.normal);
            contactPoint = hit.point;
            contactUV = hit.textureCoord;
            if (!touching || !input.CleanHeld) { previousSurface = null; return; }
            float travel = Vector3.Distance(previousPoint, contactPoint);
            bool continuous = previousSurface == surface && travel <= maximumStrokeDistance;
            Vector2 start = continuous ? previousUV : contactUV;
            float amount = data.Strength * data.CleaningSpeed * Time.deltaTime;
            // Keep aim/input separate from applying shared world state. Old single-wall scenes work offline too.
            if (actionRouter) actionRouter.Request(new CleaningStroke {
                playerId = localRig ? localRig.PlayerId : "offline-player", surfaceId = surface.SurfaceId,
                fromUV = start, toUV = contactUV, radius = data.Radius, amount = amount, falloff = data.Falloff });
            else surface.Clean(start, contactUV, data.Radius, amount, data.Falloff);
            if (!continuous || travel >= data.Radius * spacing)
            {
                previousUV = contactUV;
                previousPoint = contactPoint;
            }
            previousSurface = surface;
        }
        private void OnDisable() => previousSurface = null;
        private void OnDrawGizmos()
        {
            if (!showContactGizmo || !touching || !tool || !tool.Data) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(contactPoint, tool.Data.Radius);
        }
        private void OnGUI()
        {
            if (showUV && touching) GUI.Label(new Rect(20, 170, 300, 25), $"Contact UV: {contactUV:F3}");
        }
    }
}

using Sandouq.Cleaning;
using UnityEngine;
using UnityEngine.UI;

namespace Sandouq.UI
{
    // World-space labels belong to windows, not to a particular player/camera.
    public sealed class WindowProgressLabel : MonoBehaviour
    {
        [SerializeField] private CleanableSurface surface;
        [SerializeField] private Text label;
        private void OnEnable() { surface.OnCleaningProgressChanged += Refresh; Refresh(surface.CurrentCleanliness); }
        private void OnDisable() { if (surface) surface.OnCleaningProgressChanged -= Refresh; }
        private void Refresh(float value) => label.text = $"{surface.SurfaceId}   {value:P0}" + (surface.IsClean ? "  CLEAN" : "");
    }
}

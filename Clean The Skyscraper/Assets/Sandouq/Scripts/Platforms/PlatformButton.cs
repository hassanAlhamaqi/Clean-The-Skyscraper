using Sandouq.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Sandouq.Platforms
{
    public sealed class PlatformButton : MonoBehaviour
    {
        [SerializeField] private MovingPlatform platform;
        [SerializeField] private Text stateLabel;
        public MovingPlatform Platform => platform;
        public string Prompt => "E / X: " + platform.NextAction;
        public void Press(LocalPlayerRig player)
        {
            // Remote replicas cannot read local input and operate a shared platform.
            if (player && player.IsLocalPlayer) platform.RequestButtonPress(player.PlayerId);
        }
        private void OnEnable() { platform.StateChanged += Refresh; Refresh(platform.State); }
        private void OnDisable() { if (platform) platform.StateChanged -= Refresh; }
        private void Refresh(MovingPlatform.MotionState value)
        {
            if (stateLabel) stateLabel.text = $"LIFT: {value}\nE / X: {platform.NextAction}";
        }
    }
}

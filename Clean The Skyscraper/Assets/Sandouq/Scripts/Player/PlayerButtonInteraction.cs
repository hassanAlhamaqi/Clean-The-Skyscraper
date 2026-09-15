using Sandouq.Platforms;
using UnityEngine;
using UnityEngine.UI;

namespace Sandouq.Player
{
    // Translates a local interaction press into a button request, with the same reach checks as cleaning.
    public sealed class PlayerButtonInteraction : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerInteraction interaction;
        [SerializeField] private LocalPlayerRig localRig;
        [SerializeField, Min(0.1f)] private float reach = 2.5f;
        private Text promptLabel;
        private Collider cachedCollider;
        private PlatformButton button;
        private string previousPrompt;
        public void BindPrompt(Text label) => promptLabel = label;
        private void Update()
        {
            Collider hitCollider = interaction.TryHit(reach, out var hit) ? hit.collider : null;
            if (cachedCollider != hitCollider)
            {
                cachedCollider = hitCollider;
                button = hitCollider ? hitCollider.GetComponentInParent<PlatformButton>() : null;
            }
            string prompt = button ? button.Prompt : "Aim at the lift button and press E / X";
            if (promptLabel && prompt != previousPrompt) promptLabel.text = prompt;
            previousPrompt = prompt;
            if (button && input.InteractPressed) button.Press(localRig);
        }
        private void OnDisable() { if (promptLabel) promptLabel.text = ""; }
    }
}

using Sandouq.Cleaning;
using Sandouq.UI;
using Sandouq.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Sandouq.Core
{
    public sealed class PrototypeBindings : MonoBehaviour
    {
        [SerializeField] private CleanableSurface surface;
        [SerializeField] private CleaningToolController player;
        [SerializeField] private CleaningHUD hud;
        [SerializeField] private CleaningJob job;
        [SerializeField] private CleaningActionRouter router;
        [SerializeField] private PlayerButtonInteraction buttonInteraction;
        [SerializeField] private Text interactionPrompt;
        private void Start()
        {
            // Scene composition stays here; shared gameplay systems never look for a global player.
            if (job) hud.BindJob(job, player);
            else hud.Bind(surface, player);
            if (router) player.BindRouter(router);
            if (buttonInteraction) buttonInteraction.BindPrompt(interactionPrompt);
        }
    }
}

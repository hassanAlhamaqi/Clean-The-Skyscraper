using UnityEngine;

namespace Sandouq.Player
{
    // Only the locally owned avatar may read input, drive a camera or capture the cursor.
    [DefaultExecutionOrder(-1000)]
    public sealed class LocalPlayerRig : MonoBehaviour
    {
        [SerializeField] private bool isLocalPlayer = true;
        [SerializeField] private string playerId = "offline-player";
        [SerializeField] private Behaviour[] localBehaviours;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private AudioListener audioListener;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Renderer body;
        public bool IsLocalPlayer => isLocalPlayer;
        public string PlayerId => playerId;
        private void Awake() => SetLocalPlayer(isLocalPlayer);
        public void SetLocalPlayer(bool local)
        {
            // A future spawn/ownership callback calls this before the avatar starts acting locally.
            isLocalPlayer = local;
            foreach (var behaviour in localBehaviours) if (behaviour) behaviour.enabled = local;
            viewCamera.enabled = local;
            audioListener.enabled = local;
            characterController.enabled = local;
            if (!local) body.enabled = true;
        }
    }
}

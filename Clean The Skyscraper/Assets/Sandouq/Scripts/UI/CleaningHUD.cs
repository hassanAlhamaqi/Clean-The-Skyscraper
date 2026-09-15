using Sandouq.Cleaning;
using UnityEngine;
using UnityEngine.UI;

namespace Sandouq.UI
{
    // A display subscriber: supports the old single-wall scene and the new aggregate job without owning progress.
    public sealed class CleaningHUD : MonoBehaviour
    {
        [SerializeField] private CleanableSurface surface;
        [SerializeField] private CleaningToolController toolController;
        [SerializeField] private Text progressLabel;
        [SerializeField] private Text toolLabel;
        private CleaningJob job;
        public void BindJob(CleaningJob target, CleaningToolController controller)
        {
            Unsubscribe();
            surface = null;
            job = target;
            toolController = controller;
            if (isActiveAndEnabled) Subscribe();
        }
        public void Bind(CleanableSurface target, CleaningToolController controller)
        {
            Unsubscribe();
            job = null;
            surface = target;
            toolController = controller;
            if (isActiveAndEnabled) Subscribe();
        }
        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();
        private void Subscribe()
        {
            if (job)
            {
                job.ProgressChanged += RefreshJob;
                RefreshJob(job.Cleanliness);
                toolLabel.text = "Tool: " + (toolController ? toolController.ToolName : "None");
                return;
            }
            if (!surface) return;
            surface.OnCleaningProgressChanged += Refresh;
            surface.OnCleaningCompleted += Completed;
            Refresh(surface.CurrentCleanliness);
            toolLabel.text = "Tool: " + (toolController ? toolController.ToolName : "None");
        }
        private void Unsubscribe()
        {
            if (job) job.ProgressChanged -= RefreshJob;
            if (!surface) return;
            surface.OnCleaningProgressChanged -= Refresh;
            surface.OnCleaningCompleted -= Completed;
        }
        private void Refresh(float progress) => progressLabel.text = surface.IsClean ? "Glass Clean!" : $"Glass Clean: {progress:P0}";
        private void Completed() => progressLabel.text = "Glass Clean!";
        private void RefreshJob(float value) => progressLabel.text = $"Overall Clean: {value:P0}";
    }
}

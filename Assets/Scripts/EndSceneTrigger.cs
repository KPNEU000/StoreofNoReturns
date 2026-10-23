
using UnityEngine;

public class EndingCutsceneTrigger : MonoBehaviour
{
    [Header("Cutscene References")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private Ink.UnityIntegration.InkFile endingInkFile;

    [Header("UI")]
    [SerializeField] private GameObject endingMenu;

    private void Start()
    {
        if (endingMenu != null)
        {
            endingMenu.SetActive(false);
        }

        if (dialogueManager == null || endingInkFile == null)
        {
            Debug.LogError("Ending cutscene references are missing.");
            return;
        }

        dialogueManager.StartCutscene(endingInkFile, FinishEnding);
    }

    private void FinishEnding()
    {
        if (endingMenu != null)
        {
            endingMenu.SetActive(true);
        }
    }
}

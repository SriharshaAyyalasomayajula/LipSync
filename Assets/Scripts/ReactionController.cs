using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Controls a simple Smile/Sad reaction sequence on a humanoid model.
// Attach to the character root and assign Animator, audio source and UI button.
public class ReactionController : MonoBehaviour
{
    [Header("Idle Animation Settings")]
    public string idleStateName = "idle"; // Animator state name for idle
    public Animator animator; // expects Animator with bool or trigger parameters 'Smile' and 'Sad'
    public Button playButton;
    public Button dialogueButton;
    public AudioSource dialogueSource;
    public LipSyncFake lipSync;
    // Direct blendshape expression strengths (if you want to use blendshapes instead of animator)
    [Range(0f, 100f)] public float expressionStrength = 75f;

    bool isPlayingAnimation = false;
    bool isPlayingDialogue = false;
    Vector3 initialPosition;
    Quaternion initialRotation;
    bool hasStoredInitial = false;
    public bool restoreTransformAfter = true;
    // Optional components to disable during reaction (e.g., movement scripts, CharacterController)
    public Behaviour[] disableDuringReaction;
    Rigidbody rb;
    bool rbWasKinematic = false;

    bool isPlaying = false;

    void Start()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        if (dialogueButton != null)
            dialogueButton.onClick.AddListener(OnDialogueClicked);
        // cache initial transform
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        hasStoredInitial = true;

        rb = GetComponent<Rigidbody>();
        if (rb != null) rbWasKinematic = rb.isKinematic;
    }

    void OnPlayClicked()
    {
        if (isPlayingAnimation)
            return;
        if (isPlayingDialogue)
            return;

        // Start combined sequence: play dialogue (audio + lip sync) while running the Smile/Sad animation sequence
        StartCoroutine(PlayCombinedSequence());
    }

    void OnDialogueClicked()
    {
        if (isPlayingDialogue)
            return;
        if (isPlayingAnimation)
            return;

        StartCoroutine(PlayDialogueOnly());
    }

    IEnumerator PlayFullSequence()
    {
        isPlayingAnimation = true;

        // store and reset transform to initial to avoid cumulative drift from animations
        if (hasStoredInitial && restoreTransformAfter)
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }

        // disable movement components and rigidbody
        if (disableDuringReaction != null)
        {
            foreach (var b in disableDuringReaction) if (b != null) b.enabled = false;
        }
        if (rb != null) rb.isKinematic = true;

        // Four-step sequence: Smile, Sad, Smile, Sad (animations + blendshape expressions)
        yield return PlayReaction("Smile", 2f);
        yield return PlayReaction("Sad", 2f);
        yield return PlayReaction("Smile", 2f);
        yield return PlayReaction("Sad", 2f);

        // re-enable movement components and rigidbody
        if (disableDuringReaction != null)
        {
            foreach (var b in disableDuringReaction) if (b != null) b.enabled = true;
        }
        if (rb != null) rb.isKinematic = rbWasKinematic;

        // restore transform exactly to initial to avoid residual rotation/position from animations
        if (hasStoredInitial && restoreTransformAfter)
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }

        isPlayingAnimation = false;
    }

    // New: Play audio-driven lip sync and run the reaction animation sequence at the same time.
    IEnumerator PlayCombinedSequence()
    {
        if (dialogueSource == null)
        {
            // fallback to the animation-only sequence
            yield return PlayFullSequence();
            yield break;
        }

        isPlayingAnimation = true;
        isPlayingDialogue = true;

        // store and reset transform to initial to avoid cumulative drift from animations
        if (hasStoredInitial && restoreTransformAfter)
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }

        // disable movement components and rigidbody
        if (disableDuringReaction != null)
        {
            foreach (var b in disableDuringReaction) if (b != null) b.enabled = false;
        }
        if (rb != null) rb.isKinematic = true;

        // Start dialogue audio and lip-sync
        PlayIdleAnimation();
        dialogueSource.Play();
        if (lipSync != null)
            lipSync.StartLipSync(dialogueSource);

        // Run the same four-step reaction sequence concurrently while audio is playing.
        // We'll start a coroutine that runs the sequence but don't block here; instead wait for audio to finish
        // but ensure the sequence doesn't outlive the audio if it finishes earlier.
        Coroutine reactionRoutine = StartCoroutine(PlayFullSequence());

        // Wait for audio to finish
        yield return new WaitWhile(() => dialogueSource.isPlaying);

        // Stop lip sync and ensure reaction sequence has ended
        if (lipSync != null)
            lipSync.StopLipSync();

        // If the reaction sequence is still running, stop it and perform cleanup
        if (reactionRoutine != null)
        {
            StopCoroutine(reactionRoutine);

            // re-enable movement components and rigidbody (same cleanup as PlayFullSequence)
            if (disableDuringReaction != null)
            {
                foreach (var b in disableDuringReaction) if (b != null) b.enabled = true;
            }
            if (rb != null) rb.isKinematic = rbWasKinematic;

            // restore transform exactly to initial to avoid residual rotation/position from animations
            if (hasStoredInitial && restoreTransformAfter)
            {
                transform.position = initialPosition;
                transform.rotation = initialRotation;
            }
        }

        PlayIdleAnimation();
        isPlayingDialogue = false;
        isPlayingAnimation = false;
    }

    // Play only dialogue audio and lip-sync (no animations)
    void PlayIdleAnimation()
    {
        if (animator != null && !string.IsNullOrEmpty(idleStateName))
        {
            int idleHash = Animator.StringToHash(idleStateName);
            if (animator.HasState(0, idleHash))
                animator.CrossFade(idleHash, 0.12f, 0, 0f);
        }
    }

    IEnumerator PlayDialogueOnly()
    {
        if (dialogueSource == null)
            yield break;

        isPlayingDialogue = true;
        PlayIdleAnimation();
        dialogueSource.Play();
        if (lipSync != null)
            lipSync.StartLipSync(dialogueSource);

        // wait for audio to finish
        yield return new WaitWhile(() => dialogueSource.isPlaying);

        if (lipSync != null)
            lipSync.StopLipSync();

        PlayIdleAnimation();
        isPlayingDialogue = false;
    }

    IEnumerator PlayReaction(string param, float duration)
    {
        // Trigger animator if available
        if (animator != null)
        {
            // Prefer CrossFade to get a smooth transition between states
            int hash = Animator.StringToHash(param);
            if (animator.HasState(0, hash))
                animator.CrossFade(hash, 0.12f, 0, 0f);
            else
                animator.SetBool(param, true);
        }

        // Also trigger blendshapes for Smile/Sad so expressions are visible even if clips don't animate them
        if (lipSync != null && (string.Equals(param, "Smile", System.StringComparison.OrdinalIgnoreCase) || string.Equals(param, "Sad", System.StringComparison.OrdinalIgnoreCase)))
        {
            lipSync.ResolveIndices();
            lipSync.TriggerExpression(param, expressionStrength, 0.18f);
        }

        yield return new WaitForSeconds(duration);

        if (animator != null)
        {
            if (!animator.HasState(0, Animator.StringToHash(param)))
                animator.SetBool(param, false);
        }

        if (lipSync != null && (string.Equals(param, "Smile", System.StringComparison.OrdinalIgnoreCase) || string.Equals(param, "Sad", System.StringComparison.OrdinalIgnoreCase)))
        {
            lipSync.ClearExpression(param, 0.18f);
        }
    }
}

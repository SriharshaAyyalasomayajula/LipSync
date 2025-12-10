using System;
using System.Collections;
using UnityEngine;


// Configure blendshape names for smile, sad and a set for talking 
public class LipSyncFake : MonoBehaviour
{
    public SkinnedMeshRenderer faceMesh; // required: the mesh with blendshapes

    [Header("Expression Blendshapes")]
    public string smileBlendshape = "smile";
    public string sadBlendshape = "sad";

    [Header("Talking blendshapes (will be varied randomly)")]
    public string[] talkingBlendshapes = new string[] { "open_mouth", "open_mouth2", "o_mouth" };

    [Header("Talking settings")]
    public float talkMinWeight = 30f;
    public float talkMaxWeight = 80f;
    public int talkSimultaneous = 2; // how many talking blendshapes active at once

    [Header("Expression settings")]
    public float expressionThreshold = 75f; // expressions (smile/sad) visible only at/above this
    [Header("Audio-driven settings")]
    public bool audioDrivenLipSync = true;
    public float envelopeWindow = 0.05f; // in seconds
    public float smoothingSpeed = 8f; // higher = faster smoothing

    // resolved indices
    int smileIndex = -1;
    int sadIndex = -1;
    int[] talkingIndices = new int[0];
    float[] currentTalkingWeights = new float[0];

    // envelope precomputed from clip
    float[] envelope = null;
    float envelopeMax = 0f;

    Coroutine lipCoroutine;

    void Start()
    {
        ResolveIndices();
    }

    public void ResolveIndices()
    {
        if (faceMesh == null || faceMesh.sharedMesh == null)
        {
            smileIndex = sadIndex = -1;
            talkingIndices = new int[0];
            return;
        }

        var mesh = faceMesh.sharedMesh;
        smileIndex = FindBlendshapeIndex(mesh, smileBlendshape);
        sadIndex = FindBlendshapeIndex(mesh, sadBlendshape);

        talkingIndices = new int[talkingBlendshapes.Length];
        for (int i = 0; i < talkingBlendshapes.Length; i++)
            talkingIndices[i] = FindBlendshapeIndex(mesh, talkingBlendshapes[i]);
    }

    // Public helper to force re-resolution (callable from inspector via context menu or other scripts)
    [ContextMenu("Resolve Blendshape Indices")]
    public void EditorResolveIndices()
    {
    ResolveIndices();
    }

    public void LogBlendshapes()
    {
        // Method kept for editor use, but logs removed for production.
    }

    int FindBlendshapeIndex(Mesh mesh, string name)
    {
        if (mesh == null || string.IsNullOrEmpty(name)) return -1;
        for (int i = 0; i < mesh.blendShapeCount; i++)
            if (mesh.GetBlendShapeName(i).Equals(name, System.StringComparison.Ordinal))
                return i;
        return -1;
    }

    public void StartLipSync(AudioSource source)
    {
        ResolveIndices();
        if (lipCoroutine != null) StopCoroutine(lipCoroutine);
        // prepare audio envelope if requested
        if (audioDrivenLipSync && source != null && source.clip != null)
        {
            PrecomputeEnvelope(source.clip);
        }

        // ensure current talking weights array size
        currentTalkingWeights = new float[talkingIndices.Length];

        lipCoroutine = StartCoroutine(LipSyncRoutine(source));
    }

    public void StopLipSync()
    {
        if (lipCoroutine != null)
        {
            StopCoroutine(lipCoroutine);
            lipCoroutine = null;
        }
        // reset blendshapes
        if (faceMesh != null)
        {
            if (smileIndex >= 0) faceMesh.SetBlendShapeWeight(smileIndex, 0f);
            if (sadIndex >= 0) faceMesh.SetBlendShapeWeight(sadIndex, 0f);
            for (int i = 0; i < talkingIndices.Length; i++) if (talkingIndices[i] >= 0) faceMesh.SetBlendShapeWeight(talkingIndices[i], 0f);
        }
    }

    // Trigger a named expression ("Smile" / "Sad") with a given weight
    // Smoothly set expression weight over duration (lerp). If duration is 0, set immediately.
    public void TriggerExpression(string name, float weight, float duration = 0.15f)
    {
        ResolveIndices();
        if (faceMesh == null) return;
        if (string.Equals(name, "Smile", System.StringComparison.OrdinalIgnoreCase) && smileIndex >= 0)
            StartExpressionLerp(smileIndex, weight, duration);
        if (string.Equals(name, "Sad", System.StringComparison.OrdinalIgnoreCase) && sadIndex >= 0)
            StartExpressionLerp(sadIndex, weight, duration);
    }

    public void ClearExpression(string name, float duration = 0.15f)
    {
        if (faceMesh == null) return;
        if (string.Equals(name, "Smile", System.StringComparison.OrdinalIgnoreCase) && smileIndex >= 0)
            StartExpressionLerp(smileIndex, 0f, duration);
        if (string.Equals(name, "Sad", System.StringComparison.OrdinalIgnoreCase) && sadIndex >= 0)
            StartExpressionLerp(sadIndex, 0f, duration);
    }

    // manage running coroutines per blendshape
    System.Collections.Generic.Dictionary<int, Coroutine> expressionCoroutines = new System.Collections.Generic.Dictionary<int, Coroutine>();

    void StartExpressionLerp(int index, float target, float duration)
    {
        if (faceMesh == null || index < 0) return;
        // stop existing coroutine on this index
        if (expressionCoroutines.TryGetValue(index, out var c) && c != null)
            StopCoroutine(c);

        if (duration <= 0f)
        {
            faceMesh.SetBlendShapeWeight(index, target);
            expressionCoroutines.Remove(index);
            return;
        }

        var coroutine = StartCoroutine(ExpressionLerpRoutine(index, target, duration));
        expressionCoroutines[index] = coroutine;
    }

    IEnumerator ExpressionLerpRoutine(int index, float target, float duration)
    {
        float elapsed = 0f;
        float start = faceMesh.GetBlendShapeWeight(index);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float value = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            faceMesh.SetBlendShapeWeight(index, value);
            yield return null;
        }
        faceMesh.SetBlendShapeWeight(index, target);
        expressionCoroutines.Remove(index);
    }

    IEnumerator LipSyncRoutine(AudioSource source)
    {
        if (audioDrivenLipSync && envelope != null && envelope.Length > 0)
        {
            // audio-driven: per-frame, set target weights based on envelope and smooth towards them
            while (source != null && source.isPlaying)
            {
                float time = source.time;
                int idx = Mathf.Clamp(Mathf.FloorToInt(time / envelopeWindow), 0, envelope.Length - 1);
                float amp = envelope[idx] / (envelopeMax > 0f ? envelopeMax : 1f); // 0..1

                // map amplitude to target weight
                float targetWeight = Mathf.Lerp(talkMinWeight, talkMaxWeight, amp);

                // select a small subset and assign target weights, smooth others to zero
                System.Collections.Generic.List<int> activeIndices = new System.Collections.Generic.List<int>();
                int count = Mathf.Clamp(Mathf.CeilToInt(talkSimultaneous * Mathf.Lerp(0.5f, 1f, amp)), 1, talkingIndices.Length);
                while (activeIndices.Count < count)
                {
                    int pick = UnityEngine.Random.Range(0, talkingIndices.Length);
                    if (!activeIndices.Contains(pick) && talkingIndices[pick] >= 0) activeIndices.Add(pick);
                }

                // compute targets
                for (int i = 0; i < talkingIndices.Length; i++)
                {
                    float tgt = 0f;
                    if (activeIndices.Contains(i))
                        tgt = targetWeight * UnityEngine.Random.Range(0.8f, 1f);

                    currentTalkingWeights[i] = Mathf.Lerp(currentTalkingWeights[i], tgt, Mathf.Clamp01(smoothingSpeed * Time.deltaTime));
                    faceMesh.SetBlendShapeWeight(talkingIndices[i], currentTalkingWeights[i]);
                }

                yield return null;
            }
        }
        else
        {
            // fallback random mixing as before (but smoother using smoothingSpeed)
            while (source != null && source.isPlaying)
            {
                int count = Mathf.Clamp(UnityEngine.Random.Range(1, talkSimultaneous + 1), 1, talkingIndices.Length);
                System.Collections.Generic.List<int> activeIndices = new System.Collections.Generic.List<int>();
                while (activeIndices.Count < count)
                {
                    int pick = UnityEngine.Random.Range(0, talkingIndices.Length);
                    if (!activeIndices.Contains(pick) && talkingIndices[pick] >= 0) activeIndices.Add(pick);
                }

                // set targets
                for (int i = 0; i < talkingIndices.Length; i++)
                {
                    float tgt = activeIndices.Contains(i) ? UnityEngine.Random.Range(talkMinWeight, talkMaxWeight) : 0f;
                    currentTalkingWeights[i] = Mathf.Lerp(currentTalkingWeights[i], tgt, Mathf.Clamp01(smoothingSpeed * Time.deltaTime));
                    faceMesh.SetBlendShapeWeight(talkingIndices[i], currentTalkingWeights[i]);
                }

                yield return new WaitForSeconds(UnityEngine.Random.Range(0.06f, 0.12f));
            }
        }

        StopLipSync();
    }

    void PrecomputeEnvelope(AudioClip clip)
    {
        try
        {
            int freq = clip.frequency;
            int channels = clip.channels;
            int windowSamples = Mathf.Max(128, Mathf.RoundToInt(envelopeWindow * freq));
            int totalWindows = Mathf.CeilToInt((float)clip.samples / windowSamples);
            envelope = new float[totalWindows];

            float[] samples = new float[windowSamples * channels];
            float max = 0f;
            for (int w = 0; w < totalWindows; w++)
            {
                int offset = w * windowSamples;
                int samplesToRead = Mathf.Min(windowSamples, clip.samples - offset);
                if (samplesToRead <= 0) break;
                if (samples.Length < samplesToRead * channels) samples = new float[samplesToRead * channels];
                clip.GetData(samples, offset);

                double sum = 0.0;
                int count = samplesToRead * channels;
                for (int i = 0; i < count; i++) sum += samples[i] * samples[i];
                float rms = (float)Math.Sqrt(sum / count);
                envelope[w] = rms;
                if (rms > max) max = rms;
            }
            envelopeMax = max;
        }
        catch (System.Exception)
        {
            envelope = null;
            envelopeMax = 0f;
        }
    }
}

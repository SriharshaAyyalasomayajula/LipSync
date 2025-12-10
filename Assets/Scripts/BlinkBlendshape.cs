using UnityEngine;

// Attach to the character's face mesh GameObject. Assign the blendshape name for blinking (e.g., "blink" or "eye_blink").
public class BlinkBlendshape : MonoBehaviour
{
    public SkinnedMeshRenderer faceMesh;
    public string blinkBlendshape = "blink";
    public float blinkDuration = 0.08f; // seconds eyes stay closed
    public float blinkSpeed = 0.12f;    // seconds to close/open
    public float minBlinkInterval = 2.5f;
    public float maxBlinkInterval = 5.5f;

    int blinkIndex = -1;
    bool isBlinking = false;
    float blinkWeight = 0f;

    void Start()
    {
        if (faceMesh == null) faceMesh = GetComponent<SkinnedMeshRenderer>();
        if (faceMesh != null && faceMesh.sharedMesh != null && !string.IsNullOrEmpty(blinkBlendshape))
        {
            var mesh = faceMesh.sharedMesh;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                if (mesh.GetBlendShapeName(i).Equals(blinkBlendshape, System.StringComparison.OrdinalIgnoreCase))
                {
                    blinkIndex = i;
                    break;
                }
            }
        }
        StartCoroutine(BlinkRoutine());
    }

    System.Collections.IEnumerator BlinkRoutine()
    {
        while (true)
        {
            float wait = Random.Range(minBlinkInterval, maxBlinkInterval);
            yield return new WaitForSeconds(wait);
            yield return StartCoroutine(DoBlink());
        }
    }

    System.Collections.IEnumerator DoBlink()
    {
        if (blinkIndex < 0 || faceMesh == null) yield break;
        isBlinking = true;
        // Close eyes
        float t = 0f;
        while (t < blinkSpeed)
        {
            t += Time.deltaTime;
            blinkWeight = Mathf.Lerp(0f, 100f, t / blinkSpeed);
            faceMesh.SetBlendShapeWeight(blinkIndex, blinkWeight);
            yield return null;
        }
        faceMesh.SetBlendShapeWeight(blinkIndex, 100f);
        yield return new WaitForSeconds(blinkDuration);
        // Open eyes
        t = 0f;
        while (t < blinkSpeed)
        {
            t += Time.deltaTime;
            blinkWeight = Mathf.Lerp(100f, 0f, t / blinkSpeed);
            faceMesh.SetBlendShapeWeight(blinkIndex, blinkWeight);
            yield return null;
        }
        faceMesh.SetBlendShapeWeight(blinkIndex, 0f);
        isBlinking = false;
    }
}
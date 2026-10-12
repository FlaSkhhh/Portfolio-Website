using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MeshRenderer))]
public class MouseShaderTracker : MonoBehaviour
{
    [SerializeField] Material cpMat;
    Material backgroundMaterial;
    bool isMobile;
    void OnEnable()
    {
        GetComponent<MeshRenderer>().material = cpMat;
    }
    void Start()
    {
        // Automatically grab the runtime instance of the material
        backgroundMaterial = GetComponent<MeshRenderer>().material;

        // Detect if the user is on a mobile device (iOS/Android)
        isMobile = SystemInfo.deviceType == DeviceType.Handheld;
    }

    void Update()
    {
        if (backgroundMaterial == null) return;

        // If on mobile OR if no mouse is physically connected to the device
        if (isMobile || Mouse.current == null)
        {
            // Throw the coordinates off-screen
            backgroundMaterial.SetVector("_MousePos", new Vector4(-10f, -10f, 0, 0));
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        mousePos.x /= Screen.width;
        mousePos.y /= Screen.height;

        backgroundMaterial.SetVector("_MousePos", new Vector4(mousePos.x, mousePos.y, 0, 0));
    }

    public void TriggerGlitch()
    {
        StartCoroutine(TransitionGlitchRoutine());
    }

    IEnumerator TransitionGlitchRoutine()
    {
        if (backgroundMaterial == null) yield break;

        float intensity = 1f; // Max glitch strength

        backgroundMaterial.SetFloat("_GlitchPulse", intensity);
        while (intensity > 0f)
        {
            intensity -= Time.deltaTime * 2f;
            backgroundMaterial.SetFloat("_GlitchPulse", Mathf.Max(0, intensity));
            yield return null;
        }

        // Ensure it zeroes out completely
        backgroundMaterial.SetFloat("_GlitchPulse", 0f);
    }
}
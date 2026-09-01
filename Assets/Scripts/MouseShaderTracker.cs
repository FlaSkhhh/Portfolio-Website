using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MeshRenderer))]
public class MouseShaderTracker : MonoBehaviour
{
    private Material backgroundMaterial;
    private bool isMobile;

    private void Start()
    {
        // Automatically grab the runtime instance of the material
        backgroundMaterial = GetComponent<MeshRenderer>().material;

        // Detect if the user is on a mobile device (iOS/Android)
        isMobile = SystemInfo.deviceType == DeviceType.Handheld;
    }

    private void Update()
    {
        if (backgroundMaterial == null) return;

        // If on mobile OR if no mouse is physically connected to the device
        if (isMobile || Mouse.current == null)
        {
            // Throw the coordinates off-screen
            backgroundMaterial.SetVector("_MousePos", new Vector4(-10f, -10f, 0, 0));
            return;
        }

        // Convert pixel position to normalized screen coordinates (0 to 1)
        Vector2 mousePos = Mouse.current.position.ReadValue();
        mousePos.x /= Screen.width;
        mousePos.y /= Screen.height;

        // Send it to the GPU
        backgroundMaterial.SetVector("_MousePos", new Vector4(mousePos.x, mousePos.y, 0, 0));
    }
}
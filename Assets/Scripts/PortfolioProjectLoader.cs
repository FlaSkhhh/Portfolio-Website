using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public class PortfolioProjectLoader : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset projectCardTemplate;

    private ScrollView projectScrollView;
    private List<Texture2D> loadedTextures = new();

    private struct ProjectData
    {
        public string Title;
        public string Tag;
        public string Description;
        public string ScreenshotAddress; // Addressables address for the screenshot
    }

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("PortfolioProjectLoader requires a UIDocument component.");
            return;
        }

        if (projectCardTemplate == null)
        {
            Debug.LogError("ProjectCardTemplate UXML asset is not assigned.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        projectScrollView = root.Q<ScrollView>("ProjectScrollView");

        if (projectScrollView == null)
        {
            Debug.LogWarning("ProjectScrollView not found in UI document.");
            return;
        }

        // Load projects
        LoadProjects();
    }

    private void OnDisable()
    {
        // Clean up textures
        foreach (var texture in loadedTextures)
        {
            if (texture != null)
                Destroy(texture);
        }
        loadedTextures.Clear();
    }

    private void LoadProjects()
    {
        var projects = new List<ProjectData>
        {
            new ProjectData
            {
                Title = "Real-Time Multiplayer Hand Cricket",
                Tag = "Unity Netcode | 34MB APK",
                Description = "Performance-optimized up to 5v5 team-based multiplayer game utilizing Unity Netcode, Relay, and Lobby services with custom RPCs and cross-platform Android/WebGL simulation.",
                ScreenshotAddress = "Screenshots/HandCricket.jpg" // Replace with Addressables path
            },
            new ProjectData
            {
                Title = "State-Based 2D Boss Fight",
                Tag = "AI & FSM | Combat Systems",
                Description = "Modular boss AI designed using Finite State Machines to manage complex behavior transitions, attack patterns, and animation events across multiple combat phases.",
                ScreenshotAddress = "Screenshots/BossFight.jpg" // Replace with Addressables path
            },
            new ProjectData
            {
                Title = "Infinite Runner with Custom Shader",
                Tag = "Custom HLSL | Procedural Gen",
                Description = "Custom vertex shader simulating world curvature with procedural environment generation and multi-character selection featuring seamless restart logic.",
                ScreenshotAddress = "Screenshots/InfiniteRunner.jpg" // Replace with Addressables path
            }
        };

        foreach (var project in projects)
        {
            CreateProjectCard(project);
        }
    }

    private void CreateProjectCard(ProjectData project)
    {
        // Instantiate card from template
        VisualElement cardElement = projectCardTemplate.CloneTree();

        // Set text content
        var titleLabel = cardElement.Q<Label>(className: "card-title");
        var tagLabel = cardElement.Q<Label>(className: "card-tag");
        var descriptionLabel = cardElement.Q<Label>(className: "card-description");
        var caseStudyButton = cardElement.Q<Button>(className: "btn-case-study");

        if (titleLabel != null)
            titleLabel.text = project.Title;

        if (tagLabel != null)
            tagLabel.text = project.Tag;

        if (descriptionLabel != null)
            descriptionLabel.text = project.Description;

        if (caseStudyButton != null)
            caseStudyButton.clicked += () => OnCaseStudyClicked(project.Title);

        // Load screenshot asynchronously
        LoadProjectScreenshot(cardElement, project.ScreenshotAddress);

        // Add card to scroll view
        projectScrollView.Add(cardElement);
    }

    private void LoadProjectScreenshot(VisualElement cardElement, string addressablePath)
    {
        // Load screenshot texture asynchronously without blocking WebGL frames
        // Using Resources as fallback (or can be upgraded to Addressables when package is installed)
        StartCoroutine(LoadScreenshotAsync(cardElement, addressablePath));
    }

    private IEnumerator LoadScreenshotAsync(VisualElement cardElement, string resourcePath)
    {
        // Yield to next frame to avoid blocking
        yield return null;

        if (cardElement == null)
            yield break;

        // Attempt to load from Resources folder
        // Format: "Assets/Resources/Screenshots/HandCricket.png" -> "Screenshots/HandCricket"
        string resourceName = resourcePath
            .Replace("Assets/Resources/", "")
            .Replace(".png", "")
            .Replace(".jpg", "");

        ResourceRequest request = Resources.LoadAsync<Texture2D>(resourceName);
        yield return request;

        if (request.asset is Texture2D texture && cardElement != null)
        {
            var imageContainer = cardElement.Q<VisualElement>(className: "card-image-container");
            if (imageContainer != null)
            {
                imageContainer.style.backgroundImage = new StyleBackground(texture);
                loadedTextures.Add(texture);
            }
        }
        else
        {
            Debug.LogWarning($"Failed to load screenshot from Resources: {resourceName}");
        }
    }

    private void OnCaseStudyClicked(string projectTitle)
    {
        Debug.Log($"Case study clicked for: {projectTitle}");
        // TODO: Implement navigation to detailed project view
    }
}

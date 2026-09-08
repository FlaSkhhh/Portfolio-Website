using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public class PortfolioProjectLoaderWeb : MonoBehaviour
{
    [SerializeField] UIDocument uiDocument;
    [SerializeField] VisualTreeAsset projectShowcaseRowTemplate;
    [SerializeField] bool loadFromStreamingAssets = true;
    [SerializeField] string projectDataUrl = "projects.json";
    [SerializeField] string remoteServerUrl = "https://yournetlifydomain.netlify.app/api/projects.json";

    VisualElement projectScrollView;
    PortfolioController portfolioController;
    ScrollAnimationController scrollAnimationController;
    List<Texture2D> cachedTextures = new();

    // Loader Elements
    VisualElement loadingScreen;
    VisualElement loadingCharacter;
    VisualElement cyberSpinner;
    VisualElement scrollChevron;
    ScrollView mainScrollArea;
    Label loadingText;

    int totalProjectsToLoad = 0;
    int projectsLoaded = 0;
    bool isDataLoaded = false;
    bool isPortfolioOpen = false;
    bool hasPlayedTransition = false; // Prevents the disappearing glitch
    bool hasUserScrolled = false;
    float spinnerRotation = 0f;

    [System.Serializable]
    public struct ProjectMetadata
    {
        public string title; public string Title;
        public string tag; public string Tag;
        public string description; public string Description;
        public string screenshotUrl; public string ScreenshotAddress;
        public string caseStudyUrl; public string githubUrl;
        public string platform; public string Platform;

        //JSON fields for the full Case Study page
        public string[] bodyParagraphs; // Array of text blocks for the detail page
        public string[] extraScreenshots; // Array of paths (e.g., "Screenshots/Game_2")

        public string GetTitle() => !string.IsNullOrEmpty(title) ? title : Title;
        public string GetTag() => !string.IsNullOrEmpty(tag) ? tag : Tag;
        public string GetDescription() => !string.IsNullOrEmpty(description) ? description : Description;
        public string GetScreenshot() => !string.IsNullOrEmpty(screenshotUrl) ? screenshotUrl : ScreenshotAddress;
        public string GetPlatform() => !string.IsNullOrEmpty(platform) ? platform : (!string.IsNullOrEmpty(Platform) ? Platform : "Unity / C#");

    }

    [System.Serializable]
    struct ProjectsDataWrapper { public ProjectMetadata[] projects; }

    void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null || projectShowcaseRowTemplate == null) return;

        var root = uiDocument.rootVisualElement;

        loadingScreen = root.Q<VisualElement>("LoadingScreen");
        loadingCharacter = root.Q<VisualElement>("LoadingCharacter");
        loadingText = root.Q<Label>("LoadingText");

        projectScrollView = root.Q<VisualElement>("ProjectScrollView");
        portfolioController = GetComponent<PortfolioController>();
        scrollAnimationController = GetComponent<ScrollAnimationController>();
        cyberSpinner = root.Q<VisualElement>("CyberSpinner");

        if (projectScrollView != null)
        {
            projectScrollView.Clear();
            StartCoroutine(LoadProjectsAsync());
        }

        mainScrollArea = root.Q<ScrollView>("MainScrollArea");
        scrollChevron = root.Q<VisualElement>("ScrollChevron");

        if (mainScrollArea != null && scrollChevron != null)
        {
            // Check chevron visibility whenever the screen size changes or rows are added
            mainScrollArea.RegisterCallback<GeometryChangedEvent>(evt => CheckChevronVisibility());

            // Check chevron visibility whenever the user scrolls
            mainScrollArea.verticalScroller.valueChanged += (v) => CheckChevronVisibility();
        }
    }

    void OnDisable()
    {
        cachedTextures.Clear();
    }

    void Update()
    {
        if (cyberSpinner != null && loadingScreen != null && loadingScreen.style.display == DisplayStyle.Flex)
        {
            // Spins the ring 360 degrees per second
            spinnerRotation += Time.deltaTime * 360f;
            cyberSpinner.style.rotate = new Rotate(Angle.Degrees(spinnerRotation));
        }
    }

    public void OnPortfolioOpened()
    {
        isPortfolioOpen = true;

        if (isDataLoaded)
        {
            // If data is ready and we haven't played the intro, play it now.
            if (!hasPlayedTransition)
            {
                hasPlayedTransition = true;
                if (scrollAnimationController != null) scrollAnimationController.PrepareCardsForAnimation();
                if (loadingScreen != null)
                {
                    loadingScreen.style.display = DisplayStyle.Flex;
                    loadingScreen.style.opacity = 1f;
                }
                StartCoroutine(HideLoadingScreen());
            }
            // If we have already played it, do nothing! The UI stays perfectly intact.
        }
        else
        {
            // Still loading in the background
            if (scrollAnimationController != null) scrollAnimationController.PrepareCardsForAnimation();
        }
    }

    public void OnPortfolioClosed()
    {
        isPortfolioOpen = false;
    }

    IEnumerator LoadProjectsAsync()
    {
        if (loadingText != null) loadingText.text = "LOADING LEVEL...";
        if (loadingCharacter != null) loadingCharacter.style.left = Length.Percent(0f);

        ProjectsDataWrapper projectsData = default;
        bool loadSuccess = false;

        if (loadFromStreamingAssets)
            yield return StartCoroutine(LoadFromStreamingAssets((data) => { projectsData = data; loadSuccess = true; }));
        else
            yield return StartCoroutine(LoadFromRemoteServer((data) => { projectsData = data; loadSuccess = true; }));

        ProjectMetadata[] listToLoad = (loadSuccess && projectsData.projects != null && projectsData.projects.Length > 0 && !string.IsNullOrEmpty(projectsData.projects[0].GetTitle()))
            ? projectsData.projects : GetDefaultFallbackProjects();

        totalProjectsToLoad = listToLoad.Length;
        projectsLoaded = 0;

        if (loadingText != null) loadingText.text = "FETCHING ASSETS...";

        for (int i = 0; i < listToLoad.Length; i++)
        {
            CreateShowcaseRow(listToLoad[i], i);
        }

        if (scrollAnimationController != null) scrollAnimationController.PrepareCardsForAnimation();

        // Failsafe for empty project lists
        if (totalProjectsToLoad == 0)
        {
            isDataLoaded = true;
            if (isPortfolioOpen && !hasPlayedTransition)
            {
                hasPlayedTransition = true;
                StartCoroutine(HideLoadingScreen());
            }
        }
    }

    public IEnumerator LoadExtraImageRoutine(string path, System.Action<Texture2D> callback)
    {
        yield return StartCoroutine(LoadProjectScreenshotAsync(null, path, callback));
    }

     void OnProjectAssetLoaded()
    {
        projectsLoaded++;
        if (loadingCharacter != null)
        {
            float progress = ((float)projectsLoaded / totalProjectsToLoad) * 100f;
            loadingCharacter.style.left = Length.Percent(progress);
        }

        if (projectsLoaded >= totalProjectsToLoad && !isDataLoaded)
        {
            isDataLoaded = true;
            if (loadingText != null) loadingText.text = "READY";
            if (loadingCharacter != null) loadingCharacter.style.left = Length.Percent(100f);

            if (isPortfolioOpen && !hasPlayedTransition)
            {
                hasPlayedTransition = true;
                StartCoroutine(HideLoadingScreen());
            }
        }
    }

    IEnumerator HideLoadingScreen()
    {
        if (loadingText != null) loadingText.text = "READY";
        if (loadingCharacter != null) loadingCharacter.style.left = Length.Percent(100f);

        yield return new WaitForSeconds(0.3f);

        if (loadingScreen != null)
        {
            loadingScreen.style.opacity = 0f;
            yield return new WaitForSeconds(0.4f);
            loadingScreen.style.display = DisplayStyle.None;
        }

        if (uiDocument != null)
        {
            var scrollChevron = uiDocument.rootVisualElement.Q<Label>("ScrollChevron");
            var mainScroll = uiDocument.rootVisualElement.Q<ScrollView>("MainScrollArea");

            // Only show it if they haven't somehow already scrolled down
            if (scrollChevron != null && (mainScroll == null || mainScroll.verticalScroller.value < 50f))
            {
                scrollChevron.RemoveFromClassList("scroll-chevron--hidden");
                CheckChevronVisibility();
            }
        }

        if (scrollAnimationController != null)
        {
            scrollAnimationController.TriggerRevealAnimations();
        }
    }

    void CreateShowcaseRow(ProjectMetadata project, int index)
    {
        VisualElement rowElement = projectShowcaseRowTemplate.CloneTree();
        var rootRow = rowElement.Q<VisualElement>(className: "showcase-row") ?? rowElement.Q<VisualElement>("ShowcaseRow");
        if (index % 2 == 1 && rootRow != null) rootRow.AddToClassList("showcase-row--reverse");

        var platformBadge = rowElement.Q<Label>(className: "platform-badge") ?? rowElement.Q<Label>("ProjectPlatformTag");
        var titleLabel = rowElement.Q<Label>(className: "project-title") ?? rowElement.Q<Label>("ProjectTitle");
        var descriptionLabel = rowElement.Q<Label>(className: "project-description") ?? rowElement.Q<Label>("ProjectDescription");
        var tagContainer = rowElement.Q<VisualElement>(className: "tag-container");
        var caseStudyButton = rowElement.Q<Button>(className: "btn-primary") ?? rowElement.Q<Button>("BtnCaseStudy");
        var githubButton = rowElement.Q<Button>(className: "btn-secondary") ?? rowElement.Q<Button>("BtnGitHub");

        if (platformBadge != null) platformBadge.text = project.GetPlatform();
        if (titleLabel != null) titleLabel.text = project.GetTitle();
        if (descriptionLabel != null) descriptionLabel.text = project.GetDescription();

        if (tagContainer != null)
        {
            tagContainer.Clear();
            string[] tags = project.GetTag().Split(new char[] { ',', '|' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in tags) { var pill = new Label(t.Trim()); pill.AddToClassList("tech-pill"); tagContainer.Add(pill); }
        }

        Texture2D projectTexture = null;

        if (caseStudyButton != null)
        {
            caseStudyButton.clicked += () => { if (portfolioController != null) portfolioController.ShowProjectDetail(project, projectTexture); };
        }

        if (githubButton != null)
        {
            string url = !string.IsNullOrEmpty(project.githubUrl) ? project.githubUrl : project.caseStudyUrl;
            if (!string.IsNullOrEmpty(url)) githubButton.clicked += () => Application.OpenURL(url);
            else githubButton.style.display = DisplayStyle.None;
        }

        if (projectScrollView != null) projectScrollView.Add(rowElement);

        StartCoroutine(LoadProjectScreenshotAsync(rowElement, project.GetScreenshot(), (loadedTex) =>
        {
            projectTexture = loadedTex;
            OnProjectAssetLoaded();
        }));
    }

    IEnumerator LoadProjectScreenshotAsync(VisualElement rowElement, string screenshotPath, System.Action<Texture2D> onTextureLoaded)
    {
        if (string.IsNullOrEmpty(screenshotPath))
        {
            onTextureLoaded?.Invoke(null);
            yield break;
        }

        if (screenshotPath.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) || screenshotPath.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(screenshotPath))
            {
                www.timeout = 5;
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success)
                {
                    var tex = DownloadHandlerTexture.GetContent(www);
                    if (rowElement != null) ApplyTextureToRow(rowElement, tex);
                    onTextureLoaded?.Invoke(tex);
                }
                else onTextureLoaded?.Invoke(null);
            }
        }
        else
        {
            string resourceName = screenshotPath;
            if (resourceName.StartsWith("Assets/Resources/", System.StringComparison.OrdinalIgnoreCase)) resourceName = resourceName.Substring("Assets/Resources/".Length);
            if (resourceName.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase) || resourceName.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase)) resourceName = resourceName.Substring(0, resourceName.LastIndexOf('.'));

            ResourceRequest request = Resources.LoadAsync<Texture2D>(resourceName);
            yield return request;

            if (request.asset is Texture2D tex)
            {
                ApplyTextureToRow(rowElement, tex);
                onTextureLoaded?.Invoke(tex);
            }
            else onTextureLoaded?.Invoke(null);
        }
    }

    void ApplyTextureToRow(VisualElement rowElement, Texture2D texture)
    {
        //for extrascreenshots, we don't have a rowElement, so we just return if it's null and the invoking function will handle the callback with adding image dynamically.
        if (rowElement == null || texture == null) return;
        var projectImage = rowElement.Q<VisualElement>(className: "project-image") ?? rowElement.Q<VisualElement>("ProjectImage");
        if (projectImage != null)
        {
            projectImage.style.backgroundImage = new StyleBackground(texture);
            cachedTextures.Add(texture);
        }
    }

    ProjectMetadata[] GetDefaultFallbackProjects()
    {
        return new ProjectMetadata[]
        {
            new ProjectMetadata { title = "Real-Time Multiplayer Hand Cricket", tag = "Unity Netcode,Relay & Lobby,Custom RPCs", description = "Performance-optimized up to 5v5 team-based multiplayer game utilizing Unity Netcode, Relay, and Lobby services with custom RPCs and cross-platform Android/WebGL simulation.", screenshotUrl = "Screenshots/HandCricket", platform = "Android / WebGL | 34MB APK" },
            new ProjectMetadata { title = "State-Based 2D Boss Fight", tag = "AI & FSM,Combat Systems,Animation Events", description = "Modular boss AI designed using Finite State Machines to manage complex behavior transitions, attack patterns, and animation events across multiple combat phases.", screenshotUrl = "Screenshots/BossFight", platform = "PC / WebGL" },
            new ProjectMetadata { title = "Infinite Runner with Custom Shader", tag = "Custom HLSL,Procedural Gen,Curvature Shader", description = "Custom vertex shader simulating world curvature with procedural environment generation and multi-character selection featuring seamless restart logic.", screenshotUrl = "Screenshots/InfiniteRunner", platform = "Mobile / WebGL" }
        };
    }

    IEnumerator LoadFromStreamingAssets(System.Action<ProjectsDataWrapper> onComplete)
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, projectDataUrl);
#if UNITY_WEBGL && !UNITY_EDITOR
        using (UnityWebRequest www = UnityWebRequest.Get(path))
        {
            www.timeout = 3;
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
                onComplete?.Invoke(JsonUtility.FromJson<ProjectsDataWrapper>(www.downloadHandler.text));
            else
                onComplete?.Invoke(default);
        }
#else
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            onComplete?.Invoke(JsonUtility.FromJson<ProjectsDataWrapper>(json));
        }
        else
        {
            onComplete?.Invoke(default);
        }
        yield return null;
#endif
    }

    IEnumerator LoadFromRemoteServer(System.Action<ProjectsDataWrapper> onComplete)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(remoteServerUrl))
        {
            www.timeout = 3;
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
                onComplete?.Invoke(JsonUtility.FromJson<ProjectsDataWrapper>(www.downloadHandler.text));
            else
                yield return StartCoroutine(LoadFromStreamingAssets(onComplete));
        }
    }

    void CheckChevronVisibility()
    {
        if (mainScrollArea == null || scrollChevron == null) return;

        // highValue is 0 if all content fits on the screen perfectly.
        // We also hide the chevron if the user has scrolled down more than 10 pixels.
        var detailView = uiDocument.rootVisualElement.Q<VisualElement>("ProjectDetailView");
        bool isDetailOpen = detailView != null && detailView.style.display == DisplayStyle.Flex;

        if (mainScrollArea.verticalScroller.value > 10f)
        {
            hasUserScrolled = true;
        }

        if (isDetailOpen || hasUserScrolled || mainScrollArea.verticalScroller.highValue <= 0)
        {
            scrollChevron.AddToClassList("scroll-chevron--hidden");
        }
        else
        {
            scrollChevron.RemoveFromClassList("scroll-chevron--hidden");
        }
    }
}
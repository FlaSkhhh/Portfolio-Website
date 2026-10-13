using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using System;

public class PortfolioProjectLoaderWeb : MonoBehaviour
{
    [SerializeField] UIDocument uiDocument;
    [SerializeField] VisualTreeAsset projectShowcaseRowTemplate;
    [SerializeField] bool loadFromStreamingAssets = true;
    [SerializeField] string projectDataUrl = "projects.json";
    [SerializeField] string remoteServerUrl = "https://yournetlifydomain.netlify.app/api/projects.json";
    [SerializeField] private List<Texture2D> bonfireFrames;

    VisualElement projectScrollView;
    PortfolioController portfolioController;
    ScrollAnimationController scrollAnimationController;

    // Loader Elements
    VisualElement loadingScreen;
    VisualElement cyberSpinner;
    VisualElement profileImgElement;
    ScrollView mainScrollArea;

    int totalProjectsToLoad = 0;
    int projectsLoaded = 0;
    bool isDataLoaded = false;
    bool isPortfolioOpen = false;
    bool hasPlayedTransition = false; // Prevents the disappearing glitch
    float spinnerRotation = 0f;
    float fireFrameRate = 0.3f;
    float fireTimer;
    int currentFireFrame;

    void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null || projectShowcaseRowTemplate == null) return;

        var root = uiDocument.rootVisualElement;

        loadingScreen = root.Q<VisualElement>("LoadingScreen");

        projectScrollView = root.Q<VisualElement>("ProjectScrollView");
        portfolioController = GetComponent<PortfolioController>();
        scrollAnimationController = GetComponent<ScrollAnimationController>();
        cyberSpinner = root.Q<VisualElement>("CyberSpinner");
        mainScrollArea = root.Q<ScrollView>("MainScrollArea");
        profileImgElement = root.Q<VisualElement>("ProfileImage");

        bonfireFrames = new List<Texture2D>
        {
            Resources.Load<Texture2D>("1"),
            Resources.Load<Texture2D>("2"),
            Resources.Load<Texture2D>("3")
        };
        bonfireFrames.RemoveAll(frame => frame == null);

        if (projectScrollView != null)
        {
            projectScrollView.Clear();
            StartCoroutine(LoadProjectsAsync());
        }

    }

    void Update()
    {
        if (cyberSpinner != null && loadingScreen != null && loadingScreen.style.display == DisplayStyle.Flex)
        {
            // Spins the ring 360 degrees per second
            spinnerRotation += Time.deltaTime * 360f;
            cyberSpinner.style.rotate = new Rotate(Angle.Degrees(spinnerRotation));
        }

        if (bonfireFrames != null && bonfireFrames.Count > 0 && profileImgElement != null && mainScrollArea != null && mainScrollArea.style.display != DisplayStyle.None)
        {
            fireTimer += Time.deltaTime;
            if (fireTimer >= fireFrameRate)
            {
                fireTimer -= fireFrameRate;
                currentFireFrame = (currentFireFrame + 1) % bonfireFrames.Count;

                profileImgElement.style.backgroundImage = new StyleBackground(bonfireFrames[currentFireFrame]);
            }
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
        PortfolioDataWrapper portfolioData = default;
        bool loadSuccess = false;

        if (loadFromStreamingAssets)
            yield return StartCoroutine(LoadFromStreamingAssets((data) => { portfolioData = data; loadSuccess = true; }));
        else
            yield return StartCoroutine(LoadFromRemoteServer((data) => { portfolioData = data; loadSuccess = true; }));

        if (loadSuccess && portfolioData.about != null)
        {
            var root = uiDocument.rootVisualElement;
            if (root != null)
            {

                var expContainer = root.Q<VisualElement>("ExperienceContainer");
                if (expContainer != null && portfolioData.experience != null)
                {
                    expContainer.Clear();
                    foreach (var exp in portfolioData.experience)
                    {
                        var box = new VisualElement();
                        box.AddToClassList("timeline-box");

                        var headerRow = new VisualElement();
                        headerRow.AddToClassList("timeline-header-row");

                        var title = new Label(exp.title);
                        title.AddToClassList("timeline-title");

                        var date = new Label(exp.date);
                        date.AddToClassList("timeline-date");

                        headerRow.Add(title);
                        headerRow.Add(date);
                        box.Add(headerRow);

                        var bulletContainer = new VisualElement();
                        bulletContainer.AddToClassList("timeline-bullets");

                        if (exp.bullets != null)
                        {
                            foreach (var bullet in exp.bullets)
                            {
                                // 1. Create a horizontal row for each bullet point
                                var bulletRow = new VisualElement();
                                bulletRow.AddToClassList("bullet-row-container");

                                // 2. Add the Cyan '>' icon
                                var bulletIcon = new Label(">");
                                bulletIcon.AddToClassList("timeline-bullet-icon");

                                // 3. Add the safe white text
                                var bLabel = new Label(bullet);
                                bLabel.AddToClassList("timeline-text");

                                // 4. Assemble the row
                                bulletRow.Add(bulletIcon);
                                bulletRow.Add(bLabel);
                                bulletContainer.Add(bulletRow);
                            }
                        }
                        box.Add(bulletContainer);
                        expContainer.Add(box);
                    }
                }

                // 2. Generate Education & Skills
                var eduText = root.Q<Label>("EducationText");
                if (eduText != null) eduText.text = portfolioData.education;

                var skillsContainer = root.Q<VisualElement>("SkillsContainer");
                if (skillsContainer != null && portfolioData.skills != null)
                {
                    skillsContainer.Clear();
                    foreach (var skill in portfolioData.skills)
                    {
                        var pill = new Label(skill);
                        pill.AddToClassList("skill-pill");
                        skillsContainer.Add(pill);
                    }
                }

                if (portfolioData.about.paragraphs != null && portfolioData.about.paragraphs.Count > 0)
                {
                    var topLabel = root.Q<Label>("AboutParagraphTop");
                    var bottomLabel = root.Q<Label>("AboutParagraphBottom");
                    // Put Paragraph 0 next to the image
                    if (topLabel != null)
                    {
                        topLabel.text = portfolioData.about.paragraphs[0] +"\n" + portfolioData.about.paragraphs[1];
                    }

                    // Put all remaining paragraphs in the bottom label
                    if (bottomLabel != null && portfolioData.about.paragraphs.Count > 2)
                    {
                        int remainingCount = portfolioData.about.paragraphs.Count - 2;
                        var remainingParagraphs = portfolioData.about.paragraphs.GetRange(2, remainingCount);

                        bottomLabel.text = string.Join("\n", remainingParagraphs);
                    }
                }
            }
        }

        ProjectMetadata[] listToLoad = (loadSuccess && portfolioData.projects != null && portfolioData.projects.Length > 0 && !string.IsNullOrEmpty(portfolioData.projects[0].GetTitle()))
            ? portfolioData.projects : GetDefaultFallbackProjects();

        totalProjectsToLoad = listToLoad.Length;
        projectsLoaded = 0;

        for (int i = 0; i < listToLoad.Length; i++)
        {
            CreateShowcaseRow(listToLoad[i], i);
        }

        if (scrollAnimationController != null) scrollAnimationController.PrepareCardsForAnimation();

        //Failsafe for empty project lists
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

    public IEnumerator LoadExtraImageRoutine(string path, Action<Texture2D> callback)
    {
        yield return StartCoroutine(LoadProjectScreenshotAsync(null, path, callback));
    }

    void OnProjectAssetLoaded()
    {
        projectsLoaded++;

        if (projectsLoaded >= totalProjectsToLoad && !isDataLoaded)
        {
            isDataLoaded = true;
            if (isPortfolioOpen && !hasPlayedTransition)
            {
                hasPlayedTransition = true;
                StartCoroutine(HideLoadingScreen());
            }
        }
    }

    IEnumerator HideLoadingScreen()
    {

        yield return new WaitForSeconds(0.3f);

        if (loadingScreen != null)
        {
            loadingScreen.style.opacity = 0f;
            yield return new WaitForSeconds(0.4f);
            loadingScreen.style.display = DisplayStyle.None;
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
        if (rootRow != null) rootRow.AddToClassList("showcase-row--hidden");

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
            ApplyTextureToRow(rowElement, loadedTex, project);
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

        if (screenshotPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || screenshotPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(screenshotPath))
            {
                www.timeout = 5;
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success)
                {
                    var tex = DownloadHandlerTexture.GetContent(www);
                    onTextureLoaded?.Invoke(tex);
                }
                else onTextureLoaded?.Invoke(null);
            }
        }
        else
        {
            string resourceName = screenshotPath;
            if (resourceName.StartsWith("Assets/Resources/", StringComparison.OrdinalIgnoreCase)) resourceName = resourceName.Substring("Assets/Resources/".Length);
            if (resourceName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || resourceName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)) resourceName = resourceName.Substring(0, resourceName.LastIndexOf('.'));

            ResourceRequest request = Resources.LoadAsync<Texture2D>(resourceName);
            yield return request;

            if (request.asset is Texture2D tex)
            {
                onTextureLoaded?.Invoke(tex);
            }
            else onTextureLoaded?.Invoke(null);
        }
    }

    void ApplyTextureToRow(VisualElement rowElement, Texture2D texture, ProjectMetadata projectData)
    {
        if (rowElement == null) return;

        var projectImage = rowElement.Q<VisualElement>(className: "project-image") ?? rowElement.Q<VisualElement>("ProjectImage");
        var projectTitle = rowElement.Q<Label>(className: "project-title") ?? rowElement.Q<Label>("ProjectTitle");

        if (texture != null && projectImage != null)
        {
            projectImage.style.backgroundImage = new StyleBackground(texture);
        }

        if (projectImage != null)
            projectImage.RegisterCallback<ClickEvent>(evt => { if (portfolioController != null) portfolioController.ShowProjectDetail(projectData, texture); });

        if (projectTitle != null)
            projectTitle.RegisterCallback<ClickEvent>(evt => { if (portfolioController != null) portfolioController.ShowProjectDetail(projectData, texture); });
    }

    ProjectMetadata[] GetDefaultFallbackProjects()
    {
        return new ProjectMetadata[]{};
    }

    IEnumerator LoadFromStreamingAssets(Action<PortfolioDataWrapper> onComplete)
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, projectDataUrl);
#if UNITY_WEBGL && !UNITY_EDITOR
        using (UnityWebRequest www = UnityWebRequest.Get(path))
        {
            www.timeout = 3;
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
                onComplete?.Invoke(JsonUtility.FromJson<PortfolioDataWrapper>(www.downloadHandler.text));
            else
                onComplete?.Invoke(default);
        }
#else
        if (System.IO.File.Exists(path))
        {
            string json = System.IO.File.ReadAllText(path);
            onComplete?.Invoke(JsonUtility.FromJson<PortfolioDataWrapper>(json));
        }
        else
        {
            onComplete?.Invoke(default);
        }
        yield return null;
#endif
    }

    IEnumerator LoadFromRemoteServer(Action<PortfolioDataWrapper> onComplete)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(remoteServerUrl))
        {
            www.timeout = 3;
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
                onComplete?.Invoke(JsonUtility.FromJson<PortfolioDataWrapper>(www.downloadHandler.text));
            else
                yield return StartCoroutine(LoadFromStreamingAssets(onComplete));
        }
    }
}

[Serializable]
public struct PortfolioDataWrapper
{
    public AboutData about;
    public ExperienceData[] experience;
    public string education;
    public string[] skills;
    public ProjectMetadata[] projects;
}

[Serializable]
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
    public string role;
    public string duration;
    public string[] responsibilities; // Replaces bodyParagraphs
    public string[] videoUrls;
    public string GetTitle() => !string.IsNullOrEmpty(title) ? title : Title;
    public string GetTag() => !string.IsNullOrEmpty(tag) ? tag : Tag;
    public string GetDescription() => !string.IsNullOrEmpty(description) ? description : Description;
    public string GetScreenshot() => !string.IsNullOrEmpty(screenshotUrl) ? screenshotUrl : ScreenshotAddress;
    public string GetPlatform() => !string.IsNullOrEmpty(platform) ? platform : (!string.IsNullOrEmpty(Platform) ? Platform : "Unity / C#");

}



[Serializable]
public class AboutData
{
    public string title;
    public string subtitle;
    public string profileUrl;
    public List<string> paragraphs;
}

[Serializable]
public struct ExperienceData
{
    public string title;
    public string date;
    public string[] bullets;
}
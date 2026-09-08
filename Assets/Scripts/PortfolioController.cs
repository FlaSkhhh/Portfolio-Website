using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PortfolioController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement introView;
    private VisualElement mainPortfolioView;
    private VisualElement projectDetailView;
    private Button btnEnter;
    private Button btnBack;
    private Button btnBackToHome;
    private Button btnDetailDemo;
    private Button btnDetailGitHub;
    private Label scrollChevron;
    private float chevronBounceTimer = 0f;
    private float previousScrollPosition = 0f;
    private string currentDemoUrl;
    private string currentGithubUrl;
    private bool hasDismissedChevron = false;

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void WebGLTrapBackButton();

    void Start()
    {
        // Only run the JavaScript bridge if we are actually built for WebGL
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLTrapBackButton();
#endif
    }

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;

        introView = root.Q<VisualElement>("IntroView");
        mainPortfolioView = root.Q<VisualElement>("MainPortfolioView");
        projectDetailView = root.Q<VisualElement>("ProjectDetailView");
        btnEnter = root.Q<Button>("BtnEnter");
        btnBack = root.Q<Button>("BtnBack");
        btnBackToHome = root.Q<Button>("BtnBackToHome");
        btnDetailDemo = root.Q<Button>("BtnDetailDemo");
        btnDetailGitHub = root.Q<Button>("BtnDetailGitHub");

        if (btnEnter != null) btnEnter.clicked += OnEnterPortfolioClicked;
        if (btnBack != null) btnBack.clicked += OnBackClicked;
        if (btnBackToHome != null) btnBackToHome.clicked += OnBackToHomeClicked;
        if (btnDetailDemo != null) btnDetailDemo.clicked += () => { if (!string.IsNullOrEmpty(currentDemoUrl)) Application.OpenURL(currentDemoUrl); };
        if (btnDetailGitHub != null) btnDetailGitHub.clicked += () => { if (!string.IsNullOrEmpty(currentGithubUrl)) Application.OpenURL(currentGithubUrl); };
        var backdrop = root.Q<VisualElement>("DetailBackdrop");
        if (backdrop != null) backdrop.RegisterCallback<ClickEvent>(evt => OnBackClicked());

        scrollChevron = root.Q<Label>("ScrollChevron");
        var mainScroll = root.Q<ScrollView>("MainScrollArea");

        if (mainScroll != null && scrollChevron != null)
        {
            scrollChevron.AddToClassList("scroll-chevron--hidden");
            mainScroll.verticalScroller.valueChanged += (scrollValue) =>
            {
                if (hasDismissedChevron) return;
                if (scrollValue > 50f)
                {
                    scrollChevron.AddToClassList("scroll-chevron--hidden");
                    hasDismissedChevron = true;
                }
                else
                {
                    scrollChevron.RemoveFromClassList("scroll-chevron--hidden");
                }
            };
        }
    }

    void OnDisable()
    {
        if (btnEnter != null) btnEnter.clicked -= OnEnterPortfolioClicked;
        if (btnBack != null) btnBack.clicked -= OnBackClicked;
        if (btnBackToHome != null) btnBackToHome.clicked -= OnBackToHomeClicked;
    }

    void Update()
    {
        // Bounces the chevron up and down
        if (scrollChevron != null && !scrollChevron.ClassListContains("scroll-chevron--hidden"))
        {
            chevronBounceTimer += Time.deltaTime * 3f;
            float yOffset = Mathf.Sin(chevronBounceTimer) * -10f;
            scrollChevron.style.translate = new Translate(0, yOffset, 0);
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // If we are looking at a Case Study, go back to the Grid
            if (projectDetailView != null && projectDetailView.style.display == DisplayStyle.Flex)
            {
                OnBackClicked();
            }
            // If we are looking at the Grid, go back to the Intro/Home screen
            else if (mainPortfolioView != null && mainPortfolioView.style.display == DisplayStyle.Flex)
            {
                OnBackToHomeClicked();
            }
        }
    }

    void OnEnterPortfolioClicked()
    {
        if (introView != null) introView.style.display = DisplayStyle.None;
        if (mainPortfolioView != null) mainPortfolioView.style.display = DisplayStyle.Flex;
        if (projectDetailView != null) projectDetailView.style.display = DisplayStyle.None;

        // Tell the loader the page was opened so it can play the transition
        var loader = GetComponent<PortfolioProjectLoaderWeb>();
        if (loader != null) loader.OnPortfolioOpened();
    }


    public void ShowProjectDetail(PortfolioProjectLoaderWeb.ProjectMetadata project, Texture2D heroScreenshot)
    {
        currentDemoUrl = project.caseStudyUrl;
        currentGithubUrl = project.githubUrl;

        var mainScroll = uiDocument.rootVisualElement.Q<ScrollView>("MainScrollArea");
        if (mainScroll != null)
        {
            previousScrollPosition = mainScroll.verticalScroller.value;
            mainScroll.style.display = DisplayStyle.None;
        }

        // Hide Chevron globally while in the Case Study
        var scrollChevron = uiDocument.rootVisualElement.Q<Label>("ScrollChevron");
        if (scrollChevron != null) scrollChevron.AddToClassList("scroll-chevron--hidden");

        var detailTitle = uiDocument.rootVisualElement.Q<Label>("DetailTitle");
        var detailTagsContainer = uiDocument.rootVisualElement.Q<VisualElement>("DetailTagsContainer");
        var detailImage = uiDocument.rootVisualElement.Q<VisualElement>("DetailImage");

        // This is the empty UXML container where we will dump the JSON content
        var dynamicContent = uiDocument.rootVisualElement.Q<VisualElement>("DetailDynamicContent");

        if (detailTitle != null) detailTitle.text = project.GetTitle();

        // 1. Generate Tags
        if (detailTagsContainer != null)
        {
            detailTagsContainer.Clear();
            string[] tags = project.GetTag().Split(new char[] { '|', ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in tags)
            {
                var pill = new Label(t.Trim());
                pill.AddToClassList("tech-pill");
                detailTagsContainer.Add(pill);
            }
        }

        // 2. Set Hero Image
        if (detailImage != null && heroScreenshot != null)
        {
            detailImage.style.backgroundImage = new StyleBackground(heroScreenshot);
            detailImage.style.display = DisplayStyle.Flex;
        }
        else if (detailImage != null) detailImage.style.display = DisplayStyle.None;

        // Toggle the Action Row Buttons
        if (btnDetailDemo != null) btnDetailDemo.style.display = string.IsNullOrEmpty(currentDemoUrl) ? DisplayStyle.None : DisplayStyle.Flex;
        if (btnDetailGitHub != null) btnDetailGitHub.style.display = string.IsNullOrEmpty(currentGithubUrl) ? DisplayStyle.None : DisplayStyle.Flex;

        // 3. GENERATE DYNAMIC JSON CONTENT (Text & Extra Images)
        if (dynamicContent != null)
        {
            dynamicContent.Clear();

            string[] paragraphs = project.bodyParagraphs != null && project.bodyParagraphs.Length > 0
                ? project.bodyParagraphs
                : new string[] { project.GetDescription() };

            for (int i = 0; i < paragraphs.Length; i++)
            {
                // 1. Create the Row Container
                var rowContainer = new VisualElement();
                rowContainer.AddToClassList("detail-dynamic-row");

                string imgPath = (project.extraScreenshots != null && i < project.extraScreenshots.Length) ? project.extraScreenshots[i] : "";
                bool skipImage = string.IsNullOrEmpty(imgPath) || imgPath.Trim().ToLower() == "no" || imgPath.Trim().ToLower() == "null";

                // Alternate layout: Even rows = Text Left, Odd rows = Text Right
                if (!skipImage && i % 2 != 0)
                {
                    rowContainer.AddToClassList("detail-dynamic-row--reverse");
                }

                // 2. Create the Text
                var textLabel = new Label(paragraphs[i]);
                textLabel.AddToClassList("detail-dynamic-text");
                if (skipImage)
                {
                    // If there's no image, remove the zigzag gap padding
                    textLabel.AddToClassList("detail-dynamic-text--full");
                }
                rowContainer.Add(textLabel);
                if (!skipImage)
                {
                    var extraImg = new VisualElement();
                    extraImg.AddToClassList("detail-dynamic-image");
                    extraImg.AddToClassList("skeleton-loader"); // Yellow dashed frame

                    var skeletonLabel = new Label("AWAITING ASSET...");
                    skeletonLabel.AddToClassList("skeleton-text");
                    extraImg.Add(skeletonLabel);

                    rowContainer.Add(extraImg);

                    // Fetch Image Logic
                    var loader = GetComponent<PortfolioProjectLoaderWeb>();
                    if (loader != null)
                    {
                        StartCoroutine(loader.LoadExtraImageRoutine(project.extraScreenshots[i], (tex) => {
                            if (tex != null)
                            {
                                extraImg.RemoveFromClassList("skeleton-loader");
                                extraImg.Clear();
                                extraImg.style.backgroundImage = new StyleBackground(tex);
                            }
                            else
                            {
                                extraImg.style.display = DisplayStyle.None;
                                textLabel.style.width = new StyleLength(Length.Percent(100)); // Expand text if image fails
                            }
                        }));
                    }
                }
                else
                {
                    // If the JSON explicitly skips the image, force the text to take up 100% width
                    textLabel.style.width = new StyleLength(Length.Percent(100));
                }

                // Add the finished row to the screen
                dynamicContent.Add(rowContainer);
            }
        }

        if (mainPortfolioView != null) mainPortfolioView.style.display = DisplayStyle.None;
        if (projectDetailView != null) projectDetailView.style.display = DisplayStyle.Flex;
    }


    void OnBackClicked()
    {
        if (mainPortfolioView != null) mainPortfolioView.style.display = DisplayStyle.Flex;
        if (projectDetailView != null) projectDetailView.style.display = DisplayStyle.None;

        var mainScroll = uiDocument.rootVisualElement.Q<ScrollView>("MainScrollArea");
        if (mainScroll != null) mainScroll.style.display = DisplayStyle.Flex;
        if (mainScroll != null)
        {
            mainScroll.schedule.Execute(() =>
            {
                mainScroll.verticalScroller.value = previousScrollPosition;
                if (scrollChevron != null && !hasDismissedChevron && previousScrollPosition < 50f)
                {
                    scrollChevron.RemoveFromClassList("scroll-chevron--hidden");
                }
            }).StartingIn(10);  //some delay for the card heigh calc to complete for previous position to be set correctly
        }
    }
    void OnBackToHomeClicked()
    {
        if (introView != null) introView.style.display = DisplayStyle.Flex;
        if (mainPortfolioView != null) mainPortfolioView.style.display = DisplayStyle.None;
        if (projectDetailView != null) projectDetailView.style.display = DisplayStyle.None;

        // Reset the loader state for the next time we enter
        var loader = GetComponent<PortfolioProjectLoaderWeb>();
        if (loader != null) loader.OnPortfolioClosed();
        // Hide chevron on the home screen
        if (scrollChevron != null) scrollChevron.AddToClassList("scroll-chevron--hidden");
    }

    // This is explicitly called by our BrowserInput.jslib file when a mobile user swipes "Back"
    public void OnBrowserBack()
    {
        // Re-use the exact same logic we built for the PC Escape key
        if (projectDetailView != null && projectDetailView.style.display == DisplayStyle.Flex)
        {
            OnBackClicked();
        }
        else if (mainPortfolioView != null && mainPortfolioView.style.display == DisplayStyle.Flex)
        {
            OnBackToHomeClicked();
        }
    }
}
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class PortfolioController : MonoBehaviour
{
    UIDocument uiDocument;
    VisualElement introView;
    VisualElement mainPortfolioView;
    VisualElement projectDetailView;
    VisualElement swipeIndicatorBack;
    VisualElement swipeIndicatorForward;
    VisualElement activeIndicator; // Tracks back or forward is currently being dragged 
    VisualElement heroPortal;
    VisualElement portfolioHeaderRow;       //for non scrolling back button
    VisualElement introHeadline;
    Label headerName;
    Label scrollChevron;
    Button headerExploreBtn;
    Button btnEnter;
    Button btnBack;
    Button btnBackToHome;
    Button btnDetailDemo;
    Button btnDetailGitHub;
    Dictionary<string, float> pageScrollPositions = new Dictionary<string, float>();
    float chevronBounceTimer = 0f;
    float dynamicScrollThreshold = 50f;     //for chebron hiding
    string currentDemoUrl;
    string currentGithubUrl;
    bool hasTeleported = false;
    bool hasDismissedChevron = false;

    string activePageState = "intro";

    [DllImport("__Internal")] private static extern void WebGLInitHistory();
    [DllImport("__Internal")] private static extern void WebGLPushHistoryState(string view);
    [DllImport("__Internal")] private static extern void WebGLGoBack();

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInitHistory();   
#endif
    }

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        root.Query<ScrollView>().ForEach(scrollView =>
        {
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        });

        introView = root.Q<VisualElement>("IntroView");
        mainPortfolioView = root.Q<VisualElement>("MainPortfolioView");
        projectDetailView = root.Q<VisualElement>("ProjectDetailView");
        btnEnter = root.Q<Button>("BtnEnter");
        btnBack = root.Q<Button>("BtnBack");
        swipeIndicatorBack = root.Q<VisualElement>("SwipeIndicatorBack");
        swipeIndicatorForward = root.Q<VisualElement>("SwipeIndicatorForward");
        btnBackToHome = root.Q<Button>("BtnBackToHome");
        btnDetailDemo = root.Q<Button>("BtnDetailDemo");
        btnDetailGitHub = root.Q<Button>("BtnDetailGitHub");
        headerExploreBtn = root.Q<Button>("HeaderExploreBtn");
        headerName = root.Q<Label>("HeaderName");
        heroPortal = root.Q<VisualElement>("HeroPortal");
        introHeadline = root.Q<VisualElement>(className: "intro-headline");
        portfolioHeaderRow = root.Q<VisualElement>("PortfolioHeaderRow");

        if (btnEnter != null) btnEnter.clicked += OnEnterPortfolioClicked;
        if (btnBack != null) btnBack.clicked += OnBackClicked;
        if (btnBackToHome != null) btnBackToHome.clicked += OnBackToHomeClicked;
        if (btnDetailDemo != null) btnDetailDemo.clicked += () => { if (!string.IsNullOrEmpty(currentDemoUrl)) Application.OpenURL(currentDemoUrl); };
        if (btnDetailGitHub != null) btnDetailGitHub.clicked += () => { if (!string.IsNullOrEmpty(currentGithubUrl)) Application.OpenURL(currentGithubUrl); };
        if (headerExploreBtn != null) headerExploreBtn.clicked += OnEnterPortfolioClicked;  
        var backdrop = root.Q<VisualElement>("DetailBackdrop");
        if (backdrop != null) backdrop.RegisterCallback<ClickEvent>(evt => OnBackClicked());

        scrollChevron = root.Q<Label>("ScrollChevron");
        var mainScroll = root.Q<ScrollView>("MainScrollArea");

        if (mainScroll != null && scrollChevron != null)
        {
            scrollChevron.AddToClassList("scroll-chevron--hidden");
            dynamicScrollThreshold = 50f;


            // Ensure they are fully removed from layout on boot so they don't block clicks
            if (headerName != null) headerName.style.display = DisplayStyle.None;
            if (headerExploreBtn != null) headerExploreBtn.style.display = DisplayStyle.None;

            // Geometry listener for the Chevron...
            mainScroll.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                float contentHeight = mainScroll.contentContainer.layout.height;
                float viewportHeight = mainScroll.layout.height;
                float viewportWidth = mainScroll.layout.width;

                if (contentHeight == 0 || viewportHeight == 0) return;

                if (viewportWidth < viewportHeight)
                {
                    root.AddToClassList("is-mobile");
                    if (headerName != null) headerName.text = "SHANTANU NANDKHILE"; 
                }
                else
                {
                    root.RemoveFromClassList("is-mobile");
                    if (headerName != null) headerName.text = "SHANTANU NANDKHILE";
                }

                if (contentHeight <= viewportHeight)
                {
                    scrollChevron.AddToClassList("scroll-chevron--hidden");
                    hasDismissedChevron = true;
                }
                else
                {
                    dynamicScrollThreshold = viewportHeight * 0.1f;
                    if (mainScroll.verticalScroller.value < dynamicScrollThreshold)
                    {
                        scrollChevron.RemoveFromClassList("scroll-chevron--hidden");
                        hasDismissedChevron = false;
                    }
                }

                var heroContainer = root.Q<VisualElement>(className: "hero-portal-container");
                if (heroContainer != null)
                {
                    heroContainer.style.height = viewportHeight;
                    heroContainer.style.minHeight = viewportHeight; // Overrides the 800px in your USS
                }
            });

            // The Scroll Listener
            mainScroll.verticalScroller.valueChanged += (scrollValue) =>
            {
                if (activePageState != "intro" && activePageState != "home")
                {
                    return;
                }

                if (!hasDismissedChevron)
                {
                    if (scrollValue > dynamicScrollThreshold)
                    {
                        scrollChevron.AddToClassList("scroll-chevron--hidden");
                        hasDismissedChevron = true;
                    }
                    else scrollChevron.RemoveFromClassList("scroll-chevron--hidden");
                }

                float screenHeight = mainScroll.layout.height;
                if (screenHeight > 0)
                {
                    // 1. THE COLLISION POINT
                    // Text is at 50% (center). Portal is roughly 15% from the top. 
                    // Distance for them to meet is 35% of the screen height.
                    float collisionDistance = screenHeight * 0.35f;

                    // 2. TEXT FADE (Engulfed exactly as it hits the portal)
                    // It stays fully visible, then rapidly fades out over the 100 pixels right before collision.
                    float textFadeStart = Mathf.Max(0, collisionDistance - 100f);
                    float textAlpha = 1f - Mathf.InverseLerp(textFadeStart, collisionDistance, scrollValue);

                    if (introHeadline != null) introHeadline.style.opacity = textAlpha;
                    if (btnEnter != null) btnEnter.style.opacity = textAlpha;

                    // 3. PORTAL STICKY & FADE
                    if (heroPortal != null)
                    {
                        // CRITICAL: The portal sticks perfectly (matches scroll) UNTIL the collision distance.
                        // After collision, it locks its offset, allowing it to naturally scroll UP and off the screen.
                        float stickyY = Mathf.Clamp(scrollValue, 0, collisionDistance);
                        heroPortal.style.translate = new Translate(0, stickyY, 0);

                        // We fade the portal out slowly AFTER it starts moving up, so it doesn't pop out of existence.
                        // Starts fading at collision, and is completely gone 400 pixels later.
                        float portalAlpha = 1f - Mathf.InverseLerp(collisionDistance, collisionDistance + 400f, scrollValue);

                        heroPortal.style.opacity = portalAlpha;
                        heroPortal.style.display = portalAlpha <= 0f ? DisplayStyle.None : DisplayStyle.Flex;
                    }
                }
                // 2. THE ONE-WAY HEADER REVEAL
                // Adjust this number based on how far down your "About" section is
                float teleportThreshold = screenHeight * 0.5f;

                if (!hasTeleported && scrollValue >= teleportThreshold)
                {
                    hasTeleported = true; // Locks it so it never fires again

                    // Unhide the elements in the layout
                    if (headerName != null) headerName.style.display = DisplayStyle.Flex;
                    if (headerExploreBtn != null) headerExploreBtn.style.display = DisplayStyle.Flex;

                    // Wait 20ms for the UI layout to recalculate, then remove the hidden CSS class
                    // This triggers the smooth slide-out animation!
                    mainScroll.schedule.Execute(() =>
                    {
                        if (headerName != null) headerName.RemoveFromClassList("header-teleport-item--hidden");
                        if (headerExploreBtn != null) headerExploreBtn.RemoveFromClassList("header-teleport-item--hidden");
                    }).StartingIn(20);
                }
            };
            if (heroPortal != null)
            {
                heroPortal.schedule.Execute(() =>
                {
                    // 1. SLOW WAVE: Changed 2f to 0.5f so it takes longer to complete a cycle
                    float t = (Mathf.Sin(Time.time * 0.5f) + 1f) / 2f;

                    // 2. SMALL COLOR SHIFT: Two similar, warm colors
                    // The original neon orange
                    Color baseOrange = new Color(1f, 0.45f, 0f);
                    // A slightly darker, fiery red-orange
                    Color deepOrange = new Color(1f, 0.20f, 0f);

                    // 3. Apply the smooth, subtle blend
                    heroPortal.style.unityBackgroundImageTintColor = Color.Lerp(baseOrange, deepOrange, t);

                }).Every(16);
            }
        }
    }

    void OnDisable()
    {
        if (btnEnter != null) btnEnter.clicked -= OnEnterPortfolioClicked;
        if (btnBack != null) btnBack.clicked -= OnBackClicked;
        if (btnBackToHome != null) btnBackToHome.clicked -= OnBackToHomeClicked;
        if (headerExploreBtn != null) headerExploreBtn.clicked -= OnEnterPortfolioClicked;
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
    }
    void OnEnterPortfolioClicked()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLPushHistoryState("projects");
#endif
        OnBrowserStateChanged("projects"); // Force UI update locally
    }

    void OnBackClicked()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLGoBack(); // Triggers popstate -> OnBrowserStateChanged
#else
        OnBrowserStateChanged("projects"); // Fallback for Editor
#endif
    }

    void OnBackToHomeClicked()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLGoBack();
#endif
        OnBrowserStateChanged("intro");
    }

   /* void OnEnterPortfolioClicked()
    {
        if (introView != null) introView.style.display = DisplayStyle.None;
        if (mainPortfolioView != null) mainPortfolioView.style.display = DisplayStyle.Flex;
        if (projectDetailView != null) projectDetailView.style.display = DisplayStyle.None;

        // Tell the loader the page was opened so it can play the transition
        var loader = GetComponent<PortfolioProjectLoaderWeb>();
        if (loader != null) loader.OnPortfolioOpened();
    }*/


    public void ShowProjectDetail(PortfolioProjectLoaderWeb.ProjectMetadata project, Texture2D heroScreenshot)
    {
        currentDemoUrl = project.caseStudyUrl;
        currentGithubUrl = project.githubUrl;


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
        string projectNameRoute = project.GetTitle().Replace(" ", "-").ToLower();
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLPushHistoryState(projectNameRoute);
#endif
        OnBrowserStateChanged(projectNameRoute);

        /*if (mainPortfolioView != null) mainPortfolioView.style.display = DisplayStyle.None;
        if (projectDetailView != null) projectDetailView.style.display = DisplayStyle.Flex;*/
    }

    // Called by JS OR our C# fallback methods
    public void OnBrowserStateChanged(string view)
    {
        if (string.IsNullOrEmpty(view) || view == "undefined")
        {
            view = "intro";
        }

        var mainScroll = uiDocument.rootVisualElement.Q<ScrollView>("MainScrollArea");

        // 1. CACHE LIST SCROLL POSITIONS ONLY
        // We only save the scroll state if we are leaving a list page. We NEVER save the detail page scroll.
        if (mainScroll != null && !string.IsNullOrEmpty(activePageState))
        {
            if (activePageState == "intro" || activePageState == "projects")
            {
                pageScrollPositions[activePageState] = mainScroll.verticalScroller.value;
            }
        }

        activePageState = view;

        // 2. TOGGLE VIEWS
        if (view == "intro" || view == "home")
        {
            if (introView != null) introView.style.display = DisplayStyle.Flex;
            if (mainPortfolioView != null) mainPortfolioView.style.display = DisplayStyle.None;
            if (projectDetailView != null) projectDetailView.style.display = DisplayStyle.None;
            if (portfolioHeaderRow != null) portfolioHeaderRow.style.display = DisplayStyle.None;

            var loader = GetComponent<PortfolioProjectLoaderWeb>();
            if (loader != null) loader.OnPortfolioClosed();
            if (scrollChevron != null) scrollChevron.AddToClassList("scroll-chevron--hidden");

            // Restore Intro scroll position
            if (mainScroll != null)
            {
                mainScroll.schedule.Execute(() => {
                    mainScroll.verticalScroller.value = pageScrollPositions.ContainsKey(view) ? pageScrollPositions[view] : 0f;
                }).StartingIn(10);
            }
        }
        else if (view == "projects")
        {
            if (introView != null) introView.style.display = DisplayStyle.None;
            if (mainPortfolioView != null) mainPortfolioView.style.display = DisplayStyle.Flex;
            if (projectDetailView != null) projectDetailView.style.display = DisplayStyle.None;
            if (portfolioHeaderRow != null) portfolioHeaderRow.style.display = DisplayStyle.Flex;

            var loader = GetComponent<PortfolioProjectLoaderWeb>();
            if (loader != null) loader.OnPortfolioOpened();

            // Restore Projects list scroll position
            if (mainScroll != null)
            {
                mainScroll.style.display = DisplayStyle.Flex;
                mainScroll.schedule.Execute(() => {
                    float savedScroll = pageScrollPositions.ContainsKey(view) ? pageScrollPositions[view] : 0f;
                    mainScroll.verticalScroller.value = savedScroll;

                    if (scrollChevron != null && !hasDismissedChevron && savedScroll < 50f)
                    {
                        scrollChevron.RemoveFromClassList("scroll-chevron--hidden");
                    }
                }).StartingIn(10);
            }
        }
        else // DETAIL VIEW (A Specific Project)
        {
            if (introView != null) introView.style.display = DisplayStyle.None;
            if (mainPortfolioView != null) mainPortfolioView.style.display = DisplayStyle.None;
            if (projectDetailView != null) projectDetailView.style.display = DisplayStyle.Flex;
            if (portfolioHeaderRow != null) portfolioHeaderRow.style.display = DisplayStyle.None;

            // CRITICAL FIX: Always start a new project at the absolute top (0f)
            if (mainScroll != null)
            {
                mainScroll.style.display = DisplayStyle.Flex;
                mainScroll.schedule.Execute(() => {
                    mainScroll.verticalScroller.value = 0f;
                }).StartingIn(10);
            }
        }
    }

    public void PlaySwipeFeedback(string direction)
    {
        var swipeIndicator = uiDocument.rootVisualElement.Q<VisualElement>("SwipeIndicator");
        if (swipeIndicator == null) return;

        // Reset any ongoing animations
        swipeIndicator.RemoveFromClassList("swipe-anim--back");
        swipeIndicator.RemoveFromClassList("swipe-anim--forward");

        if (direction == "back")
        {
            // Triggers a USS class that slides it from Left to Center
            swipeIndicator.AddToClassList("swipe-anim--back");
        }
        else if (direction == "forward")
        {
            // Triggers a USS class that slides it from Right to Center
            swipeIndicator.AddToClassList("swipe-anim--forward");
        }
    }

    // Called dozens of times a second by JS during a drag
    public void UpdateSwipeVisual(string data)
    {
        // data comes in as "0.45|back"
        string[] split = data.Split('|');
        if (split.Length < 2) return;

        float screenWidth = uiDocument.rootVisualElement.layout.width;
        // Fallback just in case UI Toolkit hasn't calculated layout yet on frame 1
        if (screenWidth == 0) screenWidth = Screen.width;

        // Calculate exactly 30% of the screen width!
        float maxPullDistance = Mathf.Max(screenWidth * 0.30f, 170f + (screenWidth * 0.15f));

        if (float.TryParse(split[0], out float progress))
        {
            string direction = split[1];

            // 1. Pick the correct indicator
            activeIndicator = (direction == "back") ? swipeIndicatorBack : swipeIndicatorForward;
            if (activeIndicator == null) return;

            // 2. Unhide it
            activeIndicator.style.display = DisplayStyle.Flex;

            // 3. Math for the visual drag
            // Move inward up to 120px, scale up to 1.2x, fade in fully
            float moveX = Mathf.Lerp(0, maxPullDistance, progress);
            float scale = Mathf.Lerp(0.5f, 1.2f, progress);
            float fade = Mathf.Lerp(0f, 1f, progress);

            // Apply direction (pull right for back, pull left for forward)
            float finalX = (direction == "back") ? moveX : -moveX;

            // UI Toolkit retains the Y translate from our USS (-50%), so we keep it!
            activeIndicator.style.translate = new Translate(finalX, new Length(-50, LengthUnit.Percent), 0);
            activeIndicator.style.scale = new Scale(new Vector2(scale, scale));
            activeIndicator.style.opacity = fade;
        }
    }

    // Called once when the user lifts their finger
    public void EndSwipeVisual(string result)
    {
        if (activeIndicator == null) return;

        // 1. CAPTURE IT LOCALLY so the timers don't throw a NullReferenceException
        VisualElement targetIndicator = activeIndicator;
        activeIndicator = null;

        if (result == "success")
        {
            targetIndicator.AddToClassList("swipe-indicator--success");

            // 1. Pop up to 1.3x
            targetIndicator.style.scale = new Scale(new Vector2(1.3f, 1.3f));
            targetIndicator.style.opacity = 1f;

            // 2. Shrink completely away (0.01f prevents WebGL math crashes)
            targetIndicator.schedule.Execute(() =>
            {
                targetIndicator.style.scale = new Scale(new Vector2(0.01f, 0.01f));
                targetIndicator.style.opacity = 0f;
            }).StartingIn(140);

            // 3. Cleanup
            targetIndicator.schedule.Execute(() =>
            {
                targetIndicator.style.display = DisplayStyle.None;
                targetIndicator.RemoveFromClassList("swipe-indicator--success");

                // Reset scale to 1f while hidden so layout memory is clean
                targetIndicator.style.scale = new Scale(new Vector2(1f, 1f));
                targetIndicator.style.translate = new Translate(0, new Length(-50, LengthUnit.Percent), 0);
            }).StartingIn(150);
        }
        else
        {
            // SWIPE CANCELLED
            targetIndicator.style.translate = new Translate(0, new Length(-50, LengthUnit.Percent), 0);

            // Shrink completely away
            targetIndicator.style.scale = new Scale(new Vector2(0.01f, 0.01f));
            targetIndicator.style.opacity = 0f;

            targetIndicator.schedule.Execute(() =>
            {
                targetIndicator.style.display = DisplayStyle.None;

                // Reset scale to 1f while hidden
                targetIndicator.style.scale = new Scale(new Vector2(1f, 1f));
            }).StartingIn(10);
        }
    }
}
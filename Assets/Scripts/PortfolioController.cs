using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PortfolioController : MonoBehaviour
{
    [SerializeField] MouseShaderTracker shaderTracker;
    PortfolioProjectLoaderWeb loader;
    UIDocument uiDocument;
    ScrollView mainScroll;
    ScrollView detailScroll;
    VisualElement introView;
    VisualElement mainPortfolioView;
    VisualElement projectDetailView;
    VisualElement swipeIndicatorBack;
    VisualElement swipeIndicatorForward;
    VisualElement activeIndicator; // Tracks back or forward is currently being dragged 
    VisualElement heroPortal;
    VisualElement portfolioHeaderRow;       //for non scrolling back button
    VisualElement introHeadline;
    VisualElement introSubtitle;
    VisualElement detailTagsContainer;
    VisualElement detailImage;
    VisualElement dynamicContent;
    VisualElement modalOverlay;
    VisualElement modalBox;
    //VisualElement swipeIndicator;
    Label headerName;
    Label scrollChevron;
    Label detailTitle;
    Button headerExploreBtn;
    Button navContactBtn;
    Button btnEnter;
    Button btnBack;
    Button btnBackToHome;
    Button btnDetailDemo;
    Button btnDetailGitHub;
    Button btnCloseModal;
    Button btnContactGitHub;
    Button btnContactLinkedIn;
    Dictionary<string, float> pageScrollPositions = new Dictionary<string, float>();
    float chevronBounceTimer = 0f;
    float dynamicScrollThreshold = 50f;     //for chebron hiding
    string currentDemoUrl;
    string currentGithubUrl; 
    bool hasTeleported = false;
    bool hasDismissedChevronIntro = false;
    bool hasDismissedChevronProjects = false;
    bool isIntroTextFullyHidden = false;
    bool isPortalFullyHidden = false;
    bool isContactModalOpen = false;
    string activePageState = "intro";

    [DllImport("__Internal")] static extern void WebGLInitHistory();
    [DllImport("__Internal")] static extern void WebGLPushHistoryState(string view);
    [DllImport("__Internal")] static extern void WebGLGoBack();
    [DllImport("__Internal")] static extern void WebGLSetSwipeLock(bool isLocked);
    [DllImport("__Internal")] static extern void WebGLCancelHistoryChange();

    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInitHistory();   
#endif
    }

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        loader = GetComponent<PortfolioProjectLoaderWeb>();

        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        root.Query<ScrollView>().ForEach(scrollView =>
        {
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.mode = ScrollViewMode.Vertical;
            scrollView.mouseWheelScrollSize = 30f;
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
        introHeadline = root.Q<VisualElement>("HeroTitle");
        introSubtitle = root.Q<VisualElement>("HeroSubtitle");
        portfolioHeaderRow = root.Q<VisualElement>("PortfolioHeaderRow");
        mainScroll = root.Q<ScrollView>("MainScrollArea");
        detailScroll = root.Q<ScrollView>("DetailScrollArea");
        scrollChevron = root.Q<Label>("ScrollChevron");
        detailTitle = root.Q<Label>("DetailTitle");
        detailTagsContainer = root.Q<VisualElement>("DetailTagsContainer");
        detailImage = root.Q<VisualElement>("DetailImage");
        dynamicContent = root.Q<VisualElement>("DetailDynamicContent");
        modalOverlay = root.Q<VisualElement>("ContactModalOverlay");
        modalBox = root.Q<VisualElement>("ContactModalBox");
        btnCloseModal = root.Q<Button>("BtnCloseModal");
        navContactBtn = root.Q<Button>("NavContact");
        btnContactGitHub = root.Q<Button>("BtnContactGitHub");
        btnContactLinkedIn = root.Q<Button>("BtnContactLinkedIn");
        //swipeIndicator = uiDocument.rootVisualElement.Q<VisualElement>("SwipeIndicator");


        if (btnEnter != null) btnEnter.clicked += OnEnterPortfolioClicked;
        if (btnBack != null) btnBack.clicked += OnBackClicked;
        if (btnBackToHome != null) btnBackToHome.clicked += OnBackToHomeClicked;
        if (btnDetailDemo != null) btnDetailDemo.clicked += () => { if (!string.IsNullOrEmpty(currentDemoUrl)) Application.OpenURL(currentDemoUrl); };
        if (btnDetailGitHub != null) btnDetailGitHub.clicked += () => { if (!string.IsNullOrEmpty(currentGithubUrl)) Application.OpenURL(currentGithubUrl); };
        if (headerExploreBtn != null) headerExploreBtn.clicked += OnEnterPortfolioClicked;
        if (navContactBtn != null) navContactBtn.clicked += OpenContactModal;
        if (btnCloseModal != null) btnCloseModal.clicked += CloseContactModal;

        if (modalOverlay != null) modalOverlay.RegisterCallback<ClickEvent>(evt => CloseContactModal());
        if (modalBox != null) modalBox.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        if (btnContactGitHub != null) btnContactGitHub.clicked += () => Application.OpenURL("https://github.com/FlaSkhhh");
        if (btnContactLinkedIn != null) btnContactLinkedIn.clicked += () => Application.OpenURL("https://www.linkedin.com/in/flashhhh/");

        var backdrop = root.Q<VisualElement>("DetailBackdrop");
        if (backdrop != null) backdrop.RegisterCallback<ClickEvent>(evt => OnBackClicked());

        var heroContainer = root.Q<VisualElement>(className: "hero-portal-container");

        if (mainScroll != null && scrollChevron != null)
        {
            scrollChevron.AddToClassList("scroll-chevron--hidden");
            dynamicScrollThreshold = 50f;


            // Ensure they are fully removed from layout on boot so they don't block clicks
            if (headerName != null) headerName.style.display = DisplayStyle.None;
            if (headerExploreBtn != null) headerExploreBtn.style.display = DisplayStyle.None;


            float lastViewportWidth = 0f;
            float lastViewportHeight = 0f;
            float lastContentHeight = 0f;
            string lastPageState = "";

            mainScroll.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                float contentHeight = mainScroll.contentContainer.layout.height;
                float viewportHeight = mainScroll.layout.height;
                float viewportWidth = mainScroll.layout.width;

                if (contentHeight == 0 || viewportHeight == 0) return;

                if (Mathf.Approximately(contentHeight, lastContentHeight) &&
                    Mathf.Approximately(viewportHeight, lastViewportHeight) &&
                    Mathf.Approximately(viewportWidth, lastViewportWidth) &&
                    activePageState == lastPageState)
                {
                    return;
                }
                // Update the trackers so we know for next time
                lastContentHeight = contentHeight;
                lastViewportHeight = viewportHeight;
                lastViewportWidth = viewportWidth;
                lastPageState = activePageState;

                // --- EVERYTHING BELOW HERE NOW ONLY RUNS WHEN TRULY NECESSARY ---

                if (viewportWidth < viewportHeight) { root.AddToClassList("is-mobile"); }
                else { root.RemoveFromClassList("is-mobile"); }

                bool isCurrentlyDismissed = (activePageState == "projects") ? hasDismissedChevronProjects : hasDismissedChevronIntro;

                if (!isCurrentlyDismissed && contentHeight > viewportHeight && mainScroll.verticalScroller.value < dynamicScrollThreshold)
                {
                    scrollChevron.RemoveFromClassList("scroll-chevron--hidden");
                }
                else
                {
                    scrollChevron.AddToClassList("scroll-chevron--hidden");
                }

                
                if (heroContainer != null)
                {
                    heroContainer.style.height = viewportHeight;
                    heroContainer.style.minHeight = viewportHeight; // Overrides the 800px in your USS
                }
            });

            // The Scroll Listener (Highly Optimized)
            mainScroll.verticalScroller.valueChanged += (scrollValue) =>
            {
                if (activePageState != "intro" && activePageState != "projects") return;

                // ==========================================
                // 1. CHEVRON LOGIC (Runs on Intro and Projects)
                // ==========================================
                if (activePageState == "intro")
                {
                    if (!hasDismissedChevronIntro && scrollValue > 50f)
                    {
                        hasDismissedChevronIntro = true;
                        scrollChevron.AddToClassList("scroll-chevron--hidden");
                    }
                }
                else if (activePageState == "projects")
                {
                    if (!hasDismissedChevronProjects && scrollValue > 50f)
                    {
                        hasDismissedChevronProjects = true;
                        scrollChevron.AddToClassList("scroll-chevron--hidden");
                    }
                    return; // CRITICAL OPTIMIZATION: Stop executing! The rest is only for the Intro page.
                }

                // Use our cached screen height from the GeometryEvent instead of querying UI Toolkit layout!
                if (lastViewportHeight <= 0) return;

                float collisionDistance = lastViewportHeight * 0.4f;
                float portalFadeEnd = collisionDistance + 400f;
                float textFadeEnd = collisionDistance - 80f;
                float textFadeStart = Mathf.Max(0, textFadeEnd - 150f);

                if (scrollValue >= textFadeEnd)
                {
                    if (!isIntroTextFullyHidden)
                    {
                        if (introHeadline != null) introHeadline.style.opacity = 0f;
                        if (introSubtitle != null) introSubtitle.style.opacity = 0f;
                        if (btnEnter != null) btnEnter.style.opacity = 0f;
                        isIntroTextFullyHidden = true;
                    }
                }
                else
                {
                    isIntroTextFullyHidden = false;
                    float textAlpha = 1f - Mathf.InverseLerp(textFadeStart, textFadeEnd, scrollValue);
                    if (introHeadline != null) introHeadline.style.opacity = textAlpha;
                    if (introSubtitle != null) introSubtitle.style.opacity = textAlpha;
                    if (btnEnter != null) btnEnter.style.opacity = textAlpha;
                }

                // --- PORTAL STICKY & FADE ---
                if (scrollValue >= portalFadeEnd)
                {
                    // Put it to sleep.
                    if (!isPortalFullyHidden && heroPortal != null)
                    {
                        heroPortal.style.opacity = 0f;
                        heroPortal.style.display = DisplayStyle.None;
                        isPortalFullyHidden = true;
                    }
                }
                else
                {
                    isPortalFullyHidden = false;
                    if (heroPortal != null)
                    {
                        float stickyY = Mathf.Clamp(scrollValue, 0, collisionDistance);
                        heroPortal.style.translate = new Translate(0, stickyY, 0);

                        float portalAlpha = 1f - Mathf.InverseLerp(collisionDistance, portalFadeEnd, scrollValue);
                        heroPortal.style.opacity = portalAlpha;
                        heroPortal.style.display = DisplayStyle.Flex; // Ensure it stays visible while fading
                    }
                }

                // ==========================================
                // 3. THE ONE-WAY HEADER REVEAL
                // ==========================================
                if (!hasTeleported && scrollValue >= (lastViewportHeight * 0.5f))
                {
                    hasTeleported = true; // Locks it so it never fires again

                    if (headerName != null) headerName.style.display = DisplayStyle.Flex;
                    if (headerExploreBtn != null) headerExploreBtn.style.display = DisplayStyle.Flex;

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
        if (isContactModalOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseContactModal();
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
#else
        OnBrowserStateChanged("intro");
#endif
    }

    private void OpenContactModal()
    {
        shaderTracker.TriggerGlitch();
        isContactModalOpen = true;
        modalOverlay.RemoveFromClassList("modal-overlay--hidden");

#if UNITY_WEBGL && !UNITY_EDITOR
    WebGLSetSwipeLock(true); // Disable physical swiping!
#endif
        PlayGlitch(modalBox, openSteps, () => StartAmbientGlitch(modalBox));
    }

    private void CloseContactModal()
    {
        isContactModalOpen = false;

#if UNITY_WEBGL && !UNITY_EDITOR
    WebGLSetSwipeLock(false); // Re-enable physical swiping!
#endif
        PlayGlitch(modalBox, closeSteps, () =>
        {
            modalOverlay.AddToClassList("modal-overlay--hidden");
            ClearGlitchClasses(modalBox);  // removes glitch-out-final so the next open starts clean
        });
    }

    static readonly (string cls, long at)[] openSteps =
    {
        ("glitch-1", 0), ("glitch-2", 60), ("glitch-3", 120),
        ("glitch-4", 180), ("glitch-5", 250), ("glitch-6", 310),
        (null, 380) // stable
    };
    static readonly (string cls, long at)[] closeSteps = { ("glitch-3", 0), ("glitch-2", 30), ("glitch-1", 60), ("glitch-out-final", 90) };
    static readonly string[] allGlitchClasses = { "glitch-1", "glitch-2", "glitch-3", "glitch-4", "glitch-5", "glitch-6", "glitch-out-final" };
    readonly List<IVisualElementScheduledItem> glitchPending = new List<IVisualElementScheduledItem>();


    void PlayGlitch(VisualElement box, (string cls, long at)[] steps, System.Action onDone = null)
    {
        StopAmbientGlitch(box);
        CancelGlitch();
        long last = 0;

        foreach (var (cls, at) in steps)
        {
            var targetClass = cls;
            glitchPending.Add(box.schedule.Execute(() =>
            {
                ClearGlitchClasses(box);
                if (targetClass != null) box.AddToClassList(targetClass);
            }).StartingIn(at));

            if (at > last) last = at;
        }

        if (onDone != null)
            glitchPending.Add(box.schedule.Execute(onDone).StartingIn(last + 30));
    }

    void CancelGlitch()
    {
        foreach (var item in glitchPending) item.Pause();
        glitchPending.Clear();
    }

    static void ClearGlitchClasses(VisualElement box)
    {
        foreach (var g in allGlitchClasses) box.RemoveFromClassList(g);
    }
    static readonly string[] ambientClasses = { "glitch-amb-1", "glitch-amb-2", "glitch-amb-3" };
    readonly List<IVisualElementScheduledItem> ambientPending = new List<IVisualElementScheduledItem>();
    bool ambientRunning;

    void StartAmbientGlitch(VisualElement box)
    {
        StopAmbientGlitch(box);
        ambientRunning = true;
        ScheduleAmbientBurst(box);
    }

    void StopAmbientGlitch(VisualElement box)
    {
        ambientRunning = false;
        foreach (var item in ambientPending) item.Pause();
        ambientPending.Clear();
        foreach (var c in ambientClasses) box.RemoveFromClassList(c);
    }

    void ScheduleAmbientBurst(VisualElement box)
    {
        ambientPending.Clear(); // the previous burst has finished by now

        ambientPending.Add(box.schedule.Execute(() =>
        {
            if (!ambientRunning) return;

            int flashes = Random.Range(1, 4);
            long t = 0;

            for (int i = 0; i < flashes; i++)
            {
                string cls = ambientClasses[Random.Range(0, ambientClasses.Length)];

                ambientPending.Add(box.schedule.Execute(() =>
                {
                    foreach (var c in ambientClasses) box.RemoveFromClassList(c);
                    box.AddToClassList(cls);
                }).StartingIn(t));
                t += Random.Range(50, 100);   // how long each flash holds

                ambientPending.Add(box.schedule.Execute(() =>
                {
                    foreach (var c in ambientClasses) box.RemoveFromClassList(c);
                }).StartingIn(t));
                t += Random.Range(20, 50);   // gap between flashes inside a burst
            }

            ambientPending.Add(box.schedule.Execute(() =>
            {
                if (ambientRunning) ScheduleAmbientBurst(box);
            }).StartingIn(t));

        }).StartingIn(Random.Range(400, 1200))); // quiet gap before the next burst
    }

    public void ShowProjectDetail(ProjectMetadata project, Texture2D heroScreenshot)
    {
        currentDemoUrl = project.caseStudyUrl;
        currentGithubUrl = project.githubUrl;


        // Hide Chevron globally while in the Case Study
        if (scrollChevron != null) scrollChevron.AddToClassList("scroll-chevron--hidden");



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
        shaderTracker.TriggerGlitch();

        if (isContactModalOpen)
        {
            CloseContactModal();

#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLCancelHistoryChange(); 
#endif
            return;
        }

        if (string.IsNullOrEmpty(view) || view == "undefined")
        {
            view = "intro";
        }

        // 1. CACHE LIST SCROLL POSITIONS ONLY
        if (mainScroll != null && !string.IsNullOrEmpty(activePageState))
        {
            if (activePageState == "intro" || activePageState == "projects")
            {
                pageScrollPositions[activePageState] = mainScroll.verticalScroller.value;
            }
        }

        activePageState = view;

        // 2. TOGGLE VIEWS
        if (view == "intro")
        {
            if (introView != null) introView.style.display = DisplayStyle.Flex;
            if (mainPortfolioView != null) mainPortfolioView.style.display = DisplayStyle.None;
            if (projectDetailView != null) projectDetailView.style.display = DisplayStyle.None;
            if (portfolioHeaderRow != null) portfolioHeaderRow.style.display = DisplayStyle.None;

            if (loader != null) loader.OnPortfolioClosed();

            if (scrollChevron != null)
            {
                if (!hasDismissedChevronIntro) scrollChevron.RemoveFromClassList("scroll-chevron--hidden");
                else scrollChevron.AddToClassList("scroll-chevron--hidden");
            }
            if (hasTeleported && headerExploreBtn != null) headerExploreBtn.style.display = DisplayStyle.Flex;

            // Restore Intro scroll position
            if (mainScroll != null)
            {
                mainScroll.style.display = DisplayStyle.Flex; // CRITICAL: Turn main scroll back on
                mainScroll.style.opacity = 0f;
                mainScroll.schedule.Execute(() => {
                    mainScroll.verticalScroller.value = pageScrollPositions.ContainsKey(view) ? pageScrollPositions[view] : 0f;
                    mainScroll.style.opacity = 1f;
                }).StartingIn(50);
            }
        }
        else if (view == "projects")
        {
            if (introView != null) introView.style.display = DisplayStyle.None;
            if (mainPortfolioView != null) mainPortfolioView.style.display = DisplayStyle.Flex;
            if (projectDetailView != null) projectDetailView.style.display = DisplayStyle.None;
            if (portfolioHeaderRow != null) portfolioHeaderRow.style.display = DisplayStyle.Flex;
            if (headerExploreBtn != null) headerExploreBtn.style.display = DisplayStyle.None;

            if (loader != null) loader.OnPortfolioOpened();

            // Restore Projects list scroll position
            if (mainScroll != null)
            {
                mainScroll.style.display = DisplayStyle.Flex; // CRITICAL: Turn main scroll back on
                mainScroll.style.opacity = 0f;  
                mainScroll.schedule.Execute(() => {
                    float savedScroll = pageScrollPositions.ContainsKey(view) ? pageScrollPositions[view] : 0f;
                    mainScroll.verticalScroller.value = savedScroll;

                    if (scrollChevron != null)
                    {
                        if (!hasDismissedChevronProjects) scrollChevron.RemoveFromClassList("scroll-chevron--hidden");
                        else scrollChevron.AddToClassList("scroll-chevron--hidden");
                    }
                    mainScroll.style.opacity = 1f;
                }).StartingIn(50);
            }
        }
        else // DETAIL VIEW (A Specific Project)
        {
            if (introView != null) introView.style.display = DisplayStyle.None;
            if (mainPortfolioView != null) mainPortfolioView.style.display = DisplayStyle.None;
            if (projectDetailView != null) projectDetailView.style.display = DisplayStyle.Flex;
            if (portfolioHeaderRow != null) portfolioHeaderRow.style.display = DisplayStyle.None;
            if (headerExploreBtn != null) headerExploreBtn.style.display = DisplayStyle.None;
            if (scrollChevron != null) scrollChevron.AddToClassList("scroll-chevron--hidden");

            // CRITICAL FIX: Hide the entire MainScrollArea so it stops pushing the detail view down!
            if (mainScroll != null) mainScroll.style.display = DisplayStyle.None;

            if (detailScroll != null)
            {
                
                detailScroll.style.opacity = 0f;
                detailScroll.schedule.Execute(() => {
                    detailScroll.verticalScroller.value = 0f;
                    detailScroll.style.opacity = 1f;
                }).StartingIn(50);
            }
        }
    }

    /*public void PlaySwipeFeedback(string direction)
    {
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
    }*/

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
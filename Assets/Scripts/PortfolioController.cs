using UnityEngine;
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

    private float previousScrollPosition = 0f;
    private string currentDemoUrl;
    private string currentGithubUrl;

    private void OnEnable()
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
    }

    private void OnDisable()
    {
        if (btnEnter != null) btnEnter.clicked -= OnEnterPortfolioClicked;
        if (btnBack != null) btnBack.clicked -= OnBackClicked;
        if (btnBackToHome != null) btnBackToHome.clicked -= OnBackToHomeClicked;
    }

    private void OnEnterPortfolioClicked()
    {
        if (introView != null) introView.style.display = DisplayStyle.None;
        if (mainPortfolioView != null) mainPortfolioView.style.display = DisplayStyle.Flex;
        if (projectDetailView != null) projectDetailView.style.display = DisplayStyle.None;

        // Tell the loader the page was opened so it can play the transition
        var loader = GetComponent<PortfolioProjectLoaderWeb>();
        if (loader != null) loader.OnPortfolioOpened();
    }

    private void OnBackToHomeClicked()
    {
        if (introView != null) introView.style.display = DisplayStyle.Flex;
        if (mainPortfolioView != null) mainPortfolioView.style.display = DisplayStyle.None;
        if (projectDetailView != null) projectDetailView.style.display = DisplayStyle.None;

        // Reset the loader state for the next time we enter
        var loader = GetComponent<PortfolioProjectLoaderWeb>();
        if (loader != null) loader.OnPortfolioClosed();
    }

    public void ShowProjectDetail(string title, string description, string tagString, Texture2D screenshot, string demoUrl = "", string githubUrl = "")
    {
        currentDemoUrl = demoUrl;
        currentGithubUrl = githubUrl;

        var mainScroll = uiDocument.rootVisualElement.Q<ScrollView>("MainScrollArea");
        if (mainScroll != null) previousScrollPosition = mainScroll.verticalScroller.value;

        var detailTitle = uiDocument.rootVisualElement.Q<Label>("DetailTitle");
        var detailDescription = uiDocument.rootVisualElement.Q<Label>("DetailDescription");
        var detailTagsContainer = uiDocument.rootVisualElement.Q<VisualElement>("DetailTagsContainer");
        var detailImage = uiDocument.rootVisualElement.Q<VisualElement>("DetailImage");

        if (detailTitle != null) detailTitle.text = title;
        if (detailDescription != null) detailDescription.text = description;

        // Render tech pills dynamically
        if (detailTagsContainer != null)
        {
            detailTagsContainer.Clear();
            string[] tags = tagString.Split(new char[] { '|', ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in tags)
            {
                var pill = new Label(t.Trim());
                pill.AddToClassList("tech-pill");
                detailTagsContainer.Add(pill);
            }
        }

        if (detailImage != null && screenshot != null)
        {
            detailImage.style.backgroundImage = new StyleBackground(screenshot);
            detailImage.style.display = DisplayStyle.Flex;
        }
        else if (detailImage != null)
        {
            detailImage.style.display = DisplayStyle.None;
        }

        if (btnDetailDemo != null) btnDetailDemo.style.display = string.IsNullOrEmpty(demoUrl) ? DisplayStyle.None : DisplayStyle.Flex;
        if (btnDetailGitHub != null) btnDetailGitHub.style.display = string.IsNullOrEmpty(githubUrl) ? DisplayStyle.None : DisplayStyle.Flex;

        if (mainPortfolioView != null) mainPortfolioView.style.display = DisplayStyle.None;
        if (projectDetailView != null) projectDetailView.style.display = DisplayStyle.Flex;
    }

    private void OnBackClicked()
    {
        if (mainPortfolioView != null) mainPortfolioView.style.display = DisplayStyle.Flex;
        if (projectDetailView != null) projectDetailView.style.display = DisplayStyle.None;

        var mainScroll = uiDocument.rootVisualElement.Q<ScrollView>("MainScrollArea");
        if (mainScroll != null) mainScroll.verticalScroller.value = previousScrollPosition;
    }
}
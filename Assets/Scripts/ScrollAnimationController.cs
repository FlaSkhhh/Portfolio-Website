using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class ScrollAnimationController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private float parallaxMultiplier = 0.05f;

    private ScrollView mainScrollArea;
    private VisualElement projectScrollView;
    private List<VisualElement> showcaseRows = new();

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        mainScrollArea = root.Q<ScrollView>("MainScrollArea");
        projectScrollView = root.Q<VisualElement>("ProjectScrollView");

        if (mainScrollArea != null)
        {
            mainScrollArea.verticalScroller.valueChanged += OnScrollPositionChanged;
        }
    }

    private void OnDisable()
    {
        if (mainScrollArea != null && mainScrollArea.verticalScroller != null)
            mainScrollArea.verticalScroller.valueChanged -= OnScrollPositionChanged;
    }

    public void RefreshShowcaseRows()
    {
        if (projectScrollView == null) return;

        showcaseRows.Clear();
        showcaseRows.AddRange(projectScrollView.Query(className: "showcase-row").ToList());

        foreach (var row in showcaseRows)
        {
            if (row != null) row.style.opacity = 1f;
        }
    }

    private void OnScrollPositionChanged(float scrollValue)
    {
        if (mainScrollArea == null || showcaseRows.Count == 0) return;

        float scrollViewHeight = mainScrollArea.contentViewport.layout.height;

        foreach (var row in showcaseRows)
        {
            if (row == null) continue;

            // worldBound automatically accounts for scroll offsets to give true screen position
            float rowTopInViewport = row.worldBound.y - mainScrollArea.worldBound.y;
            float rowBottomInViewport = rowTopInViewport + row.worldBound.height;

            bool isInView = rowBottomInViewport > 0 && rowTopInViewport < scrollViewHeight;

            if (isInView)
            {
                var projectImage = row.Q<VisualElement>("ProjectImage");
                if (projectImage != null)
                {
                    float offset = (rowTopInViewport / (scrollViewHeight > 0 ? scrollViewHeight : 1000f)) * 15f * parallaxMultiplier;
                    projectImage.style.translate = new Translate(0, offset, 0);
                }
            }
        }
    }
}
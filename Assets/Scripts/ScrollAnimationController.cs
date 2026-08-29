using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class ScrollAnimationController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private float parallaxMultiplier = 0.05f;
    [SerializeField] private float fadeInThreshold = 0.85f;

    private ScrollView projectScrollView;
    private List<VisualElement> showcaseRows = new();
    private Dictionary<VisualElement, bool> hasAnimated = new();

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        projectScrollView = root.Q<ScrollView>("ProjectScrollView");

        if (projectScrollView != null)
        {
            projectScrollView.verticalScroller.valueChanged += OnScrollPositionChanged;
            root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
    }

    private void OnDisable()
    {
        if (projectScrollView != null && projectScrollView.verticalScroller != null)
            projectScrollView.verticalScroller.valueChanged -= OnScrollPositionChanged;

        if (uiDocument != null && uiDocument.rootVisualElement != null)
            uiDocument.rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    public void RefreshShowcaseRows()
    {
        if (projectScrollView == null) return;

        showcaseRows.Clear();
        showcaseRows.AddRange(projectScrollView.contentContainer.Query(className: "showcase-row").ToList());

        foreach (var row in showcaseRows)
        {
            if (!hasAnimated.ContainsKey(row))
            {
                hasAnimated[row] = false;
                row.style.opacity = 0f;
            }
        }

        OnScrollPositionChanged(projectScrollView.verticalScroller.value);
    }

    private void OnScrollPositionChanged(float scrollValue)
    {
        if (projectScrollView == null || showcaseRows.Count == 0)
            return;

        float scrollViewHeight = projectScrollView.contentViewport.layout.height;
        float totalContentHeight = projectScrollView.contentContainer.layout.height;

        foreach (var row in showcaseRows)
        {
            if (row == null) continue;

            float rowTopInViewport = row.layout.y - projectScrollView.verticalScroller.value;
            float rowBottomInViewport = rowTopInViewport + row.layout.height;

            bool isInView = rowBottomInViewport > 0 && rowTopInViewport < scrollViewHeight;

            if (isInView && hasAnimated.TryGetValue(row, out bool animated) && !animated)
            {
                row.style.opacity = 1f;
                hasAnimated[row] = true;
            }

            if (isInView)
            {
                var projectImage = row.Q<VisualElement>("ProjectImage");
                if (projectImage != null)
                {
                    float offset = (rowTopInViewport / scrollViewHeight) * 20f * parallaxMultiplier;
                    projectImage.style.translate = new Translate(0, offset, 0);
                }
            }
        }
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        if (projectScrollView != null)
            OnScrollPositionChanged(projectScrollView.verticalScroller.value);
    }
}
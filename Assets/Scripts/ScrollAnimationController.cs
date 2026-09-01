using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class ScrollAnimationController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private float parallaxMultiplier = 0.05f;
    [SerializeField] private long staggerDelayMs = 150;

    private ScrollView mainScrollArea;
    private VisualElement projectScrollView;
    private List<VisualElement> showcaseRows = new();
    private HashSet<VisualElement> revealedRows = new();

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
        if (mainScrollArea != null)
            mainScrollArea.verticalScroller.valueChanged -= OnScrollPositionChanged;
    }

    public void PrepareCardsForAnimation()
    {
        if (projectScrollView == null) return;

        showcaseRows.Clear();
        revealedRows.Clear();
        showcaseRows.AddRange(projectScrollView.Query(className: "showcase-row").ToList());

        foreach (var row in showcaseRows)
        {
            row.RemoveFromClassList("showcase-row--visible");
            row.AddToClassList("showcase-row--hidden");
        }
    }

    public void TriggerRevealAnimations()
    {
        if (mainScrollArea != null)
        {
            mainScrollArea.schedule.Execute(EvaluateRowVisibility).StartingIn(100);
        }
    }

    private void OnScrollPositionChanged(float scrollValue)
    {
        EvaluateRowVisibility();
    }

    private void EvaluateRowVisibility()
    {
        if (mainScrollArea == null || showcaseRows.Count == 0) return;

        float scrollViewHeight = mainScrollArea.contentViewport.layout.height;
        if (scrollViewHeight <= 0) return;

        // Gather all cards that need to be revealed on this frame
        List<VisualElement> rowsToRevealThisFrame = new List<VisualElement>();

        foreach (var row in showcaseRows)
        {
            if (row == null || revealedRows.Contains(row)) continue;

            float rowTopInViewport = row.worldBound.y - mainScrollArea.worldBound.y;
            float rowBottomInViewport = rowTopInViewport + row.worldBound.height;

            float revealThreshold = scrollViewHeight * 0.90f;

            if (rowTopInViewport < revealThreshold && rowBottomInViewport > 0)
            {
                rowsToRevealThisFrame.Add(row);
            }
        }

        // Sort them strictly by their physical Y position on the screen to prevent out-of-order race conditions
        rowsToRevealThisFrame.Sort((a, b) => a.worldBound.y.CompareTo(b.worldBound.y));

        for (int i = 0; i < rowsToRevealThisFrame.Count; i++)
        {
            var row = rowsToRevealThisFrame[i];
            revealedRows.Add(row);

            long delay = i * staggerDelayMs;

            row.schedule.Execute(() =>
            {
                if (row != null)
                {
                    row.RemoveFromClassList("showcase-row--hidden");
                    row.AddToClassList("showcase-row--visible");
                }
            }).StartingIn(delay);
        }

        // Apply parallax tracking
        foreach (var row in revealedRows)
        {
            if (row == null) continue;

            float rowTopInViewport = row.worldBound.y - mainScrollArea.worldBound.y;
            var projectImage = row.Q<VisualElement>("ProjectImage");

            if (projectImage != null)
            {
                float offset = (rowTopInViewport / (scrollViewHeight > 0 ? scrollViewHeight : 1000f)) * 15f * parallaxMultiplier;
                projectImage.style.translate = new Translate(0, offset, 0);
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class ScrollAnimationController : MonoBehaviour
{
    [SerializeField] UIDocument uiDocument;
    [SerializeField] long staggerDelayMs = 150;

    ScrollView mainScrollArea;
    VisualElement projectScrollView;
    List<VisualElement> showcaseRows = new();
    HashSet<VisualElement> revealedRows = new();
    Dictionary<VisualElement, VisualElement> rowImageCache = new();


    void OnEnable()
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

    void OnDisable()
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

    void OnScrollPositionChanged(float scrollValue)
    {
        EvaluateRowVisibility();
    }

    void EvaluateRowVisibility()
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
                var img = row.Q<VisualElement>("ProjectImage");
                if (img != null) rowImageCache[row] = img;

            }
        }

        rowsToRevealThisFrame.Sort((a, b) => showcaseRows.IndexOf(a).CompareTo(showcaseRows.IndexOf(b)));

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
    }
}
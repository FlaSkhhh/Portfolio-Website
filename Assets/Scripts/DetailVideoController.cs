using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

public class VideoSlot
{
    public VisualElement screen, overlay;
    public Button playButton, fallback;
    public Label status;
    public string url;
    public StyleBackground poster;
}

public class DetailVideoController : MonoBehaviour
{
    [SerializeField] string baseUrl = "";   //url for vids
    [SerializeField] int width = 1280, height = 720;
    [SerializeField] float loadTimeoutSeconds = 8f;

    VideoPlayer player;
    RenderTexture rt;
    VideoSlot active;
    IVisualElementScheduledItem timeout;

    void Awake()
    {
        rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);

        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.source = VideoSource.Url;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = rt;
        player.isLooping = true;
        player.prepareCompleted += OnPrepared;
        player.errorReceived += (vp, msg) => Fail("VIDEO UNAVAILABLE");
    }

    void OnDestroy() { if (rt != null) rt.Release(); }

    public string ResolveUrl(string path)
    {
        path = path.Trim();
        if (path.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase)) return path;

        string root = string.IsNullOrEmpty(baseUrl) ? Application.streamingAssetsPath : baseUrl.TrimEnd('/');
        return root + "/" + path.TrimStart('/');
    }

    public void Play(VideoSlot slot)
    {
        Stop();
        active = slot;
        slot.poster = slot.screen.style.backgroundImage;
        slot.playButton.style.display = DisplayStyle.None;
        slot.fallback.style.display = DisplayStyle.None;
        slot.status.text = "LOADING...";

        player.url = slot.url;
        player.Prepare();

        timeout = slot.screen.schedule.Execute(() => Fail("TAKING TOO LONG"))
                                      .StartingIn((long)(loadTimeoutSeconds * 1000f));
    }

    void OnPrepared(VideoPlayer vp)
    {
        if (active == null || vp.url != active.url) return;
        timeout?.Pause();
        active.screen.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(rt));
        active.overlay.style.display = DisplayStyle.None;
        vp.Play();
    }

    void Fail(string message)
    {
        if (active == null) return;
        timeout?.Pause();
        player.Stop();
        active.status.text = message;
        active.fallback.style.display = DisplayStyle.Flex;
    }

    public void Stop()
    {
        timeout?.Pause();
        if (player != null) player.Stop();
        if (active == null) return;

        active.screen.style.backgroundImage = active.poster;
        active.overlay.style.display = DisplayStyle.Flex;
        active.playButton.style.display = DisplayStyle.Flex;
        active.status.text = "";
        active = null;
    }
}
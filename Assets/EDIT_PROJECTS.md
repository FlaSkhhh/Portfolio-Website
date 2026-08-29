# Quick Project Data Editor

> **Tip**: You don't need to recompile or rebuild anything to update your projects!  
> Just edit `Assets/StreamingAssets/projects.json` and save.

## How to Add/Edit Projects

Open `Assets/StreamingAssets/projects.json` in any text editor:

### Template for New Project:

```json
{
  "id": "unique-project-slug",
  "title": "Your Amazing Project Name",
  "tag": "Technology | Category",
  "description": "2-3 sentence description highlighting what makes this project special. Include tech stack, impact, or unique achievement.",
  "screenshotUrl": "https://your-storage.com/images/project-screenshot.png",
  "caseStudyUrl": "https://your-portfolio.netlify.app/case-studies/unique-project-slug"
}
```

### Field Reference:

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `id` | string | Yes | Unique identifier (lowercase, hyphens ok) |
| `title` | string | Yes | Project display name |
| `tag` | string | Yes | Tech stack (appears as badge on card) |
| `description` | string | Yes | 2-3 sentences max (fits in card height) |
| `screenshotUrl` | string | Yes | Full URL to image (HTTPS recommended) |
| `caseStudyUrl` | string | No | External link when clicking "View Case Study" |

### Example: Full projects.json

```json
{
  "projects": [
	{
	  "id": "hand-cricket",
	  "title": "Real-Time Multiplayer Hand Cricket",
	  "tag": "Unity Netcode | 34MB APK",
	  "description": "Performance-optimized 5v5 multiplayer game leveraging Unity Netcode, Relay, and Lobby services. Custom RPCs handle cross-platform Android/WebGL simulation with sub-100ms latency.",
	  "screenshotUrl": "https://storage.googleapis.com/portfolio/hand-cricket.png",
	  "caseStudyUrl": "https://yourportfolio.com/hand-cricket"
	},
	{
	  "id": "boss-fight",
	  "title": "State-Based 2D Boss Fight",
	  "tag": "AI & FSM | Combat Systems",
	  "description": "Modular boss AI using Finite State Machines for complex behavior transitions. Manages attack patterns, animation events, and multi-phase progression with zero hardcoding.",
	  "screenshotUrl": "https://storage.googleapis.com/portfolio/boss-fight.png",
	  "caseStudyUrl": "https://yourportfolio.com/boss-fight"
	},
	{
	  "id": "infinite-runner",
	  "title": "Infinite Runner with Custom Shader",
	  "tag": "Custom HLSL | Procedural Gen",
	  "description": "Custom vertex shader simulates world curvature in real-time. Procedural environment generation scales infinitely with multi-character selection and seamless restart logic.",
	  "screenshotUrl": "https://storage.googleapis.com/portfolio/infinite-runner.png",
	  "caseStudyUrl": "https://yourportfolio.com/infinite-runner"
	}
  ]
}
```

## Image Hosting Options (Free)

| Service | Setup | URL Format | Notes |
|---------|-------|-----------|-------|
| **Google Cloud Storage** | 15GB free tier | `https://storage.googleapis.com/bucket/file.png` | Best for production |
| **GitHub (repo)** | Upload to `/images/` | `https://raw.githubusercontent.com/user/repo/main/images/file.png` | CDN-backed, free |
| **Imgur** | Upload image | `https://imgur.com/xxxxx.png` | Fast, no account needed |
| **Netlify Drag & Drop** | Upload to Netlify | `/images/file.png` | Hosted with your WebGL build |
| **Cloudinary** | Free tier 25GB | `https://res.cloudinary.com/user/image/upload/v1/file.png` | Image optimization included |

## Validation Checklist

Before saving `projects.json`:

- [ ] All `id` values are unique and lowercase
- [ ] All `title` are descriptive and under 50 characters
- [ ] All URLs start with `https://` (required for WebGL)
- [ ] No trailing commas in JSON
- [ ] Images load in browser (test screenshot URLs)
- [ ] JSON is valid (paste into [jsonlint.com](https://jsonlint.com) to verify)

## Testing Changes (No Rebuild!)

1. Save `projects.json`
2. In Unity Editor, press **Play**
3. Click "Enter Portfolio" button
4. Your updated projects should appear in ~1 second
5. If not, check Console for errors

## Deployment

Once you're happy with your projects:

1. Build WebGL (File → Build Settings → Build)
2. Deploy to Netlify (drag `Build/` folder)
3. **Future updates**: Just edit JSON in Netlify editor OR upload new `projects.json` file

---

**🎉 That's it! No coding required.**


# 🎬 DEPLOYMENT CHECKLIST: Your Portfolio Launch Plan

## ✅ Phase 1: Testing (Today - 30 minutes)

- [ ] Read `SETUP_INSTRUCTIONS.md` (15 min)
- [ ] Create UIRoot GameObject with UIDocument + PortfolioController
- [ ] Create ProjectLoader GameObject with PortfolioProjectLoaderWeb
- [ ] Assign `MainPortfolioLayout.uxml` to UIRoot
- [ ] Assign `ProjectCard.uxml` template to ProjectLoader
- [ ] Press Play in editor
- [ ] Click "Enter Portfolio" button
- [ ] Verify 3 sample projects appear with text
- [ ] ✅ Close Play mode

---

## ✅ Phase 2: Customization (Tomorrow - 30 minutes)

- [ ] Open `Assets/StreamingAssets/projects.json` in text editor
- [ ] Edit Project 1 (Hand Cricket):
  - [ ] Title: Your project name
  - [ ] Tag: Your tech stack
  - [ ] Description: What you built (100-150 words)
  - [ ] screenshotUrl: Link to image (HTTPS)
- [ ] Edit Project 2 (Boss Fight) with your data
- [ ] Edit Project 3 (Infinite Runner) with your data
- [ ] Verify JSON syntax (paste at jsonlint.com)
- [ ] Save file
- [ ] Test in Play mode (Ctrl+P)
- [ ] Click "Enter Portfolio"
- [ ] Verify your custom projects appear
- [ ] ✅ Close Play mode

---

## ✅ Phase 3: Build (Next Day - 15 minutes)

**Build WebGL for Web**:
- [ ] File → Build Settings
- [ ] Select **WebGL** platform (left side panel)
- [ ] Click **Player Settings** (right panel)
  - [ ] Set Product Name: "Shantanu's Portfolio"
  - [ ] Enable Compression Format: Brotli
- [ ] Click **Build** button
- [ ] Create/Select `Build/` folder
- [ ] Wait for build to complete (~5-10 minutes)
- [ ] ✅ Build successful (zero errors)

**Verify Build Output**:
- [ ] Open `Build/` folder
- [ ] Verify these files exist:
  - [ ] `index.html` (main file)
  - [ ] `Build/` subfolder (with .wasm, .js files)
  - [ ] `TemplateData/` subfolder (with styles)
- [ ] ✅ All files present

---

## ✅ Phase 4: Deploy to Netlify (Next Day - 10 minutes)

**Create Netlify Account** (if needed):
- [ ] Go to [netlify.app](https://app.netlify.app)
- [ ] Click "Sign up"
- [ ] Use GitHub/Google account (or email)
- [ ] Verify email
- [ ] ✅ Account created

**Deploy Your Portfolio**:
- [ ] In Netlify Dashboard, click "**Add new site**"
- [ ] Click "**Deploy manually**"
- [ ] **Drag entire `Build/` folder** into drop zone
- [ ] Wait for deployment (usually 30 seconds)
- [ ] ✅ You'll get a live URL like: `https://your-portfolio-abc123.netlify.app`

**Verify Deployment**:
- [ ] Open your Netlify URL in browser
- [ ] Page loads (should take 2-3 seconds)
- [ ] Header shows your name
- [ ] "Enter Portfolio" button works
- [ ] Click button → projects appear
- [ ] All 3 projects visible with images
- [ ] Buttons are clickable
- [ ] ✅ Everything works!

---

## ✅ Phase 5: Launch (Next Day - 5 minutes)

**Test on Mobile**:
- [ ] Open portfolio URL on your phone
- [ ] Verify layout adapts to mobile width
- [ ] Projects layout vertically (not side-by-side)
- [ ] All text readable
- [ ] Buttons tappable
- [ ] ✅ Mobile experience perfect

**Share Your Portfolio**:
- [ ] Copy your Netlify URL (e.g., `https://your-portfolio-123.netlify.app`)
- [ ] Share on:
  - [ ] LinkedIn (add to profile URL)
  - [ ] Twitter/X
  - [ ] Discord
  - [ ] Email signature
  - [ ] GitHub bio
  - [ ] Resume
- [ ] ✅ Portfolio live and shared

**Optional: Custom Domain**:
- [ ] In Netlify: Site Settings → Domain Management
- [ ] Click "Add custom domain"
- [ ] Enter your domain (e.g., `shantanu.dev`)
- [ ] Verify domain ownership
- [ ] ✅ Your custom domain live

---

## 📊 What's Happening Behind the Scenes

```
User visits your Netlify URL
		 ↓
Netlify edge server sends index.html (40KB)
		 ↓
Browser loads WebGL app (2.5MB)
		 ↓
PortfolioProjectLoaderWeb.cs starts
		 ↓
Loads projects.json (20KB) from StreamingAssets
		 ↓
Cards render with your project titles/descriptions (INSTANT)
		 ↓
Images load lazily as you scroll (NO BLOCKING)
		 ↓
Portfolio ready in ~2 seconds total
```

---

## 🎯 Success Criteria

✅ **Pass if:**
- Scene setup works (projects visible in editor)
- Custom projects appear when you edit JSON
- WebGL builds without errors
- Netlify deployment succeeds
- Portfolio loads in <3 seconds
- Projects visible on both desktop and mobile
- Images load without blocking UI

❌ **Check these if something fails:**
- UIDocument assigned to correct UXML file?
- PortfolioController script on UIRoot?
- ProjectCardTemplate assigned in ProjectLoader?
- projects.json valid JSON syntax?
- Image URLs are HTTPS and accessible?
- WebGL build includes StreamingAssets folder?

---

## 💾 File Locations (Quick Reference)

```
To change project data:
└─ Assets/StreamingAssets/projects.json

To change colors/styling:
├─ Assets/UI/ProjectCard.uss
└─ Assets/UI/MainPortfolioLayout.uss

To change text (hardcoded):
├─ Assets/UI/ProjectCard.uxml
└─ Assets/UI/MainPortfolioLayout.uxml

To change behavior/logic:
├─ Assets/Scripts/PortfolioController.cs
└─ Assets/Scripts/PortfolioProjectLoaderWeb.cs
```

---

## 📞 If Something Goes Wrong

| Problem | First Check | Second Check |
|---------|------------|-------------|
| **Nothing visible** | Is UIDocument assigned? | Are Scripts attached? |
| **Projects don't load** | JSON file exists? | JSON syntax valid? |
| **Build fails** | Any red errors? | Are all references assigned? |
| **Images blank** | URLs are HTTPS? | Images actually exist? |
| **Netlify shows error** | Build folder correct? | index.html present? |

→ **Still stuck?** Check the documentation files in `Assets/` folder

---

## 🎉 Celebration Moments

🎯 **Checkpoint 1**: Projects appear in editor Play mode  
🎯 **Checkpoint 2**: Your custom projects show in Play mode  
🎯 **Checkpoint 3**: WebGL builds successfully  
🎯 **Checkpoint 4**: Netlify deploys your portfolio  
🎯 **Checkpoint 5**: Portfolio live on the internet! 🚀  

---

## 📈 Metrics You Should See

| Metric | Target | Pass/Fail |
|--------|--------|-----------|
| Initial load time | <3s | ✅/❌ |
| Time to interactive | <2s | ✅/❌ |
| Bundle size | <3MB | ✅/❌ |
| Mobile responsiveness | Works on all widths | ✅/❌ |
| Image loading | Never blocks UI | ✅/❌ |
| Netlify CDN speed | <500ms global | ✅/❌ |

---

## 🚀 Post-Launch Actions

**After deploying:**
- [ ] Test from 3G/4G network (mobile data)
- [ ] Share URL with a friend (get feedback)
- [ ] Check browser console for warnings
- [ ] Monitor Netlify deployment logs
- [ ] Test on different browsers (Chrome, Firefox, Safari)
- [ ] Check on tablets (iPad, Android)

**Future updates (no rebuild needed):**
- [ ] Edit projects.json to add new projects
- [ ] Change images by updating URLs
- [ ] Update descriptions
- [ ] Reorganize project order
- [ ] All changes live in <1 minute!

---

## ✨ You Made It! 

**Your portfolio is now:**
- ✅ Deployed globally on Netlify
- ✅ Optimized for speed (2s load)
- ✅ Responsive on all screens
- ✅ Showcasing your engineering skills
- ✅ Ready for recruiters/collaborators
- ✅ Updateable without rebuilding

**Share it everywhere!** 🎉

```
Your Portfolio URL:
https://___your-netlify-url___.netlify.app

💪 Congrats on shipping your portfolio!
```


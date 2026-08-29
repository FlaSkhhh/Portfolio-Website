# Portfolio UI Toolkit Setup Instructions

## Scene Configuration Steps

### 1. Create the UI Root GameObject
1. In your main scene, create a new empty GameObject: **Right-click Hierarchy → 3D Object → Empty**
2. Name it: `UIRoot`
3. **Assign ImmediateMode Layout or Scale with Screen Size** (optional, for safety):
   - Add component → Canvas Scaler (if using Canvas)
   - Or just use UIDocument directly on a raw GameObject

### 2. Add UIDocument Component
1. Select `UIRoot` GameObject
2. **Add Component → UI Document** (UnityEngine.UIElements)
3. Drag `Assets/UI/MainPortfolioLayout.uxml` into the **UI Document** field (under "UI")
4. Under **Panel Settings**, create a new or assign existing PanelSettings:
   - If creating new: **Create → PanelSettings asset** in Assets/UI folder
   - Set **Scale Mode** to "Scale With Screen Size" (recommended for WebGL)

### 3. Attach PortfolioController Script
1. Still on `UIRoot`, **Add Component → Script**
2. Select `PortfolioController.cs`
3. The script will auto-query the UIDocument on this same GameObject

### 4. Create ProjectLoader GameObject
1. **Right-click Hierarchy → Create Empty**
2. Name it: `ProjectLoader`
3. **Add Component → UI Document**
   - Assign same PanelSettings as UIRoot
   - This is needed for PortfolioProjectLoader to access ProjectScrollView
4. **Add Component → Script → PortfolioProjectLoader.cs**
5. In the Inspector:
   - **UI Document** field: Drag `UIRoot` GameObject (the one with MainPortfolioLayout)
   - **Project Card Template** field: Drag `Assets/UI/ProjectCard.uxml`

### 5. Create PanelSettings Asset (if not done)
1. **Right-click in Assets/UI folder → Create → UI Toolkit → Panel Settings**
2. Name: `PanelSettings`
3. In Inspector:
   - **Scale Mode**: "Scale With Screen Size"
   - **Reference Resolution**: 1920 × 1080
   - **Dynamic Atlas Settings**: Enable (for memory efficiency)

### 6. Verify in Play Mode
- Press Play
- **Open Console (Window → General → Console)** — check for any errors
- You should see:
  - A dark header bar at top with your name
  - Centered "Systems & Multiplayer Game Programmer" headline
  - "Enter Portfolio" button

## Troubleshooting

| Issue | Solution |
|-------|----------|
| **Black screen / Nothing visible** | Check PanelSettings is assigned; verify MainPortfolioLayout.uxml path is correct |
| **Console errors about missing elements** | Ensure UIDocument is on same GameObject as PortfolioController, and file paths in UXML are correct |
| **Button doesn't work** | Verify PortfolioController script is enabled; check Console for query warnings |
| **No project cards appear** | Verify ProjectCardTemplate.uxml is assigned in PortfolioProjectLoader inspector |


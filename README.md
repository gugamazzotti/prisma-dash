# Prisma Dash 🔷🔶

An infinite 3D runner for **WebGL**, written entirely in code for **Unity 2022.3 LTS** (Built-in Render Pipeline). The reference frame is **portrait ~9:16** (1080×1920). On a phone the canvas fills the screen; in a desktop browser the page keeps a centered portrait frame, with dark bars on the sides.

Play at **https://gugamazzotti.github.io/prisma-dash/** (after setting up the secrets and GitHub Pages, below).

## Concept
Your sphere has a **polarity**: **Cyan** or **Magenta**. Colored portals span the whole track and only let through a sphere of the **same color** — you cannot dodge them, only *flip* to the right color. At the same time, dark walls block lanes and have to be dodged, and orbs of your color feed the **combo** (orbs of the wrong color break the combo). At high difficulty, portals and walls arrive together: a double decision in fractions of a second.

- Speed rises from 14 → 42 u/s over ~100 s; the gap between obstacles shrinks.
- Points come from distance plus bonuses (portal 10×combo, orb 5×combo). The high score is saved in `PlayerPrefs` (it persists in the browser).
- Dodging a wall at the last instant counts as a **graze**: the sphere has already left the wall's lane, but it is still close. That adds 12 to 28 points (the bonus rises with speed) and sets off a gold spark, a short shake, and the text GRAZE. A dodge with room to spare scores nothing.
- Juice: camera shake from "trauma", particles, audio synthesized at runtime, slow motion on game over.

## Controls
The game is built for phones in portrait, and the on-screen text is in English. The tutorial explains touch only:

| Action | Touch |
|---|---|
| Change lane | Tap the left or right third, or swipe sideways |
| Swap color | Tap the center, or swipe up or down |
| Start/Restart | Tap |

The keyboard still works, and it does not appear in the tutorial: `A`/`D` or `←`/`→` change lane; `Space`, `W`, `↑`, or right-click swap the color; `Space` or `Enter` start or restart. Left-click uses the same zones as touch. The gesture that starts the run does not change lane or color.

## How to open
1. Unity Hub → **Add project from disk** → select this folder (use Unity **2022.3.x**; the project is on **2022.3.20f1**. If the version differs, accept the upgrade).
2. Open `Assets/Scenes/Main.unity` and press **Play**.
   In fact, any scene works: `Bootstrap.cs` uses `[RuntimeInitializeOnLoadMethod]` and builds the camera, light, track, player, and UI on its own.
3. In the Game view, choose a high resolution, for example **1080×1920**. Any aspect ratio works: the camera widens the FOV when the screen is narrower than 9:16, so the three lanes stay in frame.

## Build WebGL
- Install the **WebGL Build Support** module in Unity Hub.
- Menu **Prisma Dash → Build WebGL** (Gzip with *decompression fallback*, portrait template, and default canvas **540×960**; written to `Build/WebGL`).
- Command line:
  ```
  Unity -batchmode -quit -projectPath . -executeMethod PrismaDash.EditorTools.WebGLBuilder.Build -buildTarget WebGL
  ```
- Local test: `npx http-server Build/WebGL` (do not open `index.html` directly via `file://`).

The template `Assets/WebGLTemplates/PrismaPortrait` draws the canvas at 9:16. If the window is wider than it is tall (desktop), the game sits in a portrait frame in the center. If the window is already portrait (phone), the canvas fills the whole screen.

## Publish
- **itch.io**: zip the *contents* of `Build/WebGL` (index.html at the root of the zip), create an "HTML" project, upload the zip, and check "This file will be played in the browser". Suggested size **540×960**; enable fullscreen and "mobile friendly".
- **GitHub Pages**: the workflow in `.github/workflows/webgl.yml` publishes on its own. See the section below.

## GitHub Actions and GitHub Pages

Each push to the `main` branch (or a manual run of the **WebGL** workflow) builds the project with [GameCI](https://game.ci/docs/github/builder) (`game-ci/unity-builder@v4`, Unity version read from `ProjectSettings/ProjectVersion.txt`) and publishes the build with `actions/deploy-pages`. The `Library` folder is cached by Actions for later builds.

Player compression is **Gzip with decompression fallback**. GitHub Pages does not send the `Content-Encoding` header; the fallback makes the loader decompress the files in the browser. Without that, the game does not start on this host.

### The three secrets

In the repository: **Settings → Secrets and variables → Actions → New repository secret**.

| Secret | What to paste |
|---|---|
| `UNITY_LICENSE` | the entire contents of the `.ulf` file |
| `UNITY_EMAIL` | the email of the Unity account used for the license |
| `UNITY_PASSWORD` | that account's password |

GameCI does not store this data: it exists only in the job, to activate the editor. The [activation documentation](https://game.ci/docs/github/activation) recommends a password made of letters and numbers only (no special characters).

#### Personal license (`.ulf` file)

Follow GameCI's **Personal** license flow — do not use the Pro/Plus serial flow:

1. Install Unity Hub and sign in with the account CI will use.
2. Open **Unity Hub → Preferences (or Settings) → Licenses → Add** and choose **Get a free personal license**. Even if a license is already in the list, use **Add**: that step is what generates the `.ulf` file.
3. Find the file (the license is not tied to a machine or an editor version; you can activate on Windows and use the same file on the Ubuntu runner):
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
   - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
4. Open the `.ulf` in a text editor and paste the **entire** contents into the `UNITY_LICENSE` secret (the full XML, not the file path).
5. Also create `UNITY_EMAIL` and `UNITY_PASSWORD` with the credentials for that same account.

If the Hub does not show the option that generates the `.ulf`, GameCI documents alternatives on the same page and in the project's Discord.

### Enable Pages

1. **Settings → Pages → Build and deployment → Source: GitHub Actions**.
2. Confirm that the three secrets exist.
3. Push to `main`, or go to **Actions → WebGL → Run workflow**.
4. When the **Deploy GitHub Pages** job turns green, the game opens at:

https://gugamazzotti.github.io/prisma-dash/

The first build downloads the editor image and can take a long time. Later builds reuse the `Library` cache.

## Files
```
Assets/
  Scenes/Main.unity              minimal scene (only a marker GameObject; linear fog on)
  Resources/Materials/Lit.mat    opaque material (shader PrismaDash/Color)
  Resources/Materials/Unlit.mat  particles (shader PrismaDash/Particle)
  Shaders/PrismaColor.shader     color + directional light + glow, few variants
  Shaders/PrismaParticle.shader  vertex color, one variant
  WebGLTemplates/PrismaPortrait  player page: portrait frame and letterbox on desktop
  Scripts/Runtime/
    Bootstrap.cs                 automatic entry via RuntimeInitializeOnLoadMethod
    GameManager.cs               states (menu/play/end), input, spawn, difficulty, lane collision
    Palette.cs                   colors; clones the Resources materials (no Shader.Find)
    CameraRig.cs                 chase camera; FOV follows the aspect ratio
    Fx.cs                        a single ParticleSystem with manual Emit
    ProceduralAudio.cs           synthesized sound effects (AudioClip.Create)
    Hud.cs                       1080×1920 Canvas + legacy Text, created in code
  Scripts/Editor/WebGLBuilder.cs WebGL build menu (also the Actions buildMethod)
Packages/manifest.json           uGUI only, plus the required modules
ProjectSettings/                 Unity 2022.3.20f1, WebGL canvas 540×960, Gzip + fallback
.github/workflows/webgl.yml      WebGL build on push to main and deploy to GitHub Pages
```

## Technical notes
- Materials: the empty scene caused WebGL to strip Standard, and putting Standard in Always Included Shaders compiled **339968 variants** (the build ran past 6 h and was cancelled). Meshes use the project shader `PrismaDash/Color` (color, directional light, and glow, fog variants only) and particles use `PrismaDash/Particle`. Both are referenced by materials in `Resources`, so they are included in the player without `Shader.Find` and without the variant explosion.
- Colors: player and portals are cyan `(0.10, 0.95, 1)` or magenta `(1, 0.15, 0.70)`; walls and floor are dark; orbs use a higher glow; particles use the vertex color.
- No TextMeshPro (this avoids importing TMP Essentials); embedded font `LegacyRuntime.ttf` (the built-in font in Unity 2022.2+).
- Collision is logical (lane + z crossing), with no physics — deterministic and cheap on WebGL.
- On WebGL, audio starts only after the first user interaction (a browser rule) — the start click takes care of that.
- The camera sits higher and farther back than in a landscape framing, and the vertical FOV is calculated so the width of the arches fits in 9:16. On even taller screens the FOV opens up; on wider screens the portrait FOV stays, so the player does not drop out of the bottom of the frame.
- The Unity Personal license requires the Unity splash screen. It stays enabled.

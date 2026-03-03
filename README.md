# HollowDescent

## Getting the project from GitHub to your machine and opening in Unity

### 1. Clone the repo to your computer

**Option A – GitHub Desktop**

1. Open [GitHub Desktop](https://desktop.github.com/).
2. **File → Clone repository**.
3. Pick the **URL** tab, paste the repo URL (e.g. `https://github.com/YOUR_USERNAME/HollowDescent.git`), choose a local path, then **Clone**.

**Option B – Command line**

```bash
git clone https://github.com/YOUR_USERNAME/HollowDescent.git
cd HollowDescent
```

Replace `YOUR_USERNAME/HollowDescent` with the actual GitHub repo URL if it’s different.

### 2. Open the project in Unity

1. Install **Unity 2021 or newer** (same major version as the project if you want to avoid upgrade prompts).
2. Open the **Unity Hub**.
3. Click **Add** (or **Open**).
4. Browse to the folder you cloned (the one that contains the `Assets` folder and the `.unity` scene file).
5. Select that folder and add/open the project.
6. Wait for Unity to import assets and compile scripts.

### 3. Run the game

1. In the Project window, open your scene (e.g. the scene that uses the graybox level).
2. Ensure the scene has an empty GameObject with **GameBootstrap** attached (or create one and add the `GameBootstrap` script).
3. Press **Play**.

---

**Note:** If the repo only contains scripts (no full Unity project), create a new Unity project first, then copy the `Assets` (or `Assets/Scripts`) folder from the clone into your new project’s `Assets` folder, then open a scene and add the bootstrap as above.

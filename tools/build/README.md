# Local test builds

## Browser

Requires the Web Build Support module for the Unity version in `ProjectSettings/ProjectVersion.txt`.

In Unity, choose **Pack It Perfect → Local Web → Switch to Web**, wait for imports, then **Build Browser Test**. Output is `builds/web/local/`. Alternatively, with the Editor closed:

```bash
/path/to/Unity -batchmode -nographics -projectPath "$PWD" -buildTarget WebGL \
  -executeMethod PackItPerfect.EditorTools.LocalWebBuild.Build -quit \
  -logFile "$PWD/builds/web/build.log"
```

Run from the repository root:

```bash
python3 tools/build/serve-web.py
```

Open **http://localhost:8080**. Keep that terminal running; Ctrl+C stops it. Use `--port 8081` if the default port is occupied. This server binds only to the local computer and serves the build directory, not the repository.

The page fits the game into a portrait canvas. Drag with the mouse, tap/click to rotate, and use the in-game controls. Files are uncompressed for simple local serving. Open through HTTP rather than double-clicking `index.html`.

Browser progress/preferences are stored in IndexedDB with Unity's `autoSyncPersistentDataPath` enabled. They are separate from Android and Editor saves. Keep the same browser, host and port to revisit the same save. Private browsing or clearing site data can discard it.

This target uses WebGL 2 and the existing gameplay scene. It is for local desktop-browser testing; Android haptics and phone performance are not reproduced. Building temporarily applies the local Web template, memory and graphics settings, then restores them. The active Unity target remains Web until switched back in Build Profiles.

## Internal Android

`release.json` supplies the version, Android versionCode and output directory for **Pack It Perfect → Release → Build Internal Android APK**. Internal APKs use the existing debug signing certificate, ARM64 and IL2CPP. The builder preserves existing release outputs and records a manifest and SHA-256.

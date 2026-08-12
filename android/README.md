# AI.Game Farm — Android

WebView APK wrapping the HD-2D farm section (Brown Dust 2 art direction).

## Install

1. Enable **Install unknown apps** for your file manager / browser on the device.
2. Sideload `releases/AI.Game-Farm-v0.2.0-debug.apk` (also attached to the GitHub Release).

Package id: `com.aigame.farm`  
Orientation: landscape  
Min SDK: 24

## Rebuild

Requires JDK 17 + the local SDK under `AI.Game/android-sdk` (or set `sdk.dir` in `android/local.properties`).

```bat
set JAVA_HOME=C:\Program Files\Microsoft\jdk-17.0.20.8-hotspot
set JAVA_TOOL_OPTIONS=-Djavax.net.ssl.trustStoreType=Windows-ROOT
cd android
..\tools\gradle-8.5\bin\gradle.bat assembleDebug
```

Output: `android/app/build/outputs/apk/debug/app-debug.apk`

## Art direction

Reference stills live in `sections/farm/art-refs/`:

- `bd2-victory-grid.png` — tactical grid, spotlight, moody forest frame
- `bd2-hd2d-field.png` — tilt-shift, god rays, painted terrain
- `bd2-sunset-cliff.png` — golden-hour grade, rock/tree density

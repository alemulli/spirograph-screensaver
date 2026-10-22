# Spirograph Screensaver

A Windows screensaver that draws spirographs. Each round places 3–5 patterns at random spots on a near-black screen, holds them for a few seconds, fades everything out and starts again.

Every pattern gets random settings:

- **Gradient:** Rainbow, Galaxy, Chameleon, Sunset, Ocean or Fire
- **Colour repeats:** 1–6
- **Wheel:** rolling inside or outside the ring
- **Ring and wheel teeth, pen hole, rotation, line thickness and opacity**
- **Size and position on screen**

Each monitor runs its own patterns. The screensaver needs Windows 10 or 11 and nothing else; it runs on the .NET Framework 4.8 that ships with Windows.

## Install

1. Open the [latest release](https://github.com/alemulli/spirograph-screensaver/releases/latest) and download **Spirograph.scr**.
2. Move it somewhere it can stay, such as `C:\Users\<you>\Screensavers\`. Windows remembers this path, so the screensaver stops working if the file is moved or deleted later.
3. Right-click the file, choose **Properties**, tick **Unblock** at the bottom of the General tab and click **OK**. If there's no Unblock box, skip this step.
4. Right-click the file and choose **Install**. On Windows 11, choose **Show more options** first to find it.
5. The Screen Saver Settings window opens with Spirograph selected. Set the **Wait** time and click **OK**.

To try it straight away, click **Preview** in that window. Move the mouse or press a key to stop it.

### "Windows protected your PC"

The file isn't code-signed, so SmartScreen may warn you the first time it runs. Click **More info**, then **Run anyway**. Unblocking the file in step 3 usually prevents this.

### Uninstall

Open Screen Saver Settings (search Start for "screen saver"), pick another screensaver or **(None)**, then delete `Spirograph.scr`.

## Build

GitHub Actions builds the screensaver on every push to `main`. Pushing a tag such as `v1.0.1` also creates a release with `Spirograph.scr` attached.

To build locally you need the .NET SDK (8 or later). It also builds on macOS and Linux:

```
dotnet build -c Release
```

The output is `bin/Release/net48/Spirograph.scr`.

## Code

- `src/Program.cs` reads the flags Windows passes in: `/s` to run, `/p` for the small preview, `/c` for settings.
- `src/ScreensaverForm.cs` is the window: one per monitor, closes on mouse movement, clicks or key presses.
- `src/Scene.cs` places, draws and fades the patterns.
- `src/Spiro.cs` holds the spirograph maths, the gradients and the random settings.

## [Download latest release](https://github.com/guzenco/msovideo_srgb/releases/latest/download/release.zip)

# About
This tool uses an ICC profile with MHC2 tag to convert colors before sending them to a wide gamut monitor to effectively clamp it to sRGB (alternatively: Display P3, Adobe RGB or BT.2020), based on the chromaticities provided in its EDID.

ICC profiles are also supported and can be used in two different ways. By default, only the primary coordinates from the ICC profile will be used in place of the values reported in the EDID. This is useful if you want to use a profile created by someone else without taking their gamma/grayscale balance data into account, as that can vary a lot between units. If you enable the `Calibrate gamma to` checkbox, a full LUT-Matrix-LUT calibration will be applied. This is similar to the hardware calibration supported by some monitors and can be used to achieve great color and grayscale accuracy on well-behaved displays.

The tool generates and applies an ICC profile that contains idealized display characteristics under MHC2 tag corrections. In other words, this profile describes the display as ideally matching the selected color space and gamma, allowing software that supports ICC profiles to deliver accurate and consistent results.

For HDR mode, ICC profiles can be used to calibrate display gamma to PQ ST2084 (hard clip) and to provide measured static metadata. For more details, see the section "Notes for HDR calibration" below.

# System requirements

The tool relies on MHC2, and therefore its requirements align with those of the [Windows hardware display color calibration pipeline](https://learn.microsoft.com/en-us/windows/win32/wcs/display-calibration-mhc#system-requirements).

Windows 10, version 2004:
* AMD:
  * AMD RX 500 400 Series, or later
  * AMD Ryzen processors with Radeon Graphics
* Intel:
  * Integrated: Intel 10th Gen GPU (Ice Lake), or later
  * Discrete: Intel DG1, or later
* NVIDIA GTX 10xx, or later (Pascal+)
* Qualcomm 8CX Gen 3, or later; 7C Gen 3, or later

# Usage
Extract `release.zip` somewhere under your user directory and run `msovideo_srgb.exe`. To enable/disable the sRGB clamp for a monitor, simply toggle the "Clamped" checkbox. For using ICC profiles and configuring dithering, click the "Advanced" button.

Generally, the clamp should persist through reboots and updates, but it can break sometimes. You can choose to leave the application running minimized in the background to have it automatically reapply the clamp. 

# Notes for use with EDID data
* If the checkbox for a monitor is locked, it means that the EDID is reporting the sRGB primaries as the monitor's primaries, so the monitor is either natively sRGB or uses an sRGB emulation mode by default. If this is not the case, complain to the manufacturer about the EDID being wrong, and try to find an ICC profile for your monitor to use instead of the EDID data.

* The reported white point is not taken into account when calculating the color space conversion matrix. Instead, the monitor is always assumed to be calibrated to D65 white.

# Notes for use with ICC profiles

* For the gamma options to work properly, the profile must report the display's black point accurately. DisplayCAL's default settings, e.g. with the sRGB preset, work fine.
* To achieve optimal results, consider creating a custom testchart in DisplayCAL with a high number of neutral (grayscale) patches, such as 256. With that, a grayscale calibration (setting "Tone curve" to anything other than "As measured") should be unnecessary unless your display lacks RGB gain controls, but can lead to better accuracy on some poorly behaved displays. The number of colored patches should not matter much. Additionally, configuring DisplayCAL to generate a "Curves + matrix" profile with "Black point compensation" disabled should also result in a lower average error than using an XYZ LUT profile. This advice is based on what worked well for a handful of users, so if you have anything else to add, please let me know.
* Only the VCGT (if present), TRC and PCS matrix parts of an ICC profile are used. If present, the A2B1 data is used to calculate (hopefully) higher quality TRC and PCS matrix values.
* The sRGB gamma option provides the best ΔE.

# Notes for HDR calibration

HDR calibration requier separate profile measured in HDR mode.
Recomended settings:
* In calibration tab: Everything set to "As measured".
* In profile tab:
  * Profile type: Curves + matrix ("Black point compensation" disable)
  * Profile quality: Hight
  * Testchart: Small testchart for matrix profiles (with a high number of neutral (grayscale) patches, such as 256)

Measure targets must be displayed in HDR (not SDR via HDR) format. To achive this, you can use [dogegen](https://github.com/ledoge/dogegen):
 1. Select Display to Resolve in DisplayCAL.
 2. Start calibration.
 3. Run dodgen with:
```
dogegen.exe "resolve_hdr 127.0.0.1"
```
 4. Measure targets displayed in the dogegen window.

Tool settings:
* Peak target: Limits display luminance in HDR mode. Limits display luminance in HDR mode. It will be ignored if higher than the display profile luminance.
* BPC threshold: Prevents black crush by linearly scaling `[profile black, threshold]` to `[0, threshold]`.

In HDR mode, the target color space is treated as Native.

# Known issues

* The color space transform does not get applied properly to the mouse cursor, which results in it having wrong gamma and colors. This should be hardly noticeable with the default Windows cursor. Workaround: Force software rendering of the cursor, e.g. using [SoftCursor](https://www.monitortests.com/forum/Thread-SoftCursor).
 

# notepadpp-wakatime

Metrics, insights, and time tracking automatically generated from your programming activity.

## Installation

### Via Plugins Admin (only if you have installed)

1. Inside Notepad++ open the Plugins Admin (`Plugins` → `Plugins Admin`).

2. Search for `WakaTime` and then check the box in the list of plugins.

3. Click the `Install` button.

4. Restart Notepad++.

5. Enter your [api key](https://wakatime.com/settings#apikey), then press `enter`.

6. Use Notepad++ like you normally do and your time will be tracked for you automatically.

7. Visit <https://wakatime.com> to see your logged time.

### Manually

1. Create a directory named **WakaTime** into Notepad++ plugins folder
    * Usually `C:\Program Files (x86)\Notepad++\plugins\` (for 32 bit) and `C:\Program Files\Notepad++\plugins\` (for 64 bit)

2. Download a [release version][1] and copy the included **WakaTime.dll** to the *plugins/WakaTime* sub-folder at your Notepad++ installation directory.

### Compatibility

This plugin requires at least

* Notepad++ 32-bit or 64-bit
* Windows
* .Net Framework 4.8 or above

It's supported on

* Notepad++ 7.6.3 and above

## Screen Shots

![Project Overview](https://wakatime.com/static/img/ScreenShots/ScreenShot-2014-10-29.png)

## Troubleshooting

WakaTime for Notepad++ logs to `%appdata%\WakaTime\notepadpp-wakatime.log`.

Turn on debug mode (click the WakaTime icon in Notepad++) then check your log file.

WakaTime Core logs to `%homepath%\.wakatime\wakatime.log`. Check there after looking in your Notepad++ WakaTime log file.

### Can't read WakaTime config file

Make sure you `%homepath%\.wakatime.cfg` is UTF-8 encoded without BOM.

More help can be found at <https://github.com/wakatime/wakatime-cli/blob/develop/TROUBLESHOOTING.md>.

  [1]: https://github.com/wakatime/notepadpp-wakatime/releases

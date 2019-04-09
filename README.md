notepadpp-wakatime
=====================

Metrics, insights, and time tracking automatically generated from your programming activity.


## Installation

### Via Plugins Admin (only if you have installed)

1. Inside Notepad++ open the Plugins Admin (`Plugins` → `Plugins Admin`).

2. Search for `WakaTime` and then check the box in the list of plugins.

3. Click the `Install` button.

4. Restart Notepad++.

3. Enter your [api key](https://wakatime.com/settings#apikey), then press `enter`.

4. Use Notepad++ like you normally do and your time will be tracked for you automatically.

5. Visit https://wakatime.com to see your logged time.


### Manually

1. Create a directory named **WakaTime** into Notepad++ plugins folder
    * Usually `C:\Program Files (x86)\Notepad++\plugins\` (for 32 bit) and `C:\Program Files\Notepad++\plugins\` (for 64 bit)

2. Download a [release version][1] and copy the included **WakaTime.dll** to the *plugins/WakaTime* sub-folder at your Notepad++ installation directory.


### Compatibility
This plugin requires at least
* Notepad++ 32-bit or 64-bit
* Windows
* .NET Framework 4.5 or above

It has been tested under the following conditions
* Notepad++ 7.5.9 64-bit
* Notepad++ 7.6.6 32-bit and 64-bit
* Windows 10 Professional (64-bit)


## Screen Shots

![Project Overview](https://wakatime.com/static/img/ScreenShots/ScreenShot-2014-10-29.png)


## Troubleshooting

WakaTime for Notepad++ logs to `C:\Users\<user>\AppData\Roaming\Notepad++\plugins\config\WakaTime.log`.

Turn on debug mode (click the WakaTime icon in Notepad++) then check your log file.

WakaTime Core logs to `C:\Users\<user>\.wakatime.log`. Check there after looking in your Notepad++ WakaTime.log file.

More help can be found at https://github.com/wakatime/wakatime#troubleshooting.

  [1]: https://github.com/wakatime/notepadpp-wakatime/releases

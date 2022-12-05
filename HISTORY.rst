
History
-------

5.0.1 (2022-12-05)
++++++++++++++++++

- Fix error "Access violation" for x86 arch.


5.0.0 (2022-12-04)
++++++++++++++++++

- Fix error "This ANSI Plugin is not compatible with your Unicode Notepadd++".
  `#46 <https://github.com/wakatime/notepadpp-wakatime/issues/46>`_
- Update to .net frmework 4.8.
- Add lines in file and current line number.
- Update `PluginInfrastructure` to latest version.
- Remove dependency do nuget library.


4.3.0 (2022-06-05)
++++++++++++++++++

- Prevent running timer continuously without user events.
  `#48 <https://github.com/wakatime/notepadpp-wakatime/issues/48>`_
- Fix appveyor builds.
  `#47 <https://github.com/wakatime/notepadpp-wakatime/issues/47>`_


4.2.3 (2021-11-13)
++++++++++++++++++

- Fix wakatime-cli location after GitHub repo renamed.
  `#44 <https://github.com/wakatime/notepadpp-wakatime/issues/44>`_


4.2.2 (2020-10-12)
++++++++++++++++++

- Check for SSL flag before download cli.


4.2.1 (2020-10-11)
++++++++++++++++++

- Check for SSL flag before download python.


4.2.0 (2020-10-10)
++++++++++++++++++

- Update NPP plugin platform with latest version.


4.1.0 (2019-04-10)
++++++++++++++++++

- Fix for x86/x64 platform.
- Update NPP plugin platform to v0.94.00.
- Only one config file instance needed for whole extension.


4.0.2 (2016-08-16)
++++++++++++++++++

- Correctly detect when user is typing in a file.


4.0.1 (2016-08-16)
++++++++++++++++++

- Fix bug causing notification handler to be called more often than needed.


4.0.0 (2016-08-15)
++++++++++++++++++

- Fix bug preventing editor events from being handled.
- Reload settings from config file every time settings menu is displayed.
- Write all exceptions to C:\Users\%USER%\AppData\Roaming\WakaTime\WakaTime.log file.


3.0.2 (2016-08-15)
++++++++++++++++++

- Fix runtime compatibility issues.


3.0.1 (2016-06-13)
++++++++++++++++++

- Refactor plugin using code from wakatime/visualstudio-wakatime.
- Prevent deleting wakatime-core when IDE started while offline.
- Queue heartbeats before sending to wakatime-cli to prevent from forking too many python processes.
- Improved dependency management and moved dependencies to AppDataWakaTime folder.
- Millisecond precision for heartbeat timestamps.


3.0.0 (2015-12-17)
++++++++++++++++++

- use embedded python instead of installing
- remove prompt before installing Python because using embeddable Python now
- prevent locking inside background thread
- better looking obfuscated api key


2.0.3 (2015-08-27)
++++++++++++++++++

- fix slow startup issue


2.0.2 (2015-08-25)
++++++++++++++++++

- upgrade wakatime cli to v4.1.1
- send hostname in X-Machine-Name header
- catch exceptions from pygments.modeline.get_filetype_from_buffer
- upgrade requests package to v2.7.0
- handle non-ASCII characters in import path on Windows, won't fix for Python2
- upgrade argparse to v1.3.0
- move language translations to api server
- move extension rules to api server
- detect correct header file language based on presence of .cpp or .c files named the same as the .h file


2.0.1 (2015-08-04)
++++++++++++++++++

- upgrade wakatime cli to v4.1.0
- correct priority for project detection
- fix offline logging
- limit language detection to known file extensions, unless file contents has a vim modeline
- guess language using multiple methods, then use most accurate guess
- use entity and type for new heartbeats api resource schema
- correctly log message from py.warnings module


2.0.0 (2015-05-30)
++++++++++++++++++

- clean up external process execution
- always use latest version of wakatime cli dependency
- cache python binary location for better performance
- move log file to AppData folder


1.1.0 (2015-05-26)
++++++++++++++++++

- add icon to menu
- follow .Net coding conventions and code cleanup


1.0.0 (2015-04-29)
++++++++++++++++++

- Birth


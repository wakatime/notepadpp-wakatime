
History
-------


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


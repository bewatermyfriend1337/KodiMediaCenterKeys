# KodiMediaCenterKeys
A windows application for the use with a Logitech G15 that runs in windowed mode that will listen for Kodi opening/closing and will enable the use of the media keys while Kodi is in the background. Also displays the currently playing song information on the display. 

<b>Note: </b> If using a media center, running the application on startup is recommended. The application will automatically stop and resume when closing/opening Kodi. Only required to run once in order for the application to listen to Kodi events.

<h2>Setup</h2>

In order to use the application, you must configure Kodi to allow control via HTTP (see: http://kodi.wiki/view/Webserver). Take note of the port used.

1. Inside the KodiMediaKeys.cs file, replace "xxx.xxx.x.x:xxxx" on line 38 (listed as "string rpcLocation") with your IP address and port of the computer running Kodi. Do not remove the "http://" or the "/jsonrpc".
2. Do the same with LogitechDisplay.cs (should be on line 63, under the same name).


~<a href="http://www.alaychak.com/">Andrew Laychak</a>

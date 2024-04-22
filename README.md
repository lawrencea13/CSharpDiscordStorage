# CSharpDiscordStorage
Based on a video from a youtuber about stealing storage from discord using JS, I decided to do the same thing in C#
Features/Differences:
A file system! Yay, you can create folders and organize the files. This is all handled client side but the information is stored on the server as well.
GUI along with the file system, it's ugly and garbage but functional.
My favorite difference is everything is handled on the client machine. It sucks you can't access the files from any device, but this is a direct upload to discord, no middleman
Otherwise, just a fun little experiment

## Current Status
4/21/2024 - I wrote the entirety of this project today so far. It's 1 am on the following day. Currently it's not feature complete and it's a fairly buggy mess

## Known Issues
The Meta data that identifies "files" and their associated locations can sometimes get messed up.(unknown if issue persists as of latest change, needs more testing)
Currently it's hard to get the GUI to update with what the server currently has because of cross thread operations
Meta data is capped at 25mb(this is the data that identifies all the chunks of the files as well as where they are in the custom file system) due to it being a single file on the server
If Meta data ever gets to a point where it would be larger than 25 MB, the next limit would be having it all loaded into memory. It's not a lot of memory but it's a theoretical issue if you have thousands of files.

## Planned Features
Haven't done much testing, but would like to see about having multiple files uploaded at once, they process on the client side pretty quick so uploading should be pretty easy
Of course the missing features in the file system, including adding folders, refreshing, navigation, etc.
Clientside encryption


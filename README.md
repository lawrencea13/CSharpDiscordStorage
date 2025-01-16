# CSharpDiscordStorage
Based on a video from a youtuber about stealing storage from discord using JS, I decided to do the same thing in C#
Features/Differences:
A file system! Yay, you can create folders and organize the files. This is all handled client side but the information is stored on the server as well.
GUI along with the file system, it's ugly and garbage but functional.
My favorite difference is everything is handled on the client machine, meaning theoretically you could have multiple clients on different machines that could access uploaded files.
Otherwise, just a fun little experiment

## Current Status
4/21/2024 - I wrote the entirety of this project today so far. It's 1 am on the following day. Currently it's not feature complete and it's a fairly buggy mess

4/22/2024 - About 2 more hours and I was able to write the majority of the file system into it. Pretty much everything but deleting folders works.

4/23/2024 - File system pretty much complete. It's ugly af but it works. Metadata issues also resolved.

## Known Issues
GUI doesn't update for certain operations like deleting a folder.
Meta data is capped at 25mb(this is the data that identifies all the chunks of the files as well as where they are in the custom file system) due to it being a single file on the server. It could be treated similarly to large file uploads and be broken up and recreated at the client level, but this is more of a learning project so there's no need.


## Usage
You can technically use this right now, but I wouldn't recommend it; it's not set up for end-users, nor is it safe to trust Discord to NOT delete the files.

## Step 1: Create a Discord bot account by following [these](https://discordpy.readthedocs.io/en/stable/discord.html) steps. The bot only needs the ability to read and send messages.
## Step 2: Create, if applicable, and invite the bot to the discord server using the invite link you can generate right on the bot installation page.
## Step 3: Create a channel for storing uploaded files and for metadata(can be 2 separate channels or the same channel.)
## Step 4: Add the relevant channel ID to the StorageChannel and MetaDataChannel variables located in Bot.cs, as well as the token to the token variable, also located in Bot.cs
## Step 5: Run the bot once, it will send a message in the channel designated as the MetaDataChannel. Copy the ID from this message and in the same place the Channel IDs were put, replace the StaticMetaDataID with the ID from this message.



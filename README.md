# JworkzNeosFixFrickenSync

[![A image of the video demonstrating a fixed sync loop](https://img.youtube.com/vi/sKnmyzFoUWY/0.jpg)
](https://youtube.com/watch?v=sKnmyzFoUWY)

A [NeosModLoader](https://github.com/neos-modding-group/NeosModLoader) mod for [Neos VR](https://neos.com/) that fixes the ["stuck sync"](https://wiki.neosvr.com/Stuck_Sync) issue and is provided as a solution for [#3915](https://github.com/Neos-Metaverse/NeosPublic/issues/3915) on NeosPublic. Additionally, this mod contains an automatic retry sync feature that will retry a failed sync based on the specific failure state.

## Installation
1. Install [NeosModLoader](https://github.com/zkxs/NeosModLoader).
2. Place [JworkzNeosFixFrickenSync.dll](https://github.com/stiefeljackal/JworkzNeosFixFrickenSync/releases/latest/download/JworkzNeosFixFrickenSync.dll) into your `nml_mods` folder. This folder should be located in the same folder as Neos. For a default installation, the typical location is `C:\Program Files (x86)\Steam\steamapps\common\NeosVR\nml_mods`. You can create it if it's missing, or if you launch the game once with NeosModLoader installed it will create the folder for you.
3. Start the game. If you want to verify that the mod is working, you can check your Neos logs.

## Config Options

|Config Option|Default        |Description|
|-------------|---------------|-----------|
|`enabled`    |`true`         |Enables the mod.|
|`retryCount` |`3`            |The number of times to retry failed sync actions.|
|`retryDelay` |`TimeSpan.Zero`|The delay between attempts to retry failed sync actions.|

# The True Explanation of Stuck Sync

Although the NeosVR wiki makes it sound like a "stuck sync" is a normal thing in Neos, this is not the case. The unfortunate truth behind the "Stuck Sync" issue is due to a bug in the sync loop where
unhandled exceptions (aka errors) are not being handled within the sync loop, causing the loop to stop. The most common exception thrown is one where the upload task is being marked as complete twice when a sync
failure occurs due to the Neos Cloud returning an HTTP error code. Because tasks cannot be marked complete more than once, an exception is thrown within the loop that causes the loop to stop.

Syncing errors should occur normally, but they should not be halting other sync tasks. If this bug was addressed, then the follow status would be shown:

![Sync Error! Check log for details](https://user-images.githubusercontent.com/20023996/245466573-25c00107-ddaf-43e5-99f2-133ea9e8b2ba.png)

## NeosPublic Issues This Mod Resolves for "Stuck Sync"

* [Syncing getting stuck due to comos dB error #3729](https://github.com/Neos-Metaverse/NeosPublic/issues/3729)
* ["BadRequest" Stuck Sync #3750](https://github.com/Neos-Metaverse/NeosPublic/issues/3750)
* [Endlessly syncing records despite sync error occurring in log #3211](https://github.com/Neos-Metaverse/NeosPublic/issues/3211)
* [Headless stops syncing world saves after receiving an "Upload failed: 520" when syncing the world asset #3668](https://github.com/Neos-Metaverse/NeosPublic/issues/3668)
* ["Failed sync" (with hot-fix) #3313](https://github.com/Neos-Metaverse/NeosPublic/issues/3313)

# Thank You

* This mod is dedicated to the people of [Creator Jam (CJ)](https://discord.gg/WFmySeSGPh) 🍞. This mod was made out of the frustration and sadness experienced during CJ 211 (Traffic Jam) due to the "stuck sync" problem.
* [Jax](https://github.com/Nihlus) for contributing  to the retry sync feature and providing additional code clean-up while I was away on vacation during that time.
* Rucio for testing this mod out on the Creator Jam headless server.
* Lux and friend for assisting me in debugging the error log entry not showing in the logs.

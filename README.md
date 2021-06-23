# NSYS SocialGuard for Yume-Chan

[SocialGuard](https://socialguard.net/) is a centralized trustlist aimed at keeping servers safe from known intruders (such as Raiders, Trolls, Chasers).  
Entries are community-driven, and aggregated with all servers using SocialGuard.



# Getting Started

## 1. Setup

**There are currently two ways of setting up SocialGuard YC :**

### 1. Setup Yume-Chan Bot on your Server
As Yume-Chan has a plugin ecosystem that includes SocialGuard natively, this means that using the official Yume-Chan Discord Bot will always grant access to SocialGuard, given setup.

You can add Yume-Chan to a server by [clicking here](https://discordapp.com/oauth2/authorize?client_id=583412458560159776&scope=bot&permissions=8).  
Be sure to grant Administrator (`0x8`) permission or relevant role to Yume-Chan, to allow Ban functions to be used.

### 2. Setup your own Yume-Chan instance and plugin
Yume-Chan is also open-source, meaning you can also self-host your own instance of Yume-Chan.  
Once having built Yume-Chan [from the official repo](https://github.com/YumeChan-DT/YumeChan), you can then build the SocialGuard YC plugin from this repository and insert the output .DLL file into the ``./plugins`` folder inside your Yume-Chan instance folder.

**From here, SocialGuard can already be used manually with the ``==sg lookup`` command. But let's take things further...**


## 2. Setting Channels

### Joinlog
**To enable automatic readouts of SocialGuard entries whenever a user joins your server, the Joinlog channel must be set.**  
Assuming the Joinlog channel is named (for example) `#joinlog`, you can set the Joinlog using ``==sg config joinlog #joinlog``, where you can quote directly your Joinlog channel.

Beware of the viewing permissions that you allow for your joinlog. Setting it to publicly viewable by your server members may be good for transparency and security demonstration for your members, but can also raise awareness of your usage of SocialGuard, which can lead to ban-evasion attempts by the most dedicated griefers.  
We advise setting your Joinlog to staff-only, for maximal security.

### Banlog
**By default, all SocialGuard bans are announced in the Banlog. However, if it has not been set before, the Banlog falls back to the same channel as set for the Joinlog.**  
Using the same pattern as the aforementioned Joinlog, you can set your Banlog using ``==sg config banlog #banlog``.  

Once again, beware of the viewing permissions allowed for your Banlog. A publicly viewable Banlog can allow transparency into the internal server politics, but can also raise ethics & privacy concerns for all banned users.  
If you didn't set your Joinlog as staff-only, we definitely recommend you set the Banlog as staff-only, unless this conflicts with your server policies.


## 3. Registering your server as SocialGuard Trustlist Emitter

**If your server is of good reputation, and trusted, SocialGuard Maintainers can allow your server to operate as a Trustlist Emitter, which in turn allows you to insert entries into the SocialGuard Trustlist.**  
For this however, several steps have to be followed prior to validation.

### Registering Credentials
SocialGuard uses a centralized API, protected with username/password login, granting JWT Tokens in turn. This Authentication mechanism is handled automatically by the SocialGuard YC plugin, and is totally transparent to normal usage once setup. However, this in turns means you entrust the SocialGuard YC plugin with storage of your username & password.  
The official Yume-Chan Discord Bot follows several encryption points, with passwords encrypted at rest inside the Database using an AES subset encryption, and the hosting server using military-grade encryption known as ACID FDE (also used within the French Military) for disk encryption. All encryption keys are handled by [NSYS Nova](mailto:nova+socialguard@nodsoft.net), and are inaccessible by our developers. 
Finally, credentials are transmitted securely to the SocialGuard central API using HTTPS Transport Encryption, standard to all Web communications.

Registering credentials with the SocialGuard API can be done like so (example with placeholder values) :  
``==sg config register jdoe-server john.doe@acme.com Alpha123*``  

Password requirements are strict, needing at least 8 characters, including Lowercase, Uppercase, Numbers and Symbols.

Once done, you can then login using this command (using above values) :  
``==sg config login jdoe-server Alpha123*``  

**BEWARE! Use these commands in a staff-only channel, as your password will be briefly visible by all users.**  

### Registering Emitter Profile
Once credentials set up, you can then set your Emitter information.  
This is used to identify the author of Trustlist Entries, and ensuring good custory of SocialGuard trusted credentials.

You can set up your emitter profile automatically to match your server info, using this command :  
``==sg emitter set server``  
**This command should be repeated whenever a definitive server name change occurs.**

### Requesting Verification
With your Emitter profile setup, you can now contact our Maintainers on the [Official NSYS Discord server](https://discord.gg/xV5nFmkbyS) to request Verification for your profile. This will in turn allow you to enter users into the Trustlist using ``==sg add`` & ``sg ban`` commands.  
In case of any issues, please contact SocialGuard Maintainer **Sakura Isayeki** on Discord.


# Commands
**This documentation assumes Yume-Chan's command prefix as ``==`` (used on official Yume-Chan Discord Bot). You can change it in ``./Config/coreproperties.json`` in a self-hosted instance.**

## Basics

*Starting from Release 2.0.0 onwards, all commands are listed under `==help socialguard`. This help will always provide runtime-exact syntax documentation, and is considered first source of truth for syntax checks.  
With this being said, this README file will provide additional context to commands, which may be missing in the help command.*

``==sg lookup <User/ID>`` Returns trustlist result for given user


**All further commands require you to be authenticated on SocialGuard (see below), and your emitter information to be manually verified by SocialGuard maintainers.**

``==sg add <User/ID> <Level> <Reason>`` Adds a Trustlist entry for given user, at given Level (N+x) :
 - **1** : Suspicious
 - **2** : Untrusted
 - **3** : Blacklisted/Dangerous

``==sg ban <User/ID> <Level> <Reason>`` Bans a user, and adds a Trustlist entry. (*This command combines ``==sg add`` with a local ban*)


## Config 
**All config commands follow a simple structure : ``==sg config <option> [values]``. Commands return current values (where applicable) when values are omitted.**

| Option & Values | Description |
| --- | --- |
| ``register <username> <email> <password>`` | Registers your SocialGuard credentials. __Use this in a private/staff-only channel, as your password will be briefly visible.__ |
| ``login <username> <password>`` | Sets your SocialGuard credentials for use with your server. __Use this in a private/staff-only channel, as your password will be briefly visible.__ | 
| ``joinlog <channel>``| Sets the Join-log channel, where all new users will have Trustlist results displayed. |
| ``banlog <channel>`` | Sets the Ban-log channel, where all SocialGuard-based bans and sync warnings are displayed (from either Autoban, Syncing from another server, or ``==sg ban`` command). |
| ``autoban <on/off>`` | Will automatically ban all Blacklisted (N+3) users on join and on sync, when activated. |
| ``joinlog-suppress <on/off>`` | Will suppress any new clean (N+0) user record from being sent to the joinlog when activated. |


## Emitter
**All Emitter commands require prior authentication with SocialGuard credentials. Be sure to set them up in config before using these commands.**

``==sg emitter info`` Displays Emitter information for your current credentials.  
``==sg emitter set-server`` Automatically sets Emitter info for your server. *(Please refresh your Emitter info after a server name change, using this command.)*  

## Debug
**In case issues arise, these debug commands can prove useful :**

``==sg debug clear-token`` Clears your Auth Token (*Useful in case your token contains an outdated access, or login fails to work.*)  
``==sg debug force-login`` Forces a new login cycle for your specified credentials.
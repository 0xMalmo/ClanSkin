# Features
* Allows players to save a skin profile
* Automatically skins any attire according to saved profile

# Permissions
* `clanskin.save` -- allows player to use the `/clanskin` command (to save a profile)

# Configuration
```json
{
  "Exclude Items (these items will not be affected by this plugin)": [
    "hat.beenie"
  ]
}
```

* **Exclude Items** - takes a list of shortnames. the items in this list will not be saved to profile.

# Chat Commands
* `/clanskin help` -- shows manual for chat commands
* `/clanskin edit` -- player/team/clan enters edit mode (attire will not be automatically skined on when put on)
* `/clanskin save` -- saves the skin id's that the player is wearing to the player/team/clan profile
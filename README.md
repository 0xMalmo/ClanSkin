## Features

* Allows players to save a skin profile
* Automatically skins any attire according to saved profile

## Permissions

* `clanskin.save` -- Allows player to use the `/clanskin` command (to save a profile)

## Configuration

```json
{
  "Exclude Items (these items will not be affected by this plugin)": [
    "hat.beenie"
  ]
}
```

* **Exclude Items** - Takes a list of shortnames. the items in this list will not be saved to profile.

## Chat Commands

* `/clanskin help` -- Show manual for chat commands
* `/clanskin edit` -- Enter the edit mode for player/team/clan (attire will not be automatically skined on when put on)
* `/clanskin save` -- Save the skin id's that the player is wearing to the player/team/clan profile
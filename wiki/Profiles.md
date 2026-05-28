# Profiles

Profiles save device configurations as JSON files so you can quickly switch between setups.

## Features

- **Create** new profiles
- **Edit** profile name, description, suffix, delay
- **Duplicate** existing profiles
- **Delete** profiles
- **Import** profiles from JSON files
- **Export** profiles to JSON files

## Profile Format

```json
{
  "id": "barcode-default",
  "name": "Default Barcode Scanner",
  "deviceType": "Scanner",
  "description": "Keyboard wedge barcode scanner simulator",
  "defaultOutputChannel": "Keyboard",
  "defaultPrefix": "",
  "defaultSuffix": "Enter",
  "delayPerCharacterMs": 10,
  "buttons": [],
  "settings": {
    "mode": "Barcode"
  }
}
```

## Storage Location

Profiles are stored in:
```
%LOCALAPPDATA%/DeviceMocker/Profiles/
```

## Sharing Profiles

1. Export a profile to JSON from the Profiles page
2. Share the JSON file with your team
3. They import it from the Profiles page

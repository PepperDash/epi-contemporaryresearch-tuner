# epi-contemporaryresearch-tuner
plugin for entire range of contemporary research devices - only two-way feedback is for power.  Feel free to fork me!

## Requirements

- PepperDash Essentials **v3** (net8.0, 4-series processors only). This plugin builds against `PepperDash.Essentials.Core` `3.0.0-rc.20`.
- For 3-series / Essentials 1.x, use the `main` branch releases.

## Building

Dependencies are restored automatically by the SDK-style project via NuGet:

`dotnet build src/epi-contemporaryresearch-tuner.csproj`

The `.cplz` and `.nupkg` are written to `output/`.

### Config Example

```javascript
{
    "key": "Tuner01",
    "uid": 1920,
    "name": "CR Tuner",
    "type": "contemporaryresearch",
    "group": "Tuners",
    "properties": 
    {
        "unitId": "1",
        "presetsList": "presets-list-name",  // optional: name of a presets list to load
        "control": 
        {
            "controlPortDevKey": "processor",
            "comParams": 
            {
                "dataBits": 8,
                "softwareHandshake": "None",
                "baudRate": 115200,
                "parity": "None",
                "stopBits": 1,
                "hardwareHandshake": "None",
                "protocol": "RS232"
            },
            "method": "com",
            "controlPortNumber": 5
        }
    }
}
```


### Bridge Joins (added in v2)

The join map extends the standard Essentials `SetTopBoxControllerJoinMap`. In addition to its joins, the plugin now drives:

| Join | Type | Direction | Description |
|------|------|-----------|-------------|
| 1 | Digital | To SIMPL | Power on feedback |
| 4 | Digital | To SIMPL | Has D-pad (true) |
| 11 | Digital | To SIMPL | Has numeric keypad (true) |
| 32 | Digital | To SIMPL | Has DVR (false) |
| 49 | Digital | To SIMPL | Is online |
| 50 | Digital | To SIMPL | Has presets (true when `presetsList` is configured) |
| 2 | Serial | To SIMPL | Make |
| 3 | Serial | To SIMPL | Model |
| 4 | Serial | To SIMPL | IP address |
| 5 | Serial | To SIMPL | MAC address |
| 6 | Serial | To SIMPL | Firmware version |

Joins are offset by the bridge `joinStart`. DVR, color, transport and accessory-key joins have no effect; the tuner does not support them.

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


## Testing

### Unit tests

`dotnet test tests` runs the xunit suite (net10 host). Command building and response parsing live in `src/Shared`, which has no Crestron dependencies and is compiled into the test project. `TunerSimulator` models the tuner from the manual's response formats (`docs/`), and the tests feed its output back through the parsers. A conformance test also checks that the device still implements the interfaces Mobile Control uses to pick its messengers (set-top box, power, volume, presets, device info, communication monitor).

Not covered by the manual, so assumed: the `ID` response format (`<1ID <model> <firmware>`).

### Mock device for bench testing

Change a device's `type` to `contemporaryresearchmock` to run the real plugin against a simulated tuner, with no hardware. Messengers, bridge joins and presets behave as on a real unit. No `control` block is needed.

```javascript
{
    "key": "Tuner01",
    "uid": 1920,
    "name": "CR Tuner",
    "type": "contemporaryresearchmock",
    "properties":
    {
        "unitId": "1",
        "presetsList": "presets-list-name",
        "channels": ["2-1", "4-1", "5-1", "7-1"],  // optional: channels up/down steps through
        "simulateOffline": false                    // optional: start unreachable
    }
}
```

The simulator answers like the tuner (about 125 ms status delay, volume ramps while VU/VD is held). It is registered as `<key>-comms` and can be driven from the console with `devjson`:

```
devjson {"deviceKey":"Tuner01-comms","methodName":"SetOffline","params":[true]}
devjson {"deviceKey":"Tuner01-comms","methodName":"SimulateLine","params":["<1TM002U0A0002x0"]}
```

Do not use the mock type in a production configuration.

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

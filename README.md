# epi-stb-contemporaryresearch
plugin for entire range of contemporary research devices - only two-way feedback is for power.  Feel free to fork me!

## Requirements

- PepperDash Essentials **v3** (net8.0, 4-series processors only). This plugin builds against `PepperDash.Essentials.Core` `3.0.0-rc.20`.
- For 3-series / Essentials 1.x, use the `main` branch releases.

## Building

Dependencies are restored automatically by the SDK-style project via NuGet:

`dotnet build src/epi-stb-contemporaryresearch.csproj`

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

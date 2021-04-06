# epi-stb-contemporaryresearch
plugin for entire range of contemporary research devices - only two-way feedback is for power.  Feel free to fork me!

## Cloning Instructions

After forking this repository into your own GitHub space, you can create a new repository using this one as the template.  Then you must install the necessary dependencies as indicated below.

## Dependencies

The [Essentials](https://github.com/PepperDash/Essentials) libraries are required. They referenced via nuget. You must have nuget.exe installed and in the `PATH` environment variable to use the following command. Nuget.exe is available at [nuget.org](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe).

### Installing Dependencies

To install dependencies once nuget.exe is installed, run the following command from the root directory of your repository:
`nuget install .\packages.config -OutputDirectory .\packages -excludeVersion`.
To verify that the packages installed correctly, open the plugin solution in your repo and make sure that all references are found, then try and build it.

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

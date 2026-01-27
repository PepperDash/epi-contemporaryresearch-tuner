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
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 1.7.6
<!-- END Minimum Essentials Framework Versions -->
<!-- START Supported Types -->

<!-- END Supported Types -->
<!-- START Join Maps -->

<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- ISetTopBoxControls
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- EssentialsBridgeableDevice
- SetTopBoxControllerJoinMap
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void DvrList(bool pressRelease)
- public void LoadPresets(string filePath)
- public void Replay(bool pressRelease)
- public void ChannelDown(bool pressRelease)
- public void ChannelUp(bool pressRelease)
- public void Exit(bool pressRelease)
- public void Guide(bool pressRelease)
- public void Info(bool pressRelease)
- public void LastChannel(bool pressRelease)
- public void PowerOn(bool pressRelease)
- public void PowerOff(bool pressRelease)
- public void PowerToggle(bool pressRelease)
- public void Blue(bool pressRelease)
- public void Green(bool pressRelease)
- public void Red(bool pressRelease)
- public void Yellow(bool pressRelease)
- public void Down(bool pressRelease)
- public void Left(bool pressRelease)
- public void Menu(bool pressRelease)
- public void Right(bool pressRelease)
- public void Select(bool pressRelease)
- public void Up(bool pressRelease)
- public void Dash(bool pressRelease)
- public void KeypadEnter(bool pressRelease)
- public void Digit0(bool pressRelease)
- public void Digit1(bool pressRelease)
- public void Digit2(bool pressRelease)
- public void Digit3(bool pressRelease)
- public void Digit4(bool pressRelease)
- public void Digit5(bool pressRelease)
- public void Digit6(bool pressRelease)
- public void Digit7(bool pressRelease)
- public void Digit8(bool pressRelease)
- public void Digit9(bool pressRelease)
- public void KeypadAccessoryButton1(bool pressRelease)
- public void KeypadAccessoryButton2(bool pressRelease)
- public void ChapMinus(bool pressRelease)
- public void ChapPlus(bool pressRelease)
- public void FFwd(bool pressRelease)
- public void Pause(bool pressRelease)
- public void Play(bool pressRelease)
- public void Record(bool pressRelease)
- public void Rewind(bool pressRelease)
- public void Stop(bool pressRelease)
- public void Poll()
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->

<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->

<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->

<!-- END String Feedbacks -->

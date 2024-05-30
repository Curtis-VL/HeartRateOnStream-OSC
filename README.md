# HeartRateOnStream-OSC
HeartRateOnStream to VRChat OSC

## What is this?
This app allows you to get realtime heartrate data from a compatible Wear OS device (Such as a Pixel Watch) and send this data to VRChat via OSC.

## Requirements
- An Android device capable of installing [HeartRateOnStream](https://play.google.com/store/apps/details?id=com.pezcraft.myapplication).
- A Wear OS device with a heartrate monitor capable of installing [HeartRateOnStream](https://play.google.com/store/apps/details?id=com.pezcraft.myapplication)

## Avatar Parameters
There are 3 different parameters, designed to mimic the parameters used in [vrc-osc-miband-hrm](https://github.com/vard88508/vrc-osc-miband-hrm) so an avatar that works with that will already work with this.

- `Heartrate` sends float value `from -1 to 1` (0-255bpm) (Use this when you need to display bpm counter)
- `Heartrate2` sends float value `from 0 to 1` (0-255bpm) (Easier to control your animations but not enough precise over network. Do not use this to display bpm counter. Use cases: making actual sound of heartbeat, making animations which speed is equal to your bpm)
- `Heartrate3` sends int value `from 0 to 255` (0-255bpm) (Useful for those who wanna bind specific event to specific heart rate. Use case: changing your outfit on avatar to sport one when your bpm goes higher than 130)

## Usage
1. Install [HeartRateOnStream](https://play.google.com/store/apps/details?id=com.pezcraft.myapplication) for OBS on both your Wear OS device and Android device.
2. Download the latest release of HeartRateOnStream-OSC from [Releases](https://github.com/Curtis-VL/HeartRateOnStream-OSC/releases).
3. Extract all files to a folder and run HeartRateOnStream-OSC.exe.
4. In the HeartRateOnStream mobile app, edit the connection details to match what is shown by HeartRateOnStream-OSC.
5. Follow the instructions in HeartRateOnStream-OSC to complete the setup.
6. Wait patiently, it can take around 30s for heart rate data to start updating outside of the app. (This seems to be an issue with the app)
7. Profit! Hopefully! Make sure OSC is enabled in VRChat in the radial menu! (A world rejoin may be required after this!)

## Example Avatar
Please see the README of [vrc-osc-miband-hrm](https://github.com/vard88508/vrc-osc-miband-hrm) for an example avatar.

## Got any issues?
Feel free to message me on Discord: CurtisVL
Or, create an issue on here!

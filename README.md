# DMD Extensions

*Features for DMD owners\* that aren't officially supported*

Real DMDs for virtual pinball cabinets are pretty cool but they need to be
supported by the software running the games. This is an attempt to fill some
gaps. 

\* And even if you don't have a real DMD, there's awesome stuff too!

## Supported Displays

Currently supported displays:

- **PinDMD v3**, full RGB support
- **PinDMD v2**, 4 bit (16 shades) support
- **PinDMD v1**, 2 bit (4 shades) support
- **PIN2DMD** and **PIN2DMD XL**, full RGB support
- **Pixelcade**, full RGB support
- **Virtual DMD** on a computer monitor, renders nice dots and is useful for 
  debugging.
- **Alphanumeric Virtual**, a high-resolution virtual segmented display for 
  pre-DMD area games.
- **Network**, use dmdext to send and receive data through the network.
- You can also stream the DMD to a browser in your LAN, because, why not!

## Features

The primary goal of this project was to add support for real DMDs for games that
don't provide support out of the box. Supported games are:

- **Pinball FX2/3** through frame grabbing from screen 
- Farsight's **The Pinball Arcade** through grabbing the DMD texture from memory
- **Pro Pinball Timeshock** through their message queue
- **Visual PinMAME** through `DmdDevice.dll` 

The command line tool can also display image files on the DMD device and render
frames to bitmap files. Many features like this are described in the command 
line options below, so have a close look at those as well.

Since v1.7, DMD Extensions also support dynamic DMD coloring previously only
available for PIN2DMD displays (both side-channel and VNI).

Since v1.8, DMD Extensions supports high-resolution rendering of segmented 
alpha-numeric displays:

<image src="https://user-images.githubusercontent.com/70426/50459439-5f81bf00-096b-11e9-9f75-f70387f2c9cc.png" width="350"/>

Documentation how to enable and customize this feature can be found [here](https://github.com/freezy/dmd-extensions/tree/master/LibDmd/Output/Virtual/AlphaNumeric).

Since v1.8, DMD Extensions come with full network support. Documentation can be found [here](Console/Server)

## Install Instructions

[Download the installer](https://github.com/freezy/dmd-extensions/releases) and
run it. A few notes:

- The installer will overwrite any existing `dmddevice.dll` in your VPM folder
  if you choose to install it (existing INI files are untouched).
- If you're on Windows x64, you probably want to install both 32 and 64-bit
  versions. They can live happily next to each other.
- The reason for installing both versions is that your VPM is probably 32-bit,
  so you'll need the 32-bit `DmdDevice.dll` and if you're using `dmdext.exe`
  with the 64-bit version of Pro Pinball, it will only work with the 64-bit 
  version.
- However, if you don't have Pro Pinball, just 32-bit will do fine, and if don't
  use VPM then just 64-bit will do fine as well. And if you're on a 32-bit
  Windows, then just take the 32-bit version.
- The installer will add the install folder of `dmdext.exe` to the `PATH` 
  environment variable, but only if the platform is the same:
   - DMD Extensions (64-bit) on Windows x64 is added to `PATH`
   - DMD Extensions (32-bit) on Windows 32-bit is added to `PATH`
   - All other combinations aren't added to `PATH`
- The installer will set the `DMDDEVICE_CONFIG` environment variable to the
  location of your `DmdDevice.ini`. That means from wherever you launch 
  `DmdDevice.dll` (or run `dmdext.exe` with `--use-ini`), the same 
  `DmdDevice.ini` will be used.
- During installation, when a feature is deactivated, that means the installer
  couldn't find the host program. For VPM that means that you haven't registered
  `VPinMAME.dll`. For Pro Pinball it means it's not installed via Steam, and
  you'll need to select its installation folder manually.


### Test

1. Open a command prompt ([Windows]+[R], `cmd`, [enter])
2. Type `dmdext test -d auto` [enter]

You should see a test image on your DMD as well as on a virtual DMD.

### Pinball FX2

1. Enable cabinet options in Pinball FX2
2. Resize the DMD to:
   - Width: `520`
   - Height: `136`
3. Move the DMD to somewhere hidden like off-screen or behind the playfield
   (usually at `0`/`0`).
4. Open a command prompt ([Windows]+[R], `cmd`, [enter])
5. Type `dmdext mirror --source=pinballfx2 --no-virtual` [enter] (or 
   `pinballfx3` for FX3)
6. Start Pinball FX2/3

For further tweaking, see options below.

### Pinball FX3

The DMD from Pinball FX3 is pulled directly from the memory.

1. Open a command line prompt ([Windows], type `cmd`, [enter])
2. Type `dmdext mirror --source=pinballfx3 --no-virtual` [enter]
3. Start Pinball FX3 and play a game.

It doesn't matter whether Pinball FX3 is started before or after `dmdext`, and
it works with or without cabinet mode.

Note that while the current memory grabber code should also work for future 
Pinball FX3 versions, we obviously can't guarantee it. If a new version breaks
`dmdext`, you should still be able to fall back to the legacy screen grabber
that is used by Pinball FX2 by using the `--fx3-legacy` flag.

### The Pinball Arcade

For TPA we're pulling frames off the DMD texture from TPA's memory. 

1. Open a command line prompt ([Windows], type `cmd`, [enter])
2. Type `dmdext mirror --source=pinballarcade --no-virtual` [enter]
3. Start the DX11 version of The Pinball Arcade.
4. Select and **start** a game (*only then* it starts mirroring, during attract
   mode it doesn't, even though TPA does display the DMD).

Currently re-running `dmdext` while TPA is running doesn't work and will be 
addressed soon.

Also note that currently only 128x32 DMD games are supported, that means no
EM games neither Frankenstein which has a different resolution.

If you happen switch tables often and the last DMD frame gets stuck on your DMD
when changing to an EM game, you can use the `--idle-after` option to clear the
screen.

### Pro Pinball Ultra

In version 1.2.1, Barnstorm Games added support for external hardware through
a message queue. To get it running, do the following:

1. Make sure you have `dmdext.exe` in your `PATH`
2. Copy `ProPinballSlave.bat` to the Pro Pinball installation folder 
   (usually at `%PROGRAMFILES(X86)%\Steam\SteamLibrary\steamapps\common\Pro Pinball Ultra`)
3. Add the following command line parameters when launching `ProPinball.exe`:
   `m3 dProPinballSlave`

Note that this currently works only with the 64-bit version of Pro Pinball. 32
bit binaries will be provided with a later release. If you want to change the
options (e.g. show the virtual DMD), edit `ProPinballSlave.bat` and adopt 
accordingly.

### Visual PinMAME

VPM introduced the possibility of delegating DMD rendering to an external DLL.
The main motivation is being able to ship a single build instead of having to
release multiple binaries for multiple DMDs. 

If you are a PC monitor user or have a RGB display (PinDMDv3 or PIN2DMD), the
advantage of dmdext is that Lucky1's coloring features are fully supported. You
can set this up as you would for a PIN2DMD:

1. Create an `altcolor` folder where VPM is installed (usually `Visual Pinball\VPinMAME`)
2. In that folder, create another one named after the ROM (e.g. `simpprty`)
3. Download the `.pal` and `.vni` file from your favorite virtual pinball
   site, put them into that folder and name them `pin2dmd.pal` and `pin2dmd.vni`
4. Run VPM setup, open the ROM's game options and enable *Use external DMD (dll)*
   as well as *Colorize DMD*.

## Build Instructions

1. Download and install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/)
2. The unmanaged exports library needs MS Build tools, which come with .NET 3.5. [Install Instructions](https://docs.microsoft.com/en-us/dotnet/framework/install/dotnet-35-windows-10)
4. *Optional:* If you want `DmdDevice.dll` copied to your VPM folder after build, point the `VPM_HOME` environment variable to your VPM installation folder.
5. Clone the repo: `git clone https://github.com/freezy/dmd-extensions.git`
3. Open the `.sln` file in Visual Studio and build the solution.

If you want to build the installer, you'll need the [WiX Toolset](http://wixtoolset.org/releases/)
and its [Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=RobMensching.WixToolsetVisualStudio2017Extension).


## Configuration

Since `DmdDevice.dll` is called by VPM, we can't pass any configuration 
parameters to it. Instead, we use `DmdDevice.ini` which must be located
in the same folder as `VPinMAME.dll` or at the path where the `DMDDEVICE_CONFIG`
environment variable is pointing to.

### Output Configuration

Since v1.8 you can also tell `dmdext.exe` to read the output configuration from
`DmdDevice.ini` instead of passing them as command line arguments. The options
are described below.

The output are described by block below.

| Command Line                   | DmdDevice.ini                                  | Description                                                                                                                                                                                                                                                                                                                                                                                                                                              |
|--------------------------------|------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `-r`, `--resize`               | [global]<br>resize                             | How to downscale SEGA 192x64 pixel games to smaller displays. Can have three values:<ul>  <li>`stretch` - Just fill the available space and ignore the aspect ratio</li>  <li>`fill` - Fill it up so the whole DMD is filled while keeping aspect ratio intact. Pixels will be cropped off.</li>  <li>`fit` - Scale it so the whole image fits on the DMD while keeping aspect ratio intact. There will be white space (uh, more like black space).</li> |
| `--flip-x`                     | [global]<br>fliphorizontally                   | Flips the image horizontally (left/right)                                                                                                                                                                                                                                                                                                                                                                                                                |
| `--flip-y`                     | [global]<br>flipvertically                     | Flips the image vertically (top/down)                                                                                                                                                                                                                                                                                                                                                                                                                    |
| *n/a*                          | [global]<br>colorize                           | Enable or disable frame-by-frame colorization (inactive in VPX bundle)                                                                                                                                                                                                                                                                                                                                                                                   |
| `-d virtual`<br>`--no-virtual` | [virtualdmd]<br>enabled                        | Shows a virtual DMD that renders on the computer screen somewhat nicely                                                                                                                                                                                                                                                                                                                                                                                  |
| `--virtual-stay-on-top`        | [virtualdmd]<br>stayontop                      | Virtual DMD stays on top of most other windows                                                                                                                                                                                                                                                                                                                                                                                                           |
| *n/a*                          | [virtualdmd]<br>ignorear                       | If true, allow free resizing of the virtual DMD, otherwise the aspect ratio is locked to the DMD's.                                                                                                                                                                                                                                                                                                                                                      |
| *n/a*                          | [virtualdmd]<br>useregistry                    | If true, read the virtual DMD position from VPM's registry.                                                                                                                                                                                                                                                                                                                                                                                              |
| `--virtual-position`           | [virtualdmd]<br>left<br>top<br>width<br>height | Position of the virtual DMD. Command line takes in all four in the shown order, where the last (height) is optional.                                                                                                                                                                                                                                                                                                                                     |
| `--virtual-dotsize`            | [virtualdmd]<br>dotsize                        | Scale dot size. Use `0.8` for previous default setting.                                                                                                                                                                                                                                                                                                                                                                                                  |
| `-d pindmdv1`                  | [pindmd1]<br>enabled                           | Enables the 2-bit pinDMD display.                                                                                                                                                                                                                                                                                                                                                                                                                        |
| `-d pindmdv2`                  | [pindmd2]<br>enabled                           | Enables the 4-bit pinDMD2 display.                                                                                                                                                                                                                                                                                                                                                                                                                       |
| `-d pindmdv3`                  | [pindmd3]<br>enabled                           | Enables the RGB24 pinDMDv3 display.                                                                                                                                                                                                                                                                                                                                                                                                                      |
| `--port`                       | [pindmd3]<br>port                              | COM port, e.g. `COM3`.                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| `-d pin2dmd`                   | [pin2dmd]<br>enabled                           | Enables the RGB24 PIN2DMD display.                                                                                                                                                                                                                                                                                                                                                                                                                       |
| *n/a*                          | [pin2dmd]<br>delay                             | Delay in milliseconds to wait after loading a palette.                                                                                                                                                                                                                                                                                                                                                                                                   |
| *n/a*                          | [video]<br>enabled                             | Enables creating an .avi video from the DMD frames.                                                                                                                                                                                                                                                                                                                                                                                                      |
| *n/a*                          | [video]<br>path                                | Path to folder or .avi file. If a folder is given, it will create a file named after the current game.                                                                                                                                                                                                                                                                                                                                                   |
| *n/a*                          | [browserstream]<br>enabled                     | Enables streaming the DMD in real time to your browser in your LAN.                                                                                                                                                                                                                                                                                                                                                                                      |
| *n/a*                          | [browserstream]<br>port                        | Port of the web server to listen on                                                                                                                                                                                                                                                                                                                                                                                                                      |
| `--pinup`                      | [pinup]<br>enable                              | Enables output to PinUP.                                                                                                                                                                                                                                                                                                                                                                                                                                 |

You can also override all options per game by using the game's name as section 
name and pre-fixing options with the name of the section (apart from `[global]`
options, which aren't prefixed). For example if you have a PinDMD2 and don't
want to use it for Baywatch which has a different resolution, you would create
a section like this:

```
[baywatch]
pindmd2 enabled = false
virtualdmd enabled = true
virtualdmd left = 2500
virtualdmd top = 800
virtualdmd width = 1024
virtualdmd height = 256
```

This is also useful if you just want to fit the virtual DMD onto different
backglasses which have slightly different positions. 

Also note that dmdext can retrieve the DMD's position from VPM's registry 
setting. That means you can use VPM to position the DMD and dmdext will take 
the same setting for each game. This behavior can be enabled using the 
`useregistry` option.

### Command Line Configuration

Input options only apply to dmdext.exe, because the DLL is called externally
from VPM where no configuration is necessary (apart from enabling it in VPM).

The following parameters are valid for all of dmdext's commands:

| Parameter              | Description                                                                                        | Default |
|------------------------|----------------------------------------------------------------------------------------------------|---------|
| `--use-ini`            | Use output configuration from `DmdDevice.ini`                                                      | *none*  |
| `-c, --color`          | Sets the color of a grayscale source that is rendered on an RGB destination.                       | ff3000  |
| `-q, --quit-when-done` | Exit the program when finished, e.g. when Pinball FX2 doesn't receive any frames anymore.          | false   |
| `--quit-after`         | Exit after n milliseconds. If set to -1, waits indefinitely or until source finishes when -q used. | -1      |
| `--no-clear`           | Don't clear screen when quitting.                                                                  | false   |
| `-o, --output-to-file` | If set, writes all frames as PNG bitmaps to the provided folder.                                   |         |


Note that all options of dmdext.exe are also available via command line:

```
dmdext --help
dmdext mirror --help
dmdext play --help
dmdext test --help
```

#### Mirror

The `mirror` command has the following additional parameters:

| Parameter           | Description                                                                                                                                                                     | Default    |
|---------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------|
| `-s, --source`      | Required. The source you want to retrieve DMD data from. One of: [ `pinballfx2`, `pinballfx3`, `pinballarcade`, `propinball`, `screen` ].                                       | *n/a*      |
| `-f, --fps`         | How many frames per second should be mirrored.                                                                                                                                  | 25         |
| `--idle-after`      | Wait for number of milliseconds until clearing the screen. Disable with 0.                                                                                                      | 0          |
| `--idle-play`       | Play this file while idleing instead of blank screen. Supported formats: JPG, PNG, GIF. Animated GIFs are supported.                                                            | *none*     |
| `--position`        | *screen* - Position and size of screen grabber source. Four values: `<Left> <Top> <Width> <Height>`.                                                                            | 0 0 128 32 |
| `--resize-to`       | *screen* - Resize captured screen to this size. Two values: <Width> <Height>.                                                                                                   | 128 32     |
| `--grid-spacing`    | *screen* - How much of the white space around the dot should be cut off (grid size is defined by --resize-to). 1 means same size as the dot, 0.5 half size, etc. 0 for disable. | 0          |
| `--propinball-args` | *propinball* - Arguments send from the Pro Pinball master process. Usually something like: `ndmd w0_0_0_0_w m392`. Will be set automatically when called through Pro Pinball.   |            |
| `--fx3-legacy`      | *pinballfx3* - If set, don't use the memory grabber but the legacy screen grabber, like Pinball FX2.                                                                            | false      |

#### Play

The `play` command has the following additional parameters:

| Parameter           | Description                                                                    | Default |
|---------------------|--------------------------------------------------------------------------------|---------|
| `-f, --file`        | Path to the file to play. Currently supported file types: PNG, JPG, BIN (raw). | *none*  |

## Compatibility

This application is based on .NET 4.5, which only runs on Windows 7 or later.
Probably on Vista too, but who cares. If you're still running WinXP then you
have my sincerest sympathy, but that's as far as I go. ;)

## Breaking Changes

### v1.8.0

- Data types in configuration files are now culture invariant. Meaning if you are running under a Windows UI Culture such as German 
  which normally represents decimals with commas instead of periods you will need to change your configuration file (such as 
  DMDDevice.ini) to use periods for decimal data. The most common example would be those users who are using `dotsize` setting
  in your DMDDevice.ini. This was done to standardize the format of numeric data accross different languages.
- Hardware DMDs aren't probed per default anymore, unless you specifically set the destination. For most people that means that 
  you'll need to provide the `-d` option when running `dmdext.exe`.
- If you are a PinDMD3 user and are using colored ROMs, you'll want to [upgrade your firmware](https://github.com/freezy/dmd-extensions/wiki/PinDMD3:-How-to-flash-a-new-firmware).


## Troubleshooting

### Flickering with PinDMDv3

Some users reported heavy flickering when running via PinballX. Seems that this
is linked to the desktop settings. This seems to help:

- Go to **My Computer**
- Click on **Performance Information and Tools** on the bottom left
- Click on **Adjust Visual Effects** at the top left.
- When the Performance Window pops up, click on **Adjust for Best Appearance**. 
  All the boxes should automatically get check marked.
- Click on **Apply**, and then OK to get out.

*Thanks to xman72!*

### Still flickering?

Try boosting how fast dmdext grabs the frames. For example, for Pinball FX2, try:

    dmdext mirror --source=pinballfx2 --no-virtual -q --fps 60

Default is 25, which seems too slow for some games.

*Thanks smoke007 for the tip!*

### DmdDevice.ini Ignored?

This project comes bundled in two versions: A library which is loaded by VPM
called `DmdDevice.dll`, and an executable, `dmdext.exe` for all the other
applications.

The executable ignores `DmdDevice.ini` because it's configured through command
line options.

### Slow rendering on certain ROMs with VPM?

Use VPM 3.1. If you can't wait there's a beta build [here](http://vpuniverse.com/forums/topic/3461-sambuild31-beta-thread/?page=2&tab=comments#comment-39309).

*Thanks to djrobx for the fix an all others at [#52](https://github.com/freezy/dmd-extensions/issues/52) for reporting.*

### Weird positioning or no DMD visible at all?

When you override *High DPI scaling* in the host app (e.g. `vpinballx.exe`), 
dmdext is put into a different coordinate system, so your `DmdDevice.ini`'s 
position settings are applied differently. This can lead to wrong positioning 
or complete off-screen rendering.

*Thanks to outhere for the tip!*

## Reporting Bugs

Make sure you include the application log. You can *usually* find it at the same 
place you copied your `DmdDevice.dll` or `dmdext.exe`. However, there are two
premises for the log to be created:

1. You need a log config file. If there's no log config file, no log is created.
   The log config file is called `DmdDevice.log.config` for `DmdDevice.dll` and
   `dmdext.log.config` for `dmdext.exe`. They must be at the same place as the DLL
   and EXE respectively.
2. The user running the host app must have write permissions to the log folder.
   Per default that's the current directory. If you have copied the DLL to the
   `Windows\SysWOW64` folder, you don't have write access. In this case you need
   to write the log elsewhere. You can do that by editing the log config file.
   The format of the log file is XML. To change the log file location, find this line:
   
       <target xsi:type="File" name="file" fileName="DmdDevice.log"

   And change the `fileName` attribute to somewhere you can write, for example:

       <target xsi:type="File" name="file" fileName="C:\Users\youruser\DmdDevice.log"

For problems with DmdDevice.dll, specially with coloring, set the log level to
`Trace` and reproduce the problem. You can do that by editing `DmdDevice.log.config`
and changing: 

```xml
<logger name="*" minlevel="Info" writeTo="file" />
```
to
```xml
<logger name="*" minlevel="Trace" writeTo="file" />
```

If you have a crash, please also include which OS/bitness you're using. If it's
about the DLL, let us know where you copied the DLL and which host application
you're using (VPM's `setup.exe` or`vpinball.exe`, also which version).

You can post bugs at [VPF](http://www.vpforums.org/index.php?showtopic=36915) or 
[VPU](http://vpuniverse.com/forums/forums/topic/2728-sam-build-with-modular-dmd-drivers-for-pindmd123-and-pin2dmd/),
but preferably [here](https://github.com/freezy/dmd-extensions/issues).

## Manual Installation

1. [Download the .zip archive](https://github.com/freezy/dmd-extensions/releases),
   copy its content to your hard drive, preferable in your `PATH`,
   and unblock the `.exe` file (right-click, properties, unblock).
2. Download and install the [Visual C++ Redistributables for Visual Studio 2017](https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads).
3. If you haven't already, download and install the [.NET Framework 4.5.2](https://www.microsoft.com/en-us/download/details.aspx?id=42643).

If you want to use DMD Extensions with VisualPinMAME:

1. Copy `DmdDevice.dll`, `DmdDevice.ini` and `DmdDevice.log.config` into your 
   VPM folder (usually at `Visual Pinball\VPinMAME`).
2. Run the VPM setup
3. Click on *Test*, select the game and click on *Game Options*.
4. Enable *Use External DMD* and optionally disable *Show DMD*.

You can also set this as the default by selecting *Default Options* in the VPM
setup, however note that games that you've already configured won't be affected.

### x64 vs x86

- For `dmdext.exe`, take the bitness of your OS. This is important for Pro 
  Pinball, where the bitness of the binary must be the same as dmdext's.
- For `dmddevice.dll` you probably want the 32-bit version unless you've set up
  VPM with `Setup64.exe` and you know what you're doing.

## Developer Setup

After cloning the repo, you'll need to restore the [DllExport](https://github.com/3F/DllExport)
dependency. There is a script that does it for you. Go into the cloned repo and run:

    DllExport -action Restore -sln-file DmdExtensions.sln

Then there is an issue with Fody third party addin that may give you an error 
ith the $(IntermediateOutputPath).

If this occurs, simply close Visual Studio and re-launch the DMD Extensions project
and the issue will go away.

[Reference](https://github.com/Fody/Fody/issues/629)

## Credits

- NoEx for the excellent TPA grabber code, as well as the Pinball FX3 memory grabber.
- Tom Speirs, Lucky1, CarnyPriest and Russdx for their help on the DMD code
- Lucky1 for instructions and details about the coloring feature
- DJRobX for helping with the dynamic coloring, better performance and much more.
- mjrgh for debugging and optimizing stuff.
- Adrian Page from Barnstorm for his help setting up the slave correctly.

<a title="IntelliJ IDEA" href="https://www.jetbrains.com/resharper/"><img src="https://raw.githubusercontent.com/freezy/dmd-extensions/master/resharper.svg?sanitize=true" alt="IntelliJ Resharper" width="250"></a>

Finally, thanks to JetBrains for their awesome tools and support of the Open Source Community!


## License

GPLv2, see [LICENSE](LICENSE).

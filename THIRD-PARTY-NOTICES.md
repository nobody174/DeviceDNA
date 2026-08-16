# Third-Party Notices

DeviceDNA is licensed All Rights Reserved (see `LICENSE`). It is built on top of the
following third-party open source components, each used unmodified under its own license.

## LibreHardwareMonitorLib

- Version: 0.9.6
- License: Mozilla Public License 2.0 (MPL-2.0)
- Project: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor
- Used unmodified as a NuGet package for live hardware sensor readings (temperature, load,
  clock speed, etc.). No source files from this library have been modified or forked.
- MPL-2.0 full text: https://www.mozilla.org/en-US/MPL/2.0/

## Microsoft.Data.Sqlite

- License: MIT
- Project: https://github.com/dotnet/efcore
- Used unmodified as a NuGet package for local, on-device scan history storage.

## Microsoft.Win32.Registry / System.Management

- License: MIT
- Project: https://github.com/dotnet/runtime
- Used unmodified as NuGet packages for Windows Registry and WMI access.

## PawnIO

- Version: 2.2.0
- License: GNU General Public License v2.0 (GPLv2), with an explicit additional exception (stated
  in PawnIO's own license text) permitting "independent modules that communicate with PawnIO
  solely through the device IO control interface" — i.e. an application that talks to the already-
  installed PawnIO driver, without statically linking PawnIO's own source, is not itself required
  to be GPL-licensed as a result.
- Project: https://github.com/namazso/PawnIO
- DeviceDNA's installer bundles and silently installs PawnIO's own official, Authenticode-signed
  installer (`PawnIO_setup.exe`, unmodified, downloaded directly from PawnIO's official GitHub
  release) as a deferred custom action. DeviceDNA does not vendor, modify, or link against any
  PawnIO source code — it only depends on the PawnIO driver being present, which
  LibreHardwareMonitorLib (above) uses internally for AMD SMU sensor access (CPU package
  temperature, live per-core clock speed). Without this driver present, LibreHardwareMonitorLib
  cannot read these specific sensors correctly on some AMD CPUs (confirmed on a Ryzen 7 5800X,
  2026-08-15) — see CHANGELOG.md for the full root-cause investigation.

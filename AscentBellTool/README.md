# Ascent Bell (AscentBellTool)

A small Windows tray app that tells the bell attender when to ring the school bell.
Standalone: no internet, API or database needed. Timings are stored on the PC.

## Build
```powershell
.\AscentBellTool\build.ps1
```
Builds `bin\Release\AscentBellTool.exe`. If **Inno Setup 6** is installed (free: https://jrsoftware.org/isdl.php)
it also builds the installer `Installer\Output\AscentBellSetup.exe` (~2–3 MB).
Bump the version in **both** `Program.cs` (`Version`) and `Installer\AscentBell.iss` (`AppVersion`).

## Install at the school
Run `AscentBellSetup.exe` → Next → Finish. Requires Windows 7 SP1+ and .NET Framework 4.8
(setup checks and links to the download). It starts with Windows and sits in the tray (bell icon).

## How it works
| When | What happens |
|---|---|
| N min before a bell (default 2) | Small amber heads-up, bottom-right, with a chime. Does not steal keyboard focus. |
| At bell time | Full-screen red **RING THE BELL** alert; bell sound loops; unmutes the PC and raises volume to ≥ 70%. |
| Not confirmed after 60 s | Volume to 100%, "STILL WAITING". Only a mouse click on **I HAVE RUNG THE BELL** closes it (keys, Esc, Alt+F4 are ignored). |
| PC was asleep / app started late | A bell up to 2 min late still alerts; older ones show as *missed* in the tray menu. |

Tray menu: today's bells (done / missed; click one to open its alert manually), test alert,
pause for today, Settings, log folder, exit. Settings, Pause and Exit can be protected by an admin password.

Other behaviour:
- **Schedules**: several (Regular, Saturday, Exam day…), each with its weekdays; a weekday may belong to only one active schedule.
- **Holidays**: a date list. Days not ticked in any schedule (e.g. Sunday) have no bells.
- **Keeps the PC awake** from 30 min before the first bell to 10 min after the last one.
- **Clock check**: when online, compares the PC clock with internet time hourly. If it is ≥ 1 min off, alerts use the corrected time and a warning balloon asks for the clock to be fixed.
- **Export / Import** (Settings → General) copies timings to another PC.

## Files (on the school PC)
- `C:\ProgramData\AscentBell\bells.json` — schedules, holidays, settings (atomic save, previous copy in `bells.json.bak`)
- `C:\ProgramData\AscentBell\logs\yyyy-MM-dd.txt` — every heads-up, alert, confirmation (with seconds late), escalation and missed bell; 180 days kept

First run seeds **De Paul EM School**'s timings (Mon–Sat, 08:50 → 15:15). Edit them in Settings for other schools.
Uninstall leaves `C:\ProgramData\AscentBell` in place so a reinstall keeps the timings.

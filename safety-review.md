# Runtime Safety Review

Date: 2026-03-13

Scope: review focused on runtime safety behavior rather than classic security. The goal is to identify places where the program could write, overwrite, delete, or launch against unintended locations.

## Findings

### 1. Elevated install flow trusts arbitrary command-line target directories

Severity: High

Relevant files:
- `src/Leigod_Auto_Pause/Program.cs`
- `src/Leigod_Auto_Pause/Installer/BootstrapArguments.cs`
- `src/Leigod_Auto_Pause/Installer/SelfInstaller.cs`

What happens:
- `--perform-install <path>` is parsed directly into `InstallDirectory`.
- When present, the program enters the elevated install path.
- `SelfInstaller.Install()` then creates the target directory, copies the executable there, and creates a desktop shortcut pointing to that location.

Why this is a safety risk:
- There is no validation that the target directory is a real Leigod install directory.
- A mistaken or malformed launch argument could cause the app to copy itself into an unrelated directory under admin privileges.
- This is especially important because the review goal is avoiding unintended file operations outside the intended app directory.

Recommended guardrails:
- Validate the install directory before elevation and again after elevation.
- Require the directory to match strong Leigod markers, not only a passed-in path.
- Explicitly reject drive roots, user profile roots, desktop, downloads, temp, and other broad directories.

### 2. Automatic install-directory detection is permissive enough to target the wrong folder

Severity: High

Relevant files:
- `src/Leigod_Auto_Pause/Installer/LeigodInstallLocator.cs`
- `src/Leigod_Auto_Pause/Installer/LaunchBootstrap.cs`
- `src/Leigod_Auto_Pause/Installer/SystemRegistryReader.cs`
- `src/Leigod_Auto_Pause/Program.cs`

What happens:
- Candidate directories are collected from registry install metadata, display icon paths, running process locations, and a few common folders.
- A directory is considered valid if it contains:
  - `resources\\app.asar`
  - `leigod_launcher.exe`
- Once selected, the program may patch `app.asar`, create a backup, and relaunch from that directory.

Why this is a safety risk:
- The validation rule is weak for a destructive flow that overwrites files.
- If registry metadata is stale or points to an unexpected location, the patch flow can still proceed.
- A false positive means writing `app.asar.bak` and replacing `app.asar` in the wrong directory.

Recommended guardrails:
- Add stronger directory identity checks before patching.
- Prefer canonical install markers over loose filename presence checks.
- Refuse to patch when candidate confidence is ambiguous.
- Consider requiring an exact executable identity or additional metadata validation.

### 3. Asar extraction does not enforce path containment within the temp directory

Severity: High

Relevant files:
- `src/Leigod_Auto_Pause/asarsharp/AsarExtractor.cs`

What happens:
- Archive entry names are combined into paths beneath the extraction directory.
- Symlink-like entries are also materialized by combining the extracted root with the archived link target.
- The code does not normalize and verify that the final output path still stays under the intended temporary extraction root.

Why this is a safety risk:
- A malformed or unexpected archive entry such as `..\\..\\outside.txt` could escape the temp directory.
- That could lead to file creation or overwrite outside the intended workspace for patching.
- This is exactly the kind of runtime safety issue that can become accidental destructive behavior.

Recommended guardrails:
- Canonicalize every output path with `Path.GetFullPath`.
- Reject any file, directory, or link target whose resolved path is outside the extraction root.
- Add regression tests for traversal-style archive entries.

## Lower-risk observations

### Config writes are reasonably scoped

Relevant file:
- `src/Leigod_Auto_Pause/SettingsManager.cs`

Notes:
- Settings are written only under `%APPDATA%\\LeigodPatcher\\settings.json`.
- This looks appropriately scoped and does not appear to risk unrelated file mutation.

### Temp directory cleanup is scoped to a generated temp path

Relevant file:
- `src/Leigod_Auto_Pause/Program.cs`

Notes:
- Cleanup deletes only the generated `tempDir`.
- The current deletion target comes from `Path.GetTempPath()` plus `Path.GetRandomFileName()`, which is a good pattern.
- The main remaining concern is not the cleanup itself, but whether archive extraction could escape that directory before cleanup.

## Testing gaps

Relevant files:
- `tests/Leigod_Auto_Pause.Tests/Installer/SelfInstallerTests.cs`
- `tests/Leigod_Auto_Pause.Tests/Installer/LaunchBootstrapTests.cs`
- `tests/Leigod_Auto_Pause.Tests/Installer/BootstrapArgumentsTests.cs`

Missing coverage:
- Rejecting install into non-Leigod directories
- Rejecting obviously unsafe directories
- Ensuring elevated install re-validates the target directory
- Preventing extraction path traversal outside the temp root
- Preventing patch execution when candidate detection is ambiguous

## Suggested next steps

1. Add a central install-directory validator used by bootstrap, installer, and patch flow.
2. Tighten candidate selection so patching only runs against strongly-verified Leigod installs.
3. Add full path containment checks to `AsarExtractor`.
4. Add tests that prove the app refuses unsafe targets instead of relying on happy-path behavior.

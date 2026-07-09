# Shelly CLI Audit — Command Syntax Consistency & Robustness

Scope: `Shelly.Cli` (all command groups: standard, `aur`, `flatpak`, `appimage`, `config`, `keyring`, utilities), the shortcode layer, completions, and the supporting runtime pieces (`RootElevator`, `ConfigManager`, `Confirm`, `PacmanKeyRunner`, `ShellyFileLog`).

Findings are ordered by severity within each section. File references are to the current branch.

---

## 1. Robustness — high severity

### 1.1 Almost every command exits 0 on failure
Every `Command.SetAction` lambda ends in `return 0;` and `GlobalSettingsCommand.ExecuteAsync` returns `ValueTask` (void) with no error channel. Failures are printed in red and swallowed:

- `install` with a failed transaction → exit 0 (`Commands/Standard/Install.cs:43-56`)
- `install` / `update` / `mark` / `aur install` with **no packages specified** → "Error: No packages specified", exit 0
- `config set badkey x` → "Failed to set configuration key", exit 0
- `keyring` failures → red message, exit 0

Only two paths in the whole CLI return a nonzero code: `flatpak uninstall` for a missing app (`Commands/Flatpak/Remove.cs:80`) and `completions` for an unknown shell. This defeats scripting (`--json` is advertised "for scripting"), CI use, and `set -e` shell usage.

**Recommendation:** change `ExecuteAsync` to return `ValueTask<int>` (or set `Environment.ExitCode`), and adopt a small exit-code contract (0 success, 1 operation failure, 2 usage error, 130 cancelled). See the deep dive below for how peer package managers define their contracts.

#### Deep dive: this is a live bug in Shelly's own GUI, not just a scripting concern

The GTK app shells out to this CLI and derives success **exclusively from the exit code**:

- `Shelly.Gtk/Services/ProcessExecutor.cs:174` and `:426` — `Success = process.ExitCode == 0`
- Dozens of GUI call sites branch on that flag: `ShellySearch.cs:496` (`installFailed = !optResult.Success`), `SetupWindow.cs:108`, `CacheCleanerDialog.cs:55`, ~10 sites in `AppImage.cs`, `Settings.cs:844/861/884`, …
- The `--ui-mode` frame stream does carry `TransactionFailed` events (`Shelly.Cli/UiFrames.cs:19-22`), but nothing in `Shelly.Gtk` consumes them for success/failure — `Services/Wire/EventRouter.cs` routes them for progress display only.

Net effect: when an install/upgrade transaction fails, the CLI prints a red message, exits 0, and **the GUI shows the success path**. Fixing exit codes fixes the GUI for free; conversely, no amount of GUI work fixes it while the CLI reports success.

Two more ironies in the current state:

- **The behavior is inverted relative to convention.** System.CommandLine already returns nonzero for *parse* errors, so `shelly install --bogus-flag` exits 1 while `shelly install <package-that-fails-to-install>` exits 0. Trivial mistakes fail loudly; real failures fail silently.
- **The propagation plumbing already exists and is wasted.** `RootElevator` faithfully forwards the elevated child's exit code (`Environment.Exit(process.ExitCode)`, `RootElevator.cs:26`), `PacmanKeyRunner` returns the real `pacman-key` exit code, and `Main` returns whatever `InvokeAsync` produces. Every link in the chain forwards the code — the commands just never produce one.

#### What every peer package manager does

There is no mainstream package manager that exits 0 on failure. The documented contracts:

| Tool | Contract (from its man page / docs) |
|---|---|
| **pacman** (what Shelly wraps) | "returns zero on success, non-zero on failure." Failed transaction, unresolvable target, and user abort at the confirm prompt all exit 1. |
| **checkupdates** (pacman-contrib) | Tri-state: 0 = updates available, 2 = no updates, 1 = error. Status bars and cron jobs depend on distinguishing "none" from "failed". |
| **yay / paru** (closest peers: AUR helpers wrapping pacman) | Propagate pacman's exit code; nonzero on build or transaction failure. |
| **flatpak** (also wrapped by Shelly) | 0 success, 1 error — Shelly discards this signal today. |
| **apt-get / apt** | "returns zero on normal operation, decimal 100 on error." User abort at prompt: 1. `unattended-upgrades` and Ansible's `apt` module key off it. |
| **dnf / yum** | 0 success, 1 error; `dnf check-update` deliberately uses 100 = updates available, 0 = none, 1 = error. |
| **zypper** | The most elaborate: documented `EXIT CODES` section — 0 success, 1–7 classes of usage/environment error, 8 commit (transaction) failure, plus informational 100–107 (100 = updates available, 102 = reboot required, 105 = aborted by signal…). Explicitly designed "for use in scripts". |
| **npm / pip / cargo / brew** | Nonzero on any failure — universal across the wider ecosystem. |

The pattern to note: the *baseline* (0 = success, nonzero = failure) is unanimous, and several managers additionally reserve codes to make **"success, but noteworthy state"** scriptable (checkupdates' 2, dnf's 100, zypper's 100–107). Shelly's `check-updates --count` is exactly the kind of command status-bar/cron consumers would use — on Arch, `checkupdates` semantics (2 = no updates) would be the familiar contract to mirror.

Downstream, the whole composition model assumes this: `set -e` / `&&` chains, systemd `ExecStart` failure handling, cron mail-on-failure, CI steps, and config management (Ansible's `pacman`/`apt` modules literally decide `failed:` from `rc != 0`). A Shelly invocation in any of those contexts currently cannot fail.

#### Suggested contract for Shelly

- `0` — success, including "nothing to do" (pacman returns 0 for an up-to-date `-Syu`)
- `1` — operation/transaction failure (matches pacman, flatpak, yay/paru; also what System.CommandLine already uses for parse errors)
- `2` — usage/validation errors (no packages specified, conflicting flags) — optional refinement; collapsing into 1 also matches pacman
- `130` — SIGINT (already implemented)
- Declining a confirmation prompt: exit 1, matching pacman and apt (both treat user abort as failure, which keeps `shelly upgrade && reboot` safe)
- `check-updates`: consider adopting `checkupdates`' tri-state (2 = no updates) as a deliberate, documented choice

Mechanically: change `GlobalSettingsCommand.ExecuteAsync`/`ExecuteUiMode` to `ValueTask<int>` and return it from each `SetAction` — ~70 call sites but each edit is trivial, and `UpgradeAll` aggregates its children (nonzero if any step failed). The GTK app needs no changes; its `ExitCode == 0` check starts working the moment the CLI tells the truth.

### 1.2 `Confirm` auto-confirms on EOF / non-interactive stdin
`Confirm.Execute` (`Interactions/Confirm.cs`): `Console.ReadLine()` returns `null` when stdin is closed or redirected, which falls into the `IsNullOrWhiteSpace` branch and returns the **default value — `true` for nearly all destructive prompts** (install, remove, update). `shelly install foo </dev/null` proceeds as if confirmed. (`upgrade-all` is the one caller passing `false`.)

**Recommendation:** treat EOF as "no" (or hard-fail with "stdin is not a TTY; use --no-confirm").

### 1.3 Argument injection into root-run `pacman-key`
`Keyring` builds argument strings by interpolation and `PacmanKeyRunner` passes them as a raw `Arguments` string (`PacmanKeyRunner.cs:16`, `Commands/Keyring/Keyring.cs`):

```csharp
PacmanKeyRunner.Run(console, $"--lsign-key {key}");
recvArgs += $" --keyserver {Keyserver}";
```

A "key ID" of `x --delete SOMEKEY` becomes extra `pacman-key` arguments in a root process. The same string-interpolation pattern appears in `ConfigManager.IsOwnedByRoot`/`FixOwnership` (`stat`, `chown -R {user}:{user} "{path}"`).

**Recommendation:** use `ProcessStartInfo.ArgumentList` everywhere; validate key IDs (`^[0-9A-Fa-f]+$`) before invoking.

### 1.4 A corrupted `config.json` crashes every command
`ConfigManager.ReadConfig` deserializes with no try/catch; malformed JSON throws `JsonException` up through whatever command touched config. Writes are non-atomic (`File.WriteAllText`), so a crash/power loss mid-write bricks the CLI until the user manually deletes the file.

**Recommendation:** catch parse failures (back up the bad file, regenerate defaults, warn); write via temp-file + `File.Move` rename.

### 1.5 Three different "am I root?" checks
- `RootElevator.EnsureRootExectuion`: `Environment.UserName == "root"` (`RootElevator.cs:9`)
- `ConfigManager.IsRunningAsRoot`: `Environment.GetEnvironmentVariable("USER") == "root"` (`ConfigManager.cs:450-451`) — `USER` is frequently unset under cron/`doas`/systemd, so root-owned config ownership fixes silently don't run
- `UserIdentity.IsRoot()`: `getuid() == 0` (correct)

**Recommendation:** use `UserIdentity.IsRoot()` everywhere.

### 1.6 `Environment.Exit` skips cleanup
- Ctrl+C handler calls `Environment.Exit(130)` (`Program.cs:59`) — `using var log` is never disposed, `WriteSessionFooter` never runs, buffered log lines can be lost (writer is `AutoFlush = false`).
- `RootElevator.EnsureRootExectuion` exits the same way after the elevated child finishes (`RootElevator.cs:26`), also skipping the footer.

**Recommendation:** flush/dispose the log (and any in-flight ALPM transaction handling) before exiting; in the elevator path, write the footer before `Environment.Exit`.

## 2. Robustness — medium severity

### 2.1 `ignore` silently does nothing without a mode flag
`shelly ignore foo` passes validation (packages are non-empty), matches none of `--add/--remove/--clear/--list`, and exits 0 having done nothing (`Commands/Standard/Ignore.cs`). Also:

- `--add --remove` together is not rejected; `--add` silently wins (precedence add > remove > clear > list is undocumented).
- `ignore --list` calls `RootElevator.EnsureRootExectuion()` before branching — a read-only listing demands root/sudo.
- In `ExecuteUiMode`, the "No packages specified" error **does not return**; execution continues.

**Recommendation:** require exactly one mode (or make them subcommands: `ignore add|remove|list|clear`, matching `config`), and elevate only for mutating modes.

### 2.2 Unchecked results in Flatpak commands
- `flatpak update`: `manager.UpdateApp(Package)` returns a string that is printed and never inspected; UI mode ends with `TxFinish(true, …)` unconditionally (`Commands/Flatpak/Update.cs`).
- `flatpak upgrade` UI mode likewise reports success regardless of results (`Commands/Flatpak/Upgrade.cs:59-68`).

### 2.3 `config set` exposes every `ShellyConfig` property
`ConfigManager.UpdateConfig` reflects over all public properties case-insensitively, including GUI-internal state (`WindowWidth`, `TrayIconPath`, `NewInstall`, `DefaultPageDropDown`…). There is no allowlist and no `config list` distinction between CLI-relevant and GUI-internal keys. `config parallel` accepts `0` and negative values (no range validation).

### 2.4 Dead config knob: `DefaultExecution`
`DefaultExecution` is validated and settable via `config set`, but nothing in the CLI reads it — the bare `shelly` action is hard-coded to `UpgradeAll` (`Program.cs:179-185`). Either wire it up or remove it. Related: bare `shelly` kicking off a full multi-source system upgrade is an aggressive default worth a deliberate decision.

### 2.5 URL installs: predictable temp path, no verification
`Install.DownloadCore` writes to `Path.GetTempPath() + <basename of URL>` — a predictable, world-writable location (pre-existing files/symlinks are followed by `File.Create`), and downloaded packages get no checksum/signature check beyond what ALPM later enforces (`Commands/Standard/Install.cs:391-403`). Prefer a private temp subdirectory (`Directory.CreateTempSubdirectory`).

### 2.6 Validation ordering and UI-mode drift
The `UiMode` branch usually runs **before** argument validation, so each command duplicates validation in both paths — and they've already diverged (`Ignore.ExecuteUiMode` missing `return`; `Aur.Search.ExecuteUiMode` is an empty no-op because UI mode is handled inline in `ExecuteAsync`, unlike every other command). Validate once, before branching.

---

## 3. Syntax consistency

### 3.1 Verb inconsistencies across managers

| Concept | standard | aur | flatpak | appimage |
|---|---|---|---|---|
| Remove | `remove` | `remove` | **`uninstall`** | `remove` |
| Targeted update | `update <pkgs...>` (multi) | `update <pkgs...>` (multi) | `update <pkg>` (**exactly one**) | — (none) |
| Upgrade everything | `upgrade` | `upgrade` | `upgrade` | `upgrade` |
| List installed | via `query -i` | `list` | `list` | `list` |
| Search | `query [pkg]` (0-or-1 arg) | `search <terms...>` (1+, joined) | `search <term>` (exactly one) | — |

- `flatpak uninstall` should be `remove` (or an alias) for parity.
- `flatpak add-remotes` / `remove-remotes` are **plural but operate on a single remote**; should be `add-remote` / `remove-remote`.
- Search argument arity differs three ways for the same user intent.
- `pacfile` is described as "Manage pacfiles" but is read-only (display only).

### 3.2 Three different command grammars for "modes"
- `config` uses **subcommands** (`get/set/list/reset/parallel`) ✔ conventional
- `ignore` uses **boolean mode flags** (`--list/--add/--remove/--clear`)
- `keyring` uses a **positional action word** (`init|list|refresh|lsign|populate|recv`)
- `mark` uses **mutually exclusive flags** (`--explicit`/`--depends`)

Pick one grammar (subcommands are the safest with System.CommandLine — they get validation, help, and completions for free).

### 3.3 Option naming and casing
- **camelCase options**: `--explicitOnly`, `--dependencyOnly` (`Commands/Aur/ListInstalled.cs`) vs. kebab-case everywhere else (`--show-hidden`, `--build-deps`).
- **PascalCase argument names** leak into help/usage: `BundlePath` (`Flatpak/InstallBundle.cs`), `RefFilePath` (`Flatpak/InstallRefFile.cs`); camelCase `downloadCount` (`Config/ConfigParallel.cs`). All other arguments are lowercase (`packages`, `query`, `remote`).
- `query --detail`/`--info` is the only option with two long aliases.

### 3.4 Same option, different short flag (or none)

| Concept | Occurrences |
|---|---|
| `--build-deps` | standard install: `-b`; **aur install: `-o`** |
| remove config | standard remove: `--remove-config -r`; appimage remove: `--remove-config -c`; flatpak uninstall: `--config -c` |
| `--show-hidden` | query: `-w`; aur list / aur list-updates: no short |
| dry run | `cache-clean --dry-run -d`, `purify --dry-run -d` ✔ consistent — use as the model |

Odd shorts chosen to dodge collisions read as noise: `check-updates --flatpak -l`, `export --name -a`.

### 3.5 Default-true boolean flags that can't be turned off naturally
- `remove --cascade -c` and `aur remove --cascade -c` default to **true** (`DefaultValueFactory = _ => true`), so passing `-c` is a no-op and disabling requires the undiscoverable `--cascade false`. Same for `--system` on `flatpak add-remotes`, `remove-remotes`, `install-bundle`, `install-ref-file` (and those have no `--user` counterpart, while `flatpak install` uses a `--user` flag with system default).
- Convention: make the default implicit and expose the negation (`--no-cascade`, `--user`), as `upgrade-all` already does with `--no-repo/--no-aur/--no-flatpak/--no-appimage`.
- Side effect: the `-SR`/`-AR` shortcode modifier `c` is currently meaningless.

### 3.6 Shortcode layer
- The `-U` shortcode type prefix collides with the global `-U/--ui-mode`; the help text even has to warn "In shortcode mode use --ui-mode instead of -U". Consider a different letter for the utility group or dropping the global `-U` short.
- `ShortcodeMaps.Modifiers` is a hand-maintained duplicate of the real option shorts and has already drifted: `('A','L')` (aur list) allows no modifiers although the command has `-e`/`-d`; new options must be remembered in two places.
- Modifiers that map to **value-taking options** are allowed (`('F','I')` allows `r` and `b` for `--remote`/`--branch`): `-FIrb foo` expands to `flatpak install -r -b foo`, which mis-parses. Modifier sets should be restricted to boolean flags.
- No test asserts parity between `ShortcodeMaps` and `Program.BuildRootCommand()` — a cheap unit test would walk the command tree and verify every mapped verb exists and every modifier is a real boolean short flag of that command.

### 3.7 Miscellaneous naming
- AppImage verbs mix noun-verb and verb-noun: `sync-meta`, `configure-updates`, `migrate-manager` vs. flatpak's `sync-remote-appstream`, `get-remote-appstream`, `app-remote-info`.
- `RootElevator.EnsureRootExectuion` — typo (`Exectuion`) in a public API.
- Checked-in `shelly.fish` / `_shelly` at the repo root are stale (missing `pacfile`) even though PKGBUILD regenerates them at build time; either delete the committed copies or regenerate them in CI to avoid confusion.
- `update` double-confirms (two consecutive prompts); intentional friction, but the second generic "Do you want to proceed?" adds nothing after "Are you absolutely sure…".

---

## 4. Suggested priorities

1. **Exit codes** (1.1) — foundational; everything scripted depends on it.
2. **Confirm-on-EOF** (1.2) and **pacman-key argument injection** (1.3) — small fixes, real-world impact.
3. **Config resilience** (1.4) and unified root detection (1.5).
4. **Deprecate-and-alias syntax cleanups** (3.1, 3.3, 3.4, 3.5): keep old names as hidden aliases for a release cycle (`uninstall` → `remove`, `add-remotes` → `add-remote`, `--explicitOnly` → `--explicit-only`, `--cascade` → `--no-cascade`).
5. **Shortcode parity test** (3.6) to stop future drift.

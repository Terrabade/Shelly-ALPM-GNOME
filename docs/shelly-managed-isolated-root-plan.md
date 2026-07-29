# Shelly-Managed Isolated AUR Build Root

## Objective

Replace Shelly's current `makechrootpkg` integration with an isolated filesystem
root whose package state is created and managed exclusively by Shelly.

The first implementation should use a fresh, operation-scoped build root.
Reusable templates, overlays, and reflink-based cloning should be deferred until
the correctness and security properties of the basic implementation are proven.

## Constraints

- Do not execute `pacman`, `makechrootpkg`, `mkarchroot`, `pkgctl`, or another
  package-manager frontend.
- Shelly's direct use of libalpm is permitted.
- `systemd-nspawn` may be used for process and filesystem isolation only.
- `makepkg` remains the PKGBUILD runner, but it must never receive
  `-s`, `--syncdeps`, `-i`, `--install`, or equivalent options.
- Having the `pacman` package inside the build root is acceptable when it
  supplies `makepkg` and libalpm, but its `pacman` executable must never run.
- Final package installation on the host remains a Shelly/libalpm operation.

The package-operation invariant is:

> All dependency resolution and package transactions are performed by Shelly.
> `makepkg` only validates dependencies, executes PKGBUILD functions, and
> creates package archives.

The current makepkg documentation confirms that `--syncdeps` installs missing
dependencies using pacman:
[makepkg(8)](https://man.archlinux.org/man/makepkg.8.en).

## Target Architecture

```text
AUR discovery and review
          |
          v
Operation-wide dependency DAG
          |
          v
Shelly creates fresh filesystem root
          |
          +-- Shelly installs base-devel through isolated ALPM handle
          +-- Shelly installs all repository build dependencies
          |
          v
AUR nodes built in dependency order
          |
          +-- makepkg runs inside root as unprivileged user
          +-- Shelly discovers produced archives
          +-- Shelly installs dependency archives into the root
          |
          v
All requested packages built successfully
          |
          v
Shelly installs runtime dependencies and final artifacts on host
```

## 1. Establish the Package-Operation Boundary

Enforce the no-pacman invariant at several layers:

1. The isolated build command must not contain `-s`, `--syncdeps`, `-i`, or
   `--install`.
2. The isolated `makepkg.conf` should set `PACMAN=/usr/bin/false` as defense in
   depth.
3. The build runner should reject forbidden makepkg arguments before spawning
   the build.
4. Integration tests should replace `/usr/bin/pacman` inside a disposable root
   with a failing sentinel and prove that a complete build never reaches it.

Do not pass `--nodeps`. Dependencies should already be installed by Shelly, and
makepkg should verify them. A missing dependency should fail the build with a
clear diagnostic instead of being ignored or delegated to pacman.

## 2. Introduce Build-Backend Abstractions

Add focused modules:

- `Shelly.PackageManager/src/aur/build_graph.zig`
- `Shelly.PackageManager/src/aur/build_root.zig`
- `Shelly.PackageManager/src/aur/build_runner.zig`

Suggested core types:

```zig
const BuildNode = struct {
    package_base: []const u8,
    commit: []const u8,
    prepared: *PreparedPackage,
    dependencies: []BuildEdge,
    effective_role: Role,
};

const BuildArtifact = struct {
    path: []const u8,
    package_name: []const u8,
    package_base: []const u8,
    version: []const u8,
    provides: []const []const u8,
};

const BuildResult = struct {
    artifacts: []BuildArtifact,
};

const BuildRoot = struct {
    operation_id: []const u8,
    root_path: []const u8,
    config_path: []const u8,
    artifact_path: []const u8,
    alpm: *AlpmManager,
};
```

Refactor `buildPreparedPackage()` in
`Shelly.PackageManager/src/aur/manager.zig` to call a build backend instead of
constructing a makepkg command directly.

Keep the existing host makepkg backend temporarily for non-isolated builds. The
new backend implements the isolated-root path.

## 3. Build an Operation-Wide Dependency DAG

The current dependency collection is flattened per requested package and uses
the host's installed-package state. That state cannot determine whether a
dependency is available in an isolated root.

Replace it for isolated builds with a DAG keyed by:

```text
(package base, reviewed commit)
```

Each edge records:

- Requested dependency name and version constraint.
- Whether it was resolved through a `provides` entry.
- Runtime, build, or check role.
- Which split-package output satisfies it.

Rules:

- Merge duplicate nodes across every requested target.
- Merge roles using the existing priority: runtime, then build, then check.
- Produce a deepest-dependency-first order through topological sorting.
- Detect and reject cycles before creating the root.
- Ignore host-installed AUR packages when deciding what must be built.
- Continue using synchronized repository metadata to determine whether a
  dependency has a repository satisfier.
- Omit check dependencies when `--check` is disabled.
- Complete discovery and review of every node before the first build.

An AUR dependency installed on the host must still be built for the isolated
root unless Shelly has a verified reusable artifact for the exact required
version and architecture.

## 4. Define the Root Layout

Use a versioned path so legacy chroots cannot be mistaken for the new
implementation:

```text
/var/lib/shelly/build-roots/v1/
└── operations/
    └── <random-operation-id>/
        ├── root/
        │   ├── build/
        │   ├── home/shelly-build/
        │   ├── var/lib/shelly/alpm/
        │   └── var/lib/shelly/artifacts/
        ├── config/
        │   ├── alpm.conf
        │   └── makepkg.conf
        ├── state.json
        └── operation.lock
```

Requirements:

- The parent and operation directories are owned by root with mode `0700`.
- Operation IDs are random and unpredictable.
- `state.json` records atomic lifecycle transitions:
  `creating`, `provisioning`, `building`, `complete`, and `failed`.
- One root is used for the entire AUR operation, not one root per package.
- Host home directories, ALPM databases, runtime sockets, and AUR checkouts are
  not mounted into the root.
- Successful and cancelled operations are cleaned after their child processes
  have exited.
- Failed roots are retained only behind an explicit diagnostic setting.
- Stale cleanup checks state, lock ownership, and age before removing anything.

The first version should create the root from scratch. A reusable base template
can be considered later.

## 5. Generate an Isolated ALPM Configuration

Shelly already parses `RootDir` and passes it to `alpm_initialize()`. Add a
dedicated isolated-root configuration writer and constructor.

The generated configuration should contain:

- `RootDir`: the operation's `root/`.
- `DBPath`: `<root>/var/lib/shelly/alpm`.
- `CacheDir`: root-local for the initial implementation.
- `LogFile`: root-local.
- Architecture copied from the host configuration.
- Repository names, usage, signature policies, and resolved server URLs.
- The host GPG directory for host-side repository signature verification.
- No host hook directories.
- No host ignore or hold rules unless explicitly supported.

Do not copy `/etc/pacman.conf` verbatim. Resolve includes and write only the
configuration values needed by the build root. This prevents host database,
cache, log, and hook paths from leaking into the isolated transaction.

Suggested API:

```zig
AlpmManager.initForRoot(
    allocator,
    environ,
    generated_config_path,
    operation_context,
)
```

The constructor must validate that every mutable path is canonical and located
under the operation directory. Read-only trust paths must be explicitly
allowlisted.

## 6. Decouple Internal Transactions from User Prompts

The build-root ALPM manager needs cancellation and progress events, but it
should not display ordinary host-install prompts for `base-devel` and build
dependencies. The user already approved these through the AUR transaction
plan.

Add transaction options:

```zig
const InteractionPolicy = enum {
    interactive,
    preapproved_internal,
};

const InstallOptions = struct {
    flags: TransFlag = .{},
    interaction: InteractionPolicy = .interactive,
    select_optional_dependencies: bool = true,
};
```

Existing public methods remain wrappers using interactive defaults.
Build-root transactions use:

- `preapproved_internal`
- no optional-dependency selection
- the shared cancellation context
- progress events labeled as build-root provisioning

## 7. Bootstrap the Root with Shelly

Provisioning sequence:

1. Create the filesystem layout.
2. Generate the isolated ALPM configuration.
3. Initialize the root-specific Shelly ALPM manager.
4. Synchronize its repository databases.
5. Install the bootstrap set through Shelly:
   - `base-devel`
   - utilities required to create the build account
   - `git`, GnuPG, and CA certificates when not guaranteed by the resolved
     bootstrap group
6. Validate expected binaries:
   - `/usr/bin/bash`
   - `/usr/bin/makepkg`
   - `/usr/bin/fakeroot`
   - `/usr/bin/git`
7. Create a fixed unprivileged `shelly-build` account inside the root.
8. Create and assign ownership of its home, build, and artifact directories.
9. Record resolved package versions in `state.json`.

The existing `AlpmManager.install_packages()` implementation supports
repository groups, so Shelly can resolve `base-devel` directly.

A feasibility spike must verify that an empty `RootDir` transaction correctly
handles package ordering, scriptlets, hooks, certificates, and the local
package database.

## 8. Install Repository Dependencies into the Root

After every PKGBUILD is reviewed and the transaction plan is accepted:

1. Collect every repository runtime, build, and enabled check dependency from
   the DAG.
2. Deduplicate them by the actual satisfying repository package.
3. Install them in as few root ALPM transactions as practical.
4. Mark them as dependency-installed inside the root.
5. Requery the root local database and verify every dependency expression.

Do not install build/check-only dependencies on the host.

If verification fails, stop before running any PKGBUILD and report the exact
missing expression and the package that introduced it.

## 9. Stage Reviewed Sources Safely

Do not bind the host AUR checkout into the root. A hostile PKGBUILD could
otherwise mutate the reviewed checkout or adjacent user-owned data.

For each build node:

1. Re-run the existing reviewed-checkout integrity check.
2. Create `/build/<node-id>` inside the root.
3. Copy the exact reviewed checkout into it, including `.git` when required by
   `pkgver()`.
4. Reject source paths and symlinks that escape the checkout.
5. Recompute the review digest from the staged copy.
6. Fail closed if the staged digest differs.
7. Change ownership only within the isolated root.

Local source files covered by the existing review digest must be copied
identically.

## 10. Configure makepkg Inside the Root

Generate a root-specific makepkg configuration derived from the host's
compilation settings but with controlled paths:

- `BUILDDIR` under the operation root.
- `PKGDEST` under `/var/lib/shelly/artifacts/<node-id>`.
- `SRCDEST` and `SRCPKGDEST` root-local initially.
- `LOGDEST` root-local.
- Preserve intended `CFLAGS`, `CXXFLAGS`, `MAKEFLAGS`, architecture, packager,
  compression, and reproducibility settings.
- Set `PACMAN=/usr/bin/false`.
- Do not expose the host `HOME`, XDG directories, or package destinations.

Source PGP verification requires a safe key handoff:

1. Export the invoking user's public keys and owner-trust information before
   elevation.
2. Import only that public material into the isolated build user's GnuPG home.
3. Never copy or bind private keys into the root.

## 11. Run makepkg Through systemd-nspawn

Use `systemd-nspawn` directly, without `arch-nspawn`.

Conceptual command:

```text
systemd-nspawn
  --directory <operation-root>
  --register=no
  --quiet
  --user shelly-build
  --chdir /build/<node-id>
  --setenv HOME=/home/shelly-build
  --setenv GNUPGHOME=/home/shelly-build/.gnupg
  makepkg -f -c --noconfirm --skippgpcheck [--nocheck]
```

Policy:

- Default Shelly behavior appends `--nocheck`.
- `--check` omits `--nocheck`.
- Never append `-s`, `--syncdeps`, `-i`, or `--install`.
- Do not append `--nodeps`.
- Use a sanitized environment allowlist.
- Run as the unprivileged build account.
- Do not boot systemd inside the container.
- Do not bind host paths.
- Forward stdout and stderr through the existing streaming infrastructure.
- Propagate cancellation by terminating nspawn and waiting for it to exit.
- Report the exit status and retained log path.

## 12. Discover and Validate Build Artifacts

Do not select packages only by filename.

After makepkg succeeds:

1. Enumerate the node's dedicated `PKGDEST`.
2. Load each candidate using Shelly's local-package/libalpm inspection.
3. Extract its package name, base, version, architecture, `provides`,
   dependencies, and signature state.
4. Reject malformed archives and unexpected package bases.
5. Map split-package outputs to DAG edges by package name and `provides`.
6. Store a `BuildArtifact` manifest.
7. Require every expected dependency output to have exactly one valid
   satisfier.

This replaces filename-oriented artifact selection for the isolated backend.

## 13. Install AUR Dependency Artifacts into the Root

Process DAG nodes in topological order.

After each dependency node builds:

1. Select the archive or split-package archives required by downstream edges.
2. Install them through the root-specific Shelly ALPM manager.
3. Mark them as dependencies inside the root.
4. Revalidate downstream dependency expressions against the root database.
5. Retain their artifact metadata for host installation when their effective
   role is runtime.

Build/check-only AUR dependencies remain solely in the operation root.

All requested packages in one operation share the root, so an artifact built
once can satisfy multiple downstream targets.

## 14. Delay Host Mutation Until Every Build Succeeds

The host should not be modified while the build graph is incomplete.

After all requested targets have valid artifacts:

1. Determine repository runtime dependencies not already satisfied on the
   host.
2. Determine AUR runtime dependency artifacts not already satisfied by an
   acceptable host version.
3. Determine requested target artifacts and requested split outputs.
4. Install repository runtime dependencies through the existing host Shelly
   manager.
5. Install AUR runtime dependencies and requested targets through Shelly's
   local-package transaction.
6. Mark runtime dependencies as dependency-installed.
7. Preserve or set requested targets as explicit.
8. Install selected optional dependencies afterward.
9. Update VCS metadata only after successful host installation.
10. Clean the build root.

Build and check dependencies must never reach the host.

A later improvement can combine repository and local packages into one
low-level ALPM transaction. The initial implementation may preserve Shelly's
current two-stage host installation behavior.

## 15. Events and User Experience

Add build-root-specific stages:

- Creating isolated root
- Synchronizing root repositories
- Installing bootstrap packages
- Installing repository build dependencies
- Staging reviewed sources
- Building AUR dependency
- Installing AUR dependency into build root
- Building requested package
- Installing completed artifacts on host
- Cleaning isolated root

Keep the existing `--chroot` option for compatibility, but change its
description to:

> Build in a Shelly-managed isolated filesystem root.

Internally, `use_chroot` can become `use_isolated_root` after the new backend is
stable.

Remove the `devtools` optional dependency because the new backend does not use
it.

## 16. Security Requirements

The implementation is incomplete unless all of these hold:

- PKGBUILD review and integrity verification happen before execution.
- Reviewed host checkouts are copied, never mounted writable.
- Builds run as a non-root account.
- The root cannot access the host home, package database, configuration
  directories, runtime sockets, or arbitrary cache paths.
- Environment variables are allowlisted.
- All operation paths are canonicalized beneath the build-root parent.
- Cleanup never follows symlinks outside the operation directory.
- Repository package signatures use the host's trusted keyring through Shelly.
- Source verification receives public keys only.
- No package-manager subprocess other than Shelly exists in the execution
  trace.
- Cancellation terminates child processes before cleanup.
- A failed or malicious build cannot change host package state.

## 17. Test Plan

### Unit Tests

- Root configuration contains isolated DB, cache, log, and hook paths.
- Root configuration cannot escape its operation directory.
- Forbidden makepkg arguments are rejected.
- `--check` and `--nocheck` mapping is correct.
- Dependency roles merge correctly.
- DAG ordering and cycle detection work.
- Host-installed packages do not suppress isolated dependencies.
- Split-package artifact mapping works.
- `provides`-based artifact mapping works.
- Runtime versus build/check host-install selection is correct.
- Root state transitions and stale-root selection are safe.
- Artifact filenames cannot spoof package metadata.

### Process Tests

Use fake `systemd-nspawn` and makepkg executables to verify:

- Exact argv and environment.
- No `-s`, `--syncdeps`, `-i`, or `--install`.
- Output streaming.
- Cancellation and child termination.
- Nonzero status propagation.
- No host-path bind arguments.

### Root-Required Integration Tests

Run these only in an explicitly enabled disposable environment:

1. Bootstrap an empty root with Shelly and `base-devel`.
2. Build a package with repository-only dependencies.
3. Build `AUR-C -> AUR-B -> requested-A`.
4. Build a dependency satisfied through `provides`.
5. Build a split-package dependency.
6. Confirm host-installed AUR dependencies are rebuilt for the root.
7. Confirm check dependencies appear only with `--check`.
8. Confirm build-only dependencies never appear in the host database.
9. Cancel during repository download and during makepkg.
10. Attempt to write to the invoking user's home and verify failure.
11. Replace `/usr/bin/pacman` inside the disposable root with a failing
    sentinel and confirm the complete build succeeds.
12. Run two simultaneous operations and verify independent roots and
    databases.
13. Verify cleanup after success, failure, and cancellation.

## 18. Delivery Phases

### Phase 0: Feasibility Spike

Estimated effort: 2-3 days.

- Generate an alternate-root ALPM configuration.
- Bootstrap `base-devel` into an empty temporary root using Shelly.
- Run a trivial makepkg build with nspawn.
- Validate scriptlets, hooks, signatures, and ownership.

Stop and reassess if empty-root libalpm transactions cannot safely bootstrap
the environment.

### Phase 1: Abstractions and DAG

Estimated effort: 3-5 days.

- Add build backend and result types.
- Introduce the operation-wide dependency graph.
- Separate host and isolated dependency state.
- Preserve review-before-build behavior.

### Phase 2: Build-Root Lifecycle

Estimated effort: 4-6 days.

- Add root layout and lifecycle state.
- Generate the isolated ALPM configuration.
- Add the internal transaction interaction policy.
- Implement the bootstrap transaction.
- Create the build user and permissions.
- Implement cleanup and cancellation.

### Phase 3: Isolated Execution and Artifacts

Estimated effort: 4-6 days.

- Stage reviewed source safely.
- Generate the isolated makepkg configuration.
- Add the nspawn runner.
- Discover artifacts from package metadata.
- Add pacman-invocation defenses.

### Phase 4: Dependency Installation and Host Commit

Estimated effort: 4-6 days.

- Install repository dependencies into the root.
- Install AUR dependencies into the root.
- Separate runtime, build, and check roles.
- Delay host mutation until all builds succeed.
- Preserve host package installation reasons.

### Phase 5: Integration, UI, and Migration

Estimated effort: 5-8 days.

- Add the root-required integration suite.
- Update CLI and UI events.
- Update packaging dependencies.
- Add failure diagnostics.
- Document lifecycle and stale-root handling.

Expected production effort: approximately 3-5 weeks.

## Definition of Done

The replacement is complete when:

- Shelly creates a clean root without executing pacman or a devtools frontend.
- A three-level mixed repository/AUR dependency chain builds successfully.
- makepkg is never asked to resolve or install dependencies.
- Every AUR build dependency exists inside the root before its consumer runs.
- Build/check-only dependencies never modify the host.
- Final packages are installed through the existing Shelly/libalpm path.
- Review, cancellation, progress, UI questions, and invoking-user ownership
  remain intact.
- The integration suite proves that a failing pacman sentinel is never reached.

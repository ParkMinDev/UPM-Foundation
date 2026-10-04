# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [10.1.5] - 2026-10-04

### Changed
- Standardized package identity and display name as `com.parkmindev.upm.foundation` / `ParkMinDev.UPM.Foundation`.
- Synchronized own-package dependency versions for this release; C# namespaces and assembly names remain unchanged.

## [10.1.4] - 2026-10-04

### Changed
- Renamed the repository to UPM-Foundation and updated repository links without changing the Unity package identity, namespaces, assemblies, or asset GUIDs.

## [10.1.3] - 2026-10-04

### Changed
- Moved repository links and dependency URLs to ParkMinDev while preserving the package identity.
- Replaced repository dependency metadata with parkmin-upm.json and aligned ParkMin dependency release versions.

## [10.1.2] - 2026-10-01

### Fixed
- Guarded HierarchyHelper editor updates against destroyed instances, removing their update subscription before accessing gameObject, and skipped updates for inactive or disabled components.

## [10.1.1] - 2026-09-12

### Fixed
- Limited ExtendedBehaviour.Dispose to destruction and disposal bookkeeping without explicitly disabling the component.

## [10.1.0] - 2026-09-06

### Added
- Added `RemoveFeatures<TFeature>()` for disposing every matching feature owned by a component without retaining feature references.

### Changed
- Updated `GetFeature<TFeature>()` and `GetOrAddFeature<TFeature>()` to find the feature assigned to the requesting owner when duplicate feature components exist.

## [10.0.0] - 2026-08-30

### Breaking Changes
- Removed `DependencyBehaviour` and its component and GameObject extension methods.

### Added
- Added the owner-based `Feature<TOwner>` component, feature lifecycle contracts, and component extensions for adding and retrieving features.
- Added `SerializableValue<T>` for serializing class-based inline values, Unity object references, and managed references through one API.
- Added a context-aware `SerializableValue<T>` inspector with scene, Prefab Mode, and ScriptableObject selection support.
- Added editor tests covering serialization modes, prefab workflows, missing references, object lookup, and undo behavior.

## [9.0.2] - 2026-08-27

### Fixed
- Prevented `ExtendedBehaviour.Dispose()` from accessing a destroyed Unity object after external destruction.

## [9.0.1] - 2026-08-25

### Changed
- Adjusted the `Assets/Create/Project` menu priority to align with the default asset creation entries.

## [9.0.0] - 2026-08-25

### Breaking Changes
- Required an explicit dependency setter when adding a `DependencyBehaviour` through component and GameObject extensions.

## [8.0.0] - 2026-08-25

### Breaking Changes
- Moved dependency validation from `ExtendedBehaviour` to the new `DependencyBehaviour` base class.
- Replaced the dependency-aware `AddExtendedBehaviour` and `GetOrAddExtendedBehaviour` extensions with `AddDependencyBehaviour`, `GetDependencyBehaviour`, and `GetOrAddDependencyBehaviour`.

### Added
- Added `DependencyBehaviour` for components whose dependencies can be injected at edit time or runtime.
- Added reusable inspector header constants for required, injectable, optional, and settings fields.

### Changed
- Executed R3 update interfaces through Unity update callbacks while outside Play Mode and limited R3 subscriptions to Play Mode.
- Made `ExtendedBehaviour` disposal safe for edit-time component removal and simplified its editor lifecycle handling.

## [7.0.0] - 2026-08-22

### Breaking Changes
- Replaced `ExtendedBehaviour` update-method declarations and virtual callbacks with opt-in R3 update interfaces.

### Added
- Added R3 update interfaces for each supported Unity player-loop phase.
- Added `IDisposable` support to `ExtendedBehaviour` for disabling and destroying an attached component.
- Added `Component.AddComponent` extensions for generic and runtime component types.

## [6.0.0] - 2026-08-20

### Changed
- Replaced `HierarchyManager` with `HierarchyHelper` under the `Components.Helpers` namespace and folder.
- Added independent opt-in controls for hierarchy expansion and scene visibility, with one-time hierarchy application on scene entry.

### Added
- Added a reusable editor-only script icon and assigned it to `HierarchyHelper`.

## [5.6.1] - 2026-08-19

### Changed
- Renamed `LatestOperationCancellationTokenSource` to `AutoRenewCancellationTokenSource`.
- Renamed `CreateToken()` to `CancelPreviousAndCreateToken()` to make the previous-operation cancellation behavior explicit.

## [5.6.0] - 2026-08-17

### Added
- Added `LatestOperationCancellationTokenSource` for canceling and disposing the previous operation whenever a newer token is created.
- Added thread-safe token capture before publishing a replacement source so concurrent `CreateToken()` calls cannot read from an already disposed source.

## [5.5.0] - 2026-08-17

### Added
- Added `CreateAssetMenuMarkerAttribute` for declaring reusable ScriptableObject creation categories through marker interfaces.
- Added the `Assets/Create/Project` menu with nested paths and support for ScriptableObjects implementing multiple marker interfaces.

## [5.4.2] - 2026-08-16

### Changed
- Added an explicit version to Git dependency metadata for PackageManager display and validation.

## [5.4.1] - 2026-08-16

### Changed
- Moved Git package dependency metadata from `package.json` to `parkmin-dependencies.json` for PackageManager discovery.

## [5.4.0] - 2026-07-30

### Added
- Added a `GameObject/Prefab/Revert Name` menu for reverting the selected prefab instance name.

## [5.3.0] - 2026-07-29

### Added
- Added `ScriptableSingleton<T>` with automatic resource asset creation in the Unity Editor.

## [5.2.0] - 2026-07-29

### Added
- Added `SceneVisibilityLocker` for persistent Scene view visibility and picking controls in the Unity Editor.
- Added `WindowBoxCamera` for maintaining a target aspect ratio across editor, display, and render texture outputs.
## [5.1.0] - 2026-07-28

### Added
- Added `DestroyOnStart` with options to destroy its GameObject or selected serialized MonoBehaviour components.

### Changed
- Moved the reusable `EditorPlayBehaviour`, `InstantiateOnceOnRuntime`, and `TargetFrameSetting` components from Workflow.Default into Foundation.

## [5.0.0] - 2026-07-25

### Breaking Changes
- Changed runtime and editor namespaces to the `ParkMinPackages.Foundation` convention.
- Moved workflow-specific actors, bindings, build settings, and project bootstrap utilities to Workflow.Default.

### Added
- Added a dedicated Foundation editor assembly.
- Added `DontDestroyOnLoadGameObject` as a reusable Unity component.

### Fixed
- Reworked the script icon window with safe selection validation, responsive layout, icon removal, and reliable importer updates.
## [3.0.1] - 2026-07-25

### Added
- Added SceneExtensions for finding components within loaded Unity scenes.

## [3.0.0] - 2026-07-25

### Breaking Changes
- Reorganized Runtime scripts and updated namespaces to match the new folder structure.

## [2.0.0] - 2026-07-25

### Breaking Changes
- Renamed public namespaces and assembly definitions from Mutant to ParkMinPackages.
- Projects using the previous namespaces or assembly names must update their references.

## [0.1.0] - 2026-04-06

### This is the first release of *\<Expansion\>*.

*Short description of this release*

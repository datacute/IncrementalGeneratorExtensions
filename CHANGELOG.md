# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Converted to a source generator
- Renamed Trace to be an overload of WithTrackingName
- Renumbered the GeneratorStage Enum values

### Added

- GeneratorStage Enum and Descriptions for use with Lightweight Tracing
- CancellationToken.ThrowIfCancellationRequested overload
- Events (and counters) now support id & instance value

## [0.0.3-alpha] - 2025-07-19

### Fixed

- Referenced the NuGet specific README

## [0.0.2-alpha] - 2025-07-19

### Added

- NuGet specific README

## [0.0.1-alpha] - 2025-07-19

### Added

- Initial release of the IncrementalGeneratorExtensions NuGet package.
- Provides source files to add additional functionality for incremental source generators in .NET.

---

[Unreleased]: https://github.com/datacute/IncrementalGeneratorExtensions/compare/0.0.3-alpha...develop
[0.0.3-alpha]: https://github.com/datacute/IncrementalGeneratorExtensions/releases/tag/0.0.3-alpha
[0.0.2-alpha]: https://github.com/datacute/IncrementalGeneratorExtensions/releases/tag/0.0.2-alpha
[0.0.1-alpha]: https://github.com/datacute/IncrementalGeneratorExtensions/releases/tag/0.0.1-alpha

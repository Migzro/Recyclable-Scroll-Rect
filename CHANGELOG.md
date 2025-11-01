## [1.0.4] - 1-11-2025
### Bug Fixes
- Fixed an issue where calling ReloadData after scrolling with a new itemCount less than current itemCount can sometimes cause null exceptions.
- Fixed an issue where calling ReloadData in a grid with more items than itemsCount would result in an unexpected behaviour.

## [1.0.3] - 29-10-2025
### Bug Fixes
- Fixed an issue where _maxExtraVisibleRowColumnInViewPort can be -1 after reloading data.

## [1.0.2] - 24-10-2025
### Bug Fixes
- Fixed an issue where editor scripts were being compiled when building the project.

## [1.0.1] - 21-10-2025
### Bug Fixes
- Fixed an issue where a non visible item that was reloading would cause a null exception.
- Updated README code snippet for better clarity.
- Adjusted some of the sample scenes.

## [1.0.0] - 20-10-2025
### First Release
- Initial release of the project with core features implemented.
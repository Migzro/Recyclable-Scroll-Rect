## [1.1.0] - 29-10-2025
### New Features
- Vastly improved scrolling and added support for scrolling with DoTween and Prime Tween.
- Added two new sample scenes for DoTween and Prime Tween integration.
- Changed how size and position calculations are done which should result in a small performance improvement.
### API Changes
- Removed UseConstantScrollingSpeed && ConstantScrollSpeed
- Changed ScrollToItem to ScrollToItemAtIndex for better clarity and adjusted its paramaters.
- Removed IPageSource functions PageFocused and PageUnfocused as they were confusing and didn't add much value.
- Added IRSRDataSource which replaces IDataSource for all non Paging and Grid RSRs.
- Moved IsItemSizeKown & GetItemSize to IRSRDataSource as they are not needed for Grid RSRs.
- Added IGridDataSource which extends IDataSource for Grid RSRs.
- Renamed IPageSource to IPageDataSource.
- Renamed SetItemPosition to RestoreItemPosition
- Please re-import any RSR samples if any exist in your current project.

## [1.0.3] - 29-10-2025
### Bug Fixes
- Fixed an issue where _maxExtraVisibleRowColumnInViewPort can be -1 after reloading data

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
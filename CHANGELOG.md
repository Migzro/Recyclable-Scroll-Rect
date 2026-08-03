## [1.2.0] - 3-8-2026
### New Features
- Added support for sections, headers, and footers with the ability to pin any header with support for header and footer prototypes.
- Added support for game objects in the scene as prototype items instead of using static items.
- Added support for Unity's new Input System in the samples.
- Added new samples to show case how to use sections, headers, and footers.

### Bug Fixes
- Fixed an issue with ReloadData where currently visible items were not reloading correctly.

### API Changes
- Added a new interface ISectionsSource which enables sections in addition to the base interface.
- IDataSource methods now use ItemData instead of itemIndex to support sections and item types.
```
public class ItemData
{
    public ItemType itemType;
    public int sectionIndex;
    public int itemIndex;
}
```
These methods have had their signatures updated to use ItemData instead of itemIndex:
```
GameObject GetItemPrototype(ItemData itemData);
void SetItemData(IItem item, ItemData itemData);
void ItemCreated(IItem item, GameObject itemGo, ItemData itemData);
void ItemHidden(IItem item, ItemData itemData);
void ScrolledToItem(IItem item, ItemData itemData);
bool IgnoreContentPadding(ItemData itemData);
float GetItemSize(ItemData itemData);
void PageWillFocus(IItem item, ItemData itemData, bool isNextPage);
void PageWillUnFocus(IItem item, ItemData itemData, bool isNextPage);
```
- Removed these functions from IDataSource as they are no longer needed:
```
int ItemsCount { get; }
bool IsItemStatic(int itemIndex);
```
- Added new functions to IDataSource for sections and item types:
```
int GetSectionsCount(int SectionIndex);
```
- Added a new function in IGridSource GetHeaderFooterSize which sets the size of the header and footer for a given section that is different from each grid item size.
```
float GetHeaderFooterSize(ItemData itemData);
```
## [1.1.3] - 02-6-2026
### New Features
- Added a new sample - Images Grid RSR

## [1.1.0] - 11-1-2025
### New Features
- Vastly improved scrolling and added support for scrolling with DoTween and Prime Tween.
- Added two new sample scenes for DoTween and Prime Tween integration.
- Changed how size and position calculations are done which should result in a small performance improvement.
- Multiple items can now show and hide per frame.

### Bug Fixes
- Fixed an issue with an item reloading while not being visible.
- Content position now clamps after reloading data.
- Fixed a bug where reloading RSRGrid to 0 items would cause a null exception.

### API Changes
- ReloadData when IsItemSizeKnown => false will cause RSR to scroll to the top.
- ReloadItem now only takes one variable (int itemIndex).
- Added IsItemFullyVisible (int itemIndex).
- Added IsItemPartiallyVisible (int itemIndex).
- Removed UseConstantScrollingSpeed && ConstantScrollSpeed
- Changed ScrollToItem to ScrollToItemAtIndex for better clarity and adjusted its paramaters.
- Removed IPageSource functions PageFocused and PageUnfocused as they were confusing and didn't add any functionality.
- Added IRSRDataSource which replaces IDataSource for all non Paging and Grid RSRs.
- Moved IsItemSizeKnown & GetItemSize to IRSRDataSource as they are not needed for Grid RSRs.
- Added IGridDataSource which extends IDataSource for Grid RSRs.
- Renamed IPageSource to IPageDataSource.
- Renamed SetItemPosition to RestoreItemPosition
- Please re-import any RSR samples if any exist in your current project.

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
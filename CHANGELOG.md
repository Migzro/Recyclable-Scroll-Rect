## [2.1.0] - 1-11-2026
### Improvements
- Lowered the minimum Unity version to 2019.4 by using C# 7.3 only.
- The package now declares its com.unity.ugui dependency, and links to its documentation, changelog and license from the Package Manager.
- Smaller demo GIFs and a reworked README with a Quick Start.

### Bug Fixes
- A scroll animation controller that was destroyed is now replaced when initializing, instead of being kept.

## [2.0.1] - 30-10-2026
### New Features
- Added carousel mode with RSRCarousel, which keeps the content still and moves the items instead, with the focused item centered.
- Added a loop for carousels, on by default, which wraps from the last item back to the first.
- Added a layout hook (LayoutItem) and ICarouselDataSource.ItemOffsetChanged to build custom carousel layouts and effects.
- Added RSRCards, a carousel where the cards overlap around the focused card, with three modes: Slide Away, where swiped cards leave the screen, Tuck Under, where they slide out and tuck back under the stack, and Fan, where the cards spread out on both sides of the focused card with each one peeking out. Fan can fill the viewport with cards and pile the cards that don't fit at its edges. The inspector only shows the settings of the selected mode.
- Added Horizontal Carousel RSR, Vertical Carousel RSR, Horizontal Cards RSR and Vertical Cards RSR samples, with Vertical Carousel RSR and Vertical Cards RSR demos in the README.

### Bug Fixes
- Fixed an issue where ReloadData reset horizontal scroll rects back to the start.
- Set the minimum Unity version to 2021.2, the first version that supports the C# features the package uses.

### API Changes
- RSRCarousel ignores headers and footers from ISectionsSource and logs a warning.
- New overridable members: RSRBase.ScrollPosition, RSRBase.GetScrollToItemTarget, RSRBase.ClampContentPosition, RSRBase.HandlesScrollInLateUpdate, RSR.StartPadding, RSR.EndPadding and RSR.ExtraItemsVisible, RSRPages.IsNextPage. RSRPages._currentPage is now protected.

## [1.3.0] - 30-10-2026
### Bug Fixes
- Fixed an issue where starting a scroll re-ran the previous scroll's finished callback with a canceled state
- Fixed an issue where calling Initialize more than once reloaded the data multiple times on resolution change
- Fixed an issue where destroying a scroll rect during scene unload could create a new ScreenResolutionDetector
- Guard HideItemAtIndex against items that are not visible

### Performance
- Cache section header indices so header pinning no longer scans all items every frame

### API Changes
- ScreenResolutionDetector moved into the RecyclableScrollRect namespace, code referencing it needs `using RecyclableScrollRect;`

## [1.2.2] - 27-8-2026
### Bug Fixes - Courtesy of @BlueVolt1
- Guard PinHeaders against empty or out of range item data
- Always clear the pinned header state when unpinning
- Use the scroll rect header index when checking if it is already pinned
- Clamp the grid min visible indices when the item count shrinks

## [1.2.1] - 5-8-2026
### Bug Fixes
- Fixed an issue with flexible grid constraint calculation (Issue #6)
- Fixed an issue with ScrollToIndex coroutine not cancelling properly (Issue #5)
- Fixed an issue with AverageItemSize calculation resulting in 0 if item sizes are still not set (Issue #5)
- Fixed an issue where scene prototype items were being pooled (Issue #4)

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
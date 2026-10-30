# Recyclable Scroll Rect for Unity UI

[![openupm](https://img.shields.io/npm/v/com.migzro.recyclablescrollrect?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.migzro.recyclablescrollrect/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)
![Unity 2021.2+](https://img.shields.io/badge/Unity-2021.2%2B-black?logo=unity)
[![GitHub stars](https://img.shields.io/github/stars/Migzro/Recyclable-Scroll-Rect?style=social)](https://github.com/Migzro/Recyclable-Scroll-Rect)

A virtualized ScrollRect for Unity UI (uGUI) that recycles item views, so even very long lists only keep the visible items alive.
Lists, grids, sections, paging, looping carousels and card stacks, all driven by one data source interface.

<p align="center">
  <img src="Images~/Vertical%20Cards%20RSR.gif" alt="Vertical Cards RSR" width="640">
</p>

## 📘 Features
**Layouts**
- Vertical and horizontal lists, with reverse arrangement.
- Grids with a configurable start axis and start corner.
- Sections with headers and footers, which can be pinned.
- Paged scrolling, one item at a time (like TikTok).

**Carousels and cards**
- Carousel mode that moves the items instead of the content, keeps the focused item centered, and can loop.
- Card stacks with Slide Away, Tuck Under and Fan modes.
- A layout hook for building your own carousel layouts and effects.

**Performance**
- Reuses (recycles) item views instead of creating and destroying them.
- Extra visible items for smoother fast scrolling.

**Data**
- Multiple item prototypes in the same list.
- Items with unknown sizes, measured at runtime.
- Reload the whole data source or single items.
- Animated scrolling to any item, with optional DOTween or PrimeTween easing.

## 🎬 Demo
<table>
  <tr>
    <td align="center"><b>Vertical Carousel RSR</b><br><img src="Images~/Vertical%20Carousel%20RSR.gif" alt="Vertical Carousel RSR"></td>
    <td align="center"><b>Vertical Cards RSR</b><br><img src="Images~/Vertical%20Cards%20RSR.gif" alt="Vertical Cards RSR"></td>
  </tr>
  <tr>
    <td align="center"><b>Vertical RSR</b><br><img src="Images~/Vertical%20RSR.gif" alt="Vertical RSR"></td>
    <td align="center"><b>Horizontal RSR</b><br><img src="Images~/Horizontal%20RSR.gif" alt="Horizontal RSR"></td>
  </tr>
  <tr>
    <td align="center"><b>Vertical RSR With Sections</b><br><img src="Images~/Vertical%20RSR%20Sections.gif" alt="Vertical RSR With Sections"></td>
    <td align="center"><b>Grid RSR</b><br><img src="Images~/Grid%20RSR.gif" alt="Grid RSR"></td>
  </tr>
  <tr>
    <td align="center"><b>Paged RSR</b><br><img src="Images~/Paged%20RSR.gif" alt="Paged RSR"></td>
    <td align="center"><b>Vertical Dynamic RSR with Data Source Reloading</b><br><img src="Images~/Vertical%20Dynamic%20RSR.gif" alt="Vertical Dynamic RSR"></td>
  </tr>
  <tr>
    <td align="center" colspan="2"><b>Images Grid RSR</b><br><img src="Images~/Images%20Grid%20RSR.gif" alt="Images Grid RSR"></td>
  </tr>
</table>

## 🚀 Installation
### 1 - OpenUPM
#### Option 1 — Install via OpenUPM CLI

If you have the [OpenUPM CLI](https://github.com/openupm/openupm-cli) installed, run this command in your Unity project root:

```bash
openupm add com.migzro.recyclablescrollrect
```

#### Option 2 — Install manually via Unity Package Manager

- Open 'Edit / Project Settings / Package Manager'.
- Add a new Scoped Registry with 
  - Name: `OpenUPM` 
  - URL: `https://package.openupm.com` 
  - Scope(s): `com.migzro.recyclablescrollrect`
- Go to 'Window / Package Manager / Packages / My Registries'.
- Install the Recyclable Scroll Rect package.

You can also add it manually by editing your `manifest.json` file:

```json
"scopedRegistries": [
    {
        "name": "OpenUPM",
        "url": "https://package.openupm.com",
        "scopes": [
          "com.migzro.recyclablescrollrect"
        ]
    }
]
```

### 2 - Unity Package
Download the latest Unity package from the [Releases](https://github.com/Migzro/Recyclable-Scroll-Rect/releases/latest) page.

## 🧩 Samples
### Importing from Unity Package Manager
After installing via Package Manager, go to `Window > Package Manager`, select `Recyclable Scroll Rect`, and click `Samples` tab to import any demo.

![Unity Package Manager](Images~/Unity%20Package%20Manager%20Samples.png)

### Importing from Unity Package
Open the sample scenes located in `Assets/Recyclable Scroll Rect/Samples` to see examples of vertical, horizontal, grid, paged, carousel and card stack layouts.

## ⚙️ Quick Start
1. Add an `RSR` component to your scroll view in place of Unity's `ScrollRect`, and assign its Viewport and Content.
2. Make an item prefab with a component that implements `IItem`, or add `BaseItem` to it.
3. Implement `IRSRDataSource` and call `Initialize`:

```csharp
public class MyList : MonoBehaviour, IRSRDataSource
{
    [SerializeField] private RSR _scrollRect;
    [SerializeField] private GameObject[] _prototypeItems;

    private readonly List<string> _items = new List<string>();

    public GameObject[] PrototypeItems => _prototypeItems;
    public bool IsItemSizeKnown => true;

    private void Start()
    {
        for (var i = 0; i < 10000; i++)
            _items.Add($"Item {i}");
        _scrollRect.Initialize(this);
    }

    public int GetItemsCount(int sectionIndex) => _items.Count;
    public float GetItemSize(ItemData itemData) => 100f;
    public GameObject GetItemPrototype(ItemData itemData) => _prototypeItems[0];

    // called whenever an item view is shown or recycled, fill it with the data at this index
    public void SetItemData(IItem item, ItemData itemData)
    {
        (item as DemoItemPrototype)?.Initialize(_items[itemData.itemIndex]);
    }

    // optional callbacks, leave them empty if you don't need them
    public void ItemCreated(IItem item, GameObject itemGo, ItemData itemData) { }
    public void ItemHidden(IItem item, ItemData itemData) { }
    public void ScrolledToItem(IItem item, ItemData itemData) { }
    public bool IgnoreContentPadding(ItemData itemData) => false;
    public void PullToRefresh() { }
    public void PushToClose() { }
    public void ReachedScrollStart() { }
    public void ReachedScrollEnd() { }
    public void LastItemIsVisible() { }
}
```

Use `RSRGrid` with `IGridDataSource` for grids, `RSRPages` with `IPageDataSource` for paging, and add `ISectionsSource` for sections, headers and footers. The samples cover each of them.

### Carousel mode
Add `RSRCarousel` instead of `RSR` and implement `IPageDataSource`, or `ICarouselDataSource` to get each item's offset from the center for effects like scale or fade. The carousel keeps the content still and moves the items, the focused item is centered, and the **Loop** toggle wraps from the last item back to the first. Headers and footers are not supported in carousel mode.

For overlapping cards, use `RSRCards` instead, and pick a mode: **Slide Away** stacks the next cards behind the focused one and slides swiped cards off the screen, **Tuck Under** tucks swiped cards back under the stack, and **Fan** spreads the cards out on both sides of the focused one. The **Horizontal Cards RSR** sample shows it.

For other layouts, derive from `RSRCarousel` and override `LayoutItem`.

## 🧪 Why Use This?
For large lists or grids (hundreds/thousands of items), regular UI instantiation is heavy.  
This component **reuses** item views to keep performance smooth and memory low.

## 🧩 Example Use Cases
- Chat message lists
- Inventories
- Infinite scrolling feeds
- Thumbnail grids
- Carousels and swipeable card stacks

## 📝 Release Notes
See `CHANGELOG.md` for details.

## 📄 License
MIT License — see `LICENSE.md`.

## 🙋 Support
Open an issue on GitHub for help or feature requests.

## ☕ Support Me
If this tool helped you, consider buying me a coffee:

<a href="https://www.buymeacoffee.com/migzro" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-yellow.png" alt="Buy Me A Coffee" style="height: 60px !important;width: 217px !important;" ></a>
---
Thanks for using **Recyclable Scroll Rect** — happy scrolling! 🎉

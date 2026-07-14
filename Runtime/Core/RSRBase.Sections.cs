using UnityEngine;

namespace RecyclableScrollRect
{
    public partial class RSRBase
    {
        private int _sectionsCount;
        private int _currentSection;
        
        protected PinnedHeaderState _rsrHeaderPin;
        protected PinnedHeaderState _sectionHeaderPin;
        
        private float RSRHeaderCoverage => _rsrHeaderPin.IsPinned ? _itemPositions[_rsrHeaderPin.index].itemSize[_axis] + _spacing[_axis] : 0f;
        
        private void UnpinHeaders()
        {
            if (_rsrHeaderPin.IsPinned)
            {
                var rsrHeaderPosition = _itemPositions[_rsrHeaderPin.index];
                if (_contentTopLeftCorner[_axis] < rsrHeaderPosition.absTopLeftPosition[_axis])
                {
                    UnpinHeader(_rsrHeaderPin);
                }
            }

            if (_sectionHeaderPin.IsPinned)
            {
                var sectionHeaderPosition = _itemPositions[_sectionHeaderPin.index];
                if (_contentTopLeftCorner[_axis] < sectionHeaderPosition.absTopLeftPosition[_axis] - RSRHeaderCoverage)
                {
                    UnpinHeader(_sectionHeaderPin);
                }
            }
        }
        
        private void UnpinHeader(PinnedHeaderState headerPin)
        {
            if (!headerPin.IsPinned)
                return;
            
            var currentHeaderPosition =  _itemPositions[headerPin.index];
            var currentHeaderSection = _itemData[headerPin.index].sectionIndex;
            Debug.Log($"Unpinning current header {headerPin.index} " + _contentTopLeftCorner[_axis] + " " + currentHeaderPosition.absTopLeftPosition[_axis] + " " + _currentSection + " " + currentHeaderSection);
            if (_visibleItems.TryGetValue(headerPin.index, out var headerItem))
            {
                headerItem.transform.SetParent(content, true);
                headerItem.transform.SetSiblingIndex(headerPin.index);
                RestoreItemPosition(headerPin.index);
                // hide the header as if its no longer in the viewport and the section has changed
                if (headerPin.index < _minExtraVisibleRowColumnInViewPort && _currentSection != currentHeaderSection)
                {
                    HideItemAtIndex(headerPin.index);
                }
                headerPin.index = -1;
            }
        }

        private void PinHeaders()
        {
            _currentSection = _itemData[GetEffectiveMinVisibleIndex()].sectionIndex;
            
            if (_sectionsSource == null)
                return;
            
            if (_sectionsSource.ScrollRectHasHeader && _sectionsSource.ScrollRectHeaderIsPinned && !_rsrHeaderPin.IsPinned)
                PinHeader(_rsrHeaderPin, true);
            
            if (_sectionsSource.SectionHasHeader(_currentSection) && _sectionsSource.HeaderIsPinned(_currentSection))
                PinHeader(_sectionHeaderPin, false);
        }
        
        private int GetEffectiveMinVisibleIndex()
        {
            if (!_rsrHeaderPin.IsPinned)
                return _minVisibleRowColumnInViewPort;

            var rsrHeaderCoverage = RSRHeaderCoverage;
            var effectiveContentTop = _contentTopLeftCorner[_axis] + rsrHeaderCoverage;

            var index = _minVisibleRowColumnInViewPort;
            while (index < _itemsCount - 1 && effectiveContentTop >= _itemPositions[index].absBottomRightPosition[_axis])
            {
                index++;
            }
            return index;
        }

        private void PinHeader(PinnedHeaderState headerPin, bool isRSRHeader)
        {
            var currentHeaderIndex = GetHeaderIndexFromSection(_currentSection);
            if (headerPin.index == currentHeaderIndex)
                return;

            var headerIndex = isRSRHeader ? 0 : GetHeaderIndexFromSection(_currentSection);
            if (!ShouldBePinned(headerIndex, isRSRHeader))
                return;

            if (!_visibleItems.ContainsKey(headerIndex))
                ShowItemAtIndex(headerIndex);
            
            _visibleItems.TryGetValue(headerIndex, out var headerItem);
            headerItem.transform.SetParent(viewport, true);
            headerItem.transform.SetAsLastSibling();

            var headerPosition = _itemPositions[headerIndex];
            var newItemPosition = headerPosition.topLeftPosition;
            if (vertical)
            {
                if (isRSRHeader || !_rsrHeaderPin.IsPinned)
                {
                    newItemPosition.y = -_padding.top;
                }
                else
                {
                    newItemPosition.y = -_itemPositions[_rsrHeaderPin.index].absBottomRightPosition.y - _spacing[_axis];
                }
            }
            else
            {
                if (isRSRHeader || !_rsrHeaderPin.IsPinned)
                {
                    newItemPosition.x = _padding.left;
                }
                else
                {
                    newItemPosition.x = _itemPositions[_rsrHeaderPin.index].absBottomRightPosition.x + _spacing[_axis];
                }
            }
            headerItem.transform.anchoredPosition = newItemPosition;
            
            UnpinHeader(_sectionHeaderPin);
            headerPin.index = headerIndex;
            Debug.Log($"Pinning current header {headerPin.index}");
        }
        
        private bool ShouldBePinned(int headerIndex, bool isRSRHeader)
        {
            var itemPosition = _itemPositions[headerIndex];
            var isFirstHeader = (!isRSRHeader && !_rsrHeaderPin.IsPinned) || isRSRHeader;
            if (isFirstHeader && _contentTopLeftCorner[_axis] >= itemPosition.absTopLeftPosition[_axis])
            {
                return true;
            }

            if (!isFirstHeader && _contentTopLeftCorner[_axis] >= itemPosition.absTopLeftPosition[_axis] - RSRHeaderCoverage)
            {
                return true;
            }
            return false;
        }
        
        private int GetHeaderIndexFromSection(int sectionIndex)
        {
            for (int i = 0; i < _itemData.Count; i++)
            {
                var data = _itemData[i];
                if (data.sectionIndex == sectionIndex && data.itemType == ItemType.Header)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}

// Copyright (c) 2026 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UnityEngine;

namespace RecyclableScrollRect
{
    public interface ISectionsSource
    {
        int SectionsCount { get; }
        bool ScrollRectHasHeader { get; }
        bool ScrollRectHasFooter { get; }
        bool ScrollRectHeaderIsPinned { get; }
        bool SectionHasHeader(int sectionIndex);
        bool SectionHasFooter(int sectionIndex);
        bool HeaderIsPinned (int sectionIndex);
    }
}
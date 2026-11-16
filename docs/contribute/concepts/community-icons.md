---
title: Community icons
description: Understand how OpenMyo harvests the yotoicons.com catalog, searches it offline and falls back to it when Yoto's own icons fit poorly.
ms.topic: concept-article
ms.date: 11/16/2026
---

# Community icons

yotoicons.com is a free community site with many more icons than Yoto's catalog. To keep searches local
and the load on the site low, OpenMyo downloads its whole listing once and searches it offline.

- [`CommunityCatalogHarvester`](../../../src/OpenMyo.Core/CommunityIcons/CommunityCatalogHarvester.cs) reads
  page 1 to learn the total, then the other pages a few at a time, retrying failures. Pages that still fail
  are skipped and the catalog is marked incomplete.
- [`CommunityCatalogProvider`](../../../src/OpenMyo.Core/CommunityIcons/CommunityCatalogProvider.cs) loads the
  saved catalog while it is fresh, otherwise harvests. All callers share one in-flight load. An incomplete
  catalog is used but harvested again on the next launch.
- The catalog reuses the same index as Yoto's icons, and for automatic matching multiplies the score by
  *focus* (how much of the icon's own wording the title matched). A focused `fence` icon beats a song-lyric
  icon that happens to contain "fence". Ties go to the most downloaded icon.
- [`CommunityIconFallback`](../../../src/OpenMyo.Core/Icons/CommunityIconFallback.cs) uses a community icon only
  when it is a strong match and beats Yoto's best for that title (ties go to Yoto, since its icons need no
  upload). If the site is unreachable, it returns nothing rather than failing.
- The importer downloads the chosen PNG and uploads it as the user's own icon, at most once per icon per run.

## Related content

- [Icon matching](icon-matching.md)
- [AI icon search](ai-icon-search.md)
- [Cross-cutting concerns](../architecture/cross-cutting-concerns.md#caching)

---
title: Improve icon matches
description: Fix a poor automatic icon match by adding a failing test, then adjusting synonyms, weak words, query cleaning or scoring thresholds in order.
ms.topic: how-to
ms.date: 11/16/2026
---

# Improve icon matches

In this article, you fix a track title that gets a poor icon, using the smallest change that works.

## Steps

1. Add the failing title as a test case in
   [`IconAutoAssignerTests`](../../../tests/OpenMyo.Core.Tests/Icons/IconAutoAssignerTests.cs) or
   [`IconQueryTests`](../../../tests/OpenMyo.Core.Tests/Text/IconQueryTests.cs).

1. Make the smallest change that fixes it, in this order of preference:

   1. A related word is missing: add a line to [`Synonyms.txt`](../../../src/OpenMyo.Core/Text/Synonyms.txt).
   1. A filler word is winning: add it to [`WeakWords.txt`](../../../src/OpenMyo.Core/Text/WeakWords.txt),
      sorted, lower case, singular.
   1. Title noise reaches the query: extend [`IconQuery`](../../../src/OpenMyo.Core/Text/IconQuery.cs).
   1. Otherwise, change the weights or thresholds in [`IconScorer`](../../../src/OpenMyo.Core/Icons/IconScorer.cs) or [`IconSuggester`](../../../src/OpenMyo.Core/Icons/IconSuggester.cs).

## Related content

- [Icon matching](../concepts/icon-matching.md)
- [Community icons](../concepts/community-icons.md)
- [Add domain logic](add-domain-logic.md)

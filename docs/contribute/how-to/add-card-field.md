---
title: Add a field to a card or chapter
description: Store a new field on a Yoto card or chapter by extending the domain record, the mapper and the composers without losing unknown fields.
ms.topic: how-to
ms.date: 11/16/2026
---

# Add a field to a card or chapter

In this article, you add a new property to a card or chapter so OpenMyo reads it from Yoto and writes it back on save, without dropping fields it doesn't model.

## Steps

1. Add the property to the record in [`Core/Playlists`](../../../src/OpenMyo.Core/Playlists).

1. Read it in [`CardMapper`](../../../src/OpenMyo.Yoto/Content/CardMapper.cs).

1. Write it in [`CardComposer`](../../../src/OpenMyo.Yoto/Content/CardComposer.cs) or
   [`ChapterComposer`](../../../src/OpenMyo.Yoto/Content/ChapterComposer.cs) by setting that one field on the
   cloned node.

   > [!IMPORTANT]
   > Never build the node from scratch. A save replaces the whole card, so a rebuilt node deletes fields OpenMyo doesn't model. See [Read and save cards](../concepts/read-and-save-cards.md).

1. Add a round-trip case to [`CardComposerTests`](../../../tests/OpenMyo.Yoto.Tests/Content/CardComposerTests.cs)
   with the sample JSON in [`Samples`](../../../tests/OpenMyo.Yoto.Tests/Content/Samples.cs), and assert that an
   unrelated field is still present after the save.

## Related content

- [Card model](../concepts/card-model.md)
- [Read and save cards](../concepts/read-and-save-cards.md)
- [Invariants](../architecture/invariants.md)

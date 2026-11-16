---
title: Call a new Yoto endpoint
description: Add DTOs, make an authenticated and retried call to a new Yoto API endpoint, validate ids, register the service and test it with stubbed HTTP.
ms.topic: how-to
ms.date: 11/16/2026
---

# Call a new Yoto endpoint

In this article, you add a call to a Yoto API endpoint that gets retries and authentication for free. Every change also follows [CODESTANDARDS.md](../../../CODESTANDARDS.md) and passes the checks in [CONTRIBUTING.md](../../../CONTRIBUTING.md#running-checks).

## Steps

1. Add request and response DTOs in [`Yoto/Json`](../../../src/OpenMyo.Yoto/Json) and register each type with a
   `[JsonSerializable]` line on [`YotoJsonContext`](../../../src/OpenMyo.Yoto/Json/YotoJsonContext.cs).
   Serialisation is source-generated, so an unregistered type fails at runtime.

1. Make the call in the class that owns the area (cards, media or icons; see [`YotoPlaylistRepository`](../../../src/OpenMyo.Yoto/Content/YotoPlaylistRepository.cs) for the pattern). Create the client with
   `httpClientFactory.CreateClient(YotoHttpClients.Api)` to get retries and auth, and read the response
   with [`YotoResponse`](../../../src/OpenMyo.Yoto/Http/YotoResponse.cs).

1. Check any id from outside with [`CardIds`](../../../src/OpenMyo.Yoto/Content/CardIds.cs) before it goes into a URL, and escape query values.

1. If Core needs the feature, add a method to the Core interface (or a new interface in
   [`Core/Abstractions`](../../../src/OpenMyo.Core/Abstractions)) and register the implementation in
   [`AddYoto`](../../../src/OpenMyo.Yoto/ServiceCollectionExtensions.cs).

1. Test it with [`StubHttpHandler`](../../../tests/OpenMyo.Yoto.Tests/Support/StubHttpHandler.cs) in
   [`tests/OpenMyo.Yoto.Tests`](../../../tests/OpenMyo.Yoto.Tests).

1. If it needs a new OAuth scope, add it to [`YotoAuthEndpoints.Scope`](../../../src/OpenMyo.Yoto/Auth/YotoAuthEndpoints.cs). Existing users are asked to sign in
   again on next launch (see [Sign-in and tokens](../concepts/sign-in-and-tokens.md)).

## Related content

- [Yoto HTTP pipeline](../concepts/yoto-http-pipeline.md)
- [Sign-in and tokens](../concepts/sign-in-and-tokens.md)
- [Add a field to a card or chapter](add-card-field.md)

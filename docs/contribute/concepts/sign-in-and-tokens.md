---
title: Sign-in and tokens
description: Understand OpenMyo's OAuth sign-in with PKCE, token storage, and the locks that keep single-use refresh tokens safe.
ms.topic: concept-article
ms.date: 11/16/2026
---

# Sign-in and tokens

## Sign-in flow

Each user registers their own public OAuth client at yoto.dev and pastes its id into Settings. There is no
client secret. Sign-in is Authorization Code + PKCE with a loopback redirect:

```mermaid
sequenceDiagram
    accTitle: Signing in with PKCE and a loopback redirect
    accDescr: The app starts a local listener and opens Yoto's sign-in page in the browser. Yoto redirects back to the listener with a code, which the app exchanges for tokens and stores the refresh token.
    participant A as App
    participant L as Loopback listener
    participant B as System browser
    participant Y as Yoto login
    participant S as Secret store

    A->>A: PKCE verifier, challenge, state
    A->>L: Start listening on /callback
    L->>B: Open authorize URL
    B->>Y: User signs in and approves
    Y->>B: Redirect to /callback?code&state
    B->>L: GET /callback
    L-->>A: code (state checked)
    A->>Y: code + verifier
    Y-->>A: access token, refresh token
    A->>S: Store refresh token and granted scope
```

The listener ignores requests whose `state` does not match and gives up after a timeout. The loopback
redirect URI is fixed, because the user must register it with their OAuth client.

## Token ownership

[`YotoSession`](../../../src/OpenMyo.Yoto/Auth/YotoSession.cs) owns the tokens afterwards:

- The **access token** lives only in memory. It is refreshed shortly before expiry, or when a request gets
  a 401.
- The **refresh token** lives in the secret store. **Yoto refresh tokens are single-use:** each refresh
  returns a new one, and the old one stops working. Two refreshes with the same token sign the user out.
  So every refresh is serialised twice:
  - within the process by a `SemaphoreSlim`;
  - across processes (the app and the Claude Desktop connector can run at once) by
    [`RefreshLock`](../../../src/OpenMyo.Yoto/Auth/RefreshLock.cs), an exclusively opened file in the data
    folder. The OS releases it if a process dies.

  The refresh token is read *inside* both locks, because the other process may have just replaced it.
- `RefreshAccessTokenAsync(rejectedToken)` only refreshes if the current token is still the rejected one.
  So ten requests that all get a 401 cause one refresh, not ten.

The diagram shows why the refresh token is read inside the lock: when the app and the connector both
need a new token, the second one finds the token already replaced and uses the new one.

```mermaid
sequenceDiagram
    accTitle: Refreshing a single-use token from two processes
    accDescr: The app takes the file lock, reads the refresh token, refreshes and stores the new token. The connector waits for the lock, then reads the new token, so the old one is never used twice.
    participant App
    participant Lock as Refresh lock file
    participant Store as Secret store
    participant Yoto as Yoto login
    participant Conn as Connector

    App->>Lock: Acquire
    Conn->>Lock: Acquire (waits)
    App->>Store: Read refresh token R1
    App->>Yoto: Refresh with R1
    Yoto-->>App: Access token + refresh token R2
    App->>Store: Store R2
    App->>Lock: Release
    Lock-->>Conn: Acquired
    Conn->>Store: Read refresh token R2
    Conn->>Yoto: Refresh with R2
    Note over App,Conn: Reading R1 outside the lock would send it twice and sign the user out
```
- A 400/401/403 from the token endpoint clears the session (the user must sign in again).
- `TryRestoreAsync` also compares the stored scope with the current scope list. If the app now asks for
  more scopes, the old token is dropped, because refreshing cannot widen a grant.

## Related content

- [Yoto HTTP pipeline](yoto-http-pipeline.md)
- [App start-up and composition](app-startup.md)
- [Claude Desktop connector](claude-desktop-connector.md)
- [Cross-cutting concerns](../architecture/cross-cutting-concerns.md)

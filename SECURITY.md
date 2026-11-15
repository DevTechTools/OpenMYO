# Security Policy

## Reporting a vulnerability

Please do not open a public issue. Use GitHub's private vulnerability reporting: open the
repository's **Security** tab and choose **Report a vulnerability**. Include the affected version or
commit, reproduction steps and the impact you see. We aim to acknowledge reports within a few days.

## In scope

- Handling of the Yoto refresh token and Anthropic API key (storage in Keychain / DPAPI, logging,
  leakage into settings, URLs or crash output).
- The OAuth Authorization Code + PKCE flow and the loopback redirect listener.
- Processing of untrusted input: file names, audio tags, images and API responses.
- Dependencies shipped with the app that have known vulnerabilities.

## Out of scope

- Vulnerabilities in Yoto's services or the Anthropic API; report those to the respective vendor.
- Issues that require an already-compromised local account or machine.
- Using your own credentials in ways the services' terms already prohibit.

OpenMyo is unofficial and not affiliated with Yoto. Never post tokens or API keys in issues; if you
have exposed one, revoke it at the provider immediately.

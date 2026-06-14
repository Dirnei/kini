# Kini CLI

Command-line client for a [Kini](../../) server. Signs you up, signs you in
via SSH-key challenge/response, publishes keys, and manages API tokens for
non-interactive automation.

## Build

```sh
cd src/kini-cli
go mod tidy
make build           # produces ./kini
# or:
go build -o kini .
```

Requires Go 1.20+ and `ssh-keygen` available on `PATH` (for signing
challenges). Tested on Linux and macOS.

## Quickstart

```sh
# Bootstrap an organization (generates a fresh ed25519 key under ~/.ssh)
./kini signup --org "Acme" --username alice --email alice@acme.tld --generate

# Subsequent sign-in (uses ssh-agent if the .pub is loaded there)
./kini login --email alice@acme.tld

# See where you are
./kini whoami

# Publish another key
./kini publish --key ~/.ssh/id_yubi.pub

# Owner-only: onboard a colleague without making them register
./kini identity add --username bob --email bob@acme.tld \
    --key /tmp/bob.pub

# Mint an API token for CI
./kini token create "github-actions"
```

## Configuration

Persists to `~/.config/kini/config.json` (XDG-aware via `os.UserConfigDir`).
Stores:

- `server` — Kini server URL
- `token` — current bearer session
- `email`, `username` — for command-line defaults

Override the server with `--server URL` on any command, or with the
`KINI_SERVER` environment variable.

## Commands

```
kini signup       Bootstrap a new organization
kini login        Sign in via SSH-key challenge/response
kini logout       Revoke the current session
kini whoami       Show the currently signed-in identity
kini publish      Add an SSH key to the published directory
kini keys list    List published keys
kini identity     Manage org members (owner only for `add`)
kini token        Manage API tokens for automation
```

Run any subcommand with `--help` for flags and examples.

## How sign-in works

`kini login` does the SSH-key challenge ceremony end-to-end without
copy/paste:

1. Asks the server for a nonce: `POST /v1/auth/ssh/challenge`.
2. Pipes the nonce into `ssh-keygen -Y sign -n kini -f <key>` — the
   `.pub` path lets `ssh-agent` (or `gpg-agent` emulating it, or a FIDO2
   resident key) do the actual cryptographic work. The private half
   never moves.
3. Sends the armored signature back: `POST /v1/auth/ssh/verify`.
4. Stores the returned session token in your config.

For users whose admin published a key *for* them (member without
prior registration), this same flow auto-claims the identity on first
successful sign-in.

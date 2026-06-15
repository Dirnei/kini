package ssh

import (
	"bytes"
	"fmt"
	"io"
	"net/http"
	"net/url"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
)

// AgentIdentity is one line of `ssh-add -L` output.
type AgentIdentity struct {
	Algo    string
	Blob    string // base64-encoded wire format
	Comment string
	Line    string // the original "algo blob comment" line
}

// agentIdentities returns whatever ssh-add -L reports. Empty slice (not error)
// if the agent is unreachable or has no keys.
func agentIdentities() []AgentIdentity {
	out, err := exec.Command("ssh-add", "-L").Output()
	if err != nil {
		return nil
	}
	var ids []AgentIdentity
	for _, line := range strings.Split(strings.TrimSpace(string(out)), "\n") {
		line = strings.TrimSpace(line)
		if line == "" || strings.Contains(line, "no identities") {
			continue
		}
		fields := strings.Fields(line)
		if len(fields) < 2 {
			continue
		}
		comment := ""
		if len(fields) >= 3 {
			comment = strings.Join(fields[2:], " ")
		}
		ids = append(ids, AgentIdentity{
			Algo:    fields[0],
			Blob:    fields[1],
			Comment: comment,
			Line:    line,
		})
	}
	return ids
}

// FetchPublishedSshKeys hits the unauthenticated /{username}.keys endpoint
// and returns "<algo> <blob>" strings (without comments) for each line.
func FetchPublishedSshKeys(serverURL, username string) []string {
	if username == "" || serverURL == "" {
		return nil
	}
	u := strings.TrimRight(serverURL, "/") + "/" + url.PathEscape(username) + ".keys"
	res, err := http.Get(u)
	if err != nil {
		return nil
	}
	defer res.Body.Close()
	if res.StatusCode != 200 {
		return nil
	}
	body, _ := io.ReadAll(res.Body)
	var lines []string
	for _, line := range strings.Split(strings.TrimSpace(string(body)), "\n") {
		fields := strings.Fields(strings.TrimSpace(line))
		if len(fields) >= 2 {
			lines = append(lines, fields[0]+" "+fields[1])
		}
	}
	return lines
}

// Resolved describes the key chosen for signing + how it'll be sourced
// (agent vs file), plus a cleanup func the caller must defer.
type Resolved struct {
	Path    string // path passed to ssh-keygen -Y sign -f
	Source  string // human label: "agent (cardno:...)", "agent (first)", "file ~/.ssh/id_ed25519.pub"
	Cleanup func()
}

// ResolveForSigning picks the right path for `ssh-keygen -Y sign -f`.
//
// Order of preference:
//  1. The first ssh-agent identity whose blob matches one of `publishedMatches`
//     (algo+blob strings from /{username}.keys). The matching line is written
//     to a temp .pub file — ssh-keygen falls through to the agent because
//     there's no sibling private key file on disk.
//  2. The first ssh-agent identity overall, same temp-file mechanism.
//  3. The first .pub file under ~/.ssh.
//
// This avoids the trap where the user has a file-backed key AND a YubiKey:
// using `~/.ssh/id_ed25519.pub` literally finds `~/.ssh/id_ed25519` next to
// it and prompts for the passphrase, instead of routing through the agent
// where the YubiKey lives.
func ResolveForSigning(publishedMatches []string) (Resolved, error) {
	ids := agentIdentities()

	var picked *AgentIdentity
	var source string

	// Try to match against what Kini published for this user.
	if len(publishedMatches) > 0 {
		wanted := make(map[string]struct{}, len(publishedMatches))
		for _, m := range publishedMatches {
			wanted[m] = struct{}{}
		}
		for i := range ids {
			fp := ids[i].Algo + " " + ids[i].Blob
			if _, ok := wanted[fp]; ok {
				picked = &ids[i]
				label := ids[i].Comment
				if label == "" {
					label = ids[i].Algo
				}
				source = "agent (" + label + ", matched published key)"
				break
			}
		}
	}

	// Fall back to first agent identity if no match.
	if picked == nil && len(ids) > 0 {
		picked = &ids[0]
		label := ids[0].Comment
		if label == "" {
			label = ids[0].Algo
		}
		source = "agent (" + label + ", first identity)"
	}

	if picked != nil {
		tmp, err := os.CreateTemp("", "kini-agent-*.pub")
		if err != nil {
			return Resolved{}, fmt.Errorf("create temp pubkey: %w", err)
		}
		if _, err := tmp.WriteString(picked.Line + "\n"); err != nil {
			tmp.Close()
			_ = os.Remove(tmp.Name())
			return Resolved{}, err
		}
		tmp.Close()
		path := tmp.Name()
		return Resolved{
			Path:    path,
			Source:  source,
			Cleanup: func() { _ = os.Remove(path) },
		}, nil
	}

	// No agent — fall back to a default key file on disk.
	def := DefaultKey()
	if def == "" {
		return Resolved{}, fmt.Errorf("no SSH key found: agent has no identities and no default key exists under ~/.ssh")
	}
	return Resolved{
		Path:    def,
		Source:  "file " + def,
		Cleanup: func() {},
	}, nil
}

// (Re-exposed for clarity; was previously private to sign.go.)
func defaultKeyDir() string {
	home, err := os.UserHomeDir()
	if err != nil {
		return ""
	}
	return filepath.Join(home, ".ssh")
}

var _ = defaultKeyDir // referenced indirectly by DefaultKey; kept for future use

var _ = bytes.TrimSpace // silence vet if we later drop the bytes import

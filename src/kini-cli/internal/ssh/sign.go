// Package ssh wraps ssh-keygen for the operations kini needs — signing
// challenge payloads with the user's local key (or agent-backed key) and
// generating fresh keypairs on disk.
package ssh

import (
	"bytes"
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
)

// DefaultKey returns the path of the first SSH public key file that exists
// under ~/.ssh, in conventional preference order. Empty string if none.
func DefaultKey() string {
	home, err := os.UserHomeDir()
	if err != nil {
		return ""
	}
	candidates := []string{
		"id_ed25519.pub", "id_ed25519_sk.pub",
		"id_ecdsa.pub", "id_ecdsa_sk.pub",
		"id_rsa.pub",
	}
	for _, name := range candidates {
		p := filepath.Join(home, ".ssh", name)
		if st, err := os.Stat(p); err == nil && !st.IsDir() {
			return p
		}
	}
	return ""
}

// Sign signs the given payload using ssh-keygen -Y sign, returning the
// armored signature block. The keyPath may point at a .pub file — ssh-keygen
// will then route through ssh-agent for the actual signing operation, which
// is how YubiKey / FIDO2 / gpg-agent identities work.
func Sign(keyPath, namespace, payload string) (string, error) {
	if keyPath == "" {
		return "", fmt.Errorf("no SSH key specified and no default found under ~/.ssh")
	}
	cmd := exec.Command("ssh-keygen", "-Y", "sign", "-n", namespace, "-f", keyPath)
	cmd.Stdin = bytes.NewReader([]byte(payload))
	var stdout, stderr bytes.Buffer
	cmd.Stdout = &stdout
	cmd.Stderr = &stderr
	if err := cmd.Run(); err != nil {
		return "", fmt.Errorf("ssh-keygen sign failed: %w: %s", err, stderr.String())
	}
	return stdout.String(), nil
}

// ReadPubKey loads a public key file (one authorized_keys-format line).
func ReadPubKey(path string) (string, error) {
	b, err := os.ReadFile(path)
	if err != nil {
		return "", fmt.Errorf("read %s: %w", path, err)
	}
	return string(bytes.TrimSpace(b)), nil
}

// Generate creates an ed25519 keypair via ssh-keygen at the given path
// (no passphrase, supplied comment). Returns the public key contents.
func Generate(path, comment string) (string, error) {
	if err := os.MkdirAll(filepath.Dir(path), 0o700); err != nil {
		return "", err
	}
	if _, err := os.Stat(path); err == nil {
		return "", fmt.Errorf("key already exists at %s — refusing to overwrite", path)
	}
	cmd := exec.Command("ssh-keygen", "-t", "ed25519", "-N", "", "-f", path, "-C", comment, "-q")
	var stderr bytes.Buffer
	cmd.Stderr = &stderr
	if err := cmd.Run(); err != nil {
		return "", fmt.Errorf("ssh-keygen generate failed: %w: %s", err, stderr.String())
	}
	return ReadPubKey(path + ".pub")
}

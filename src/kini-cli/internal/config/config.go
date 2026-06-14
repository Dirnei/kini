// Package config persists Kini CLI state — server URL, session token, the
// email of the currently signed-in identity — under the platform's
// per-user config directory (XDG-aware via os.UserConfigDir).
package config

import (
	"encoding/json"
	"errors"
	"os"
	"path/filepath"
)

const fileName = "config.json"

type Config struct {
	Server   string `json:"server,omitempty"`
	Token    string `json:"token,omitempty"`
	Email    string `json:"email,omitempty"`
	Username string `json:"username,omitempty"`
}

// Path returns the absolute path to the kini config file.
func Path() (string, error) {
	dir, err := os.UserConfigDir()
	if err != nil {
		return "", err
	}
	return filepath.Join(dir, "kini", fileName), nil
}

// Load reads the config from disk. A missing file is not an error — returns
// a zero-valued Config so first-run callers don't have to handle ENOENT.
func Load() (*Config, error) {
	p, err := Path()
	if err != nil {
		return nil, err
	}
	b, err := os.ReadFile(p)
	if errors.Is(err, os.ErrNotExist) {
		return &Config{}, nil
	}
	if err != nil {
		return nil, err
	}
	cfg := &Config{}
	if err := json.Unmarshal(b, cfg); err != nil {
		return nil, err
	}
	return cfg, nil
}

// Save writes the config to disk with 0600 permissions (the token is
// effectively a secret).
func Save(cfg *Config) error {
	p, err := Path()
	if err != nil {
		return err
	}
	if err := os.MkdirAll(filepath.Dir(p), 0o755); err != nil {
		return err
	}
	b, err := json.MarshalIndent(cfg, "", "  ")
	if err != nil {
		return err
	}
	return os.WriteFile(p, b, 0o600)
}

// Clear wipes credentials but keeps the server URL.
func Clear() error {
	cfg, err := Load()
	if err != nil {
		return err
	}
	cfg.Token = ""
	cfg.Email = ""
	cfg.Username = ""
	return Save(cfg)
}

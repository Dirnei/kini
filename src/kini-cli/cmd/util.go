package cmd

import (
	"bufio"
	"fmt"
	"os"
	"strings"
)

// envOrEmpty wraps os.Getenv so callers don't have to import os for a value lookup.
func envOrEmpty(key string) string { return os.Getenv(key) }

// promptLine reads a single line from stdin with a prompt. Returns trimmed string.
func promptLine(prompt string) (string, error) {
	fmt.Print(prompt)
	r := bufio.NewReader(os.Stdin)
	s, err := r.ReadString('\n')
	if err != nil {
		return "", err
	}
	return strings.TrimSpace(s), nil
}

// home returns the user's home directory or "" on failure.
func home() string {
	h, err := os.UserHomeDir()
	if err != nil {
		return ""
	}
	return h
}

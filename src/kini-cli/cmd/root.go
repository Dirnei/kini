package cmd

import (
	"github.com/spf13/cobra"

	"kini/internal/config"
)

// version is filled in at build time via `-ldflags "-X kini/cmd.version=..."`.
var version = "dev"

// Set by --server / KINI_SERVER; falls back to config / default.
var serverFlag string

var rootCmd = &cobra.Command{
	Use:   "kini",
	Short: "Hardware-friendly key directory client",
	Long: `kini is the command-line companion to a Kini server.

It signs you up, signs you in (via SSH agent / hardware key), uploads keys
to your directory, and manages API tokens for non-interactive automation.`,
	Version:           version,
	SilenceUsage:      true,
	SilenceErrors:     true,
	DisableAutoGenTag: true,
}

func init() {
	rootCmd.PersistentFlags().StringVar(&serverFlag, "server", "", "Kini server URL (overrides config / KINI_SERVER)")
}

func Execute() error {
	return rootCmd.Execute()
}

// resolveServer returns the configured server URL, honoring the precedence:
// --server flag > config file > $KINI_SERVER > default localhost.
func resolveServer(cfg *config.Config) string {
	if serverFlag != "" {
		return serverFlag
	}
	if cfg != nil && cfg.Server != "" {
		return cfg.Server
	}
	if env := envOrEmpty("KINI_SERVER"); env != "" {
		return env
	}
	return "http://localhost:5001"
}

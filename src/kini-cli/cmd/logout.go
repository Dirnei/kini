package cmd

import (
	"fmt"

	"github.com/spf13/cobra"

	"kini/internal/api"
	"kini/internal/config"
)

var logoutCmd = &cobra.Command{
	Use:   "logout",
	Short: "Revoke the current session",
	RunE: func(_ *cobra.Command, _ []string) error {
		cfg, err := config.Load()
		if err != nil {
			return err
		}
		if cfg.Token == "" {
			fmt.Println("Already signed out.")
			return nil
		}
		c := api.New(resolveServer(cfg), cfg.Token)
		// Best-effort server-side revocation. If the server is unreachable
		// we still want to clear local credentials.
		if err := c.SignOut(); err != nil {
			fmt.Fprintf(rootCmd.ErrOrStderr(), "warning: server sign-out failed (%v); clearing local token anyway.\n", err)
		}
		if err := config.Clear(); err != nil {
			return err
		}
		fmt.Println("✓ Signed out.")
		return nil
	},
}

func init() { rootCmd.AddCommand(logoutCmd) }

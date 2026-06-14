package cmd

import (
	"fmt"

	"github.com/spf13/cobra"

	"kini/internal/api"
	"kini/internal/config"
)

var whoamiCmd = &cobra.Command{
	Use:   "whoami",
	Short: "Show the currently signed-in identity",
	RunE: func(_ *cobra.Command, _ []string) error {
		cfg, err := config.Load()
		if err != nil {
			return err
		}
		if cfg.Token == "" {
			return fmt.Errorf("not signed in (try `kini login` or `kini signup`)")
		}
		c := api.New(resolveServer(cfg), cfg.Token)
		me, err := c.Me()
		if err != nil {
			return err
		}
		fmt.Printf("Username:    %s\n", me.Identity.Username)
		fmt.Printf("Email:       %s\n", me.Identity.Email)
		fmt.Printf("Role:        %s\n", me.Identity.Role)
		fmt.Printf("Organization: %s\n", me.Organization.Name)
		fmt.Printf("Server:      %s\n", cfg.Server)
		fmt.Printf("Session:     %s (expires %s)\n", me.Session.ID, me.Session.ExpiresAt.Local().Format("2006-01-02 15:04"))
		return nil
	},
}

func init() { rootCmd.AddCommand(whoamiCmd) }

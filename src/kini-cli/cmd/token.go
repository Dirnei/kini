package cmd

import (
	"fmt"
	"os"
	"text/tabwriter"

	"github.com/spf13/cobra"

	"kini/internal/api"
	"kini/internal/config"
)

var tokenCmd = &cobra.Command{
	Use:     "token",
	Aliases: []string{"tokens"},
	Short:   "Manage long-lived API tokens for the CLI / CI / automation",
}

var tokenListCmd = &cobra.Command{
	Use:   "list",
	Short: "List your API tokens",
	RunE: func(_ *cobra.Command, _ []string) error {
		cfg, err := config.Load()
		if err != nil {
			return err
		}
		c := api.New(resolveServer(cfg), cfg.Token)
		ts, err := c.ListApiTokens()
		if err != nil {
			return err
		}
		w := tabwriter.NewWriter(os.Stdout, 0, 0, 2, ' ', 0)
		fmt.Fprintln(w, "ID\tNAME\tCREATED\tLAST USED")
		for _, t := range ts {
			last := "never"
			if t.LastUsedAt != nil {
				last = t.LastUsedAt.Local().Format("2006-01-02 15:04")
			}
			fmt.Fprintf(w, "%s\t%s\t%s\t%s\n", t.ID, t.Name, t.CreatedAt.Local().Format("2006-01-02"), last)
		}
		return w.Flush()
	},
}

var tokenCreateCmd = &cobra.Command{
	Use:   "create <name>",
	Short: "Mint a new API token (plaintext shown ONCE)",
	Args:  cobra.ExactArgs(1),
	RunE: func(_ *cobra.Command, args []string) error {
		cfg, err := config.Load()
		if err != nil {
			return err
		}
		c := api.New(resolveServer(cfg), cfg.Token)
		t, err := c.CreateApiToken(args[0], nil)
		if err != nil {
			return err
		}
		fmt.Printf("✓ Minted %s (%s)\n\n", t.Name, t.ID)
		fmt.Println("Plaintext token — copy it now, it is NOT recoverable:")
		fmt.Println()
		fmt.Println("  " + t.Token)
		fmt.Println()
		fmt.Println("Use it as:  Authorization: Bearer <token>")
		return nil
	},
}

var tokenRevokeCmd = &cobra.Command{
	Use:   "revoke <id>",
	Short: "Revoke an API token by id",
	Args:  cobra.ExactArgs(1),
	RunE: func(_ *cobra.Command, args []string) error {
		cfg, err := config.Load()
		if err != nil {
			return err
		}
		c := api.New(resolveServer(cfg), cfg.Token)
		if err := c.RevokeApiToken(args[0]); err != nil {
			return err
		}
		fmt.Println("✓ Revoked.")
		return nil
	},
}

func init() {
	rootCmd.AddCommand(tokenCmd)
	tokenCmd.AddCommand(tokenListCmd)
	tokenCmd.AddCommand(tokenCreateCmd)
	tokenCmd.AddCommand(tokenRevokeCmd)
}

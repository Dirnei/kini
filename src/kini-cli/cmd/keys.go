package cmd

import (
	"fmt"
	"os"
	"text/tabwriter"

	"github.com/spf13/cobra"

	"kini/internal/api"
	"kini/internal/config"
)

var keysCmd = &cobra.Command{
	Use:   "keys",
	Short: "Inspect published keys for an identity",
}

var keysListCmd = &cobra.Command{
	Use:   "list",
	Short: "List published keys for an identity (yourself by default)",
	RunE: func(_ *cobra.Command, _ []string) error {
		cfg, err := config.Load()
		if err != nil {
			return err
		}
		email := cfg.Email
		if email == "" {
			return fmt.Errorf("not signed in (or --email not yet supported on list)")
		}
		c := api.New(resolveServer(cfg), cfg.Token)
		ks, err := c.ListKeys(email)
		if err != nil {
			return err
		}
		w := tabwriter.NewWriter(os.Stdout, 0, 0, 2, ' ', 0)
		fmt.Fprintln(w, "FINGERPRINT\tALG\tPROVENANCE\tCOMMENT")
		for _, k := range ks {
			cm := ""
			if k.Comment != nil {
				cm = *k.Comment
			}
			fmt.Fprintf(w, "%s\t%s\t%s\t%s\n", k.Fingerprint, k.Algorithm, k.Provenance, cm)
		}
		return w.Flush()
	},
}

func init() {
	rootCmd.AddCommand(keysCmd)
	keysCmd.AddCommand(keysListCmd)
}

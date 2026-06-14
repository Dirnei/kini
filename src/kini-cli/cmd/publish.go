package cmd

import (
	"fmt"

	"github.com/spf13/cobra"

	"kini/internal/api"
	"kini/internal/config"
	"kini/internal/ssh"
)

var (
	publishKey   string
	publishEmail string
)

var publishCmd = &cobra.Command{
	Use:   "publish",
	Short: "Publish an SSH public key to the directory",
	Long: `Adds an SSH public key to the published directory for an identity.

Self-publish (default): uploads for the signed-in identity.
Owner cross-publish: pass --email to publish on behalf of another member.`,
	RunE: runPublish,
}

func init() {
	rootCmd.AddCommand(publishCmd)
	publishCmd.Flags().StringVarP(&publishKey, "key", "k", "", "Path to an SSH public key (default: first key under ~/.ssh)")
	publishCmd.Flags().StringVarP(&publishEmail, "email", "e", "", "Target identity email (default: yourself)")
}

func runPublish(_ *cobra.Command, _ []string) error {
	cfg, err := config.Load()
	if err != nil {
		return err
	}
	if cfg.Token == "" {
		return fmt.Errorf("not signed in (try `kini login`)")
	}

	keyPath := publishKey
	if keyPath == "" {
		keyPath = ssh.DefaultKey()
	}
	if keyPath == "" {
		return fmt.Errorf("no SSH key specified and no default found under ~/.ssh; pass --key")
	}
	pubkey, err := ssh.ReadPubKey(keyPath)
	if err != nil {
		return err
	}

	email := publishEmail
	if email == "" {
		email = cfg.Email
	}
	if email == "" {
		return fmt.Errorf("no email known — sign in first or pass --email")
	}

	c := api.New(resolveServer(cfg), cfg.Token)
	k, err := c.PublishKey(email, pubkey)
	if err != nil {
		return err
	}
	fmt.Printf("✓ Published %s for %s.\n", k.Fingerprint, email)
	return nil
}

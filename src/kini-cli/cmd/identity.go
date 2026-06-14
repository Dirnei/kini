package cmd

import (
	"encoding/json"
	"fmt"
	"os"
	"strings"
	"text/tabwriter"

	"github.com/spf13/cobra"

	"kini/internal/api"
	"kini/internal/config"
	"kini/internal/ssh"
)

var identityCmd = &cobra.Command{
	Use:     "identity",
	Aliases: []string{"identities"},
	Short:   "Manage identities in your organization (owner only for create)",
}

var identityListCmd = &cobra.Command{
	Use:   "list",
	Short: "List identities in your organization",
	RunE:  runIdentityList,
}

var (
	addUsername   string
	addEmail      string
	addDisplay    string
	addKey        string
	addPublishGen bool
)

var identityAddCmd = &cobra.Command{
	Use:   "add",
	Short: "Add a new member identity (owner only)",
	Long: `Owner-only. Creates a new identity in your org. By default the member
has no credentials and never has to register — anyone resolving their
/{username}.keys URL still sees whatever public keys you upload for them.

If --key is set, the file is also published as the member's first directory
entry. If --generate is set, a fresh keypair is created under ~/.ssh/ first.`,
	RunE: runIdentityAdd,
}

func init() {
	rootCmd.AddCommand(identityCmd)
	identityCmd.AddCommand(identityListCmd)
	identityCmd.AddCommand(identityAddCmd)

	identityAddCmd.Flags().StringVarP(&addUsername, "username", "u", "", "Member's username (required)")
	identityAddCmd.Flags().StringVarP(&addEmail, "email", "e", "", "Member's email (required)")
	identityAddCmd.Flags().StringVarP(&addDisplay, "name", "n", "", "Member's display name (optional)")
	identityAddCmd.Flags().StringVarP(&addKey, "key", "k", "", "SSH public key file to publish for the member (optional)")
	identityAddCmd.Flags().BoolVar(&addPublishGen, "generate", false, "Generate a fresh keypair for the member under ~/.ssh")
}

func runIdentityList(_ *cobra.Command, _ []string) error {
	cfg, err := config.Load()
	if err != nil {
		return err
	}
	if cfg.Token == "" {
		return fmt.Errorf("not signed in")
	}

	c := api.New(resolveServer(cfg), cfg.Token)
	me, err := c.Me()
	if err != nil {
		return err
	}
	list, err := c.ListIdentities(me.Identity.OrgID)
	if err != nil {
		return err
	}

	w := tabwriter.NewWriter(os.Stdout, 0, 0, 2, ' ', 0)
	fmt.Fprintln(w, "USERNAME\tEMAIL\tROLE\tDISPLAY NAME\tCREATED")
	for _, i := range list {
		name := ""
		if i.DisplayName != nil {
			name = *i.DisplayName
		}
		fmt.Fprintf(w, "%s\t%s\t%s\t%s\t%s\n", i.Username, i.Email, i.Role, name, i.CreatedAt.Local().Format("2006-01-02"))
	}
	return w.Flush()
}

func runIdentityAdd(_ *cobra.Command, _ []string) error {
	if addUsername == "" || addEmail == "" {
		return fmt.Errorf("--username and --email are required")
	}

	cfg, err := config.Load()
	if err != nil {
		return err
	}
	if cfg.Token == "" {
		return fmt.Errorf("not signed in")
	}

	c := api.New(resolveServer(cfg), cfg.Token)
	me, err := c.Me()
	if err != nil {
		return err
	}
	if me.Identity.Role != "owner" {
		return fmt.Errorf("only owners can create identities (you are %q)", me.Identity.Role)
	}

	identity, err := c.CreateMemberIdentity(me.Identity.OrgID, strings.ToLower(addUsername), addEmail, addDisplay)
	if err != nil {
		return err
	}
	fmt.Printf("✓ Created %s <%s> (role=%s)\n", identity.Username, identity.Email, identity.Role)

	// Optionally publish a key alongside.
	var pubkey string
	if addPublishGen {
		path := home() + "/.ssh/id_kini_" + identity.Username
		fmt.Printf("Generating ed25519 keypair at %s …\n", path)
		pubkey, err = ssh.Generate(path, identity.Email)
		if err != nil {
			return err
		}
	} else if addKey != "" {
		pubkey, err = ssh.ReadPubKey(addKey)
		if err != nil {
			return err
		}
	}

	if pubkey != "" {
		k, err := c.PublishKey(identity.Email, pubkey)
		if err != nil {
			return fmt.Errorf("identity created, but key upload failed: %w", err)
		}
		fmt.Printf("  Published %s — resolves at %s/%s.keys\n", k.Fingerprint, cfg.Server, identity.Username)
	}

	// Pretty-print the JSON for scripting use too.
	if jsonOutput {
		b, _ := json.MarshalIndent(identity, "", "  ")
		fmt.Println(string(b))
	}
	return nil
}

// Reused by other commands that take a global --json toggle.
var jsonOutput bool

func init() {
	rootCmd.PersistentFlags().BoolVar(&jsonOutput, "json", false, "Emit machine-readable JSON where supported")
}

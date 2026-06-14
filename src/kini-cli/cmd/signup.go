package cmd

import (
	"fmt"
	"path/filepath"
	"strings"

	"github.com/spf13/cobra"

	"kini/internal/api"
	"kini/internal/config"
	"kini/internal/ssh"
)

var (
	signupOrg       string
	signupUsername  string
	signupEmail     string
	signupDisplay   string
	signupKey       string
	signupGenerate  bool
	signupNoPublish bool
	signupDomain    string
)

var signupCmd = &cobra.Command{
	Use:   "signup",
	Short: "Bootstrap a new organization on a Kini server",
	Long: `Create an organization, an identity, and your first SSH credential in one shot.

If --generate is set, a fresh ed25519 keypair is written to ~/.ssh/id_kini_{username}
and the public half is used for sign-up. Otherwise --key (or the first key found
in ~/.ssh/) is uploaded.

By default the key is also published to the directory — pass --no-publish to keep
it private (auth-only).`,
	RunE: runSignup,
}

func init() {
	rootCmd.AddCommand(signupCmd)
	signupCmd.Flags().StringVarP(&signupOrg, "org", "o", "", "Organization name (required)")
	signupCmd.Flags().StringVarP(&signupUsername, "username", "u", "", "Your username (required; lowercase, 2-32 chars)")
	signupCmd.Flags().StringVarP(&signupEmail, "email", "e", "", "Your email (required)")
	signupCmd.Flags().StringVarP(&signupDisplay, "name", "n", "", "Display name (optional)")
	signupCmd.Flags().StringVarP(&signupKey, "key", "k", "", "Path to an SSH public key (default: first key under ~/.ssh)")
	signupCmd.Flags().BoolVar(&signupGenerate, "generate", false, "Generate a fresh ed25519 keypair instead of using an existing one")
	signupCmd.Flags().BoolVar(&signupNoPublish, "no-publish", false, "Do NOT publish the key to the directory")
	signupCmd.Flags().StringVar(&signupDomain, "domain", "", "Primary organization domain (optional)")
}

func runSignup(_ *cobra.Command, _ []string) error {
	if signupOrg == "" || signupUsername == "" || signupEmail == "" {
		return fmt.Errorf("--org, --username, and --email are all required")
	}

	cfg, _ := config.Load()
	if cfg == nil {
		cfg = &config.Config{}
	}

	var pubkey string
	switch {
	case signupGenerate:
		path := filepath.Join(home(), ".ssh", "id_kini_"+signupUsername)
		fmt.Printf("Generating ed25519 keypair at %s …\n", path)
		var err error
		pubkey, err = ssh.Generate(path, signupEmail)
		if err != nil {
			return err
		}
	case signupKey != "":
		var err error
		pubkey, err = ssh.ReadPubKey(signupKey)
		if err != nil {
			return err
		}
	default:
		def := ssh.DefaultKey()
		if def == "" {
			return fmt.Errorf("no key specified and no default found under ~/.ssh; pass --key or --generate")
		}
		var err error
		pubkey, err = ssh.ReadPubKey(def)
		if err != nil {
			return err
		}
		fmt.Printf("Using key %s\n", def)
	}

	publish := !signupNoPublish
	req := api.SignUpRequest{
		OrganizationName: signupOrg,
		PrimaryDomain:    signupDomain,
		Username:         strings.ToLower(signupUsername),
		Email:            signupEmail,
		DisplayName:      signupDisplay,
		SshPublicKey:     pubkey,
		PublishKey:       &publish,
	}

	c := api.New(resolveServer(cfg), "")
	resp, err := c.SignUp(req)
	if err != nil {
		return err
	}

	cfg.Server = resolveServer(cfg)
	cfg.Token = resp.Session.Token
	cfg.Email = resp.Identity.Email
	cfg.Username = resp.Identity.Username
	if err := config.Save(cfg); err != nil {
		return fmt.Errorf("save config: %w", err)
	}

	fmt.Printf("\n✓ Welcome to %s. Signed in as %s.\n", resp.Organization.Name, resp.Identity.Username)
	if publish {
		fmt.Printf("  Public key resolves at: %s/%s.keys\n", cfg.Server, resp.Identity.Username)
	}
	return nil
}

package cmd

import (
	"fmt"

	"github.com/spf13/cobra"

	"kini/internal/api"
	"kini/internal/config"
	"kini/internal/ssh"
)

var (
	loginEmail string
	loginKey   string
)

var loginCmd = &cobra.Command{
	Use:   "login",
	Short: "Sign in to a Kini server via SSH-key challenge/response",
	Long: `Runs the SSH-key sign-in ceremony:

  1. Request a one-time challenge for your email.
  2. Sign the challenge with your SSH key (via ssh-keygen, agent-backed
     when possible, so YubiKey / FIDO2 / gpg-agent identities work).
  3. Send the signature back; receive a session token.

The token is stored in your kini config; subsequent commands use it
automatically.`,
	RunE: runLogin,
}

func init() {
	rootCmd.AddCommand(loginCmd)
	loginCmd.Flags().StringVarP(&loginEmail, "email", "e", "", "Email to sign in as (default: from config)")
	loginCmd.Flags().StringVarP(&loginKey, "key", "k", "", "SSH key to sign with (default: first key under ~/.ssh, .pub for agent)")
}

func runLogin(_ *cobra.Command, _ []string) error {
	cfg, err := config.Load()
	if err != nil {
		return err
	}

	email := loginEmail
	if email == "" {
		email = cfg.Email
	}
	if email == "" {
		email, err = promptLine("Email: ")
		if err != nil {
			return err
		}
	}
	if email == "" {
		return fmt.Errorf("--email is required (or signed up first via `kini signup`)")
	}

	server := resolveServer(cfg)

	// Pick the right signing key. With an explicit --key, honor it. Otherwise
	// query ssh-agent and prefer the identity matching Kini's published key
	// for this user — this is the YubiKey-vs-passphraseful-file distinction.
	var resolved ssh.Resolved
	if loginKey != "" {
		resolved = ssh.Resolved{Path: loginKey, Source: "--key " + loginKey, Cleanup: func() {}}
	} else {
		published := ssh.FetchPublishedSshKeys(server, cfg.Username)
		resolved, err = ssh.ResolveForSigning(published)
		if err != nil {
			return err
		}
	}
	defer resolved.Cleanup()

	c := api.New(server, "")
	chal, err := c.RequestSshChallenge(email)
	if err != nil {
		return err
	}

	fmt.Printf("Signing challenge via %s …\n", resolved.Source)
	sig, err := ssh.Sign(resolved.Path, chal.Namespace, chal.Nonce)
	if err != nil {
		return err
	}

	sess, err := c.VerifySshSignature(email, chal.Nonce, sig)
	if err != nil {
		return err
	}

	cfg.Server = server
	cfg.Token = sess.Token
	cfg.Email = email
	if err := config.Save(cfg); err != nil {
		return fmt.Errorf("save config: %w", err)
	}

	fmt.Printf("✓ Signed in. Session expires %s.\n", sess.ExpiresAt.Local().Format("2006-01-02 15:04"))
	return nil
}

// Kini CLI — hardware-friendly key directory client.
//
// One binary that drives sign-up, sign-in, publishing keys, and managing
// API tokens against a Kini server. See `kini --help`.
package main

import (
	"fmt"
	"os"

	"kini/cmd"
)

func main() {
	if err := cmd.Execute(); err != nil {
		fmt.Fprintln(os.Stderr, "error:", err)
		os.Exit(1)
	}
}

package api

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"net/url"
	"strings"
	"time"
)

type Client struct {
	BaseURL string
	Token   string
	HTTP    *http.Client
}

func New(baseURL, token string) *Client {
	return &Client{
		BaseURL: strings.TrimRight(baseURL, "/"),
		Token:   token,
		HTTP:    &http.Client{Timeout: 30 * time.Second},
	}
}

func (c *Client) do(method, path string, body any, out any) error {
	var reqBody io.Reader
	if body != nil {
		buf, err := json.Marshal(body)
		if err != nil {
			return err
		}
		reqBody = bytes.NewReader(buf)
	}

	req, err := http.NewRequest(method, c.BaseURL+path, reqBody)
	if err != nil {
		return err
	}
	if body != nil {
		req.Header.Set("Content-Type", "application/json")
	}
	if c.Token != "" {
		req.Header.Set("Authorization", "Bearer "+c.Token)
	}
	req.Header.Set("Accept", "application/json")

	resp, err := c.HTTP.Do(req)
	if err != nil {
		return err
	}
	defer resp.Body.Close()

	rawBody, _ := io.ReadAll(resp.Body)

	if resp.StatusCode >= 400 {
		ae := &APIError{Status: resp.StatusCode}
		_ = json.Unmarshal(rawBody, ae)
		if ae.Code == "" && ae.Message == "" {
			ae.Message = fmt.Sprintf("HTTP %d", resp.StatusCode)
		}
		return ae
	}

	if out != nil && len(rawBody) > 0 {
		if err := json.Unmarshal(rawBody, out); err != nil {
			return fmt.Errorf("decode response: %w", err)
		}
	}
	return nil
}

// --- Endpoints ----------------------------------------------------------

func (c *Client) SignUp(req SignUpRequest) (*SignUpResponse, error) {
	var out SignUpResponse
	if err := c.do(http.MethodPost, "/v1/sign-up", req, &out); err != nil {
		return nil, err
	}
	return &out, nil
}

func (c *Client) RequestSshChallenge(email string) (*SshChallengeResponse, error) {
	var out SshChallengeResponse
	if err := c.do(http.MethodPost, "/v1/auth/ssh/challenge", map[string]string{"email": email}, &out); err != nil {
		return nil, err
	}
	return &out, nil
}

func (c *Client) VerifySshSignature(email, nonce, signature string) (*SessionWithToken, error) {
	var out SessionWithToken
	req := map[string]string{"email": email, "nonce": nonce, "signature": signature}
	if err := c.do(http.MethodPost, "/v1/auth/ssh/verify", req, &out); err != nil {
		return nil, err
	}
	return &out, nil
}

func (c *Client) Me() (*MeResponse, error) {
	var out MeResponse
	if err := c.do(http.MethodGet, "/v1/auth/me", nil, &out); err != nil {
		return nil, err
	}
	return &out, nil
}

func (c *Client) SignOut() error {
	return c.do(http.MethodPost, "/v1/auth/sign-out", nil, nil)
}

func (c *Client) PublishKey(email, pubkey string) (*Key, error) {
	var out Key
	req := UploadKeyRequest{Type: "ssh", PublicKey: pubkey}
	path := "/v1/identities/" + url.PathEscape(email) + "/keys"
	if err := c.do(http.MethodPost, path, req, &out); err != nil {
		return nil, err
	}
	return &out, nil
}

func (c *Client) ListKeys(email string) ([]Key, error) {
	var out []Key
	path := "/v1/identities/" + url.PathEscape(email) + "/keys"
	if err := c.do(http.MethodGet, path, nil, &out); err != nil {
		return nil, err
	}
	return out, nil
}

func (c *Client) CreateApiToken(name string, scopes []string) (*CreateApiTokenResponse, error) {
	var out CreateApiTokenResponse
	req := CreateApiTokenRequest{Name: name, Scopes: scopes}
	if err := c.do(http.MethodPost, "/v1/api-tokens", req, &out); err != nil {
		return nil, err
	}
	return &out, nil
}

func (c *Client) ListApiTokens() ([]ApiToken, error) {
	var out []ApiToken
	if err := c.do(http.MethodGet, "/v1/api-tokens", nil, &out); err != nil {
		return nil, err
	}
	return out, nil
}

func (c *Client) RevokeApiToken(id string) error {
	return c.do(http.MethodDelete, "/v1/api-tokens/"+url.PathEscape(id), nil, nil)
}

func (c *Client) CreateMemberIdentity(orgID, username, email, displayName string) (*Identity, error) {
	var out Identity
	req := map[string]string{"username": username, "email": email}
	if displayName != "" {
		req["displayName"] = displayName
	}
	path := "/v1/orgs/" + url.PathEscape(orgID) + "/identities"
	if err := c.do(http.MethodPost, path, req, &out); err != nil {
		return nil, err
	}
	return &out, nil
}

func (c *Client) ListIdentities(orgID string) ([]Identity, error) {
	var out []Identity
	path := "/v1/orgs/" + url.PathEscape(orgID) + "/identities"
	if err := c.do(http.MethodGet, path, nil, &out); err != nil {
		return nil, err
	}
	return out, nil
}

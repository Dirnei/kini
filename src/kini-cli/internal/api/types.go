package api

import "time"

type SignUpRequest struct {
	OrganizationName string `json:"organizationName"`
	PrimaryDomain    string `json:"primaryDomain,omitempty"`
	Username         string `json:"username"`
	Email            string `json:"email"`
	DisplayName      string `json:"displayName,omitempty"`
	SshPublicKey     string `json:"sshPublicKey"`
	PublishKey       *bool  `json:"publishKey,omitempty"`
}

type Identity struct {
	ID          string    `json:"id"`
	OrgID       string    `json:"orgId"`
	Username    string    `json:"username"`
	Email       string    `json:"email"`
	Role        string    `json:"role"`
	DisplayName *string   `json:"displayName"`
	CreatedAt   time.Time `json:"createdAt"`
	VerifiedAt  *time.Time `json:"verifiedAt"`
}

type Organization struct {
	ID            string  `json:"id"`
	Name          string  `json:"name"`
	PrimaryDomain *string `json:"primaryDomain"`
}

type SessionWithToken struct {
	ID         string     `json:"id"`
	IdentityID string     `json:"identityId"`
	OrgID      string     `json:"orgId"`
	CreatedAt  time.Time  `json:"createdAt"`
	ExpiresAt  time.Time  `json:"expiresAt"`
	RevokedAt  *time.Time `json:"revokedAt"`
	Token      string     `json:"token"`
}

type Session struct {
	ID        string    `json:"id"`
	ExpiresAt time.Time `json:"expiresAt"`
}

type SignUpResponse struct {
	Organization  Organization     `json:"organization"`
	Identity      Identity         `json:"identity"`
	SshCredential map[string]any   `json:"sshCredential"`
	PublishedKey  map[string]any   `json:"publishedKey"`
	Session       SessionWithToken `json:"session"`
}

type SshChallengeResponse struct {
	Nonce     string    `json:"nonce"`
	Namespace string    `json:"namespace"`
	ExpiresAt time.Time `json:"expiresAt"`
}

type MeResponse struct {
	Identity     Identity     `json:"identity"`
	Organization Organization `json:"organization"`
	Session      Session      `json:"session"`
}

type UploadKeyRequest struct {
	Type      string `json:"type"`
	PublicKey string `json:"publicKey"`
}

type Key struct {
	ID          string    `json:"id"`
	IdentityID  string    `json:"identityId"`
	OrgID       string    `json:"orgId"`
	Type        string    `json:"type"`
	Fingerprint string    `json:"fingerprint"`
	Algorithm   string    `json:"algorithm"`
	PublicKey   string    `json:"publicKey"`
	Comment     *string   `json:"comment"`
	Provenance  string    `json:"provenance"`
	CreatedAt   time.Time `json:"createdAt"`
}

type CreateApiTokenRequest struct {
	Name   string   `json:"name"`
	Scopes []string `json:"scopes,omitempty"`
}

type CreateApiTokenResponse struct {
	ID         string    `json:"id"`
	Name       string    `json:"name"`
	Token      string    `json:"token"`
	CreatedAt  time.Time `json:"createdAt"`
}

type ApiToken struct {
	ID         string     `json:"id"`
	Name       string     `json:"name"`
	Scopes     []string   `json:"scopes"`
	CreatedAt  time.Time  `json:"createdAt"`
	LastUsedAt *time.Time `json:"lastUsedAt"`
	RevokedAt  *time.Time `json:"revokedAt"`
}

type APIError struct {
	Status  int    `json:"-"`
	Code    string `json:"code"`
	Message string `json:"message"`
}

func (e *APIError) Error() string {
	if e.Message != "" {
		return e.Code + ": " + e.Message
	}
	if e.Code != "" {
		return e.Code
	}
	return "api error"
}

// AAGUIDs identify the authenticator MODEL (not the individual credential).
// When a credential is registered, the server stores the AAGUID; we resolve
// it to a human-readable label here.
//
// Design choice: all YubiKey AAGUIDs collapse to the generic "YubiKey". The
// specific model / firmware revision isn't useful in the UI; what matters is
// "the YubiKey I just touched."

const YUBIKEY_AAGUIDS = new Set<string>([
  'cb69481e-8ff7-4039-93ec-0a2729a154a8', // YubiKey 5 Series
  'fa2b99dc-9e39-4257-8f92-4a30d23c4118', // YubiKey 5 Series with NFC
  'c5ef55ff-ad9a-4b9f-b580-adebafe026d0', // YubiKey 5C / 5Ci
  '73bb0cd4-e502-49b8-9c6f-b59445bf720b', // YubiKey 5 FIPS Series
  'c1f9a0bc-1dd2-404a-b27f-8e29047a43fd', // YubiKey 5 FIPS Series with NFC
  'f8a011f3-8c0a-4d15-8006-17111f9edc7d', // Security Key by Yubico
  '0bb43545-fd2c-4185-87dd-feb0b2916ace', // Security Key NFC by Yubico
  'b92c3f9a-c014-4056-887f-140a2501163b', // Security Key by Yubico (firmware 5.x)
  'd8522d9f-575b-4866-88a9-ba99fa02f35b', // YubiKey Bio Series
  '24673149-6c86-42e7-98d9-433fab6b06b1', // YubiKey 5 Series (firmware 5.7+)
  'a25342c0-3cdc-4414-8e46-f4807fca511c', // YubiKey 5 Series with NFC (firmware 5.7+)
])

const PLATFORM_AAGUIDS: Record<string, string> = {
  '08987058-cadc-4b81-b6e1-30de50dcbe96': 'Windows Hello',
  '6028b017-b1d4-4c02-b4b3-afcdafc96bb2': 'Windows Hello',
  '9ddd1817-af5a-4672-a2b9-3e3dd95000a9': 'Windows Hello',
  '6e96969e-a5cf-4aad-9b56-305fe6c82795': 'Windows Hello',
  'fbfc3007-154e-4ecc-8c0b-6e020557d7bd': 'iCloud Keychain',
  'dd4ec289-e01d-41c9-bb89-70fa845d4bf2': 'iCloud Keychain',
  'adce0002-35bc-c60a-648b-0b25f1f05503': 'Chrome on Mac',
}

/** Resolve an authenticator AAGUID to a human label. Unknown → "Security key". */
export function authenticatorName(aaguid: string | null | undefined): string {
  if (!aaguid) return 'Security key'
  const key = aaguid.toLowerCase()
  if (YUBIKEY_AAGUIDS.has(key)) return 'YubiKey'
  if (key in PLATFORM_AAGUIDS) return PLATFORM_AAGUIDS[key]
  return 'Security key'
}

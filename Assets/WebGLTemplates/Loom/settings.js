window.LOOM_SETTINGS = {
  userInfoStorageKey: 'loomUserInfo',
  auth: {
    domain: 'loomx.auth0.com',
    audience: 'https://keystore.loomx.io/',
    clientId: '25pDQvX4O5j7wgwT052Sh3UzXVR9X6Ud',
    // Where Auth0 should redirect after user signs in
    redirectUrl: window.location.origin,
    // Where Auth0 should redirect after user signs out
    logoutUrl: window.location.origin
  },
  vault: {
    url: 'https://stage-vault.delegatecall.com/v1/',
    prefix: 'unity3d-sdk'
  }
}

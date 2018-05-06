const AUTH_DOMAIN = 'loomx.auth0.com';
const AUTH_CLIENT_ID = '25pDQvX4O5j7wgwT052Sh3UzXVR9X6Ud';
const USER_INFO_STORAGE_KEY = 'loomUserInfo';

function createWebAuth() {
  return new auth0.WebAuth({
    domain: AUTH_DOMAIN,
    clientID: AUTH_CLIENT_ID,
    responseType: 'token',
    audience: 'https://keystore.loomx.io/',
    scope: 'openid profile email picture',
    redirectUri: window.location.origin
  });
}

function waitForLogin(webAuth, resumeAuth, silentAuth) {
  return new Promise(function (resolve, reject) {
    function setSession(authResult) {
      const accessToken = authResult.accessToken;
      console.log('Got access token: ' + accessToken);
      // exchange token for key using loom-js
      const cfg = new loom.VaultStoreConfig()
      cfg.url = "https://stage-vault.delegatecall.com/v1/";
      cfg.vaultPrefix = "unity3d-sdk";
      cfg.accessToken = accessToken;
      const auth = new auth0.Authentication({
        domain: AUTH_DOMAIN,
        clientID: AUTH_CLIENT_ID,
      });
      auth.userInfo(accessToken, (error, result) => {
        if (error) {
          reject(error);
        }
        loom.KeyStoreFactory.createVaultStore(cfg).then(function (keyStore) {
          const username = result.email.split('@')[0];
          const identityProvider = new loom.IdentityProvider(accessToken, keyStore);
          identityProvider.getIdentityAsync(username).then(function (identity) {
            const userInfo = JSON.stringify({
              username: identity.username,
              key: loom.CryptoUtils.bytesToHex(identity.privateKey)
            });
            // NOTE: local storage key should match one given to
            // AuthClientFactory.Configure().WithPrivateKeyLocalStoragePath()
            window.localStorage.setItem(USER_INFO_STORAGE_KEY, userInfo);
            resolve();
          });
        });
      })
    }

    function parseAccessToken() {
      webAuth.parseHash(function(err, authResult) {
        if (authResult && authResult.accessToken) {
          window.location.hash = '';
          setSession(authResult);
        } else if (err) {
          reject(err);
        }
      });
    }

    if (resumeAuth) {
      parseAccessToken();
    } else if (!silentAuth) {
      webAuth.authorize();
    } else {
      // Try to get an Auth0 access token silently.
      // NOTE: The host site base URL must be specified under Allowed Web Origins in Auth0 settings,
      //       otherwise silent token renewal will fail every time.
      webAuth.checkSession({}, function (err, authResult) {
        if (err) {
          // fall back to interactive sign-in
          webAuth.authorize();
          reject(err);
        } else {
          setSession(authResult);
        }
      });
    }
  });
};

/**
 * Signs in the user with Auth0, retrieves their private key from Vault, and stores the key in
 * Local Storage.
 * @param resumeAuth If `true` the function will only check for an access token from a previously
 *                   initiated auth-flow, it will not initiate a new auth-flow.
 * @param silentAuth If `true` silent non-interactive auth will be attempted first,
 *                   if that fails the interactive auth-flow will be initiated.
 *                   This parameter is ignored if `resumeAuth` is true.
 * @returns A promise that will be resolved when the user signs in.
 */
function authenticate(resumeAuth, silentAuth) {
  const webAuth = createWebAuth()
  return waitForLogin(webAuth, resumeAuth, silentAuth);
}

/**
 * Starts the auth-flow from within the WebGL game.
 * Non-interactive sign-in will be attempted first to prevent needless redirects.
 * @returns A promise that will be resolved when the user signs in.
 */
function authenticateFromGame() {
  return authenticate(false, true);
}

/**
 * Starts/resumes the auth-flow from the host page.
 * @param {HTMLButtonElement} loginBtn Button that should start auth flow when user presses it.
 * @returns A promise that will be resolved when the user signs in.
 */
function authenticateFromPage(loginBtn) {
  if (loginBtn) {
    loginBtn.addEventListener('click', function(e) {
      e.preventDefault();
      webAuth.authorize();
    });
  }
  
  return authenticate(true).then(function () {
    if (loginBtn) {
      loginBtn.style.display = 'none';
    }
  });
}

/**
 * @returns Previously stored string (if any) containing JSON-econded user info.
 */
function getUserInfo() {
  // look up serialized JSON value
  return window.localStorage.getItem(USER_INFO_STORAGE_KEY);
}

/**
 * @returns Signs out the current user and clears out any stored user info.
 */
function clearUserInfo() {
  window.localStorage.removeItem(USER_INFO_STORAGE_KEY);
  const webAuth = createWebAuth();
  webAuth.logout({
    clientID: AUTH_CLIENT_ID,
    returnTo: window.location.origin
  });
}

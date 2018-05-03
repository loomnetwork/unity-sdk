function waitForLogin(onUserLoggedIn) {
  const AUTH_DOMAIN = 'loomx.auth0.com';
  const AUTH_CLIENT_ID = '25pDQvX4O5j7wgwT052Sh3UzXVR9X6Ud';

  const webAuth = new auth0.WebAuth({
    domain: AUTH_DOMAIN,
    clientID: AUTH_CLIENT_ID,
    responseType: 'token',
    audience: 'https://keystore.loomx.io/',
    scope: 'openid profile email picture',
    redirectUri: window.location.href
  });

  const loginBtn = document.getElementById('btn-login');

  loginBtn.addEventListener('click', function(e) {
    e.preventDefault();
    webAuth.authorize();
  });

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
      loom.KeyStoreFactory.createVaultStore(cfg).then(function (keyStore) {
        const username = result.email.split('@')[0];
        const identityProvider = new loom.IdentityProvider(accessToken, keyStore);
        identityProvider.getIdentityAsync(username).then(function (identity) {
          const userInfo = JSON.stringify({
            username: identity.username,
            key: loom.CryptoUtils.bytesToHex(identity.privateKey)
          });
          window.localStorage.setItem("etherboyUserInfo", userInfo);
          onUserLoggedIn();
        });
      });
    })
  }

  function parseAccessToken() {
    webAuth.parseHash(function(err, authResult) {
      if (authResult && authResult.accessToken) {
        window.location.hash = '';
        setSession(authResult);
        loginBtn.style.display = 'none';
      } else if (err) {
        console.log(err);
      }
    });
  }

  parseAccessToken();
};

function authenticate() {
    return new Promise(function (resolve, reject) {
        waitForLogin(resolve);
    });
}

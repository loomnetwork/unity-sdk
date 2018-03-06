using Chaos.NaCl;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class authSample : MonoBehaviour {
    public Text statusTextRef;

    private static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

    // Use this for initialization
    void Start () {
        
    }

    // Update is called once per frame
    void Update () {
		
	}

    public async void SignIn()
    {
        var authClient = new LoomAuthClient();
        var account = await authClient.SignInFromNativeApp();
        this.statusTextRef.text = "Signed in as " + account.Username;
    }

    public async void SendTx()
    {
        var chainClient = new LoomChainClient("http://stage-rancher.loomapps.io:46657");
        var tx = new DummyTx
        {
            Val = "Hello World!"
        };
        // TODO: use LoomAccount.PrivateKey
        byte[] randomBytes = new byte[32];
        rngCsp.GetBytes(randomBytes);
        var privateKeySeed = randomBytes;
        byte[] publicKey;
        byte[] privateKey;
        Ed25519.KeyPairFromSeed(out publicKey, out privateKey, privateKeySeed);

        var payload = chainClient.SignTx(tx, privateKey, publicKey);
        var result = await chainClient.CommitTx(payload);
    }
}

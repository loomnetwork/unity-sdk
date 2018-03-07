using Chaos.NaCl;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;

public class authSample : MonoBehaviour {
    public Text statusTextRef;

    private LoomIdentity identity;

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
        this.identity = await authClient.SignInFromNativeApp();
        this.statusTextRef.text = "Signed in as " + this.identity.Username;
    }

    public async void SendTx()
    {
        if (this.identity == null)
        {
            throw new System.Exception("Not signed in!");
        }
        var chainClient = new LoomChainClient("http://stage-rancher.loomapps.io:46657");
        var tx = new DummyTx
        {
            Val = "Hello World!"
        };
        var payload = chainClient.SignTx(tx, this.identity.PrivateKey);
        var result = await chainClient.CommitTx(payload);
    }
}

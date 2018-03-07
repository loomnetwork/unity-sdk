using Chaos.NaCl;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class authSample : MonoBehaviour {
    public Text statusTextRef;

    private LoomIdentity identity;

    // Use this for initialization
    void Start () {

    }

    // Update is called once per frame
    void Update () {
		
	}

    public async void SignIn()
    {
        this.identity = await SignInFromNativeApp();
        this.statusTextRef.text = "Signed in as " + this.identity.Username;
    }

    public async Task<LoomIdentity> SignInFromNativeApp()
    {
        var authClient = new LoomAuthClient("unity3d-sdk");
        var accessToken = await authClient.GetAccessTokenForNativeApp();
        return await authClient.GetLoomIdentity(accessToken);
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
            Val = "Hello World " + (Random.value * 100000)
        };
        Debug.Log("Tx Val: " + tx.Val);
        var payload = chainClient.SignTx(tx, this.identity.PrivateKey);
        var result = await chainClient.CommitTx(payload);
        this.statusTextRef.text = "Committed Tx to Block " + result.Height;
    }
}

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class authSample : MonoBehaviour {
    public Text statusTextRef;

    private LoomIdentity identity;

    // Use this for initialization
    void Start () {
        // By default the editor won't respond to network IO or anything if it doesn't have input focus,
        // which is super annoying when input focus is given to the web browser for the Auth0 sign-in.
        Application.runInBackground = true;
    }

    // Update is called once per frame
    void Update () {
		
	}

    public async void SignIn()
    {
#if UNITY_EDITOR
        this.identity = await SignInFromNativeApp();
#endif
#if UNITY_ANDROID
        this.identity = await SignInFromAndroidApp();
#endif
        this.statusTextRef.text = "Signed in as " + this.identity.Username;
    }

    public async Task<LoomIdentity> SignInFromNativeApp()
    {
        var authClient = new LoomAuthClient("unity3d-sdk");
        var accessToken = await authClient.GetAccessTokenForNativeApp();
        return await authClient.GetLoomIdentity(accessToken);
    }

#if UNITY_ANDROID
    public async Task<LoomIdentity> SignInFromAndroidApp()
    {
        var authClient = new LoomAuthClient("unity3d-sdk");
        var accessToken = await authClient.GetAccessTokenForAndroidApp();
        return await authClient.GetLoomIdentity(accessToken);
    }
#endif

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

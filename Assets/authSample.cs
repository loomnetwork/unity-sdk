using UnityEngine;
using UnityEngine.UI;

public class authSample : MonoBehaviour {
    public Text statusTextRef;

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
}

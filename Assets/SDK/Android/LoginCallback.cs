using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LoomNetwork.Unity3dSDK.Android
{
    class LoginCallback : AndroidJavaProxy
    {
        public Action<string> OnSuccess;
        public Action<string> OnFailure;

        public LoginCallback() : base("io.loomx.unity3d.LoginCallback")
        {
        }

        public void onFailure(string exception)
        {
            this.OnFailure(exception);
        }

        public void onSuccess(string accessToken)
        {
            this.OnSuccess(accessToken);
        }
    }
}
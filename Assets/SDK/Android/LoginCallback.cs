using System;
using UnityEngine;

namespace Loom.Unity3d.Android
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
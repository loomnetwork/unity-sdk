package io.loomx.unity3d;

import android.app.Dialog;
import android.support.annotation.NonNull;

import com.auth0.android.authentication.AuthenticationException;
import com.auth0.android.result.Credentials;

/**
 * Callback called on success/failure of an Identity Provider authentication.
 * Only one of the success or failure methods will be called for a single authentication request.
 */
public interface LoginCallback {
    /**
     * Called with an AuthenticationException that describes the error.
     *
     * @param exception cause of the error
     */
    void onFailure(String exception);

    /**
     * Called when the authentication is successful using web authentication against Auth0
     *
     * @param accessToken Auth0 access token
     */
    void onSuccess(@NonNull String accessToken);
}

package io.loomx.unity3d;

import android.app.Dialog;
import android.content.Context;
import android.os.Bundle;
import android.app.Fragment;
import android.support.annotation.NonNull;

import com.auth0.android.Auth0;
import com.auth0.android.authentication.AuthenticationException;
import com.auth0.android.provider.AuthCallback;
import com.auth0.android.provider.WebAuthProvider;
import com.auth0.android.result.Credentials;
import com.google.gson.Gson;
import com.unity3d.player.UnityPlayer;

/**
 * A simple {@link Fragment} subclass.
 * Use the {@link AuthFragment#newInstance} factory method to
 * create an instance of this fragment.
 */
public class AuthFragment extends Fragment {
    public static class AuthConfig {
        public String ClientId;
        public String Domain;
        public String Scheme;
        public String Audience;
        public String Scope;
    }

    public static AuthFragment singleton;
    public static final String TAG = "LoomAuthFragment";

    public AuthFragment() {
        // Required empty public constructor
    }

    public static void start() {
        singleton = newInstance();
        UnityPlayer.currentActivity.getFragmentManager().beginTransaction().add(singleton, TAG).commit();
    }

    public static void login(String configJSON, final LoginCallback callback) {
        Gson gson = new Gson();
        AuthConfig cfg = gson.fromJson(configJSON, AuthConfig.class);
        Auth0 auth0 = new Auth0(cfg.ClientId, cfg.Domain);
        auth0.setOIDCConformant(true);
        WebAuthProvider.init(auth0)
                .withScheme(cfg.Scheme)
                .withAudience(cfg.Audience)
                .withScope(cfg.Scope)
                .start(UnityPlayer.currentActivity, new AuthCallback() {
                    @Override
                    public void onFailure(@NonNull final Dialog dialog) {
                        UnityPlayer.currentActivity.runOnUiThread(new Runnable() {
                            @Override
                            public void run() {
                                dialog.show();
                            }
                        });
                        // TODO: let Unity know something went wrong?
                    }

                    @Override
                    public void onFailure(AuthenticationException exception) {
                        callback.onFailure(exception.getDescription());
                    }

                    @Override
                    public void onSuccess(@NonNull Credentials credentials) {
                        callback.onSuccess(credentials.getAccessToken());
                    }
                });
    }

    /**
     * Use this factory method to create a new instance of
     * this fragment using the provided parameters.
     *
     * @return A new instance of fragment AuthFragment.
     */
    public static AuthFragment newInstance() {
        return new AuthFragment();
    }

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setRetainInstance(true);
    }

    @Override
    public void onAttach(Context context) {
        super.onAttach(context);
    }

    @Override
    public void onDetach() {
        super.onDetach();
    }
}

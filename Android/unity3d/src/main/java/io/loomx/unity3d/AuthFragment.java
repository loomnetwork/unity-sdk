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
import com.unity3d.player.UnityPlayer;

/**
 * A simple {@link Fragment} subclass.
 * Use the {@link AuthFragment#newInstance} factory method to
 * create an instance of this fragment.
 */
public class AuthFragment extends Fragment {
    // TODO: Rename parameter arguments, choose names that match
    // the fragment initialization parameters, e.g. ARG_ITEM_NUMBER
    private static final String ARG_PARAM1 = "param1";
    private static final String ARG_PARAM2 = "param2";

    // TODO: Rename and change types of parameters
    private String mParam1;
    private String mParam2;

    public static AuthFragment singleton;
    public static final String TAG = "LoomAuthFragment";

    public AuthFragment() {
        // Required empty public constructor
    }

    public static void start() {
        singleton = newInstance("1", "2");
        UnityPlayer.currentActivity.getFragmentManager().beginTransaction().add(singleton, TAG).commit();
    }

    public static void login(final LoginCallback callback) {
        String clientId = "25pDQvX4O5j7wgwT052Sh3UzXVR9X6Ud";
        String domain = "loomx.auth0.com";
        Auth0 auth0 = new Auth0(clientId, domain);
        auth0.setOIDCConformant(true);
        WebAuthProvider.init(auth0)
                .withScheme("io.loomx.unity3d")
                .withAudience("https://keystore.loomx.io/")
                .withScope("openid profile email picture")
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
     * @param param1 Parameter 1.
     * @param param2 Parameter 2.
     * @return A new instance of fragment AuthFragment.
     */
    // TODO: Rename and change types and number of parameters
    public static AuthFragment newInstance(String param1, String param2) {
        AuthFragment fragment = new AuthFragment();
        Bundle args = new Bundle();
        args.putString(ARG_PARAM1, param1);
        args.putString(ARG_PARAM2, param2);
        fragment.setArguments(args);
        return fragment;
    }

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setRetainInstance(true);
        if (getArguments() != null) {
            mParam1 = getArguments().getString(ARG_PARAM1);
            mParam2 = getArguments().getString(ARG_PARAM2);
        }
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

mergeInto(LibraryManager.library, {
  // Starts the auth-flow on the host page.
  // @param authHandlerName Name of a function attached to `window` that should be invoked to
  //                        start the auth flow.
  StartLoomAuthFlow: function (authHandlerName) {
    const handler = Pointer_stringify(authHandlerName);
    window[handler]();
  },

  // Get user info from Local Storage.
  // @param localStorageKey Key that should be used to look up user info in Local Storage.
  // @returns User info previously stored by the host page.
  GetLoomUserInfo: function (localStorageKey) {
    const key = Pointer_stringify(localStorageKey);
    // look up serialized JSON value
    const userInfo = window.localStorage.getItem(key) || "{}";
    return allocateStringBuffer(userInfo);
  },
});
mergeInto(LibraryManager.library, {
  // Starts the auth-flow on the host page.
  // @param authHandlerName Name of a function attached to `window` that should be invoked to
  //                        start the auth flow.
  StartLoomAuthFlow: function (authHandlerName) {
    const handler = Pointer_stringify(authHandlerName);
    window[handler]();
  },

  // Get user info from Local Storage.
  // @param handlerName Name of a function attached to `window` that should be invoked to
  //                    retrieve user info stored in the host page.
  // @returns User info previously stored by the host page.
  GetLoomUserInfo: function (handlerName) {
    const handler = Pointer_stringify(handlerName);
    const userInfo = window[handler]() || "{}";
    return allocateStringBuffer(userInfo);
  },

  // Clears out any user data stored by the host page.
  // @param handlerName Name of a function attached to `window` that should be invoked to
  //                    clear user data stored in the host page.
  ClearLoomUserInfo: function (handlerName) {
    const handler = Pointer_stringify(handlerName);
    window[handler]();
  }
});

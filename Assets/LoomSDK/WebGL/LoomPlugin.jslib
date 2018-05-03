mergeInto(LibraryManager.library, {
  // Returns user info previously stored by the host page.
  GetLoomUserInfo: function (localStorageKey) {
    const key = Pointer_stringify(localStorageKey);
    // lookup serialized JSON value
    const userInfo = window.localStorage.getItem(key) || "{}";
    return allocateStringBuffer(userInfo);
  },
});
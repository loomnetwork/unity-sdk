function allocateStringBuffer(str) {
  const bufferSize = lengthBytesUTF8(str) + 1;
  const buffer = _malloc(bufferSize);
  stringToUTF8(str, buffer, bufferSize);
  return buffer;
}

mergeInto(LibraryManager.library, {

  // Returns user info previously stored by the host page.
  GetLoomUserInfo: function (localStorageKey) {
    const key = Pointer_stringify(localStorageKey);
    // lookup serialized JSON value
    const userInfo = window.localStorage.get(key) || "{}";
    return allocateStringBuffer(userInfo);
  },

});
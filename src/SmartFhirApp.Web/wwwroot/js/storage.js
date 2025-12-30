window.smartStorage = {
    get: function (key) {
        if (!key) {
            return null;
        }
        return window.localStorage.getItem(key);
    },
    set: function (key, value) {
        if (!key) {
            return;
        }
        window.localStorage.setItem(key, value ?? "");
    },
    remove: function (key) {
        if (!key) {
            return;
        }
        window.localStorage.removeItem(key);
    }
};

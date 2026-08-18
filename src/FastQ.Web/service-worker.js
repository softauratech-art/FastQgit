"use strict";

var CACHE_NAME = "fastq-static-v1";
var APP_ROOT = new URL("./", self.location.href);
var STATIC_ASSETS = [
  "Content/site.css?v=1.1.20260818",
  "Content/modal.css?v=1.1.20260818",
  "Content/icons/fastq-192.png",
  "Content/icons/fastq-512.png",
  "Scripts/jquery-3.7.1.js",
  "Scripts/jquery.signalR-2.4.3.min.js",
  "Scripts/fastq.live.js"
].map(function (path) {
  return new URL(path, APP_ROOT).href;
});

self.addEventListener("install", function (event) {
  event.waitUntil(
    caches.open(CACHE_NAME).then(function (cache) {
      return cache.addAll(STATIC_ASSETS);
    })
  );
  self.skipWaiting();
});

self.addEventListener("activate", function (event) {
  event.waitUntil(
    caches.keys().then(function (keys) {
      return Promise.all(keys.filter(function (key) {
        return key.indexOf("fastq-static-") === 0 && key !== CACHE_NAME;
      }).map(function (key) {
        return caches.delete(key);
      }));
    }).then(function () {
      return self.clients.claim();
    })
  );
});

self.addEventListener("fetch", function (event) {
  var request = event.request;
  var url = new URL(request.url);

  // Keep pages, authentication, SignalR, API calls, and all writes on the network.
  if (request.method !== "GET" ||
      request.mode === "navigate" ||
      url.origin !== self.location.origin ||
      url.pathname.indexOf("/signalr") !== -1 ||
      STATIC_ASSETS.indexOf(url.href) === -1) {
    return;
  }

  // Prefer the newest static asset. Use the cache only during a network failure.
  event.respondWith(
    fetch(request).then(function (response) {
      if (response && response.ok) {
        var responseCopy = response.clone();
        caches.open(CACHE_NAME).then(function (cache) {
          cache.put(request, responseCopy);
        });
      }
      return response;
    }).catch(function () {
      return caches.match(request);
    })
  );
});

// Preserve the existing browser-notification behavior in the root-scoped worker.
self.addEventListener("notificationclick", function (event) {
  event.notification.close();

  if (event.action === "done") {
    event.waitUntil(
      self.clients.matchAll({ type: "window", includeUncontrolled: true })
        .then(function (clientList) {
          clientList.forEach(function (client) {
            client.postMessage({ type: "fastq-notification-done" });
          });
        })
    );
    return;
  }

  event.waitUntil(
    self.clients.matchAll({ type: "window", includeUncontrolled: true })
      .then(function (clientList) {
        if (clientList.length && "focus" in clientList[0]) {
          return clientList[0].focus();
        }
        if (self.clients.openWindow) {
          return self.clients.openWindow(APP_ROOT.href);
        }
        return null;
      })
  );
});

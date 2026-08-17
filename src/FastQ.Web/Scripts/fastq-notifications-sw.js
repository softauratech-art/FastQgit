self.addEventListener("notificationclick", function (event) {
  if (event.action === "done") {
    event.notification.close();
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
        return null;
      })
  );
});

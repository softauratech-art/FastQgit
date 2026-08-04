/* FastQ live client (SignalR 2.x)
   - Joins groups: loc:{locationId}, queue:{queueId}, appt:{appointmentId}
   - On events, triggers page hooks (window.onFastQQueueUpdated / window.onFastQAppointmentUpdated)
*/
(function () {
  function safe(v) { return (v || "").toString(); }
  function log() {
    if (!window.console || !console.log) return;
    var args = Array.prototype.slice.call(arguments);
    args.unshift("[FastQ Live]");
    console.log.apply(console, args);
  }
  function warn() {
    if (!window.console || !console.warn) return;
    var args = Array.prototype.slice.call(arguments);
    args.unshift("[FastQ Live]");
    console.warn.apply(console, args);
  }

  window.FastQLive = {
    hub: null,
    started: false,
    joined: { loc: null, queue: null, appt: null, notifications: false },

    start: function () {
      log("start requested", {
        hasJquery: !!window.$,
        hasSignalR: !!(window.$ && $.connection),
        hasQueueHub: !!(window.$ && $.connection && $.connection.queueHub),
        path: window.location.pathname
      });

      if (!window.$ || !$.connection || !$.connection.queueHub) {
        warn("cannot start; missing jquery, signalR, or queueHub proxy");
        return;
      }

      this.hub = $.connection.queueHub;

      var self = this;

      this.hub.client.queueUpdated = function (locationId, queueId) {
        log("queueUpdated received", { locationId: safe(locationId), queueId: safe(queueId) });
        if (window.onFastQQueueUpdated) window.onFastQQueueUpdated(safe(locationId), safe(queueId));
        else warn("queueUpdated hook missing on page");
      };

      this.hub.client.appointmentUpdated = function (appointmentId, status, providerId) {
        log("appointmentUpdated received", {
          appointmentId: safe(appointmentId),
          status: safe(status),
          providerId: safe(providerId),
          hasPageHook: !!window.onFastQAppointmentUpdated
        });
        if (window.onFastQAppointmentUpdated) {
          window.onFastQAppointmentUpdated(safe(appointmentId), safe(status), safe(providerId));
        } else {
          warn("appointmentUpdated hook missing on page");
        }
      };

      this.hub.client.notify = function (message) {
        if (!message) return;
        var safeMsg = safe(message);
        log("notify received", { message: safeMsg, hasPageHook: !!window.onFastQNotify });
        if (window.onFastQNotify) window.onFastQNotify(safeMsg);
        else warn("notify hook missing on page");
        self.toast(safeMsg);
      };

      $.connection.hub.stateChanged(function (change) {
        log("stateChanged", { oldState: change.oldState, newState: change.newState });
      });

      $.connection.hub.disconnected(function () {
        warn("disconnected");
      });

      $.connection.hub.reconnecting(function () {
        warn("reconnecting");
      });

      $.connection.hub.reconnected(function () {
        log("reconnected");
        self.tryJoinGroups();
      });

      var debugContext = window.FASTQ_SIGNALR_DEBUG || {};
      if (debugContext.key && debugContext.userId) {
        $.connection.hub.qs = {
          debug: safe(debugContext.key),
          debuguserid: safe(debugContext.userId)
        };
      }

      $.connection.hub.start()
        .done(function () {
          self.started = true;
          log("connected", {
            connectionId: $.connection.hub.id,
            transport: $.connection.hub.transport && $.connection.hub.transport.name
          });
          self.toast("Live connected");
          self.tryJoinGroups();
        })
        .fail(function (err) {
          warn("connect failed", err);
          self.toast("Live connect failed");
        });
    },

    tryJoinGroups: function () {
      if (!this.started || !this.hub) {
        log("tryJoinGroups skipped", { started: this.started, hasHub: !!this.hub });
        return;
      }

      var ctx = window.FASTQ_CONTEXT || {};
      var loc = safe(ctx.locationId);
      var q = safe(ctx.queueId);
      var appt = safe(ctx.appointmentId);

      var self = this;
      log("tryJoinGroups", { context: ctx, joined: this.joined });

      var notificationQueueIds = Array.isArray(window.FASTQ_NOTIFICATION_QUEUE_IDS)
        ? window.FASTQ_NOTIFICATION_QUEUE_IDS
        : [];
      if (!this.joined.notifications && notificationQueueIds.length) {
        this.hub.server.joinNotificationQueues(notificationQueueIds).done(function () {
          self.joined.notifications = true;
          log("joined authorized notification groups", { queueIds: notificationQueueIds });
        }).fail(function (err) {
          warn("join notification groups failed", { error: err });
        });
      }

      if (loc && this.joined.loc !== loc) {
        this.hub.server.joinLocation(loc).done(function () {
          self.joined.loc = loc;
          log("joined location group", { locationId: loc });
        }).fail(function (err) {
          warn("join location failed", { locationId: loc, error: err });
        });
      }

      if (q && this.joined.queue !== q) {
        this.hub.server.joinQueue(q).done(function () {
          self.joined.queue = q;
          log("joined queue group", { queueId: q });
        }).fail(function (err) {
          warn("join queue failed", { queueId: q, error: err });
        });
      }

      if (appt && this.joined.appt !== appt) {
        this.hub.server.joinAppointment(appt).done(function () {
          self.joined.appt = appt;
          log("joined appointment group", { appointmentId: appt });
        }).fail(function (err) {
          warn("join appointment failed", { appointmentId: appt, error: err });
        });
      }
    },

    toast: function (msg) {
      try {
        var host = document.getElementById("fastq_toast");
        if (!host) return;

        var el = document.createElement("div");
        el.className = "toast-item";
        el.textContent = msg;
        host.appendChild(el);

        setTimeout(function () {
          try { host.removeChild(el); } catch (e) { }
        }, 3000);
      } catch (e) { }
    }
  };

  // Start after DOM ready
  if (window.$) {
    $(function () {
      log("document ready; starting live client");
      window.FastQLive.start();

      // Some pages set context after first AJAX snapshot; keep trying a bit.
      var tries = 0;
      var iv = setInterval(function () {
        tries++;
        window.FastQLive.tryJoinGroups();
        if (tries >= 20) clearInterval(iv);
      }, 500);
    });
  }
})();

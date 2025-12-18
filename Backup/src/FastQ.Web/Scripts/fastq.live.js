/* FastQ live client (SignalR 2.x)
   - Joins groups: loc:{locationId}, queue:{queueId}, appt:{appointmentId}
   - On events, triggers page hooks (window.onFastQQueueUpdated / window.onFastQAppointmentUpdated)
*/
(function () {
  function safe(v) { return (v || "").toString(); }

  window.FastQLive = {
    hub: null,
    started: false,
    joined: { loc: null, queue: null, appt: null },

    start: function () {
      if (!window.$ || !$.connection || !$.connection.queueHub) return;

      this.hub = $.connection.queueHub;

      var self = this;

      this.hub.client.queueUpdated = function (locationId, queueId) {
        if (window.onFastQQueueUpdated) window.onFastQQueueUpdated(safe(locationId), safe(queueId));
      };

      this.hub.client.appointmentUpdated = function (appointmentId, status) {
        if (window.onFastQAppointmentUpdated) window.onFastQAppointmentUpdated(safe(appointmentId), safe(status));
      };

      $.connection.hub.start()
        .done(function () {
          self.started = true;
          self.toast("Live connected");
          self.tryJoinGroups();
        })
        .fail(function (err) {
          self.toast("Live connect failed");
          // console && console.error(err);
        });
    },

    tryJoinGroups: function () {
      if (!this.started || !this.hub) return;

      var ctx = window.FASTQ_CONTEXT || {};
      var loc = safe(ctx.locationId);
      var q = safe(ctx.queueId);
      var appt = safe(ctx.appointmentId);

      var self = this;

      if (loc && this.joined.loc !== loc) {
        this.hub.server.joinLocation(loc).done(function () {
          self.joined.loc = loc;
        });
      }

      if (q && this.joined.queue !== q) {
        this.hub.server.joinQueue(q).done(function () {
          self.joined.queue = q;
        });
      }

      if (appt && this.joined.appt !== appt) {
        this.hub.server.joinAppointment(appt).done(function () {
          self.joined.appt = appt;
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
        }, 3200);
      } catch (e) { }
    }
  };

  // Start after DOM ready
  if (window.$) {
    $(function () {
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

(function (window, document) {
  "use strict";

  var common = window.FastQProviderCommon = window.FastQProviderCommon || {};
  var providerActionOptions = null;
  var providerActionClickBound = false;
  var meetingOptions = null;
  var meetingClickBound = false;
  var expandableClickBound = false;
  var accordionClickBound = false;

  function noop() { }

  function closest(element, selector) {
    return element && element.closest ? element.closest(selector) : null;
  }

  function getRowDate(button) {
    var dateHost = closest(button, "[data-date]");
    return dateHost ? (dateHost.getAttribute("data-date") || "") : "";
  }

  common.refreshProviderActionButtonStates = function (container) {
    var options = providerActionOptions || {};
    var root = container || document;
    var getStatus = options.getStatusForAppointment || function () { return ""; };
    var canAction = options.canAction || function () { return true; };

    root.querySelectorAll("[data-provider-action]").forEach(function (button) {
      var action = (button.getAttribute("data-action") || "").toLowerCase();
      var appointmentId = button.getAttribute("data-appointment-id") || "";
      var queueId = button.getAttribute("data-queue-id") || "";
      var status = appointmentId ? getStatus(appointmentId) : "";
      button.disabled = !canAction(status, action, queueId, getRowDate(button));
    });
  };

  common.bindProviderActionButtons = function (options) {
    providerActionOptions = options || providerActionOptions || {};
    common.refreshProviderActionButtonStates();

    if (providerActionClickBound) {
      return;
    }

    providerActionClickBound = true;
    document.addEventListener("click", function (event) {
      var button = closest(event.target, "[data-provider-action]");
      if (!button) {
        return;
      }

      event.preventDefault();
      event.stopPropagation();

      var options = providerActionOptions || {};
      var appointmentId = button.getAttribute("data-appointment-id") || "";
      var action = (button.getAttribute("data-action") || "").toLowerCase();
      var srcType = (button.getAttribute("data-src-type") || "A").toUpperCase();
      var queueId = button.getAttribute("data-queue-id") || "";

      if (!appointmentId || !action) {
        return;
      }

      var getStatus = options.getStatusForAppointment || function () { return ""; };
      var canAction = options.canAction || function () { return true; };
      var status = getStatus(appointmentId);
      if (!canAction(status, action, queueId, getRowDate(button))) {
        window.alert("Action not allowed for the current status.");
        return;
      }

      if (action === "transfer") {
        (options.openRouteModal || noop)("transfer", appointmentId, srcType);
        return;
      }

      if (action === "end") {
        (options.openEndServiceModal || noop)(appointmentId, srcType);
        return;
      }

      if (action === "remove") {
        (options.openCancelServiceModal || noop)(appointmentId, srcType);
        return;
      }

      var postForm = options.postForm;
      if (!postForm) {
        return;
      }

      postForm(options.providerActionUrl, { action: action, appointmentId: appointmentId, srcType: srcType })
        .then(function (res) {
          if (!res || !res.ok) {
            window.alert(res && res.error ? res.error : "Action failed.");
            return;
          }
          (options.queueLiveSync || noop)();
        });
    });
  };

  common.bindMeetingLinks = function (options) {
    meetingOptions = options || meetingOptions || {};
    if (meetingClickBound) {
      return;
    }

    meetingClickBound = true;
    document.addEventListener("click", function (event) {
      var link = closest(event.target, ".meeting-link");
      if (!link) {
        return;
      }

      event.preventDefault();
      event.stopPropagation();

      var appointmentId = link.getAttribute("data-appointment-id") || "";
      var srcType = link.getAttribute("data-src-type") || "A";
      var mid = document.getElementById("mid");
      var fieldMap = {
        meetingDate: "date",
        meetingTime: "time",
        meetingTitle: "title",
        meetingCustomer: "customer",
        meetingEmail: "email",
        meetingPhone: "phone",
        meetingQueue: "queue-name",
        meetingService: "service-name",
        meetingRefValue: "ref-value",
        meetingContact: "contact",
        meetingLanguage: "language",
        meetingStatus: "status-text",
        meetingStampUser: "stamp-user"
      };

      if (mid) {
        mid.textContent = appointmentId;
        mid.setAttribute("data-appointment-id", appointmentId);
        mid.setAttribute("data-src-type", srcType);
        mid.setAttribute("data-original-notes", link.getAttribute("data-notes") || "");
      }

      Object.keys(fieldMap).forEach(function (id) {
        var field = document.getElementById(id);
        if (field) {
          field.textContent = link.getAttribute("data-" + fieldMap[id]) || "";
        }
      });

      var guestUrlInput = document.getElementById("mGuestURL");
      var urlInput = document.getElementById("mURL");
      var previousNotes = document.getElementById("mPreviousNotes");
      var notesInput = document.getElementById("mNotes");
      if (guestUrlInput) guestUrlInput.value = link.getAttribute("data-meeting-url") || "";
      if (urlInput) urlInput.value = link.getAttribute("data-meeting-url-host") || "";
      if (previousNotes) previousNotes.textContent = link.getAttribute("data-notes") || "-";
      if (notesInput) notesInput.value = "";

      document.querySelectorAll(".online-meeting-data-row").forEach(function (element) {
        element.style.display = link.getAttribute("data-contact") === "Online Meeting" ? "" : "none";
      });

      (meetingOptions.clearModalFeedback || noop)("meetingModal");
      (meetingOptions.openModal || noop)("meetingModal");
    });
  };

  common.bindExpandableText = function () {
    if (expandableClickBound) {
      return;
    }

    expandableClickBound = true;
    document.addEventListener("click", function (event) {
      var toggle = closest(event.target, ".expandable-text");
      if (!toggle) {
        return;
      }

      event.preventDefault();
      event.stopPropagation();

      var isExpanded = toggle.classList.toggle("is-expanded");
      toggle.setAttribute("aria-expanded", isExpanded ? "true" : "false");
    });
  };

  common.bindAccordionRows = function () {
    if (accordionClickBound) {
      return;
    }

    accordionClickBound = true;
    document.addEventListener("click", function (event) {
      if (closest(event.target, "button,a,input,select,textarea,label")) {
        return;
      }

      var row = closest(event.target, ".accordion-toggle[data-target]");
      if (!row) {
        return;
      }

      var targetId = row.getAttribute("data-target");
      if (!targetId) {
        return;
      }

      document.querySelectorAll(".row-actions").forEach(function (detail) {
        if (detail.id !== targetId) {
          detail.classList.remove("is-open");
        }
      });

      var target = document.getElementById(targetId);
      if (target) {
        target.classList.toggle("is-open");
      }
    });
  };
})(window, document);

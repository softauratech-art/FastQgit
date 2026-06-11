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

  function twoDigits(value) {
    return value < 10 ? "0" + value : String(value);
  }

  common.toTwoDigits = twoDigits;

  common.toIsoDate = function (dateValue) {
    if (!common.isValidDateValue(dateValue)) {
      return "";
    }
    return dateValue.getFullYear() + "-" + twoDigits(dateValue.getMonth() + 1) + "-" + twoDigits(dateValue.getDate());
  };

  common.isValidDateValue = function (dateValue) {
    return dateValue instanceof Date && !isNaN(dateValue.getTime());
  };

  common.startOfMonth = function (dateValue) {
    var safeDate = common.isValidDateValue(dateValue) ? dateValue : new Date();
    return new Date(safeDate.getFullYear(), safeDate.getMonth(), 1);
  };

  common.addMonths = function (dateValue, count) {
    var safeDate = common.isValidDateValue(dateValue) ? dateValue : new Date();
    return new Date(safeDate.getFullYear(), safeDate.getMonth() + count, 1);
  };

  common.getEntryCalendarMonthLabel = function (dateValue) {
    if (!common.isValidDateValue(dateValue)) {
      dateValue = new Date();
    }
    var months = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
    return months[dateValue.getMonth()] + " " + dateValue.getFullYear();
  };

  common.getEntryCalendarMonthKey = function (dateValue) {
    if (!common.isValidDateValue(dateValue)) {
      dateValue = new Date();
    }
    return dateValue.getFullYear() + "-" + twoDigits(dateValue.getMonth() + 1);
  };

  common.formatEntryDateDisplay = function (value) {
    if (!value) {
      return "";
    }

    var parts = value.split("-");
    if (parts.length !== 3) {
      return value;
    }

    return parts[1] + "/" + parts[2] + "/" + parts[0];
  };

  common.parseEntryDateDisplay = function (value) {
    var match = /^\s*(\d{1,2})\/(\d{1,2})\/(\d{4})\s*$/.exec(value || "");
    if (!match) {
      return "";
    }

    var month = parseInt(match[1], 10);
    var day = parseInt(match[2], 10);
    var year = parseInt(match[3], 10);
    var candidate = new Date(year, month - 1, day);
    if (candidate.getFullYear() !== year || candidate.getMonth() !== month - 1 || candidate.getDate() !== day) {
      return "";
    }

    return common.toIsoDate(candidate);
  };

  common.getPreferredEntryDateValue = function (dateSelect, selectedCalendarDate) {
    var value = dateSelect ? (dateSelect.getAttribute("data-current-value") || dateSelect.value || "") : "";
    if (value) {
      return value;
    }
    if (selectedCalendarDate instanceof Date) {
      return common.isValidDateValue(selectedCalendarDate) ? common.toIsoDate(selectedCalendarDate) : "";
    }
    return selectedCalendarDate || "";
  };

  common.setEntryDateDisplay = function (prefix, value) {
    var displayInput = document.getElementById(prefix + "DateDisplay");
    if (!displayInput) {
      return;
    }

    displayInput.value = common.formatEntryDateDisplay(value);
  };

  common.openEntryDatePicker = function (prefix) {
    var picker = document.getElementById(prefix + "DatePicker");
    var displayInput = document.getElementById(prefix + "DateDisplay");
    if (!picker) {
      return;
    }

    picker.classList.add("is-open");
    if (displayInput) {
      displayInput.setAttribute("aria-expanded", "true");
    }
  };

  common.closeEntryDatePicker = function (prefix) {
    var picker = document.getElementById(prefix + "DatePicker");
    var displayInput = document.getElementById(prefix + "DateDisplay");
    if (!picker) {
      return;
    }

    picker.classList.remove("is-open");
    if (displayInput) {
      displayInput.setAttribute("aria-expanded", "false");
    }
  };

  common.normalizeUsPhoneNumber = function (value) {
    var digits = (value || "").replace(/\D/g, "");
    if (digits.length === 11 && digits.charAt(0) === "1") {
      digits = digits.substring(1);
    }
    if (digits.length !== 10) {
      return "";
    }
    if (digits.charAt(0) === "0" || digits.charAt(0) === "1" || digits.charAt(3) === "0" || digits.charAt(3) === "1") {
      return "";
    }
    return "(" + digits.substring(0, 3) + ")-" + digits.substring(3, 6) + "-" + digits.substring(6);
  };

  common.validateUsPhoneNumber = function (value) {
    var normalized = common.normalizeUsPhoneNumber(value);
    return {
      ok: !!normalized,
      value: normalized,
      error: normalized ? "" : "Enter a valid US phone number."
    };
  };

  common.attachPhoneFormatting = function (inputId, modalId, showModalFeedback) {
    var input = document.getElementById(inputId);
    if (!input) {
      return;
    }
    input.addEventListener("blur", function () {
      var value = (input.value || "").trim();
      if (!value) {
        return;
      }
      var validation = common.validateUsPhoneNumber(value);
      if (!validation.ok) {
        (showModalFeedback || noop)(modalId, validation.error, true);
        return;
      }
    });
  };

  common.postForm = function (url, data) {
    return fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8" },
      credentials: "same-origin",
      body: new URLSearchParams(data).toString()
    }).then(function (r) {
      return r.text().then(function (text) {
        try {
          var parsed = JSON.parse(text);
          if (!r.ok && parsed && parsed.ok !== false) {
            parsed.ok = false;
          }
          return parsed;
        } catch (err) {
          var fallbackMessage = "Request failed.";
          if (r.status) {
            fallbackMessage = "Request failed (" + r.status + " " + (r.statusText || "Error") + ").";
          }
          console.error("postForm JSON parse failed.", {
            url: url,
            status: r.status,
            bodyPreview: (text || "").slice(0, 500),
            error: err
          });
          return {
            ok: false,
            error: fallbackMessage
          };
        }
      });
    }).catch(function (err) {
      console.error("postForm request failed.", {
        url: url,
        error: err
      });
      return {
        ok: false,
        error: "Network error. Please try again."
      };
    });
  };

  common.getJson = function (url) {
    return fetch(url, {
      method: "GET",
      credentials: "same-origin"
    }).then(function (r) { return r.json(); });
  };

  common.appendNotes = function (existingNotes, newNotes) {
    var current = (existingNotes || "").trim();
    var incoming = (newNotes || "").trim();
    if (!incoming) {
      return current;
    }
    if (!current) {
      return incoming;
    }
    if (incoming === current || incoming.indexOf(current) === 0) {
      return incoming;
    }
    return current + "\n" + incoming;
  };

  common.buildCustomerName = function (firstName, lastName) {
    return [firstName || "", lastName || ""].join(" ").trim();
  };

  common.setFieldDisabled = function (id, disabled) {
    var input = document.getElementById(id);
    if (input) {
      input.readOnly = !!disabled;
      input.classList.toggle("autofilled-lock", !!disabled);
      input.setAttribute("aria-disabled", disabled ? "true" : "false");
    }
  };

  common.setSelectOptions = function (selectElement, items, placeholderText) {
    if (!selectElement) {
      return;
    }

    selectElement.innerHTML = "";
    var placeholder = document.createElement("option");
    placeholder.value = "";
    placeholder.textContent = placeholderText;
    selectElement.appendChild(placeholder);

    (items || []).forEach(function (item) {
      var option = document.createElement("option");
      option.value = item.code || "";
      option.textContent = item.name || item.code || "";
      selectElement.appendChild(option);
    });
  };

  common.setSelectValues = function (selectElement, values, placeholder) {
    if (!selectElement) {
      return;
    }

    selectElement.innerHTML = "";
    var placeholderOption = document.createElement("option");
    placeholderOption.value = "";
    placeholderOption.textContent = placeholder;
    selectElement.appendChild(placeholderOption);

    (values || []).forEach(function (item) {
      var option = document.createElement("option");
      option.value = item.value;
      option.textContent = item.label;
      option.disabled = !!item.disabled;
      if (!(typeof item.endtime === "undefined")) {
        option.setAttribute("data-slotEnd", item.endtime);
      }
      selectElement.appendChild(option);
    });
  };

  common.parseIsoDurationToMinutes = function (text) {
    var match = /^P(?:(\d+)D)?(?:T(?:(\d+)H)?(?:(\d+)M)?)?$/i.exec((text || "").trim());
    if (!match) {
      return 0;
    }
    var days = parseInt(match[1] || "0", 10);
    var hours = parseInt(match[2] || "0", 10);
    var minutes = parseInt(match[3] || "0", 10);
    return (((days * 24) + hours) * 60) + minutes;
  };

  common.normalizeScheduleWindow = function (openMinutes, closeMinutes) {
    var dayMinutes = 24 * 60;
    if (openMinutes < 0 || closeMinutes < 0) {
      return null;
    }
    if (closeMinutes <= openMinutes) {
      closeMinutes += 12 * 60;
    }
    if (closeMinutes <= openMinutes) {
      closeMinutes += 12 * 60;
    }
    if (closeMinutes > dayMinutes) {
      closeMinutes = dayMinutes;
    }
    if (closeMinutes <= openMinutes) {
      return null;
    }
    return { open: openMinutes, close: closeMinutes };
  };

  common.getWeekdayCode = function (dateValue) {
    var day = dateValue.getDay();
    return day === 0 ? "7" : String(day);
  };

  common.parseDateOnly = function (value) {
    if (!value) {
      return null;
    }
    var raw = value.toString().trim();
    if (!raw) {
      return null;
    }

    var date = new Date(raw);
    if (!isNaN(date.getTime())) {
      date.setHours(0, 0, 0, 0);
      return date;
    }

    var iso = /^(\d{4})-(\d{2})-(\d{2})/.exec(raw);
    if (iso) {
      date = new Date(parseInt(iso[1], 10), parseInt(iso[2], 10) - 1, parseInt(iso[3], 10));
      date.setHours(0, 0, 0, 0);
      return date;
    }

    var mdy = /^(\d{1,2})\/(\d{1,2})\/(\d{4})/.exec(raw);
    if (mdy) {
      date = new Date(parseInt(mdy[3], 10), parseInt(mdy[1], 10) - 1, parseInt(mdy[2], 10));
      date.setHours(0, 0, 0, 0);
      return date;
    }

    return null;
  };

  common.isScheduleActiveOnDate = function (schedule, dateValue) {
    if (!schedule || !dateValue) {
      return false;
    }
    var day = new Date(dateValue.getTime());
    day.setHours(0, 0, 0, 0);
    var begin = common.parseDateOnly(schedule.dateBegin || "");
    var end = common.parseDateOnly(schedule.dateEnd || "");
    if (begin && day < begin) {
      return false;
    }
    if (end && day > end) {
      return false;
    }
    var weekly = (schedule.weeklySch || "").toString();
    if (!weekly) {
      return true;
    }
    return weekly.indexOf(common.getWeekdayCode(dateValue)) >= 0;
  };

  common.getMinimumFutureMinutes = function (dateValue) {
    if (!(dateValue instanceof Date)) {
      return null;
    }

    var now = new Date();
    if (now.getFullYear() !== dateValue.getFullYear() ||
        now.getMonth() !== dateValue.getMonth() ||
        now.getDate() !== dateValue.getDate()) {
      return null;
    }

    return (now.getHours() * 60) + now.getMinutes() + 1;
  };

  common.getEntryDateWindowLimits = function () {
    var minDate = new Date();
    minDate.setHours(0, 0, 0, 0);
    var maxDate = new Date(minDate.getTime());
    maxDate.setMonth(maxDate.getMonth() + 6);
    return {
      min: common.toIsoDate(minDate),
      max: common.toIsoDate(maxDate)
    };
  };

  common.getEntryCalendarBounds = function () {
    var limits = common.getEntryDateWindowLimits();
    return {
      min: new Date(limits.min + "T00:00:00"),
      max: new Date(limits.max + "T00:00:00")
    };
  };

  common.to24Hour = function (value) {
    var match = /^\s*(\d{1,2})\:(\d{2})\s*([AaPp][Mm])\s*$/.exec(value || "");
    if (!match) {
      return "00:00";
    }
    var hour = parseInt(match[1], 10) % 12;
    if (match[3].toUpperCase() === "PM") {
      hour += 12;
    }
    return twoDigits(hour) + ":" + match[2];
  };

  common.parseClockMinutes = function (value) {
    var text = common.to24Hour(value);
    var parts = text.split(":");
    if (parts.length !== 2) {
      return null;
    }

    var hour = parseInt(parts[0], 10);
    var minute = parseInt(parts[1], 10);
    if (isNaN(hour) || isNaN(minute)) {
      return null;
    }

    return (hour * 60) + minute;
  };

  common.formatRouteDateLabel = function (dateValue) {
    var labels = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
    var months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
    return labels[dateValue.getDay()] + ", " + months[dateValue.getMonth()] + " " + dateValue.getDate() + ", " + dateValue.getFullYear();
  };

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

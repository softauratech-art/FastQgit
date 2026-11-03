(function () {
    'use strict';
    var queues = [], page = 0, turn = 0, paused = false, stopped = false, state = 'connecting', refreshTimer;
    var root = document.getElementById('queues'), updated = document.getElementById('updated');
    function size() { return window.innerWidth <= 700 ? 1 : window.innerWidth <= 1100 ? 2 : 3; }
    function node(tag, text, cls) {
        var n = document.createElement(tag);
        if (text !== undefined) n.textContent = text;
        if (cls) n.className = cls;
        return n;
    }
    function setState(value) {
        state = value; document.body.dataset.state = value;
        root.setAttribute('aria-busy', String(value === 'connecting'));
        var label = document.getElementById('connectionLabel');
        if (label) label.textContent = value === 'live' ? 'Live updates' : value === 'connecting' ? 'Connecting' : value === 'denied' ? 'Access ended' : 'Reconnecting';
    }
    function section(title, people, limit, serving) {
        var el = node('section', undefined, 'section' + (serving ? ' serving' : ''));
        var heading = node('div', undefined, 'section-head');
        heading.appendChild(node('h3', title));
        var count = node('span', String(people.length), 'section-count');
        count.setAttribute('aria-label', people.length + ' ' + title.toLowerCase());
        heading.appendChild(count); el.appendChild(heading);
        var start = serving ? (turn % Math.max(1, Math.ceil(people.length / limit))) * limit : 0;
        people.slice(start, start + limit).forEach(function (person) {
            var row = node('div', undefined, 'person'), label = node('div', undefined, 'person-name');
            label.appendChild(node('strong', person.name || 'Guest'));
            label.appendChild(node('small', person.kind, person.kind === 'Walk-in' ? 'walkin' : 'appointment'));
            var time = node('div', undefined, 'visit-time');
            time.appendChild(node('small', person.kind === 'Walk-in' ? 'Arrived' : 'Scheduled'));
            time.appendChild(node('time', person.time));
            row.appendChild(label); row.appendChild(time); el.appendChild(row);
        });
        if (!serving) {
            for (var slot = Math.min(people.length, limit); slot < limit; slot++) {
                var placeholder = node('div', '\u2014', 'person person-placeholder');
                placeholder.setAttribute('aria-label', 'Empty waiting slot');
                el.appendChild(placeholder);
            }
        } else if (!people.length) el.appendChild(node('p', 'No one being served', 'empty'));
        if (serving && people.length > limit) el.appendChild(node('div', 'Showing ' + (start + 1) + '\u2013' + Math.min(start + limit, people.length) + ' of ' + people.length, 'range'));
        return el;
    }
    function rowLimits() {
        return { serving: window.innerHeight >= 1200 ? 2 : 1, waiting: 3 };
    }
    function render() {
        var count = Math.max(1, Math.ceil(queues.length / size())); page = (page + count) % count;
        root.replaceChildren();
        if (!queues.length) root.appendChild(node('div', stopped ? 'Display access is unavailable.'
            : state === 'connecting' ? 'Connecting to the queue service...'
            : state === 'offline' ? 'Queue information is temporarily unavailable. Please wait while we reconnect.'
            : 'No queues available for display.', 'notice'));
        var limits = rowLimits();
        queues.slice(page * size(), (page + 1) * size()).forEach(function (queue) {
            var card = node('article', undefined, 'queue'), heading = node('div', undefined, 'queue-head');
            heading.appendChild(node('h2', queue.name)); card.appendChild(heading);
            card.appendChild(section('Now serving', queue.serving, limits.serving, true));
            card.appendChild(section('Waiting', queue.waiting, limits.waiting, false)); root.appendChild(card);
        });
        document.getElementById('slideLabel').textContent = 'Queues ' + (queues.length ? page * size() + 1 : 0) + '\u2013' + Math.min((page + 1) * size(), queues.length) + ' / ' + queues.length;
    }
    function move(delta) { page += delta; if (page >= Math.ceil(queues.length / size()) || page < 0) turn++; render(); }
    function clock() {
        var now = new Date();
        document.getElementById('clock').textContent = now.toLocaleTimeString('en-US', {timeZone:'America/New_York',hour:'numeric',minute:'2-digit'});
        document.getElementById('date').textContent = now.toLocaleDateString('en-US', {timeZone:'America/New_York',weekday:'long',month:'long',day:'numeric',year:'numeric'});
    }
    async function refresh() {
        if (stopped) return;
        var controller = new AbortController(), timeout = setTimeout(function () { controller.abort(); }, 12000);
        try {
            var response = await fetch(document.body.dataset.feed, {credentials:'same-origin',cache:'no-store',signal:controller.signal});
            if (response.status === 401 || response.status === 403 || response.redirected) {
                stopped = true; setState('denied'); queues = []; render(); throw new Error('Access ended. Sign in again or contact your administrator.');
            }
            if (!response.ok) throw new Error('Connection unavailable. Retrying\u2026');
            var data = await response.json();
            if (!Array.isArray(data.queues)) throw new Error('Invalid display response. Retrying\u2026');
            queues = data.queues; setState('live'); render(); updated.className = '';
            updated.textContent = 'Updated ' + new Date().toLocaleTimeString('en-US',{timeZone:'America/New_York',hour:'numeric',minute:'2-digit',second:'2-digit'}) + ' \u00b7 Eastern Time';
        } catch (error) {
            // Remove names on failure instead of presenting old data as current.
            if (!stopped) setState('offline');
            queues = []; render(); updated.className = 'error';
            updated.textContent = error.name === 'AbortError' ? 'Connection timed out. Retrying\u2026' : error.message;
        } finally {
            clearTimeout(timeout);
            if (!stopped) refreshTimer = setTimeout(refresh, 15000);
        }
    }
    document.getElementById('previous').onclick = function () { move(-1); };
    document.getElementById('next').onclick = function () { move(1); };
    document.getElementById('pause').onclick = function () {
        paused = !paused; this.textContent = paused ? 'Resume' : 'Pause'; this.setAttribute('aria-pressed', String(paused));
        var label = document.getElementById('rotationLabel');
        if (label) label.textContent = paused ? 'Rotation paused' : 'Rotates every 10 seconds';
    };
    document.getElementById('fullscreen').onclick = function () {
        if (!document.documentElement.requestFullscreen) { updated.textContent = 'Full screen unavailable in this browser.'; return; }
        var action = document.fullscreenElement ? document.exitFullscreen() : document.documentElement.requestFullscreen();
        if (action && action.catch) action.catch(function () { updated.textContent = 'Full screen unavailable in this browser.'; });
    };
    window.addEventListener('resize', render);
    window.addEventListener('pagehide', function () { stopped = true; clearTimeout(refreshTimer); });
    setInterval(function () { if (!paused) move(1); }, 10000);
    setInterval(clock, 1000); setState('connecting'); render(); clock(); refresh();
})();

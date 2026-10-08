(function () {
    'use strict';
    var queues = [], page = 0, turn = 0, paused = false, stopped = false, refreshTimer;
    var root = document.getElementById('queues'), updated = document.getElementById('updated');
    function size() { return window.innerWidth <= 700 ? 1 : window.innerWidth <= 1100 ? 2 : 3; }
    function node(tag, text, cls) {
        var n = document.createElement(tag);
        if (text !== undefined) n.textContent = text;
        if (cls) n.className = cls;
        return n;
    }
    function section(title, people, limit, serving) {
        var el = node('section', undefined, 'section' + (serving ? ' serving' : ''));
        el.appendChild(node('h3', title + ' · ' + people.length));
        var start = (turn % Math.max(1, Math.ceil(people.length / limit))) * limit;
        people.slice(start, start + limit).forEach(function (person) {
            var row = node('div', undefined, 'person'), label = node('div');
            label.appendChild(node('strong', person.name || 'Guest'));
            label.appendChild(node('small', person.kind));
            row.appendChild(label); row.appendChild(node('time', person.time)); el.appendChild(row);
        });
        if (!people.length) el.appendChild(node('p', serving ? 'No one being served' : 'No one waiting', 'empty'));
        if (people.length > limit) el.appendChild(node('div', 'Showing ' + (start + 1) + '–' + Math.min(start + limit, people.length) + ' of ' + people.length, 'range'));
        return el;
    }
    function render() {
        var count = Math.max(1, Math.ceil(queues.length / size())); page = (page + count) % count;
        root.replaceChildren();
        if (!queues.length) root.appendChild(node('div', stopped ? 'Display access is unavailable.' : 'No queues available for display.', 'notice'));
        queues.slice(page * size(), (page + 1) * size()).forEach(function (queue) {
            var card = node('article', undefined, 'queue'); card.appendChild(node('h2', queue.name));
            card.appendChild(section('Now serving', queue.serving, window.innerHeight >= 1000 ? 2 : 1, true));
            card.appendChild(section('Waiting', queue.waiting, window.innerHeight >= 1100 ? 5 : window.innerHeight >= 900 ? 4 : window.innerHeight > 720 ? 3 : 2, false)); root.appendChild(card);
        });
        document.getElementById('slideLabel').textContent = 'Queues ' + (queues.length ? page * size() + 1 : 0) + '–' + Math.min((page + 1) * size(), queues.length) + ' / ' + queues.length;
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
                stopped = true; queues = []; render(); throw new Error('Access ended. Sign in again or contact your administrator.');
            }
            if (!response.ok) throw new Error('Connection unavailable. Retrying…');
            var data = await response.json();
            if (!Array.isArray(data.queues)) throw new Error('Invalid display response. Retrying…');
            queues = data.queues; render(); updated.className = '';
            updated.textContent = 'Updated ' + new Date().toLocaleTimeString('en-US',{timeZone:'America/New_York',hour:'numeric',minute:'2-digit',second:'2-digit'}) + ' · rotates every 10s';
        } catch (error) {
            // Remove names on failure instead of presenting old data as current.
            queues = []; render(); updated.className = 'error';
            updated.textContent = error.name === 'AbortError' ? 'Connection timed out. Retrying…' : error.message;
        } finally {
            clearTimeout(timeout);
            if (!stopped) refreshTimer = setTimeout(refresh, 15000);
        }
    }
    document.getElementById('previous').onclick = function () { move(-1); };
    document.getElementById('next').onclick = function () { move(1); };
    document.getElementById('pause').onclick = function () { paused = !paused; this.textContent = paused ? 'Resume' : 'Pause'; this.setAttribute('aria-pressed', String(paused)); };
    document.getElementById('fullscreen').onclick = function () {
        if (!document.documentElement.requestFullscreen) { updated.textContent = 'Full screen unavailable in this browser.'; return; }
        var action = document.fullscreenElement ? document.exitFullscreen() : document.documentElement.requestFullscreen();
        if (action && action.catch) action.catch(function () { updated.textContent = 'Full screen unavailable in this browser.'; });
    };
    window.addEventListener('resize', render);
    window.addEventListener('pagehide', function () { stopped = true; clearTimeout(refreshTimer); });
    setInterval(function () { if (!paused) move(1); }, 10000);
    setInterval(clock, 1000); clock(); refresh();
})();

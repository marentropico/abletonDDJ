let ws;
let jogAngles = { 'JogWheel_Left': 0, 'JogWheel_Right': 0 };
let activeStates = {}; // Keep track of button states

function connectWebSocket() {
    ws = new WebSocket("ws://localhost:5000/api/ws");
    const statusText = document.getElementById("status-overlay");

    ws.onopen = () => {
        console.log("WebSocket connected");
        statusText.textContent = "DDJ-400 Online";
        statusText.style.background = "rgba(16, 185, 129, 0.8)";
        setTimeout(() => { statusText.style.opacity = '0'; }, 2000);
    };

    ws.onclose = () => {
        statusText.textContent = "ConexÃ£o perdida. Tentando novamente...";
        statusText.style.background = "rgba(220, 38, 38, 0.8)";
        statusText.style.opacity = '1';
        setTimeout(connectWebSocket, 1000);
    };

    ws.onmessage = (event) => {
    const logConsole = document.getElementById("log-console");
    if (logConsole && event.data) { logConsole.textContent = "Raw recv: " + event.data.substring(0,50); }
        try {
            const data = JSON.parse(event.data);
            if(!data) return;
            handleMidiUpdate(data.control, data.value);
        } catch (e) {
            // Handle plain string fallback just in case
            if (typeof event.data === 'string' && event.data.startsWith('{')) {
                 const data = JSON.parse(event.data);
                 handleMidiUpdate(data.control, data.value);
            } else {
                 console.log("Msg:", event.data);
            }
        }
    };
}

function handleMidiUpdate(control, value) {
    const logConsole = document.getElementById("log-console");
    if (logConsole) {
        logConsole.textContent = `Recebido: ${control} | Valor: ${value}`;
        logConsole.style.color = "#38bdf8";
        setTimeout(() => logConsole.style.color = "#f8fafc", 100);
    }
    const el = document.getElementById(control);
    if (!el) return;

    // Detect Type
    const isKnob = el.classList.contains('knob') || el.querySelector('.knob-pointer') !== null;
    const isFader = el.classList.contains('fader');
    const isJog = el.classList.contains('jog');
    const isBtn = el.classList.contains('btn') || el.classList.contains('pad');

    // Button / Pad Glow
    if (isBtn) {
        if (value > 0) {
            el.classList.add('active-midi');
            activeStates[control] = true;
        } else {
            el.classList.remove('active-midi');
            activeStates[control] = false;
        }
    } else {
        // Flash briefly for knobs and faders on move
        el.classList.add('active-midi');
        clearTimeout(activeStates[control]);
        activeStates[control] = setTimeout(() => {
            el.classList.remove('active-midi');
        }, 100);
    }

    if (isKnob) {
        // angle from -135 to +135
        const angle = ((value - 64) / 63) * 135;
        const base = el.querySelector('.knob-base');
        if (base) {
            const cx = base.getAttribute('cx');
            const cy = base.getAttribute('cy');
            // Just rotate the pointer and face, not the base (base is static)
            const ptr = el.querySelector('.knob-pointer');
            const face = el.querySelector('.knob-face');
            if (ptr) ptr.setAttribute('transform', `rotate(${angle}, ${cx}, ${cy})`);
            if (face) face.setAttribute('transform', `rotate(${angle}, ${cx}, ${cy})`);
        }
    } 
    else if (isFader) {
        const track = el.querySelector('.fader-track');
        const cap = el.querySelector('.fader-cap');
        if (track && cap) {
            const tw = parseFloat(track.getAttribute('width'));
            const th = parseFloat(track.getAttribute('height'));
            const cw = parseFloat(cap.getAttribute('width'));
            const ch = parseFloat(cap.getAttribute('height'));

            if (th > tw) {
                // Vertical Fader
                const maxDist = (th - ch) / 2;
                let offset = -((value - 64) / 63) * maxDist;
                // Exception for TEMPO fader: higher value usually means lower pitch (slider down), 
                // but let's just stick to raw value: value 127 = top (offset negative), value 0 = bottom (offset positive).
                cap.setAttribute('transform', `translate(0, \$\{offset\})`);
                const groove = el.querySelector('.fader-groove');
                if (groove) groove.setAttribute('transform', `translate(0, \$\{offset\})`);
            } else {
                // Horizontal (Crossfader)
                const maxDist = (tw - cw) / 2;
                let offset = ((value - 64) / 63) * maxDist;
                cap.setAttribute('transform', `translate(\$\{offset\}, 0)`);
                const groove = el.querySelector('.fader-groove');
                if (groove) groove.setAttribute('transform', `translate(\$\{offset\}, 0)`);
            }
        }
    }
    else if (isJog) {
        // value > 64 is forward, value < 64 is backward (relative)
        const outer = el.querySelector('.jog-outer');
        if (outer) {
            const cx = outer.getAttribute('cx');
            const cy = outer.getAttribute('cy');
            
            let delta = 0;
            if (value > 0 && value < 64) {
                 delta = value; // Forward
            } else if (value >= 64) {
                 delta = -(128 - value); // Backward
            }
            
            jogAngles[control] += (delta * 3); // speed multiplier
            
            // Apply rotation to the whole group except the outer ring and logo
            const children = el.children;
            for(let i=0; i<children.length; i++) {
                const child = children[i];
                if (!child.classList.contains('jog-outer') && !child.classList.contains('jog-logo-box')) {
                    child.setAttribute('transform', `rotate(${jogAngles[control]}, ${cx}, ${cy})`);
                }
            }
        }
    }
}

document.addEventListener("DOMContentLoaded", () => {
    const tip = document.getElementById('tooltip');
    document.querySelectorAll('.hit').forEach(function(el){
        el.addEventListener('mousemove', function(e){
            if (el.getAttribute('data-tip')) {
                tip.textContent = el.getAttribute('data-tip');
                tip.style.left = (e.clientX + 16) + 'px';
                tip.style.top = (e.clientY + 16) + 'px';
                tip.classList.add('show');
            }
        });
        el.addEventListener('mouseleave', function(){
            tip.classList.remove('show');
        });
    });

    connectWebSocket();
});







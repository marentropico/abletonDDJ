let ws;
let jogAngles = { 'JogWheel_Left': 0, 'JogWheel_Right': 0 };
let activeStates = {}; // Keep track of button states
let calibrationMappings = {}; // Load calibration dynamically

const MAPPING_INFO = {
    // Transporte
    "Play_Left": { 
        "title": "Play/Pause (Deck 1)", 
        "desc": "Alterna a reprodução global do Ableton Live (toca ou pausa a música).", 
        "shift": "Stop All Clips: Para imediatamente a reprodução de todos os clipes que estiverem tocando na Session View (Panic Button)." 
    },
    "Play_Right": { 
        "title": "Play/Pause (Deck 2)", 
        "desc": "Alterna a reprodução global do Ableton Live (toca ou pausa a música).", 
        "shift": "Stop All Clips: Para imediatamente a reprodução de todos os clipes que estiverem tocando na Session View (Panic Button)." 
    },
    "Cue_Left": { 
        "title": "CUE (Deck 1)", 
        "desc": "Continua a reprodução da música exatamente a partir do local atual da agulha/marcador.", 
        "shift": "Back to Arrangement: Restaura a reprodução para a Timeline do Arrangement View." 
    },
    "Cue_Right": { 
        "title": "CUE (Deck 2)", 
        "desc": "Continua a reprodução da música exatamente a partir do local atual da agulha/marcador.", 
        "shift": "Back to Arrangement: Restaura a reprodução para a Timeline do Arrangement View." 
    },
    "ReloopExit_Left": { 
        "title": "Reloop/Exit (Deck 1)", 
        "desc": "Looper inteligente contextual:\n• Slot vazio: Arma a pista e grava novo clipe.\n• Slot gravando: Finaliza gravação e inicia loop.\n• Slot parado/tocando: Inicia Overdub.", 
        "shift": "N/A" 
    },
    "ReloopExit_Right": { 
        "title": "Reloop/Exit (Deck 2)", 
        "desc": "Arrangement Record: Liga ou desliga a gravação global da timeline do projeto.", 
        "shift": "N/A" 
    },
    "Sync_Left": { 
        "title": "Beat Sync (Deck 1)", 
        "desc": "Quantização rápida: Quantiza as notas MIDI selecionadas no clipe ativo (atalho Ctrl+U).", 
        "shift": "Metrônomo: Liga ou desliga o metrônomo do Ableton Live (atalho Ctrl+Option+M)." 
    },
    "Sync_Right": { 
        "title": "Beat Sync (Deck 2)", 
        "desc": "Livre para mapeamento MIDI genérico no Ableton Live.", 
        "shift": "N/A" 
    },

    // Jogs
    "JogWheel_Left": { 
        "title": "Jog Wheel (Deck 1)", 
        "desc": "Navegação na Timeline: Girar o anel externo simula o scroll horizontal do mouse para caminhar pelo arranjo com follow de tela ativo.", 
        "shift": "N/A" 
    },
    "JogWheel_Right": { 
        "title": "Jog Wheel (Deck 2)", 
        "desc": "Zoom de Timeline: Girar o anel externo simula as teclas + e - do teclado para controlar o Zoom Horizontal no Arrangement View.", 
        "shift": "N/A" 
    },

    // Mixer
    "Volume_Left": { 
        "title": "Volume (Canal 1)", 
        "desc": "Fader físico desativado temporariamente na ponte C# devido a oscilações físicas de mau contato (hardware noise).", 
        "shift": "N/A" 
    },
    "Volume_Right": { 
        "title": "Volume (Canal 2)", 
        "desc": "Fader físico desativado temporariamente na ponte C# devido a oscilações físicas de mau contato (hardware noise).", 
        "shift": "N/A" 
    },
    "Crossfader": { 
        "title": "Crossfader", 
        "desc": "Needle Search: Move a agulha de reprodução (playhead) ao longo de todo o arranjo.", 
        "shift": "Timeline Loop Maker: Segure o botão FX ON/OFF e arraste o Crossfader para definir e redimensionar dinamicamente o tamanho do Loop ativo." 
    },
    "Trim_Left": { 
        "title": "Gain Trim (Canal 1)", 
        "desc": "Volume da Track: Controla o volume principal da pista que estiver selecionada/focada na tela.", 
        "shift": "N/A" 
    },
    "Trim_Right": { 
        "title": "Gain Trim (Canal 2)", 
        "desc": "Panning: Controla o Panning estéreo (balanço Esquerdo/Direito) da pista selecionada na tela.", 
        "shift": "N/A" 
    },
    "HeadphoneCue_Left": { 
        "title": "Headphones CUE (Canal 1)", 
        "desc": "Solo: Monitora (SOLO) a faixa selecionada.", 
        "shift": "N/A" 
    },
    "HeadphoneCue_Right": { 
        "title": "Headphones CUE (Canal 2)", 
        "desc": "Mute: Silencia (MUTE) a faixa selecionada.", 
        "shift": "N/A" 
    },

    // EQs & Knobs
    "EQ_High_Left": { 
        "title": "EQ High (Canal 1)", 
        "desc": "Controla a primeira Macro/Parâmetro do plugin ou rack focado na pista ativa (EQ High).", 
        "shift": "Ajuste Fino: Ajusta com velocidade reduzida a 25% para máxima precisão." 
    },
    "EQ_Mid_Left": { 
        "title": "EQ Mid (Canal 1)", 
        "desc": "Controla a segunda Macro/Parâmetro do plugin ou rack focado na pista ativa (EQ Mid).", 
        "shift": "Ajuste Fino: Ajusta com velocidade reduzida a 25% para máxima precisão." 
    },
    "EQ_Low_Left": { 
        "title": "EQ Low (Canal 1)", 
        "desc": "Controla a terceira Macro/Parâmetro do plugin ou rack focado na pista ativa (EQ Low).", 
        "shift": "Ajuste Fino: Ajusta com velocidade reduzida a 25% para máxima precisão." 
    },
    "Filter_Left": { 
        "title": "Filter (Canal 1)", 
        "desc": "Controla a quarta Macro/Parâmetro do plugin ou rack focado na pista ativa (Filter Cutoff).", 
        "shift": "Ajuste Fino: Ajusta com velocidade reduzida a 25% para máxima precisão." 
    },
    "EQ_High_Right": { 
        "title": "EQ High (Canal 2)", 
        "desc": "Controla a quinta Macro/Parâmetro do plugin focado na pista ativa (EQ High 2).", 
        "shift": "Ajuste Fino: Ajusta com velocidade reduzida a 25% para máxima precisão." 
    },
    "EQ_Mid_Right": { 
        "title": "EQ Mid (Canal 2)", 
        "desc": "Controla a sexta Macro/Parâmetro do plugin focado na pista ativa (EQ Mid 2).", 
        "shift": "Ajuste Fino: Ajusta com velocidade reduzida a 25% para máxima precisão." 
    },
    "EQ_Low_Right": { 
        "title": "EQ Low (Canal 2)", 
        "desc": "Controla a sétima Macro/Parâmetro do plugin focado na pista ativa (EQ Low 2).", 
        "shift": "Ajuste Fino: Ajusta com velocidade reduzida a 25% para máxima precisão." 
    },
    "Filter_Right": { 
        "title": "Filter (Canal 2)", 
        "desc": "Controla a oitava Macro/Parâmetro do plugin focado na pista ativa (Filter 2).", 
        "shift": "Ajuste Fino: Ajusta com velocidade reduzida a 25% para máxima precisão." 
    },

    // Globais e Fone
    "HeadphoneMixing": { 
        "title": "Headphones Mixing", 
        "desc": "BPM Master: Controla dinamicamente a velocidade (BPM geral) do Ableton Live.", 
        "shift": "N/A" 
    },
    "HeadphoneLevel": { 
        "title": "Headphones Level", 
        "desc": "Knob analógico de hardware da controladora. Controla o volume de monitoramento físico nos fones.", 
        "shift": "N/A" 
    },
    "MasterLevel": { 
        "title": "Master Level", 
        "desc": "Knob analógico de hardware da controladora. Controla o volume master físico enviado para a saída de áudio.", 
        "shift": "N/A" 
    },
    "MasterCue": {
        "title": "Master CUE",
        "desc": "Envia o canal Master do Ableton Live para monitoramento de fone de ouvido.",
        "shift": "N/A"
    },
    "TempoSlider_Left": { 
        "title": "Pitch Fader (Deck 1)", 
        "desc": "Ajuste fino de BPM: Permite calibrar o BPM do projeto ativamente com base na escala analógica do fader.", 
        "shift": "N/A" 
    },
    "TempoSlider_Right": { 
        "title": "Pitch Fader (Deck 2)", 
        "desc": "Livre para mapeamentos MIDI manuais adicionais no Ableton Live.", 
        "shift": "N/A" 
    },

    // Navegação e Views
    "Load_Left": { 
        "title": "Load Left", 
        "desc": "Alternador de Foco: Alterna o foco do teclado do Ableton entre o Navegador (Browser lateral) e as Pistas (Tracks).", 
        "shift": "Related Tracks: Abre a aba de faixas recomendadas." 
    },
    "Load_Right": { 
        "title": "Load Right", 
        "desc": "Alternador de Telas: Alterna visualmente o Ableton entre a Session View (Grelha de Clipes) e o Arrangement View (Timeline).", 
        "shift": "N/A" 
    },
    "LoopIn_Left": { 
        "title": "Loop In (Deck 1)", 
        "desc": "Duplicar: Duplica instantaneamente o clipe, pista ou cena selecionada (simula atalho Ctrl+D).", 
        "shift": "N/A" 
    },
    "LoopOut_Left": { 
        "title": "Loop Out (Deck 1)", 
        "desc": "Deletar: Exclui o clipe, pista ou elemento selecionado (simula tecla Delete).", 
        "shift": "N/A" 
    },
    "LoopCallLeft_Left": { 
        "title": "Loop Call ◁ (Deck 1)", 
        "desc": "Undo: Desfaz a última alteração realizada no projeto (simula atalho Ctrl+Z).", 
        "shift": "Loop 1/2X: Corta o tamanho do loop atual pela metade." 
    },
    "LoopCallRight_Left": { 
        "title": "Loop Call ▷ (Deck 1)", 
        "desc": "Redo: Refaz a última alteração desfeita no projeto (simula atalho Ctrl+Y).", 
        "shift": "Loop 2X: Dobra o tamanho do loop atual." 
    },
    "BrowseEncoder_Click": { 
        "title": "Browse Selector", 
        "desc": "Seletor de Browser:\n• Girar: Navega verticalmente pelas pastas, faixas ou plugins (simula setas Cima/Baixo).\n• Clicar: Carrega o sample/dispositivo ou abre pastas (simula Enter).", 
        "shift": "Seta Esquerda: Volta um nível de diretório ou recolhe a pasta atual." 
    },

    // Beat FX Section
    "BeatLeft": { 
        "title": "Beat FX Button Left ◁", 
        "desc": "Navegar Dispositivos: Move a seleção de dispositivos/efeitos para a esquerda na cadeia da pista ativa.", 
        "shift": "N/A" 
    },
    "BeatRight": { 
        "title": "Beat FX Button Right ▷", 
        "desc": "Navegar Dispositivos: Move a seleção de dispositivos/efeitos para a direita na cadeia da pista ativa.", 
        "shift": "N/A" 
    },
    "FxSelectDown": { 
        "title": "FX Select Down ▽", 
        "desc": "Criar faixa de Áudio: Cria uma nova pista de áudio no projeto (simula atalho Ctrl+T).", 
        "shift": "Criar faixa MIDI: Cria uma nova pista MIDI no projeto (simula atalho Ctrl+Shift+T)." 
    },
    "FxSelectUp": { 
        "title": "FX Select Up △", 
        "desc": "Criar faixa de Áudio: Cria uma nova pista de áudio no projeto (simula atalho Ctrl+T).", 
        "shift": "Criar faixa MIDI: Cria uma nova pista MIDI no projeto (simula atalho Ctrl+Shift+T)." 
    },
    "FxChannelSelect": { 
        "title": "FX Channel Select", 
        "desc": "Chave seletora física (1 / 2 / Master) de canal alvo para efeitos. Livre para mapeamento MIDI.", 
        "shift": "N/A" 
    },
    "LevelDepth": { 
        "title": "Level/Depth Knob", 
        "desc": "Knob Curinga: Controla diretamente o valor do parâmetro que estiver atualmente sob o foco do mouse no Ableton Live.", 
        "shift": "N/A" 
    },
    "FxOnOff": { 
        "title": "FX ON/OFF Button", 
        "desc": "Loop Helper:\n• Ao pressionar: Guarda a agulha atual e ativa o Loop.\n• Ao soltar: Trava a região como o loop ativo da música (simula atalho Ctrl+Shift+L).", 
        "shift": "N/A" 
    },

    // Pad Modes (Left & Right)
    "HotCueMode_Left": { "title": "Mode Button: Hot Cue (Deck 1)", "desc": "Alterna os pads do deck esquerdo para controlar o Lançamento de Clipes.", "shift": "Ativa modo Keyboard." },
    "BeatLoopMode_Left": { "title": "Mode Button: Beat Loop (Deck 1)", "desc": "Alterna os pads do deck esquerdo para controle do plugin de Looper.", "shift": "Ativa modo Pad FX 1." },
    "BeatJumpMode_Left": { "title": "Mode Button: Beat Jump (Deck 1)", "desc": "Alterna os pads do deck esquerdo para acionamento de efeitos momentâneos.", "shift": "Ativa modo Pad FX 2." },
    "SamplerMode_Left": { "title": "Mode Button: Sampler (Deck 1)", "desc": "Alterna os pads do deck esquerdo para tocar Drum Racks no canal MIDI 1.", "shift": "Ativa modo Key Shift." },
    "HotCueMode_Right": { "title": "Mode Button: Hot Cue (Deck 2)", "desc": "Alterna os pads do deck direito para controlar ativação e ligar/desligar botões de efeitos.", "shift": "Ativa modo Keyboard." },
    "BeatLoopMode_Right": { "title": "Mode Button: Beat Loop (Deck 2)", "desc": "Alterna os pads do deck direito para controle do plugin de Looper.", "shift": "Ativa modo Pad FX 1." },
    "BeatJumpMode_Right": { "title": "Mode Button: Beat Jump (Deck 2)", "desc": "Alterna os pads do deck direito para acionamento de efeitos momentâneos.", "shift": "Ativa modo Pad FX 2." },
    "SamplerMode_Right": { "title": "Mode Button: Sampler (Deck 2)", "desc": "Alterna os pads do deck direito para tocar Drum Racks no canal MIDI 2.", "shift": "Ativa modo Key Shift." },

    // Performance Pads (Left)
    "Pad1_Left": { "title": "Pad 1 (Deck 1)", "desc": "Sampler: Nota C1. Hot Cue: Dispara clipe 1 da Track 1. Beat Loop: Record/Play/Overdub.", "shift": "Hot Cue: Para ou apaga o clipe." },
    "Pad2_Left": { "title": "Pad 2 (Deck 1)", "desc": "Sampler: Nota C#1. Hot Cue: Dispara clipe 2 da Track 1. Beat Loop: Looper Stop.", "shift": "Hot Cue: Para ou apaga o clipe." },
    "Pad3_Left": { "title": "Pad 3 (Deck 1)", "desc": "Sampler: Nota D1. Hot Cue: Dispara clipe 3 da Track 1. Beat Loop: Looper Undo.", "shift": "Hot Cue: Para ou apaga o clipe." },
    "Pad4_Left": { "title": "Pad 4 (Deck 1)", "desc": "Sampler: Nota D#1. Hot Cue: Dispara clipe 4 da Track 1. Beat Loop: Looper Clear.", "shift": "Hot Cue: Para ou apaga o clipe." },
    "Pad5_Left": { "title": "Pad 5 (Deck 1)", "desc": "Sampler: Nota E1. Hot Cue: Dispara clipe 5 da Track 1. Beat Loop: N/A.", "shift": "Hot Cue: Para ou apaga o clipe." },
    "Pad6_Left": { "title": "Pad 6 (Deck 1)", "desc": "Sampler: Nota F1. Hot Cue: Dispara clipe 6 da Track 1. Beat Loop: N/A.", "shift": "Hot Cue: Para ou apaga o clipe." },
    "Pad7_Left": { "title": "Pad 7 (Deck 1)", "desc": "Sampler: Nota F#1. Hot Cue: Dispara clipe 7 da Track 1. Beat Loop: N/A.", "shift": "Hot Cue: Para ou apaga o clipe." },
    "Pad8_Left": { "title": "Pad 8 (Deck 1)", "desc": "Sampler: Nota G1. Hot Cue: Dispara clipe 8 da Track 1. Beat Loop: N/A.", "shift": "Hot Cue: Para ou apaga o clipe." },

    // Performance Pads (Right)
    "Pad1_Right": { "title": "Pad 1 (Deck 2)", "desc": "Sampler: Nota C1. Hot Cue: Inverte/liga botão ou interruptor 1 do plugin focado.", "shift": "N/A" },
    "Pad2_Right": { "title": "Pad 2 (Deck 2)", "desc": "Sampler: Nota C#1. Hot Cue: Inverte/liga botão ou interruptor 2 do plugin focado.", "shift": "N/A" },
    "Pad3_Right": { "title": "Pad 3 (Deck 2)", "desc": "Sampler: Nota D1. Hot Cue: Inverte/liga botão ou interruptor 3 do plugin focado.", "shift": "N/A" },
    "Pad4_Right": { "title": "Pad 4 (Deck 2)", "desc": "Sampler: Nota D#1. Hot Cue: Inverte/liga botão ou interruptor 4 do plugin focado.", "shift": "N/A" },
    "Pad5_Right": { "title": "Pad 5 (Deck 2)", "desc": "Sampler: Nota E1. Hot Cue: Inverte/liga botão ou interruptor 5 do plugin focado.", "shift": "N/A" },
    "Pad6_Right": { "title": "Pad 6 (Deck 2)", "desc": "Sampler: Nota F1. Hot Cue: Inverte/liga botão ou interruptor 6 do plugin focado.", "shift": "N/A" },
    "Pad7_Right": { "title": "Pad 7 (Deck 2)", "desc": "Sampler: Nota F#1. Hot Cue: Inverte/liga botão ou interruptor 7 do plugin focado.", "shift": "N/A" },
    "Pad8_Right": { "title": "Pad 8 (Deck 2)", "desc": "Sampler: Nota G1. Hot Cue: Inverte/liga botão ou interruptor 8 do plugin focado.", "shift": "N/A" },
};

async function loadCalibration() {
    try {
        const response = await fetch("/api/mappings");
        if (response.ok) {
            const data = await response.json();
            if (data && data.Controls) {
                calibrationMappings = data.Controls;
                console.log("Calibrated mappings loaded:", calibrationMappings);
            }
        }
    } catch (e) {
        console.error("Failed to load calibration mappings:", e);
    }
}

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
        statusText.textContent = "Conexão perdida. Tentando novamente...";
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
                cap.setAttribute('transform', `translate(0, ${offset})`);
                const groove = el.querySelector('.fader-groove');
                if (groove) groove.setAttribute('transform', `translate(0, ${offset})`);
            } else {
                // Horizontal (Crossfader)
                const maxDist = (tw - cw) / 2;
                let offset = ((value - 64) / 63) * maxDist;
                cap.setAttribute('transform', `translate(${offset}, 0)`);
                const groove = el.querySelector('.fader-groove');
                if (groove) groove.setAttribute('transform', `translate(${offset}, 0)`);
            }
        }
    }
    else if (isJog) {
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
            
            const children = el.children;
            for(let i=0; i<children.length; i++) {
                const child = children[i];
                if (!child.classList.contains('jog-outer') && !child.classList.contains('jog-logo-box') && !child.classList.contains('jog-led-ring')) {
                    child.setAttribute('transform', `rotate(${jogAngles[control]}, ${cx}, ${cy})`);
                }
            }
        }
    }
}

document.addEventListener("DOMContentLoaded", async () => {
    const tip = document.getElementById('tooltip');
    
    // Setup hover tooltips
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

    // Setup click details panel
    document.querySelectorAll('.hit').forEach(el => {
        el.addEventListener('click', (e) => {
            e.stopPropagation();
            const controlId = el.id;
            if (!controlId) return;

            const info = MAPPING_INFO[controlId] || { 
                "title": controlId.replace(/_/g, " "), 
                "desc": "Controle físico da DDJ-400 no Ableton Live.", 
                "shift": "N/A" 
            };
            
            // Populate panel
            document.getElementById('info-title').textContent = info.title;
            document.getElementById('info-desc').innerHTML = info.desc.replace(/\n/g, "<br>");
            document.getElementById('info-shift').innerHTML = (info.shift || '-').replace(/\n/g, "<br>");
            
            // Look up midi calibration details
            // For controls like Jogs or Browse Encoder that map left/right, try looking up base mapping name
            let calKey = controlId;
            if (controlId === "BrowseEncoder_Click") calKey = "BrowseEncoder_Click";
            else if (controlId === "JogWheel_Left") calKey = "JogWheel_Top_Touch";
            else if (controlId === "JogWheel_Right") calKey = "JogWheel_Top_Touch_Right";
            else if (controlId === "Volume_Left") calKey = "VolumeSlider_Left";
            else if (controlId === "Volume_Right") calKey = "VolumeSlider_Right";

            const cal = calibrationMappings[calKey] || calibrationMappings[controlId];
            if (cal) {
                document.getElementById('midi-status').textContent = "0x" + cal.Status.toString(16).toUpperCase() + " (" + cal.Status + ")";
                document.getElementById('midi-cc').textContent = "0x" + cal.Data1.toString(16).toUpperCase() + " (" + cal.Data1 + ")";
                document.getElementById('midi-type').textContent = cal.IsContinuous ? "Sim (Knob / Fader)" : "Não (Botão / Pad)";
            } else {
                document.getElementById('midi-status').textContent = 'N/A';
                document.getElementById('midi-cc').textContent = 'N/A';
                document.getElementById('midi-type').textContent = 'Mapeamento Virtual';
            }
            
            // Open panel
            document.getElementById('info-panel').classList.add('open');
        });
    });

    // Close panel event
    document.getElementById('info-close').addEventListener('click', (e) => {
        e.stopPropagation();
        document.getElementById('info-panel').classList.remove('open');
    });

    // Close panel when clicking outside
    document.body.addEventListener('click', () => {
        document.getElementById('info-panel').classList.remove('open');
    });
    
    document.getElementById('info-panel').addEventListener('click', (e) => {
        e.stopPropagation(); // prevent closing when clicking inside panel
    });

    // Load calibration and connect WS
    await loadCalibration();
    connectWebSocket();
});

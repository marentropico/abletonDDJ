# Mapeamento Dinâmico de Dispositivos: Ableton Native Devices (Live 12)

Para substituirmos a atual lógica genérica de `find_param_by_keywords` por algo realmente "inteligente", precisamos identificar a classe do plugin (`device.class_name`) e mapear os parâmetros **exatos** para os nossos 8 botões (4 do Deck Esquerdo, 4 do Deck Direito). 

Abaixo está o levantamento robusto dos parâmetros expostos pela API do Ableton Live 12 para os principais plugins, e uma proposta de como alocá-los ergonomicamente nos botões da DDJ-400.

---

## 🎚️ 1. EQs e Filtros (Onde a precisão importa)

### **EQ Eight** (`Eq8`)
O EQ Eight possui 8 bandas, mas por padrão as bandas 1 a 4 vêm ativadas (sendo 1 Low Cut/Shelf e 4 High Cut/Shelf). A nossa controladora possui exatamente 8 botões disponíveis.
- **Parâmetros da API:** `1 Gain A`, `2 Gain A`, `3 Gain A`, `4 Gain A`, `1 Frequency A`, `2 Frequency A`, `3 Frequency A`, `4 Frequency A`, `1 Resonance A`, etc.
- **Mapeamento Proposto:**
  - **Deck Esquerdo (Ganho):**
    - High (Knob 1): `4 Gain A` (Agudos)
    - Mid (Knob 2): `3 Gain A` (Médios-Altos)
    - Low (Knob 3): `2 Gain A` (Médios-Graves)
    - Filter (Knob 4): `1 Gain A` (Graves/Sub)
  - **Deck Direito (Frequência):**
    - High (Knob 5): `4 Frequency A`
    - Mid (Knob 6): `3 Frequency A`
    - Low (Knob 7): `2 Frequency A`
    - Filter (Knob 8): `1 Frequency A`

### **Channel EQ** (`ChannelEq`)
Desenhado para ser simples, imitando o mixer de um DJ.
- **Parâmetros da API:** `Low`, `Mid`, `High`, `Mid Freq`, `Output`, `HPF On`
- **Mapeamento Proposto (Centralizado no Deck Esquerdo):**
  - High (1): `High`
  - Mid (2): `Mid`
  - Low (3): `Low`
  - Filter (4): `Mid Freq` (Ajusta a varredura do médio)

### **Auto Filter** (`AutoFilter`)
- **Parâmetros da API:** `Frequency`, `Resonance`, `Filter Type`, `LFO Amount`, `LFO Rate`, `Env. Modulation`
- **Mapeamento Proposto:**
  - High (1): `Frequency` (Cutoff)
  - Mid (2): `Resonance` (Q)
  - Low (3): `Env. Modulation`
  - Filter (4): `LFO Amount`

---

## 💥 2. Dinâmica e Cor (Saturação e Compressão)

### **Saturator** (`Saturator`)
- **Parâmetros da API:** `Drive`, `Color`, `Base`, `Freq`, `Width`, `Depth`, `Dry/Wet`, `Output`
- **Mapeamento Proposto:**
  - High (1): `Drive`
  - Mid (2): `Base`
  - Low (3): `Freq`
  - Filter (4): `Depth`
  - High (5): `Dry/Wet` (Deck Direito)

### **Drum Buss** (`DrumBuss`)
- **Parâmetros da API:** `Drive`, `Crunch`, `Damp`, `Transients`, `Boom`, `Freq`, `Decay`, `Dry/Wet`
- **Mapeamento Proposto:**
  - High (1): `Drive`
  - Mid (2): `Crunch`
  - Low (3): `Transients`
  - Filter (4): `Boom`
  - High (5): `Freq` (Ajuste tonal do Boom no Deck Direito)

### **Roar** (Novo no Live 12 - `Roar`)
Dispositivo modular complexo de saturação.
- **Parâmetros da API:** `Drive`, `Tone`, `Feedback`, `Amount`, `Blend`
- **Mapeamento Proposto:**
  - High (1): `Drive`
  - Mid (2): `Tone`
  - Low (3): `Feedback`
  - Filter (4): `Amount`
  - High (5): `Blend` (Deck Direito)

### **Glue Compressor** (`GlueCompressor`)
- **Parâmetros da API:** `Threshold`, `Ratio`, `Attack`, `Release`, `Makeup`, `Dry/Wet`
- **Mapeamento Proposto:**
  - High (1): `Threshold`
  - Mid (2): `Makeup`
  - Low (3): `Attack`
  - Filter (4): `Release`
  - High (5): `Dry/Wet` (Deck Direito)

---

## 🌌 3. Tempo e Espaço (Reverbs e Delays)

### **Delay** (`Delay`)
- **Parâmetros da API:** `L Sync Rate`, `R Sync Rate`, `Feedback`, `Dry/Wet`, `Filter Freq`, `Filter Width`
- **Mapeamento Proposto:**
  - High (1): `L Sync Rate` (Tempo)
  - Mid (2): `Feedback`
  - Low (3): `Filter Freq` (Filtro do delay)
  - Filter (4): `Dry/Wet`

### **Reverb** (`Reverb`)
- **Parâmetros da API:** `PreDelay`, `DecayTime`, `Dry/Wet`, `Size`, `Reflect`, `Diffuse`
- **Mapeamento Proposto:**
  - High (1): `DecayTime`
  - Mid (2): `Size`
  - Low (3): `PreDelay`
  - Filter (4): `Dry/Wet`

---

## 🛠️ 4. Utilitários

### **Utility** (`Utility`)
- **Parâmetros da API:** `Gain`, `Width`, `Panorama`, `Mono`, `Bass Mono`
- **Mapeamento Proposto:**
  - High (1): `Gain`
  - Mid (2): `Width`
  - Low (3): `Panorama`
  - Filter (4): `Bass Mono Freq`

---

## 🧠 Lógica Proposta para o Novo Código Python

Para implementar isso de forma inteligente e resolver o problema de CPU, a estrutura em `DDJ_LiveBridge.py` deverá ser um Dicionário de Classes:

```python
DEVICE_MAPPING_DICTIONARY = {
    "Eq8": ["4 Gain A", "3 Gain A", "2 Gain A", "1 Gain A", "4 Frequency A", "3 Frequency A", "2 Frequency A", "1 Frequency A"],
    "ChannelEq": ["High", "Mid", "Low", "Mid Freq", None, None, None, None],
    "AutoFilter": ["Frequency", "Resonance", "Env. Modulation", "LFO Amount", None, None, None, None],
    "Saturator": ["Drive", "Base", "Freq", "Depth", "Dry/Wet", None, None, None],
    "DrumBuss": ["Drive", "Crunch", "Transients", "Boom", "Freq", None, None, None],
    "Reverb": ["DecayTime", "Size", "PreDelay", "Dry/Wet", None, None, None, None]
}
```

**Comportamento:**
1. Ao focar no dispositivo, o script captura o `device.class_name` **apenas uma vez**.
2. Se o dispositivo estiver no dicionário (ex: `Eq8`), o mapeamento obedece rigidamente à ordem perfeita (4 ganhos na esquerda, 4 frequências na direita).
3. Se for um VST de terceiros ou dispositivo desconhecido, o script usa a busca semântica (`high`, `mid`, `low`) original como *Fallback*.
4. Mapeamento estático e armazenado na memória (O(1)), erradicando picos de CPU.

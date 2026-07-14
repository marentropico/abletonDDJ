# Guia Definitivo da API do Ableton Live 12 (LOM & Control Surface)

Este documento foi compilado com base na arquitetura interna atualizada do Ableton Live 12 (Python 3.11), no Live Object Model (LOM) oficial e nos frameworks de script MIDI (`_Framework` e `ableton.v2/v3`). O objetivo é dar total previsibilidade no mapeamento da nossa DDJ-400, eliminando a abordagem "às cegas".

---

## 1. Live Object Model (LOM) Hierarchy
O coração do Ableton Live é o LOM, uma árvore gigantesca de objetos que podem ser acessados e modificados por código.

**Importante:** Todos os scripts de controle começam acessando a raiz `self.song()` (que aponta para a classe `Live.Song.Song` da API nativa C++ do Ableton exposta no Python).

A hierarquia básica é:
- **Application (`self.application()`)**: Fornece controle de view (ex: alternar Session/Arrangement).
- **Song (`self.song()`)**: Controla Play, Stop, Record, Loop, Metrônomo, Tempo (BPM), etc.
  - **Tracks (`song.tracks`)**: Acesso individual aos canais (Volume, Pan, Mute, Solo, Sends). Inclui pistas normais.
  - **Master Track (`song.master_track`)**: Canal Master.
  - **Return Tracks (`song.return_tracks`)**: Canais de efeito/retorno.
  - **Scenes (`song.scenes`)**: Cenas do Session View (horizontal).

Para cada **Track**, a árvore desce para:
- `track.devices`: Lista os plugins inseridos na pista (EQ, Compressor, Synth).
- `track.clip_slots`: Slots onde os "Clips" (quadrados) são inseridos.
  - `clip_slot.clip`: O conteúdo musical. Onde ficam as notas MIDI, warp, cores.

---

## 2. Eventos e Listeners Nativos (Observers)
O Ableton usa um sistema poderoso baseado em listeners atrelados a "propriedades observáveis". Quando uma propriedade muda no Ableton (ex: usuário usou o mouse para clicar na Track 2), o Ableton notifica automaticamente a Control Surface através das callbacks registradas.

### Como funciona no Python (Live 11/12):
Para escutar uma propriedade (ex: `selected_track`), usa-se o padrão `add_NOME_listener(callback)`:

```python
# Registrando para saber quando a trilha selecionada mudar
self.song().view.add_selected_track_listener(self._on_selected_track_changed)

def _on_selected_track_changed(self):
    nova_track = self.song().view.selected_track
    print("O usuário clicou na Track:", nova_track.name)
```
**Regras de Ouro:**
1. Você *precisa* desconectar (`remove_NOME_listener`) os listeners no método `disconnect()` do seu script, caso contrário o Ableton dará "Crash" ou "Memory Leak".
2. Você pode observar Volumes (`add_value_listener` em um `Parameter`), Solos, Mutes, etc.

---

## 3. Control Surface Framework (`_Framework` e `ableton.v3`)
Para não termos que escrever listeners manualmente para tudo, a engenharia do Ableton criou os **Components**. Eles são caixas prontas que encapsulam as lógicas complexas e se ligam sozinhas às propriedades do LOM.

### A Divisão Clássica (MVC):
- **Model**: A LOM e o estado do software (Volume, Play, Pan).
- **View (Elements)**: As peças físicas da sua controladora (Knobs, Pads, Faders). Eles recebem o MIDI Bruto (`SliderElement`, `ButtonElement`, `EncoderElement`).
- **Controller (Components)**: A cola. Você diz a um "Component" qual é o "Element" físico. O componente faz o resto.

### Principais Components Disponíveis:
- `MixerComponent`: Controla volume, pan e sends de uma quantidade definida de trilhas (incluindo Mute/Solo se configurado). Nativamente ele resolve o "Takeover" (Value Scaling) do Ableton.
- `DeviceComponent` / `DeviceParameterComponent`: Controla os Macro-parâmetros ou parâmetros diretos dos Plugins (EQs, Filtros). No momento em que você seleciona um dispositivo no Ableton (aparece a mãozinha azul), o `DeviceComponent` "gruda" nos parâmetros daquele plugin automaticamente.
- `TransportComponent`: Controla o Play, Stop, Record, Nudge (Pitch Bend), Tap Tempo e Metrônomo.
- `SessionComponent`: Cuida de lançar Clips e Cenas. Muito usado pelo Launchpad e APC40. Lida com a famosa "Red Box" (caixa vermelha que delimita as pistas que você está visualizando no momento).

**Atenção:**
No Live 11/12 (`ableton.v3`), **todos os components** devem ser atrelados à arquitetura raiz. Componentes como o `DeviceComponent` dependem da função de registro oficial `self.set_device_component()` (em versões modernas, omitir a inicialização dos parents ou chamar funções depreciadas, como o antigo `self.set_mixer_component` dependendo do import, pode causar o aborto imediato do carregamento do script).

---

## 4. MIDI Avançado e Particularidades da DDJ-400

### Tipos de Input e Controles Contínuos:
O Ableton reconhece 3 tipos primários de movimento nos nossos **Elements**:
1. **Absoluto (`MIDI_CC_TYPE`)**: Faders e Knobs normais que enviam valores de 0 a 127 de acordo com a posição estrita do knob.
2. **Relativo (Encoders Infinitos)**: O *Browse Encoder* da DDJ. Não envia posições, mas "deltas" (+1 / -1 / Two's Compliment). Mapeado no Ableton usando `Live.MidiMap.MapMode.relative_two_compliment`.
3. **MIDI de 14-Bits (Pitch Faders / Tempo Sliders)**:
   Equipamentos Pioneer enviam controles de alta resolução emitindo DOIS CCs simultâneos no mesmo movimento (MSB e LSB). O C# logará 2 valores (Ex: CC 8 e CC 40 no canal 7). O Ableton é capaz de unir isso e ler o Pitch Fader com precisão.

### Value Scaling e Takeover Mode
Quando rodamos scripts no Python nativamente, o Ableton toma a dianteira na "Embreagem". Se a sua DDJ-400 envia o Valor Absoluto 0 para o Filtro de um Plugin, mas o plugin está na posição 100 na tela do software, **o Ableton não fará nada (bloqueará o sinal)** até que você gire o botão físico até chegar próximo à posição atual do software (Value Scaling). 

Dica: A rotina "C# Embreagem" só fazia sentido em mapeamentos customizados de mouse/teclado. O roteamento oficial via Python torna redundante filtros de pulo (jumps) no código C#, deixando a integração muito mais limpa e fluida.

---

## 5. Próximos Passos para a nossa Aplicação

Agora que não estamos mais às cegas, este é o roadmap para o domínio absoluto da DDJ-400 no Ableton:
1. **Pitch / Tempo Controls:** Mapear o MSB/LSB do Pitch Fader da DDJ e atrelar a um `TransportComponent` ou controlar o pitch de clips.
2. **Session Matrix Oficial:** Substituir a navegação baseada em atalhos de teclado (setinhas enviadas via C#) por um `SessionComponent` que fará a navegação nativa e controlará a `Red Box` (retângulo vermelho na tela).
3. **Luzes (LED Feedback):** Aproveitar a comunicação bidirecional do Python. Quando um botão é pressionado na tela (ex: Mute acionado pelo mouse), o `MixerComponent` enviará via MIDI OUT um sinal para o C# acender o botão físico da controladora!

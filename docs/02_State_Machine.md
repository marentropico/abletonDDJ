# State Machine (Máquina de Estados)

A Pioneer DDJ-400 possui um layout fixo de controles (8 pads por deck, um jog wheel, EQs de 3 bandas, etc). Para transformá-la num "Joystick Completo", a lógica do C# implementa o conceito de camadas (Layers) e zonas (Zonas de Trabalho).

## O Modificador SHIFT Global

Na DDJ-400, existe um botão físico `SHIFT`. No modo padrão, o firmware apenas envia uma mensagem MIDI diferente quando um botão é pressionado junto com o Shift.
No DDJ-LiveBridge, o C# mantém o estado do Shift globalmente:
- **Estado Booleano**: `IsShiftActive`.
- **Efeito**: Muda instantaneamente a matriz de mapeamento carregada na memória.
- *Exemplo Prático*: Pressionar o Pad 1 pode disparar um clipe. `Shift` + Pad 1 pode duplicar a cena selecionada no Ableton (simulando `Ctrl+D`).

## Modos de Operação (Zonas de Trabalho)

Os Decks (Esquerdo e Direito) não operam mais apenas como reprodutores de faixa, mas como **Módulos Focados**. O usuário pode trocar o modo de um Deck usando botões dedicados (ex: botões de Loop).

### 1. Browser & Session Mode (O Pilar da Navegação)
Quando este modo está ativo:
- **Jog Wheel**: Rola verticalmente pelo navegador de arquivos ou pela Session View (simulando Setas Cima/Baixo).
- **Pads**: Funcionam para abrir/fechar pastas no navegador (Seta Direita/Esquerda), carregar arquivos (Enter), ou disparar cenas.
- **Shift**: Altera o foco entre painéis (simula `Tab` ou `Shift+Tab`).

### 2. Performance / Mixing Mode (O Pilar da Mixagem)
Modo padrão analógico.
- **EQs (Low, Mid, High)**: Atuam no EQ Three.
- **Faders de Volume**: Nativos.
- **Pads**: Drum Rack ou Clip Launch tradicional.

### 3. Edit / Arrangement Mode (O Pilar do "Joystick")
Para trabalhar na linha do tempo.
- **Jog Wheel**: MIDI Relativo para varrer o Arrangement View (Scrubbing).
- **Pitch Fader**: Atua no parâmetro de Time Stretch do sample ou clipe de áudio focado, manipulando Warp Markers através de macros customizados.

## O Papel do `StateManager` no C#

A classe `StateManager` é instanciada diretamente no ponto de entrada do programa (`Program.cs`) e fornecida ao `ActionRouter` no modo de produção. Ela gerencia o estado global e local de cada deck.
Ela contém a árvore de estado atual. Sempre que o `InputListener` capta um evento, ele passa esse evento e o estado atual para o `ActionRouter` tomar a decisão, garantindo assim comportamentos diferentes para o mesmo botão baseados no estado da máquina.

# Arquitetura DDJ-LiveBridge

Este documento descreve a arquitetura geral do middleware C# projetado para interceptar a controladora Pioneer DDJ-400 e remapeá-la como uma interface de navegação e performance para o Ableton Live.

## Visão Geral

O projeto atua como um "Homem no Meio" (Man-in-the-Middle) entre o hardware da DDJ-400 e o software Ableton Live.
Para atingir o objetivo de reduzir o uso do mouse e teclado a zero, o C# simula atalhos de teclado do SO (para navegação e controle de interface) e emite comandos MIDI processados via uma porta virtual (para controle de plugins e canais).

## Domínios Principais (Módulos)

O sistema é dividido nos seguintes domínios lógicos:

### 1. InputListener
A camada mais próxima do hardware.
- **Responsabilidade**: Ler a porta MIDI de entrada onde a DDJ-400 está fisicamente conectada.
- **Transformação**: Converte códigos brutos (Hexadecimal/MIDI Bytes) para objetos de C# mais legíveis (ex: enumeração de botões físicos).
- **Processamento de Jog Wheel**: Identifica mensagens relativas das platters e calcula o incremento/decremento real da agulha.

### 2. StateManager (Máquina de Estados)
O cérebro que dá contexto aos botões físicos.
- **Responsabilidade**: Manter o rastreamento global de modificadores (ex: botão *Shift* físico sendo pressionado) e Zonas (Decks).
- **Modos**: Sabe se o *Deck Esquerdo* está no "Modo de Navegação" ou no "Modo de Performance".
- Evita concorrência de comandos (se dois eventos de Shift ocorrerem).

### 3. ActionRouter
A encruzilhada de decisões.
- **Responsabilidade**: Baseado na entrada do `InputListener` e no contexto do `StateManager`, decide qual ação deve ser tomada consultando o esquema de mapeamento (JSON).
- **Roteamento de Destino**:
  - Encaminha todos os comandos para a porta MIDI Virtual (loopMIDI).
  - Comandos de áudio padrão (Filter, EQ, Faders) vão em canais/CCs regulares.
  - Comandos de **Navegação e Interface** (Tab, Seta, Delete) são enviados como sinais MIDI de "Sistema" (em CCs reservados) que serão interceptados por um **Remote Script em Python** rodando nativamente dentro do Ableton Live, eliminando a necessidade de foco da janela (foco em background).

### 4. OutputEmitter
A saída de dados limpos.
- **Responsabilidade**: Emitir os sinais.
- Para o Ableton: Envia os sinais MIDI processados para a porta loopMIDI.
- Para a DDJ-400 (Feedback Visual): Envia mensagens SysEx ou NoteOn/Off de volta para o hardware da Pioneer para acender/piscar LEDs conforme as mudanças de estado no C# (ex: acender os Pads quando o Modo Drum Rack for selecionado).

### 5. Interface Gráfica (WPF GUI)
Embora a lógica do middleware seja executada em uma thread de background, o aplicativo possui uma interface gráfica desenvolvida em WPF.
- **Responsabilidade**: Renderizar uma controladora virtual da DDJ-400 na tela do usuário.
- **Funcionamento**: A interface reage a eventos MIDI em tempo real (mudando o preenchimento de botões e rotacionando faders, knobs e jogs virtuais) e fornece um painel lateral interativo que serve como manual de consulta rápida e monitor de calibração quando o usuário clica sobre qualquer botão na GUI.

## Tecnologias Envolvidas
- **Linguagem (Middleware)**: C# (.NET 10) - Aplicação Desktop WPF (Windows Presentation Foundation) com arquitetura em thread de background.
- **Linguagem (Control Surface)**: Python (Ableton Remote Scripts API).
- **MIDI IO**: `Melanchall.DryWetMidi`
- **Virtualização MIDI**: Requer `loopMIDI` (Tobias Erichsen).
- **Comunicação Legada**: Contém um arquivo `UdpServer.py` (inativo na versão atual, que utiliza puramente comunicação MIDI na porta virtual via loopMIDI no Canal 16).

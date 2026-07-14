# Manual do Usuário - LiveBridge (Pioneer DDJ-400)

Este manual documenta o comportamento oficial de cada controle da controladora DDJ-400 quando utilizada no Ableton Live através do aplicativo **LiveBridge**.

Os controles listados aqui operam nativamente sem a necessidade de mapeamento manual na maioria das funções (graças à ponte C# e ao nosso Remote Script Python).

---

## 1. Seção de Transporte (Deck Esquerdo e Direito)

Esta seção controla a reprodução e gravação de áudio no Ableton Live.

| Botão | Ação no Ableton | Comportamento Detalhado |
| :--- | :--- | :--- |
| **Play/Pause** | Tocar / Pausar | Alterna o estado de reprodução global do Ableton (`song.is_playing`). |
| **Shift + Play** | Stop All Clips (CC 12) | Envia um sinal para parar todos os clipes que estiverem tocando na Session View. |
| **Cue** | Play de Agulha (CC 11) | Continua a reprodução da música exatamente a partir da posição atual da agulha. |
| **Shift + Cue** | Back to Arrangement (CC 13) | Restaura a reprodução para a Timeline do Arrangement View se um clipe da Session estava ativo. |
| **Reloop/Exit (Deck Esquerdo)** | Looper Contextual (CC 48) | **Gravação inteligente baseada no slot de clipe focado:**<br>• *Slot vazio:* Arma a pista e inicia a gravação de um novo clipe.<br>• *Slot gravando:* Para de gravar e inicia a reprodução do clipe em loop.<br>• *Slot parado/tocando:* Inicia gravação de Overdub/gravação cumulativa no clipe. |
| **Reloop/Exit (Deck Direito)** | Arrangement Record (CC 49) | Alterna o botão de gravação geral da timeline do Arrangement View (`song.record_mode`). |

---

## 2. Mixer Dinâmico e Controle de Pista Selecionada

O mixer se auto-ajusta dinamicamente para a pista (Track) selecionada no Ableton Live.

> [!NOTE]
> Os faders físicos de volume do Deck Esquerdo e Direito estão desativados temporariamente devido a problemas físicos de mau contato no hardware da controladora.

- **Trim Deck A (Esquerdo)**: Controla o **Volume** da pista selecionada na tela.
- **Trim Deck B (Direito)**: Controla o **PAN** (balanço esquerda/direita) da pista selecionada na tela.
- **Botão CUE (Deck Esquerdo)**: Liga/Desliga o **SOLO** da pista selecionada.
- **Botão CUE (Deck Direito)**: Liga/Desliga o **MUTE** da pista selecionada.

---

## 3. Navegação da Timeline (Arrangement) e Loop Dinâmico

A mobilidade pelo arranjo do projeto e o controle de loops foram otimizados:

- **Crossfader (Timeline)**: Posiciona a linha do playhead (agulha de reprodução) do Ableton. Todo à esquerda vai para o início do projeto, todo à direita vai para o final do projeto.
- **Anel Externo do Jog Esquerdo (Ajuste Fino)**: Ajusta de forma fina o posicionamento da linha de reprodução (passos curtos e simulação de scroll horizontal).
- **Jog Direito (Zoom)**: Controla o Zoom horizontal do Arrangement. Girar para a esquerda dá **Zoom In**, girar para a direita dá **Zoom Out**.
- **FX ON/OFF (Seção Beat FX)**:
  - **Segurar FX ON/OFF + Arrastar Crossfader**: Marca o início do loop na agulha atual e permite redimensionar o tamanho do loop ativamente arrastando o crossfader.
  - **Soltar FX ON/OFF**: Define a região selecionada como o loop ativo e o habilita no Ableton (simula o atalho `Ctrl + Shift + L`).

---

## 4. Browser e Navegação de Pistas

O botão prateado central (Selector Knob) possui dois modos de operação, alternados pelo botão **LOAD Deck A**:

### Modo Pista (Padrão)
- **Ativação**: Pressione o botão **LOAD Deck A** (o foco mudará para as pistas).
- **Rotacionar Selector**: Navega verticalmente selecionando a pista de cima ou de baixo.
- **TRIM e CUE**: Controlam Volume, PAN, Solo e Mute da pista que foi selecionada.

### Modo Browser (Navegador do Ableton)
- **Ativação**: Pressione o botão **LOAD Deck A** novamente (o foco mudará para o Browser lateral do Ableton).
- **Rotacionar Selector**: Navega pelas pastas e arquivos de efeitos, samples ou plugins (simula as teclas de seta Cima/Baixo).
- **Pressionar Selector (Click)**:
  - Se for uma pasta: **Entra/Expande** a pasta (simula Enter).
  - Se for um sample/plugin: **Carrega** o sample/plugin na track ativa (simula Enter).
- **Shift + Pressionar Selector**: **Recolhe** a pasta atual ou **Volta** um diretório para trás (simula Seta Esquerda).

---

## 5. Visualização e Criação de Tracks

- **Botão LOAD Deck B**: Alterna a tela do Ableton Live entre a **Session View** (tela de clipes) e a **Arrangement View** (linha do tempo).
- **Seletor FX ▽ / △ (FxSelect)**:
  - **Normal**: Cria uma nova **Pista de Áudio** (simula `Ctrl + T`).
  - **Shift**: Cria uma nova **Pista MIDI** (simula `Ctrl + Shift + T`).

---

## 6. Atalhos de Teclado Mapeados no Painel

Vários botões físicos da DDJ-400 simulam atalhos padrão do Ableton Live para acelerar a produção:

- **Beat ◀ / Beat ▶**: Navega lateralmente na cadeia de dispositivos e efeitos (simula setas Esquerda/Direita).
- **Loop In (Deck Esquerdo)**: Duplica o clipe ou pista selecionada (simula `Ctrl + D`).
- **Loop Out (Deck Esquerdo)**: Deleta o clipe ou pista selecionada (simula `Delete`).
- **Loop Call ◀ (Deck Esquerdo)**: Desfazer ação (simula `Ctrl + Z`).
- **Loop Call ▶ (Deck Esquerdo)**: Refazer ação (simula `Ctrl + Y`).
- **Beat Sync (Deck Esquerdo)**:
  - **Normal**: Quantiza notas MIDI (simula `Ctrl + U`).
  - **Shift**: Liga/Desliga o metrônomo do Ableton (simula `Ctrl + Option + M`).

---

## 7. Controle de Efeitos e Sintetizadores (Device Control)

Qualquer plugin ou rack que estiver selecionado e em foco ativo (mãozinha azul) será controlado pelos knobs de equalização da controladora, divididos em 8 controles:

- **Deck Esquerdo (High, Mid, Low, Filter)**: Controlam os **Parâmetros de 1 a 4** do plugin selecionado (Ex: Low, Mid, High e Cutoff de um EQ).
- **Deck Direito (High, Mid, Low, Filter)**: Controlam os **Parâmetros de 5 a 8** do mesmo plugin.

### 🎯 Modo de Alta Precisão (Smart Knobs)
- **Fine-Tuning (Ajuste Fino)**: Segure o `SHIFT` enquanto gira o EQ ou Filtro para entrar no modo Lupa. A sensibilidade do knob cai para 25%, permitindo ajustes cirúrgicos.
- **Embreagem (Takeover)**: Se você fizer um ajuste com `SHIFT` e depois soltar, as posições do knob físico e do Ableton estarão desalinhadas. Ao voltar a girar sem `SHIFT`, o Ableton ignorará o movimento até que o botão físico "cruze" a posição atual da tela, evitando pulos bruscos no som.

---

## 8. Modos de Performance dos Pads

Pressione um dos botões de modo (`Hot Cue`, `Beat Loop`, `Beat Jump`, `Sampler`) para alterar a função dos 8 pads de cada deck:

### Modo SAMPLER (MPC Style)
- **Pads 1 ao 8**: Enviam Notas MIDI padrão (C1 a G1 / Notas 36 a 43) no **Canal 1** (Deck Esquerdo) e **Canal 2** (Deck Direito). Ideal para tocar Drum Racks ou samplers de percussão diretamente.

### Modo HOT CUE (Lançamento de Clipes & Controle de Plugin)
- **Deck Esquerdo (Lançamento de Clipes)**:
  - **Pads 1 ao 8**: Disparam os clipes nos slots correspondentes da pista selecionada (CC 50-57, Canal 16).
  - **Shift + Pads 1 ao 8**: Para a reprodução do clipe correspondente.
- **Deck Direito (Controle de Botões de Efeitos)**:
  - **Pads 1 ao 8**: Enviam CCs virtuais 60-67 (Canal 16) $\rightarrow$ **Python:** Inverte e alterna o estado de parâmetros booleanos (como On/Off, Ativar/Desativar) do plugin selecionado.

### Modo BEAT LOOP e BEAT JUMP
- Estes modos enviam CCs dedicados para mapeamento manual de loops e efeitos customizados no Ableton Live através do Canal 16:
  - **Beat Loop**: CCs 70-77 (Esquerdo) e 80-87 (Direito).
  - **Beat Jump**: CCs 90-97 (Esquerdo) e 100-107 (Direito).

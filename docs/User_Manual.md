# Manual do Usuário - LiveBridge (Pioneer DDJ-400)

Este manual documenta o comportamento oficial de cada controle da controladora DDJ-400 quando utilizada no Ableton Live através do aplicativo **LiveBridge**. 

Os controles listados aqui já operam nativamente sem necessidade de mapeamento manual (graças ao nosso Remote Script).

---

## 1. Sessão de Transporte (Deck Esquerdo e Direito)

Esta seção controla a reprodução de áudio e a posição da música no Ableton.

| Botão | Ação no Ableton | Comportamento Detalhado |
| :--- | :--- | :--- |
| **Play/Pause** | Tocar / Pausar | Funciona exatamente como a barra de espaço do teclado. Se o som estiver parado, ele toca. Se estiver tocando, ele pausa e a "agulha" (playhead) retorna para o marcador de onde você iniciou. |
| **Shift + Play** | Stop All Clips | Para a reprodução de todos os clipes que estiverem tocando na Session View (útil como um "Panic Button" para silenciar tudo ao mesmo tempo). |
| **Cue** | Play da Agulha | Inicia a reprodução da música exatamente a partir do local atual da agulha/insert marker. |
| **Shift + Cue** | Back to Arrangement | Se você estava tocando na Session View e o botão de Arrangement acendeu, esse comando restaura a reprodução para a Timeline do Arrangement View. |

---

## 2. Mixer Dinâmico e Controle de Pista Selecionada

O mixer agora se auto-ajusta dinamicamente para a pista (Track) selecionada no Ableton Live. 

*Nota: Os faders físicos de volume do Deck Esquerdo e Direito foram desativados temporariamente devido a mau contato físico.*

- **Trim Deck A (Esquerdo)**: Controla o **Volume** da pista selecionada na tela.
- **Trim Deck B (Direito)**: Controla o **PAN** (balanço esquerda/direita) da pista selecionada na tela.
- **Botão CUE (Deck Esquerdo)**: Liga/Desliga o **SOLO** da pista selecionada.
- **Botão CUE (Deck Direito)**: Liga/Desliga o **MUTE** da pista selecionada.

---

## 3. Navegação da Timeline (Arrangement) e Zoom

A mobilidade pelo arranjo do projeto é feita com os seguintes controles, agora com **Auto-Scroll da Tela (Display Scroll Follow)**:

- **Crossfader (Timeline)**: Posiciona a linha do playhead (agulha de reprodução) do Ableton. Todo à esquerda vai para o início do projeto, todo à direita vai para o final do projeto. **O display acompanha o movimento rolando a tela horizontalmente para mostrar onde a agulha está!**
- **Anel Externo do Jog Esquerdo (Ajuste Fino)**: Ajusta de forma fina o posicionamento da linha de reprodução (passos curtos de 0.25 beats). **A tela rola horizontalmente junto com o ajuste fino para você nunca perder a linha de vista!**
- **Jog Direito (Zoom)**: Controla o Zoom horizontal do Arrangement. Girar para a esquerda dá **Zoom In** (aumenta o zoom), girar para a direita dá **Zoom Out** (diminui o zoom).

---

## 4. Browser e Navegação de Pistas

O botão prateado central (Selector Knob) possui dois modos de operação, alternados pelo botão **LOAD Deck A**:

### Modo Pista (Padrão)
- **Ativação**: Pressione o botão **LOAD Deck A** (o foco da tela mudará para as pistas).
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

## 5. Visualização (Views)

- **Botão LOAD Deck B**: Alterna a tela do Ableton Live entre a **Session View** (tela de clipes em colunas) e a **Arrangement View** (linha do tempo horizontal).

---

## 6. Criação de Pistas (Tracks)

Você pode criar novas pistas no projeto de forma extremamente simples:
- **Botão FX Select (Mixer/FX)**: Cria uma nova **Pista de Áudio** (simula `Ctrl + T`).
- **Shift + Botão FX Select**: Cria uma nova **Pista MIDI** (simula `Ctrl + Shift + T`).

---

## 7. Controle de Efeitos e Sintetizadores (Device Control)

Qualquer plugin ou rack que estiver selecionado (com a "mãozinha azul") será controlado pelos botões de EQ!
Isso transforma seus EQs em um painel de "8 Macros" portátil.
- **Deck Esquerdo (High, Mid, Low, Filter)**: Controlam os **Parâmetros de 1 a 4** do plugin selecionado (Ex: Low, Mid, High e Output de um Channel EQ).
- **Deck Direito (High, Mid, Low, Filter)**: Controlam os **Parâmetros de 5 a 8** do mesmo plugin selecionado.

### 🎯 Modo de Alta Precisão (Smart Knobs)
Os EQs e Filtros da DDJ-400 agora são botões inteligentes!
- **Fine-Tuning**: Segure o `SHIFT` enquanto gira o EQ ou Filtro para entrar no modo Lupa. A velocidade de rotação no Ableton cairá para 25%, permitindo ajustes cirúrgicos.
- **Embreagem (Takeover)**: Se você girar com `SHIFT` e depois soltar o botão, o botão físico estará em um lugar diferente da tela (Desync). Ao girar sem o SHIFT novamente, o Ableton **ignorará** o movimento até que o botão físico "cruze" a posição atual da tela. Assim, a música nunca dá "pulos" de volume ou frequência de surpresa!
- **Botões Liga/Desliga**: Gire devagar (com ou sem shift) para passar pela marca de 50% e ligar/desligar parâmetros do tipo Switch com total segurança.

---

## 8. Modos de Performance dos Pads (Pads Dinâmicos)

Pressione um dos botões de modo (`Hot Cue`, `Beat Loop`, `Beat Jump`, `Sampler`) para alterar a função dos 8 pads de cada deck:

### Modo HOT CUE (Lançamento de Clipes)
- **Pads 1 ao 8**: Disparam os clipes nos slots de 1 a 8 no Ableton (Deck Esquerdo controla a **Track 1** e Deck Direito controla a **Track 2**).
- **Shift + Pads 1 ao 8**: Para a reprodução do clipe correspondente.

### Modo BEAT LOOP (Controle de Looper)
Controla automaticamente o primeiro plugin `Looper` presente na Track 1 (Deck Esquerdo) ou Track 2 (Deck Direito).
- **Pad 1**: Record / Play / Overdub (botão multiuso do Looper).
- **Pad 2**: Stop (para a gravação/reprodução).
- **Pad 3**: Undo (desfaz a última gravação).
- **Pad 4**: Clear (limpa o buffer do Looper).

### Modo BEAT JUMP (Efeitos Momentâneos)
Transforma os pads em botões de efeitos momentâneos.
- **Pads 1 ao 8**: Controlam os **Parâmetros de 1 a 8 (Macros)** do plugin selecionado. O efeito só fica ativo enquanto você mantiver o pad pressionado (momentâneo).

### Modo SAMPLER (Drum Rack / MPC Style)
- **Pads 1 ao 8**: Enviam Notas MIDI padrão de Bumbo, Caixa, etc. (C1 a G1) para tocar instrumentos no Ableton (Canal 1 para Deck Esquerdo, Canal 2 para Deck Direito).
- *Ideal para tocar Drum Racks carregados no Ableton.*

---

*(Mais seções serão adicionadas conforme o desenvolvimento avança...)*

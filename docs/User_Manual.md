# Manual do Usuário — AbleToDJ v1.0.1 (Mapeamento Completo)

Este guia prático descreve o comportamento oficial de cada controle físico da controladora **Pioneer DDJ-400** quando integrada ao **Ableton Live** utilizando o middleware **AbleToDJ**.

Os comandos estão listados por seção, associando o controle físico diretamente à sua ação correspondente.

---

## 1. Seção de Transporte e Gravação (Controles Globais)

| Botão / Controle Físico | O que faz no Ableton Live (Ação / Comportamento) |
| :--- | :--- |
| **Play/Pause (Deck Esq ou Dir)** | Toca / Pausa a reprodução global (`Spacebar` behavior). |
| **Shift + Play/Pause (Qualquer Deck)** | Para a reprodução de todos os clipes ativos na Session View (`Stop All Clips`). |
| **Cue (Deck Esq ou Dir)** | Toca a música a partir da posição atual da agulha (Play contínuo). |
| **Shift + Cue (Qualquer Deck)** | Retorna a reprodução global para a timeline linear (`Back to Arrangement`). |
| **Reloop/Exit (Deck Esquerdo)** | **Gravação Contextual de Slot de Clipe:**<br>• *Slot vazio*: Inicia a gravação de um novo clipe na linha selecionada.<br>• *Slot gravando*: Para de gravar e inicia a reprodução del clipe em loop.<br>• *Slot com clipe*: Ativa o Overdub (gravação cumulativa). |
| **Reloop/Exit (Deck Direito)** | Gravação de Arrangement (liga/desliga gravação geral na timeline). |

---

## 2. Mixer Dinâmico, EQs e Knobs de Efeitos

Os canais físicos de volume foram intencionalmente omitidos devido a desgastes mecânicos no hardware da controladora. As funções foram redistribuídas de forma mais robusta e eficiente:

| Botão / Controle Físico | O que faz no Ableton Live (Ação / Comportamento) |
| :--- | :--- |
| **Trim Deck A (Esquerdo)** | Controla o **Volume** da pista (Track) selecionada. |
| **Trim Deck B (Direito)** | Controla o **PAN** (balanço L/R) da pista (Track) selecionada. |
| **Headphone Cue (Deck Esquerdo)** | Liga / Desliga o modo **SOLO** da pista selecionada. |
| **Headphone Cue (Deck Direito)** | Liga / Desliga o modo **MUTE** da pista selecionada. |
| **EQ High (Deck Esquerdo)** | Controla o **Parâmetro 1** do efeito/plugin selecionado (mãozinha azul). |
| **EQ Mid (Deck Esquerdo)** | Controla o **Parâmetro 2** do efeito/plugin selecionado. |
| **EQ Low (Deck Esquerdo)** | Controla o **Parâmetro 3** do efeito/plugin selecionado. |
| **Filter (Deck Esquerdo)** | Controla o **Parâmetro 4** do efeito/plugin selecionado. |
| **EQ High (Deck Direito)** | Controla o **Parâmetro 5** do efeito/plugin selecionado. |
| **EQ Mid (Deck Direito)** | Controla o **Parâmetro 6** do efeito/plugin selecionado. |
| **EQ Low (Deck Direito)** | Controla o **Parâmetro 7** do efeito/plugin selecionado. |
| **Filter (Deck Direito)** | Controla o **Parâmetro 8** do efeito/plugin selecionado. |
| **Shift + Qualquer Knob (EQ/Filter)** | **Modo de Ajuste Fino (FINE-TUNING):** Reduz a sensibilidade do knob em 75% para ajustes milimétricos. Ao soltar o Shift, entra em ação a embreagem inteligente (Takeover) para evitar pulos bruscos no valor. |
| **Headphone Mixing** | Controla o volume Master geral do projeto como alternativa de fader. |
| **Tempo Slider Deck Esquerdo** | Controla o andamento global (**BPM**) do projeto de forma contínua. |

---

## 3. Navegação da Timeline (Arrangement) e Loops

| Botão / Controle Físico | O que faz no Ableton Live (Ação / Comportamento) |
| :--- | :--- |
| **Crossfader** | Navega diretamente pela timeline do Arrangement (Esquerda = início do projeto, Direita = fim do projeto). |
| **Borda do Jog Wheel Esquerdo** | Navegação fina da timeline por passos (Scrubbing / Needle Drop). |
| **Borda do Jog Wheel Direito** | Ajusta o Zoom horizontal do Arrangement (Girar p/ esquerda = Zoom In, Girar p/ direita = Zoom Out). |
| **FX ON/OFF (Segurar)** | Inicia a seleção de um loop. Arraste o Crossfader enquanto segura este botão para determinar a extensão do loop. |
| **FX ON/OFF (Soltar)** | Define a região selecionada como loop ativo e liga o loop no Ableton (`Ctrl + Shift + L`). |

---

## 4. Foco, Navegação do Browser e Criação de Trilhas

| Botão / Controle Físico | O que faz no Ableton Live (Ação / Comportamento) |
| :--- | :--- |
| **LOAD Deck A** | **Alternador de Foco:** Alterna o foco do teclado do Ableton entre as **Pistas (Tracks)** e o **Navegador Lateral (Browser)**.<br>• *Ao focar o Browser:* Move o cursor automaticamente para a lista de arquivos (Content Pane) após 100ms. |
| **Shift + LOAD Deck A** | *Desabilitado* para evitar desalinhamento acidental no foco das telas. |
| **Selector Knob (Girar)** | Navega verticalmente pelas pastas do Browser ou seleciona as Pistas (simula setas Cima / Baixo). |
| **Selector Knob (Clicar)** | **Entrar / Abrir:** Abre uma pasta expandindo-a ou carrega um sample/efeito na pista ativa (simula tecla `Enter`). |
| **Shift + Selector Knob (Clicar)** | **Voltar:** Recolhe a pasta atual ou volta uma pasta no histórico (simula seta Esquerda). |
| **LOAD Deck B** | **Alternador de Tela:** Alterna o foco de tela entre a **Session View** (clipes) e a **Arrangement View** (timeline). |
| **FX SELECT** | Cria uma nova **Pista de Áudio** no projeto (`Ctrl + T`). |
| **Shift + FX SELECT** | Cria uma nova **Pista MIDI** no projeto (`Ctrl + Shift + T`). |

---

## 5. Atalhos e Edição de Performance

| Botão / Controle Físico | O que faz no Ableton Live (Ação / Comportamento) |
| :--- | :--- |
| **Beat ◀ / Beat ▶** | Navega lateralmente selecionando os dispositivos/efeitos da pista ativa (simula setas Esquerda / Direita). |
| **Loop In (Deck Esquerdo)** | Duplica o clipe ou pista atualmente selecionado (`Ctrl + D`). |
| **Loop Out (Deck Esquerdo)** | Deleta o clipe ou pista atualmente selecionado (`Delete`). |
| **Loop Call ◀ (Deck Esquerdo)** | Desfazer a última ação (`Ctrl + Z`). |
| **Loop Call ▶ (Deck Esquerdo)** | Refazer a última ação (`Ctrl + Y`). |
| **Beat Sync (Deck Esquerdo)** | Quantiza as notas MIDI do clipe ativo na tela (`Ctrl + U`). |
| **Shift + Beat Sync (Deck Esquerdo)** | Liga / Desliga o Metrônomo do Ableton. |

---

## 6. Pads de Performance: Modo SAMPLER (Teclado Cromático)

Ativado pressionando o botão **Sampler** na controladora. Transforma os 16 pads em um teclado musical cromático clássico no **Canal MIDI 1**.

```
DECK ESQUERDO             DECK DIREITO
[C#] [D#] [ - ] [F#]     [G#] [A#] [ - ] [+8va]   ← Linha de cima (Sustenidos)
[C ] [D ] [E  ] [F ]     [G ] [A ] [B  ] [-8va]   ← Linha de baixo (Naturais)
```

| Pad Físico | Deck Esquerdo (Notas C a F) | Deck Direito (Notas G a B e Oitavas) |
| :--- | :--- | :--- |
| **Pad 1 (Cima)** | **C#** (Dó Sustenido) | **G#** (Sol Sustenido) |
| **Pad 2 (Cima)** | **D#** (Ré Sustenido) | **A#** (Lá Sustenido) |
| **Pad 3 (Cima)** | *Sem função* (Mi# não existe) | *Sem função* (Si# não existe) |
| **Pad 4 (Cima)** | **F#** (Fá Sustenido) | **Subir Oitava (+1)** |
| **Pad 5 (Baixo)** | **C** (Dó) | **G** (Sol) |
| **Pad 6 (Baixo)** | **D** (Ré) | **A** (Lá) |
| **Pad 7 (Baixo)** | **E** (Mi) | **B** (Si) |
| **Pad 8 (Baixo)** | **F** (Fá) | **Descer Oitava (-1)** |

---

## 7. Pads de Performance: Modo HOT CUE (Teclado Cromático Linear)

Ativado pressionando o botão **Hot Cue** na controladora. Configura um layout sequencial contínuo de 16 notas no **Canal MIDI 1** (utilizando a mesma oitava configurada no modo Sampler).

```
DECK ESQUERDO             DECK DIREITO
[E ] [F ] [F#] [G ]      [C1] [C#] [D1] [D#]  ← Linha de cima (Notas agudas)
[C ] [C#] [D ] [D#]      [G#] [A ] [A#] [B ]  ← Linha de baixo (Notas médias)
```

| Pad Físico | Deck Esquerdo (Notas C a G) | Deck Direito (Notas G# a D#+1) |
| :--- | :--- | :--- |
| **Pad 5 (Baixo)** | **C** (Dó) | **G#** (Sol Sustenido) |
| **Pad 6 (Baixo)** | **C#** (Dó Sustenido) | **A** (Lá) |
| **Pad 7 (Baixo)** | **D** (Ré) | **A#** (Lá Sustenido) |
| **Pad 8 (Baixo)** | **D#** (Ré Sustenido) | **B** (Si) |
| **Pad 1 (Cima)** | **E** (Mi) | **C (+1 Oitava)** |
| **Pad 2 (Cima)** | **F** (Fá) | **C# (+1 Oitava)** |
| **Pad 3 (Cima)** | **F#** (Fá Sustenido) | **D (+1 Oitava)** |
| **Pad 4 (Cima)** | **G** (Sol) | **D# (+1 Oitava)** |

---

## 8. Modos Auxiliares (Pads de Beat Loop e Beat Jump)

Mapeamentos diretos de controle contínuo MIDI no **Canal 16** para customização livre (MIDI Mapping nativo do Ableton):

*   **Modo Beat Loop:**
    *   *Deck Esquerdo*: Pads 1 ao 8 enviam **MIDI CC 70 a 77**.
    *   *Deck Direito*: Pads 1 ao 8 enviam **MIDI CC 80 a 87**.
*   **Modo Beat Jump:**
    *   *Deck Esquerdo*: Pads 1 ao 8 enviam **MIDI CC 90 a 97**.
    *   *Deck Direito*: Pads 1 ao 8 enviam **MIDI CC 100 a 107**.

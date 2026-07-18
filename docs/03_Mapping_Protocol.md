# Mapping Protocol (Esquema JSON)

> [!IMPORTANT]
> **Nota de Implementação Real vs. Planejada:**
> O protocolo JSON dinâmico descrito abaixo (agrupando controles por decks, zonas de trabalho e ações configuráveis) trata-se de uma **proposta de arquitetura futura**.
>
> Na implementação atual:
> 1. O mapeamento de comportamento (rotas das ações de teclado, CCs e comandos do Ableton) está **hardcoded** diretamente no C# na classe [ActionRouter](file:///w:/Desktop/AbletonDDJ/src/LiveBridge.Core/ActionRouter.cs).
> 2. O arquivo JSON real utilizado pelo programa ([mappings_calibrated.json](file:///w:/Desktop/AbletonDDJ/docs/mappings_calibrated.json)) atua exclusivamente como **Calibração Física de Hardware**, mapeando os status e notas MIDI brutas da controladora física para os nomes abstratos de controle (como `Play_Left`, `Cue_Left`).

Abaixo está documentada a estrutura original que foi idealizada para o arquivo JSON de mapeamento dinâmico planejado:

## Estrutura Básica do JSON Planejado

O JSON de mapeamento agrupará as configurações primeiro pelo **Deck/Zona** e depois pelo **Modo** (State).

```json
{
  "Version": "1.0",
  "Decks": {
    "Left": {
      "Modes": {
        "SessionBrowser": {
          "Controls": {
            "JogWheel": {
              "Type": "RelativeScrub",
              "Action": "ArrowKeys_Vertical"
            },
            "Pad1": {
              "Type": "Button",
              "Action": "RemoteScriptCmd",
              "Command": "Session_Enter"
            },
            "Pad1_Shift": {
              "Type": "Button",
              "Action": "RemoteScriptCmd",
              "Command": "Session_ShiftTab"
            }
          }
        },
        "Mixing": {
          "Controls": {
            "EQ_High": {
              "Type": "AbsoluteKnob",
              "Action": "MidiOut",
              "MidiChannel": 1,
              "MidiCC": 12
            }
          }
        }
      }
    }
  }
}
```

## Tipos de Ação (Action Types)

O `ActionRouter` no C# usará a propriedade `Action` para decidir como despachar o comando.

1.  **`RemoteScriptCmd`**: O evento deve ser convertido em uma nota MIDI específica (ou CC reservado) que o nosso Remote Script em Python dentro do Ableton está escutando. A propriedade `Command` indicará qual ação disparar (ex: `Nav_Up`, `Nav_Down`, `Load_Clip`, `Session_Record`).
2.  **`MidiOut`**: O evento deve ser reformatado e enviado para a porta virtual MIDI para controle direto de parâmetros. Exige as propriedades `MidiChannel` e `MidiCC` (ou `Note`).
3.  **`ModeChange`**: Uma ação interna do middleware que não produz saída para o Ableton, mas avisa o `StateManager` para mudar a máquina de estados local (ex: trocar de *SessionBrowser* para *Mixing*).

## Identificadores de Controles (Physical IDs)

O C# converterá os bytes recebidos da DDJ-400 em Enums fixos para identificação segura:
- `Pad1` a `Pad8`
- `JogWheel`
- `EQ_High`, `EQ_Mid`, `EQ_Low`
- `FilterKnob`
- `PlayPause`
- `Cue`
- etc.

O sufixo `_Shift` na chave do JSON é uma convenção para indicar a ação daquele controle quando o estado de Shift Global for verdeadeiro.

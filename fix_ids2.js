const fs = require('fs');
let html = fs.readFileSync('./src/LiveBridge.App/wwwroot/index.html', 'utf8');

const map = {
    'TRIM (Canal 1)': 'Trim_Left',
    'EQ HI (Canal 1)': 'EQ_High_Left',
    'EQ MID (Canal 1)': 'EQ_Mid_Left',
    'EQ LOW (Canal 1)': 'EQ_Low_Left',
    'FILTER (Canal 1)': 'Filter_Left',
    'CUE (fone, Canal 1)': 'HeadphoneCue_Left',
    'CHANNEL FADER 1': 'Volume_Left',

    'TRIM (Canal 2)': 'Trim_Right',
    'EQ HI (Canal 2)': 'EQ_High_Right',
    'EQ MID (Canal 2)': 'EQ_Mid_Right',
    'EQ LOW (Canal 2)': 'EQ_Low_Right',
    'FILTER (Canal 2)': 'Filter_Right',
    'CUE (fone, Canal 2)': 'HeadphoneCue_Right',
    'CHANNEL FADER 2': 'Volume_Right',

    'HEADPHONES MIXING': 'HeadphoneMixing',
    'HEADPHONES LEVEL': 'HeadphoneLevel',
    'MASTER LEVEL': 'MasterLevel',
    'CUE (fone, Master)': 'MasterCue',
    'CROSSFADER': 'Crossfader',
    
    'Seletor giratório de navegação': 'BrowseEncoder_Turn',
    'LOAD (INST. DOUBLES) Canal 1': 'Load_Left',
    'LOAD (INST. DOUBLES) Canal 2': 'Load_Right',

    'BEAT ': 'BeatLeft',  // Will match both, but BeatLeft first. Let's fix this below
    'FX SELECT ': 'FxSelect_Left',
    'Seletor 1 / 2 / MASTER': 'FxChannelSelect',
    'LEVEL/DEPTH': 'LevelDepth',
    'ON/OFF': 'FxOnOff'
};

let count = 0;
for (const key in map) {
    const id = map[key];
    const searchStr = 'data-tip="' + key;
    let idx = html.indexOf(searchStr);
    if (idx !== -1) {
        let gIdx = html.lastIndexOf('<g ', idx);
        if (gIdx !== -1) {
            let segment = html.substring(gIdx, idx);
            if (segment.indexOf('id="') === -1) {
                html = html.substring(0, gIdx + 2) + ' id="' + id + '"' + html.substring(gIdx + 2);
                count++;
            }
        }
    }
}
fs.writeFileSync('./src/LiveBridge.App/wwwroot/index.html', html, 'utf8');
console.log('Replaced ' + count + ' elements');

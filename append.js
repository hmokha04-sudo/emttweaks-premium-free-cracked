const fs = require('fs');
const B = 'C:/Users/hmokh.DESKTOP-7GHFSLU/AppData/Local/Temp/opencode/setup_build';
const exe = fs.readFileSync(B + '/setup_raw.exe');
const payload = fs.readFileSync(B + '/payload.zip');
const magic = Buffer.from('EMTPAYLOADv1', 'ascii');
const footer = Buffer.alloc(magic.length + 8);
magic.copy(footer, 0);
footer.writeBigInt64LE(BigInt(payload.length), magic.length);
const final = Buffer.concat([exe, payload, footer]);
const out = B + '/EMTweaks-Setup.exe';
fs.writeFileSync(out, final);
console.log('installer:', final.length, 'bytes; payload:', payload.length);
// verify
const check = fs.readFileSync(out);
const m = check.length - footer.length;
console.log('magic ok:', check.slice(m, m + magic.length).equals(magic));
console.log('length ok:', check.readBigInt64LE(m + magic.length) === BigInt(payload.length));

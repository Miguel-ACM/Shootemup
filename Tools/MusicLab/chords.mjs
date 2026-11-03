// Offline analyzer. Input is gapless mono f32le PCM from analyze.py.
import { readFileSync } from 'node:fs';
import chromagram from '@audio/mir-nnls-chroma';
import chordino from '@audio/mir-chordino';

const pcm = readFileSync(0);
const rate = Number(process.argv[2]);
const samples = new Float32Array(pcm.buffer, pcm.byteOffset, pcm.byteLength / 4);
const frames = chromagram(samples, { fs: rate });
const chords = chordino(frames, { end: samples.length / rate });
process.stdout.write(JSON.stringify(chords));

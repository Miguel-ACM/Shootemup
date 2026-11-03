import { createServer } from 'node:http';
import { createReadStream } from 'node:fs';
import { mkdir, readFile, readdir, rename, stat } from 'node:fs/promises';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const lab = path.dirname(fileURLToPath(import.meta.url));
const resources = path.resolve(lab, '../../Assets/Resources');
const files = (await readdir(path.join(resources, 'Music'))).filter(name => name.endsWith('.mp3'));
const districts = ['fields', 'snow', 'city', 'desert', 'thunder', 'tunnel', 'candyland', 'space', 'underwater', 'hell'];
const names = { fields: 'Plains', stage1bossOST: 'Stage 1 Boss', greenlandsOST: 'Greenlands', stageComplete: 'Stage Complete' };
const tracks = [...new Set([...districts, ...files.map(file => path.basename(file, '.mp3'))])]
    .filter(id => files.includes(id + '.mp3'))
    .map(id => ({ id, title: names[id] ?? id[0].toUpperCase() + id.slice(1), district: districts.includes(id) }));
const routes = new Map([['/', ['dist/index.html', 'text/html']], ['/style.css', ['dist/style.css', 'text/css']],
    ['/app.js', ['dist/app.js', 'text/javascript']], ['/transport.js', ['dist/transport.js', 'text/javascript']]]);
for (const { id } of tracks) {
    routes.set('/audio/' + id, [path.join(resources, 'Music', id + '.mp3'), 'audio/mpeg']);
    routes.set('/cues/' + id, [path.join(resources, 'MusicAnalysis', id + '.json'), 'application/json']);
}

// Render only on first audition. atempo keeps pitch intact; the extra source tail supplies the crossfade.
const render = promisify(execFile);
const jobs = new Map();
async function fastFile(id) {
    const original = path.join(resources, 'Music', id + '.mp3');
    const cue = JSON.parse(await readFile(path.join(resources, 'MusicAnalysis', id + '.json'), 'utf8'));
    if (!cue.adaptive) throw new Error('Linear cue');
    const { start, end } = cue.sections.peak;
    const dir = path.resolve(lab, '../../Build/MusicLabCache');
    const stamp = Math.round((await stat(original)).mtimeMs);
    const file = path.join(dir, [id, stamp, start, end, '1.06'].join('-') + '.mp3');
    if (!jobs.has(file)) {
        const job = (async () => {
            try { await stat(file); return file; } catch {}
            await mkdir(dir, { recursive: true });
            const tmp = file + '.' + process.pid + '.mp3';
            await render('ffmpeg', ['-nostdin','-loglevel','error','-y','-ss',String(start),'-i',original,
                '-af','atrim=duration=' + (end-start+2) + ',atempo=1.06','-vn','-c:a','libmp3lame','-q:a','2',tmp],
                { windowsHide: true, timeout: 90000 });
            await rename(tmp, file);
            return file;
        })();
        jobs.set(file, job);
        job.catch(() => jobs.delete(file));
    }
    return jobs.get(file);
}

const port = Number(process.argv[2] ?? 8766);
const server = createServer(async (req, res) => {
    if (req.method !== 'GET' && req.method !== 'HEAD') {
        res.writeHead(405, { Allow: 'GET, HEAD' }).end();
        return;
    }
    const url = req.url.split('?')[0];
    if (url === '/tracks') {
        res.writeHead(200, { 'Content-Type': 'application/json' }).end(JSON.stringify(tracks));
        return;
    }
    let route = routes.get(url);
    if (url.startsWith('/fast/') && tracks.some(t => t.id === url.slice(6))) {
        try { route = [await fastFile(url.slice(6)), 'audio/mpeg']; }
        catch { res.writeHead(503).end('Fast audition requires FFmpeg.'); return; }
    }
    if (!route) {
        res.writeHead(404).end();
        return;
    }
    try {
        const file = path.resolve(lab, route[0]);
        const info = await stat(file);
        res.writeHead(200, { 'Content-Type': route[1], 'Content-Length': info.size, 'Cache-Control': 'no-cache' });
        if (req.method === 'HEAD') res.end();
        else createReadStream(file).on('error', () => res.destroy()).pipe(res);
    } catch {
        res.writeHead(404).end();
    }
});
server.on('error', err => { console.error(err.message); process.exitCode = 1; });
server.listen(port, '127.0.0.1', () => console.log('Music Lab · http://127.0.0.1:' + server.address().port + '/'));

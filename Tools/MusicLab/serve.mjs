import { createServer } from 'node:http';
import { closeSync, createReadStream, openSync, readFileSync, renameSync, rmSync, writeFileSync } from 'node:fs';
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
async function fastFile(id, full) {
    const original = path.join(resources, 'Music', id + '.mp3');
    const cue = JSON.parse(await readFile(path.join(resources, 'MusicAnalysis', id + '.json'), 'utf8'));
    if (!cue.adaptive) throw new Error('Linear cue');
    const { start, end } = full ? { start:0, end:cue.duration } : cue.sections.peak;
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

function saveLoop(file, input) {
    const lock = file + '.lock~', tmp = file + '.new~';
    let handle;
    try {
        handle = openSync(lock, 'wx');
        const cue = JSON.parse(readFileSync(file, 'utf8'));
        const { start, end, previous } = input ?? {};
        if (!cue.adaptive || !cue.boundaries.includes(start) || !cue.boundaries.includes(end) || start >= end)
            return [400, 'Choose two ordered progression boundaries.'];
        const peak = cue.sections.peak;
        if (!(peak.start === start && peak.end === end) && (previous?.start !== peak.start || previous?.end !== peak.end))
            return [409, 'This song was edited elsewhere. Reload to use the latest loop.'];
        cue.sections.peak = { start, end };
        cue.peakEdited = true;
        writeFileSync(tmp, JSON.stringify(cue) + '\n');
        renameSync(tmp, file);
        return [200, 'Saved'];
    } catch (err) {
        return err.code === 'EEXIST' ? [409, 'Song is being written. Try again.'] : [500, 'Could not write the game cue sheet.'];
    } finally {
        if (handle !== undefined) {
            try { rmSync(tmp, {force:true}); }
            finally { closeSync(handle); rmSync(lock, {force:true}); }
        }
    }
}

const server = createServer(async (req, res) => {
    const url = req.url.split('?')[0];
    if (req.method === 'PUT' && url.startsWith('/cues/') && routes.has(url)) {
        const host = req.headers.host, localPort = server.address().port;
        if (!['127.0.0.1:' + localPort, 'localhost:' + localPort].includes(host) ||
            (req.headers.origin && req.headers.origin !== 'http://' + host)) {
            res.writeHead(403).end(); return;
        }
        if (req.headers['content-type']?.split(';')[0] !== 'application/json') {
            res.writeHead(415).end(); return;
        }
        try {
            let body = '';
            for await (const chunk of req) {
                body += chunk.toString();
                if (body.length > 1024) { res.writeHead(413).end(); return; }
            }
            const [status, message] = saveLoop(routes.get(url)[0], JSON.parse(body));
            res.writeHead(status, { 'Content-Type':'text/plain; charset=utf-8' }).end(message);
        } catch { res.writeHead(400).end('Invalid save request.'); }
        return;
    }
    if (req.method !== 'GET' && req.method !== 'HEAD') {
        res.writeHead(405, { Allow: url.startsWith('/cues/') && routes.has(url) ? 'GET, HEAD, PUT' : 'GET, HEAD' }).end();
        return;
    }
    if (url === '/tracks') {
        res.writeHead(200, { 'Content-Type': 'application/json' }).end(JSON.stringify(tracks));
        return;
    }
    let route = routes.get(url);
    if (url.startsWith('/fast/') && tracks.some(t => t.id === url.slice(6))) {
        try { route = [await fastFile(url.slice(6), req.url.endsWith('?full=1')), 'audio/mpeg']; }
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

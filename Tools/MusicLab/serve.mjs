import { createServer } from 'node:http';
import { createReadStream } from 'node:fs';
import { readdir, stat } from 'node:fs/promises';
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
    const route = routes.get(url);
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

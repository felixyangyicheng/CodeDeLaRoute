/* Manifest version: J81nT0k4 */
// Service worker for Code de la Route PWA (published)
// Identical to dev version — ensures consistent behavior in production.

const CACHE_NAME = 'cdlr-v1';
const ASSETS_TO_CACHE = [
    '/',
    '/index.html',
    '/favicon.svg',
    '/favicon.png',
    '/icon-192.png',
    '/icon-512.png',
    '/manifest.json',
    '/css/app.css',
    '/data/questions.json',
    '/data/questions_moto.json',
    '_content/MudBlazor/MudBlazor.min.css',
    '_content/MudBlazor/MudBlazor.min.js',
    '_framework/blazor.webassembly.js'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            console.log('[SW] Caching app shell');
            return cache.addAll(ASSETS_TO_CACHE);
        }).then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys => {
            return Promise.all(
                keys.filter(key => key !== CACHE_NAME)
                    .map(key => caches.delete(key))
            );
        }).then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);

    if (event.request.method !== 'GET') return;

    if (url.pathname.startsWith('/_framework/') ||
        url.pathname.startsWith('/_content/') ||
        url.pathname.startsWith('/css/') ||
        url.pathname.endsWith('.svg') ||
        url.pathname.endsWith('.png') ||
        url.pathname.endsWith('.ico') ||
        url.pathname.endsWith('.json') ||
        url.pathname === '/manifest.json') {

        event.respondWith(
            caches.match(event.request).then(cached => {
                return cached || fetch(event.request).then(response => {
                    return caches.open(CACHE_NAME).then(cache => {
                        cache.put(event.request, response.clone());
                        return response;
                    });
                });
            })
        );
        return;
    }

    if (event.request.mode === 'navigate') {
        event.respondWith(
            fetch(event.request).catch(() => caches.match('/index.html'))
        );
        return;
    }

    event.respondWith(
        fetch(event.request).catch(() => caches.match(event.request))
    );
});

const BASE = location.protocol + "//" + location.host ;

const PREFIX = "V15";

const CACHED_FILES = [
    `${BASE}/offline.html`
    //"/offline.html"
];

self.addEventListener("install",(event)=>{
    self.skipWaiting();
    event.waitUntil((async ()=>{
        const cache = await caches.open(PREFIX);

        await Promise.all([...CACHED_FILES].map((path) => {
            return cache.add(new Request(path));
        }))
    })())
    console.log(`${PREFIX} : Install `);
});

self.addEventListener("activate",(event)=>{
    clients.claim();

    event.waitUntil((async ()=>{
        const keys = await caches.keys();

        await Promise.all(keys.map(key => {
            if(!key.includes(PREFIX)){
                return caches.delete(key);
            }
        }));
    })());

    console.log(`${PREFIX} : Activ `);
});

self.addEventListener("fetch",(event)=>{
    console.log(`${PREFIX} Fetching : ${event.request.url}  Mode ${event.request.mode}`);
    if(event.request.mode = "navigate"){
        event.respondWith((async ()=>{

            try{
                const preloadResponse = await event.preloadResponse;

                if(preloadResponse){
                    return preloadResponse;
                }

                return await fetch(event.request);
            }
            catch{
                const cache = await caches.open(PREFIX);

                return await cache.match("/offline.html");
            }
        })())
    }
    else if(CACHED_FILES.includes(event.request.url)){
        event.respondWith(caches.match(event.request));
    }
})
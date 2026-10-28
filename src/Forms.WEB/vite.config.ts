import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import http from 'node:http';

const API_TARGETS = [
  process.env.VITE_API_TARGET,
  'http://localhost:5250',
  'http://localhost:5000',
].filter((t): t is string => Boolean(t));

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    plugin(),
    {
      name: 'smart-api-proxy',
      configureServer(server) {
        server.middlewares.use((req, res, next) => {
          if (!req.url || !req.url.startsWith('/api')) {
            return next();
          }

          const bodyChunks: Buffer[] = [];
          req.on('data', (chunk: Buffer) => bodyChunks.push(chunk));
          req.on('end', () => {
            const bodyBuffer = Buffer.concat(bodyChunks);

            const tryTarget = (index: number) => {
              if (index >= API_TARGETS.length) {
                res.statusCode = 502;
                res.setHeader('Content-Type', 'application/json');
                res.end(
                  JSON.stringify({
                    status: false,
                    message:
                      'Não foi possível conectar à API (tentado em localhost:5250 e localhost:5000). Verifique se o projeto Forms.API está em execução.',
                  })
                );
                return;
              }

              const targetUrl = new URL(req.url!, API_TARGETS[index]);
              const proxyReq = http.request(
                {
                  hostname: targetUrl.hostname,
                  port: targetUrl.port,
                  path: targetUrl.pathname + targetUrl.search,
                  method: req.method,
                  headers: {
                    ...req.headers,
                    host: targetUrl.host,
                  },
                },
                (proxyRes) => {
                  res.statusCode = proxyRes.statusCode || 200;
                  Object.entries(proxyRes.headers).forEach(([k, v]) => {
                    if (v !== undefined) res.setHeader(k, v);
                  });
                  proxyRes.pipe(res);
                }
              );

              proxyReq.on('error', () => {
                tryTarget(index + 1);
              });

              if (bodyBuffer.length > 0) {
                proxyReq.write(bodyBuffer);
              }
              proxyReq.end();
            };

            tryTarget(0);
          });
        });
      },
    },
  ],
  server: {
    port: 63061,
  },
});

FROM node:24.14.1-alpine AS build
WORKDIR /app
COPY Defender.Portal.Mcp/package.json Defender.Portal.Mcp/package-lock.json ./
RUN npm ci
COPY Defender.Portal.Mcp/src ./src
COPY Defender.Portal.Mcp/tsconfig.json ./
RUN npm run build && npm prune --omit=dev

FROM node:24.14.1-alpine
WORKDIR /app
ENV NODE_ENV=production
COPY --from=build --chown=node:node /app/node_modules ./node_modules
COPY --chown=node:node Defender.Portal.Mcp/package.json ./
COPY --from=build --chown=node:node /app/dist ./dist
USER node
EXPOSE 3000
CMD ["node", "dist/index.js"]

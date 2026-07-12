Defender Portal frontend

## Toolchain

- React 19 with Material UI 9
- Vite 8 and Vitest 4
- TypeScript 6.0.3

TypeScript is intentionally pinned to 6.0.3. TypeScript 7.0.2 is the npm
`latest` release, but `typescript-eslint` 8.63.0 declares support for
TypeScript versions `>=4.8.4 <6.1.0`; npm therefore rejects the TypeScript 7
dependency tree. Upgrade both together after `typescript-eslint` adds support.

MUI X 9.9.0 ships several invalid declaration files when dependency checking
is enabled. Narrow `patch-package` patches under `patches/` correct declaration
visibility, missing queue typing, and invalid extension-point conditionals.
`npm install` applies these patches automatically.

Both date libraries remain intentional: MUI X uses the `date-fns` adapter and
existing tables use its locale/format helpers, while budget and lottery date
arithmetic uses Day.js. Moment and its duplicate runtime cost were removed.

## Quality commands

```shell
npm run typecheck
npm run lint -- --max-warnings=0
npm test
npm run build
npm audit
npx playwright install chromium
npm run test:e2e
```

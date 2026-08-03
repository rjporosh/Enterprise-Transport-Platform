# @transport/shared-ui-library

Framework-parallel component libraries shared by `bus-ticketing-customer-web`
(Angular) and `bus-ticketing-admin` (React). Both apps also share
`design-tokens/` so a brand/theme change happens in exactly one place.

## Why "source consumption" instead of a published package

There's no npm registry access assumed here, and adding a build/publish step
(ng-packagr for Angular, tsup/vite-lib for React) is one more thing that can
break in a demo environment. Instead:

- Angular consumes `angular/src/*` directly via a TypeScript path alias
  (`@shared-ui/angular/*` in `tsconfig.json`) — the Angular CLI's esbuild
  builder resolves and compiles it as if it were local source.
- React/Vite consumes `react/src/*` the same way, via `resolve.alias` in
  `vite.config.ts` plus a matching `paths` entry in `tsconfig.json` for
  type-checking.

This is the standard pattern in Nx/Turborepo-style monorepos before anyone
bothers standing up a package registry — same DX (`import { Button } from
'@shared-ui/react'`), zero extra build step.

## Layout

```
shared-ui-library/
  design-tokens/
    tokens.css              # CSS custom properties — the single brand source of truth
    tailwind-preset.cjs     # Tailwind theme extension both apps' configs `presets: []` in
  react/
    src/
      components/           # Button, Card, Badge, Input, Select, Spinner, Modal,
      index.ts               # EmptyState, StatCard, PageHeader, DataTable, Pagination, Toast
  angular/
    src/
      lib/                   # ui-button, ui-card, ui-badge, ui-input, ui-spinner,
      public-api.ts           # ui-empty-state, ui-stat-card, ui-page-header, ui-modal
```

## Keeping the two in sync

The Angular and React components are deliberately parallel — same prop/input
names (`variant`, `size`, `tone`, `loading`), same Tailwind classes, same
`statusToBadgeTone()` mapping. When you add a component to one side, add its
twin to the other and note it in both files' doc comments, the way the
existing components do.

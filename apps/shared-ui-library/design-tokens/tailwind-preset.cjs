/**
 * @transport/design-tokens — Tailwind preset
 * -----------------------------------------------------------------------
 * Shared theme extension for every front-end app in the platform. An app's
 * own tailwind.config.cjs stays tiny — it just points its `content` glob at
 * its own source and lists this preset:
 *
 *   module.exports = {
 *     presets: [require('../../shared-ui-library/design-tokens/tailwind-preset.cjs')],
 *     content: ['./src/**\/*.{html,ts,tsx}'],
 *   };
 *
 * Keeping this as CommonJS (.cjs) is deliberate: Tailwind/PostCSS load
 * config files via `require()` regardless of an app's package.json
 * `"type"` field, and giving it an explicit .cjs extension sidesteps the
 * "require() of ES module" crash that hits plain .js configs inside an
 * ESM package (this exact bug was one of the admin app's build errors).
 */
module.exports = {
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        ink: {
          950: 'var(--color-ink-950)',
          900: 'var(--color-ink-900)',
          800: 'var(--color-ink-800)',
          700: 'var(--color-ink-700)',
          600: 'var(--color-ink-600)'
        },
        saffron: {
          400: 'var(--color-saffron-400)',
          500: 'var(--color-saffron-500)',
          600: 'var(--color-saffron-600)',
          700: 'var(--color-saffron-700)'
        },
        slate: {
          50: 'var(--color-slate-50)',
          100: 'var(--color-slate-100)',
          200: 'var(--color-slate-200)'
        },
        success: { DEFAULT: 'var(--color-success-500)', bg: 'var(--color-success-bg)' },
        danger: { DEFAULT: 'var(--color-danger-500)', bg: 'var(--color-danger-bg)' },
        warning: { DEFAULT: 'var(--color-warning-500)', bg: 'var(--color-warning-bg)' },
        info: { DEFAULT: 'var(--color-info-500)', bg: 'var(--color-info-bg)' }
      },
      fontFamily: {
        display: ['Fraunces', 'serif'],
        body: ['Inter', 'sans-serif']
      },
      borderRadius: {
        sm: 'var(--radius-sm)',
        md: 'var(--radius-md)',
        lg: 'var(--radius-lg)',
        xl: 'var(--radius-xl)'
      },
      boxShadow: {
        card: 'var(--shadow-card)',
        popover: 'var(--shadow-popover)'
      },
      keyframes: {
        'fade-in': { from: { opacity: 0 }, to: { opacity: 1 } },
        'slide-up': { from: { opacity: 0, transform: 'translateY(8px)' }, to: { opacity: 1, transform: 'translateY(0)' } }
      },
      animation: {
        'fade-in': 'fade-in var(--duration-base) var(--ease-standard)',
        'slide-up': 'slide-up var(--duration-base) var(--ease-standard)'
      }
    }
  },
  plugins: []
};

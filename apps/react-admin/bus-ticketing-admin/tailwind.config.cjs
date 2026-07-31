/** @type {import('tailwindcss').Config} */
module.exports = {
  presets: [require('../../shared-ui-library/design-tokens/tailwind-preset.cjs')],
  content: ['./index.html', './src/**/*.{ts,tsx}', '../../shared-ui-library/react/src/**/*.{ts,tsx}']
};

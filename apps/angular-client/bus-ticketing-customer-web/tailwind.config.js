/** @type {import('tailwindcss').Config} */
module.exports = {
  presets: [require('../../shared-ui-library/design-tokens/tailwind-preset.cjs')],
  content: ['./src/**/*.{html,ts}', '../../shared-ui-library/angular/src/**/*.ts']
};
